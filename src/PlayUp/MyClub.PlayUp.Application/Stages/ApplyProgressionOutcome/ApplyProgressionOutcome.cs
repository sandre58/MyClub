// -----------------------------------------------------------------------
// <copyright file="ApplyProgressionOutcome.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Progression;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: resolve fixture outcome and apply progression paths
/// to destination population and/or Place (Auto dual-write).
/// </summary>
/// <remarks>
/// Preflights destinations before any mutation (known orchestration failures → zero writes).
/// Population targets call <see cref="Stage.AddResolvedPopulationEntry"/>;
/// Place targets dual-write population membership then <see cref="Stage.ApplyResolvedEntry"/>.
/// Persistence: caller loads source/destination Stages (and Matches) into one scope, invokes this
/// use case, then calls <c>IUnitOfWork.SaveChangesAsync</c> once.
/// </remarks>
public static class ApplyProgressionOutcome
{
    /// <summary>
    /// Applies progression for a fixture onto already-loaded competition stages.
    /// </summary>
    /// <returns>Applied progression instructions; empty when no path targets the fixture.</returns>
    public static IReadOnlyList<ProgressionInstruction> Execute(
        Stage sourceStage,
        FixtureId fixtureId,
        IReadOnlyList<Match> matches,
        IReadOnlyList<Stage> competitionStages,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(sourceStage);
        ArgumentNullException.ThrowIfNull(matches);
        ArgumentNullException.ThrowIfNull(competitionStages);
        ArgumentNullException.ThrowIfNull(clock);

        var canonicalSource = ResolveCanonicalStage(sourceStage.Id, competitionStages);
        var fixture = canonicalSource.GetFixture(fixtureId);
        var tieFormat = ResolveRoundTieFormat(canonicalSource, fixtureId);
        EnsureFixtureMatchCoherence(canonicalSource, fixture, matches, tieFormat);

        var paths = canonicalSource.Regulation.ProgressionRules?.Paths
            .Where(p => p.SourceFixtureId.Equals(fixtureId))
            .ToArray() ?? [];

        if (paths.Length == 0)
        {
            return [];
        }

        var snapshot = FixtureConfrontationSnapshotAssembler.Assemble(fixture, matches);
        var outcome = FixtureOutcomeResolver.Resolve(tieFormat, snapshot);

        var instructions = paths
            .Select(path => ProgressionApplier.Apply(path, fixtureId, outcome))
            .ToArray();

        var destinations = new Stage[instructions.Length];
        for (var i = 0; i < instructions.Length; i++)
        {
            var instruction = instructions[i];
            var destination = ResolveCanonicalStage(instruction.StageId, competitionStages);
            if (instruction.TargetsSlot && destination.FindSlot(instruction.SlotKey!) is null)
            {
                throw new ApplicationFailureException(
                    $"Progression destination slot '{instruction.SlotKey}' was not found on stage '{destination.Id}'.",
                    ApplicationErrorCodes.DanglingFeedTarget);
            }

            if (instruction.TargetsGroup && destination.FindGroup(instruction.GroupId!.Value) is null)
            {
                throw new ApplicationFailureException(
                    $"Progression destination group '{instruction.GroupId}' was not found on stage '{destination.Id}'.",
                    ApplicationErrorCodes.DanglingFeedTarget);
            }

            destinations[i] = destination;
        }

        for (var i = 0; i < instructions.Length; i++)
        {
            var instruction = instructions[i];
            if (!instruction.TargetsSlot)
            {
                continue;
            }

            SlotOccupancyConflictGuard.EnsureCompatible(
                destinations[i],
                new SlotAssignmentInstruction(instruction.StageId, instruction.SlotKey!, instruction.EntryId));
        }

        for (var i = 0; i < instructions.Length; i++)
        {
            var instruction = instructions[i];
            var path = paths[i];
            destinations[i].AddResolvedPopulationEntry(instruction.EntryId, clock);

            // Form: Path.Destination.TargetsForm retains Placement intent; materialization is
            // AddResolvedPopulationEntry only (no second Composition write).
            if (path.Destination.TargetsForm)
            {
                destinations[i].RecordFormPathResolution(
                    FormPathResolutionKey.FromProgression(canonicalSource.Id, path),
                    instruction.EntryId);
            }

            if (instruction.TargetsGroup)
            {
                destinations[i].ApplyResolvedGroupEntry(
                    instruction.GroupId!.Value,
                    instruction.EntryId);
            }
            else if (instruction.TargetsSlot)
            {
                destinations[i].ApplyResolvedEntry(instruction.SlotKey!, instruction.EntryId, clock);
            }
        }

        return instructions;
    }

    private static Stage ResolveCanonicalStage(
        StageId stageId,
        IReadOnlyList<Stage> competitionStages)
    {
        var stage = competitionStages.FirstOrDefault(s => s.Id.Equals(stageId));
        return stage
            ?? throw new ApplicationFailureException(
                $"Stage '{stageId}' is not part of the competition stages list.",
                ApplicationErrorCodes.StageNotInCompetition);
    }

    private static TieFormat ResolveRoundTieFormat(Stage stage, FixtureId fixtureId)
    {
        var round = stage.Rounds.FirstOrDefault(r => r.Fixtures.Any(f => f.Id.Equals(fixtureId)))
            ?? throw new ApplicationFailureException(
                $"Fixture '{fixtureId}' is not hosted by a Round (Matchday fixtures cannot drive Progression).",
                ApplicationErrorCodes.TieFormatRequired);
        return TieFormat.OrDefaultOneLeg(round.TieFormat);
    }

    private static void EnsureFixtureMatchCoherence(
        Stage canonicalSource,
        Fixture fixture,
        IReadOnlyList<Match> matches,
        TieFormat tieFormat)
    {
        if (fixture.Attachments.Count != tieFormat.NumberOfLegs)
        {
            throw new ApplicationFailureException(
                $"Fixture '{fixture.Id}' attachments count must equal TieFormat.NumberOfLegs ({tieFormat.NumberOfLegs}).",
                ApplicationErrorCodes.FixtureInvalid);
        }

        if (matches.Count != fixture.Attachments.Count)
        {
            throw new ApplicationFailureException(
                $"Fixture '{fixture.Id}' requires all attached matches to be provided.",
                ApplicationErrorCodes.FixtureInvalid);
        }

        var provided = matches.Select(m => m.Id).ToHashSet();
        foreach (var attachment in fixture.Attachments)
        {
            if (!provided.Contains(attachment.MatchId))
            {
                throw new ApplicationFailureException(
                    $"Match '{attachment.MatchId}' attached to fixture '{fixture.Id}' was not provided.",
                    ApplicationErrorCodes.FixtureInvalid);
            }
        }

        if (matches.Any(match => !fixture.MatchIds.Contains(match.Id)
                                 || !match.StageId.Equals(canonicalSource.Id)
                                 || !match.CompetitionId.Equals(canonicalSource.CompetitionId)))
        {
            throw new ApplicationFailureException(
                $"Fixture '{fixture.Id}' is not coherently bound to the provided matches.",
                ApplicationErrorCodes.FixtureInvalid);
        }
    }
}

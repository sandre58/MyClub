// -----------------------------------------------------------------------
// <copyright file="ApplyProgressionOutcome.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Match;
using MyClub.PlayUp.Domain.Progression;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stage;
using StageAggregate = MyClub.PlayUp.Domain.Stage.Stage;

namespace MyClub.PlayUp.Application.Stage;

/// <summary>
/// Application use case: resolve fixture outcome and apply progression paths to destination slots.
/// </summary>
/// <remarks>
/// Preflights destination stages/slots before any mutation (known orchestration failures → zero writes).
/// Mutations then call <see cref="StageAggregate.ApplyResolvedEntry"/> successively.
/// V1 does not provide transactional atomicity; no unit of work, repository, or EF transaction belongs here.
/// A later Domain rejection mid-loop may leave earlier in-memory mutations applied.
/// </remarks>
public static class ApplyProgressionOutcome
{
    /// <summary>
    /// Applies progression for a fixture onto already-loaded competition stages.
    /// </summary>
    /// <param name="sourceStage">Stage that owns the fixture (membership resolved via <paramref name="competitionStages"/>).</param>
    /// <param name="fixtureId">Fixture whose outcome drives progression.</param>
    /// <param name="matches">Already-loaded matches for all fixture legs.</param>
    /// <param name="competitionStages">All competition stages (canonical instances for mutations).</param>
    /// <param name="clock">Clock for domain events.</param>
    /// <returns>Applied progression instructions; empty when no path targets the fixture.</returns>
    public static IReadOnlyList<SlotAssignmentInstruction> Execute(
        StageAggregate sourceStage,
        FixtureId fixtureId,
        IReadOnlyList<Match> matches,
        IReadOnlyList<StageAggregate> competitionStages,
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

        var destinations = new StageAggregate[instructions.Length];
        for (var i = 0; i < instructions.Length; i++)
        {
            var instruction = instructions[i];
            var destination = ResolveCanonicalStage(instruction.StageId, competitionStages);
            if (destination.FindSlot(instruction.SlotKey) is null)
            {
                throw new ApplicationFailureException(
                    $"Progression destination slot '{instruction.SlotKey}' was not found on stage '{destination.Id}'.",
                    ApplicationErrorCodes.DanglingFeedTarget);
            }

            destinations[i] = destination;
        }

        for (var i = 0; i < instructions.Length; i++)
        {
            var instruction = instructions[i];
            destinations[i].ApplyResolvedEntry(instruction.SlotKey, instruction.EntryId, clock);
        }

        return instructions;
    }

    private static StageAggregate ResolveCanonicalStage(
        StageId stageId,
        IReadOnlyList<StageAggregate> competitionStages)
    {
        var stage = competitionStages.FirstOrDefault(s => s.Id.Equals(stageId));
        return stage
            ?? throw new ApplicationFailureException(
                $"Stage '{stageId}' is not part of the competition stages list.",
                ApplicationErrorCodes.StageNotInCompetition);
    }

    private static TieFormat ResolveRoundTieFormat(StageAggregate stage, FixtureId fixtureId)
    {
        var round = stage.Rounds.FirstOrDefault(r => r.Fixtures.Any(f => f.Id.Equals(fixtureId))) ?? throw new ApplicationFailureException(
                $"Fixture '{fixtureId}' is not hosted by a Round with TieFormat (Matchday fixtures cannot drive Progression).",
                ApplicationErrorCodes.TieFormatRequired);
        return round.TieFormat
            ?? throw new ApplicationFailureException(
                $"Round '{round.Id}' has no TieFormat; cannot resolve fixture progression outcome.",
                ApplicationErrorCodes.TieFormatRequired);
    }

    private static void EnsureFixtureMatchCoherence(
        StageAggregate canonicalSource,
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

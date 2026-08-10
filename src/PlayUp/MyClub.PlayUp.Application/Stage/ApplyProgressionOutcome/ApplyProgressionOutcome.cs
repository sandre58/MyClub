// -----------------------------------------------------------------------
// <copyright file="ApplyProgressionOutcome.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Match;
using MyClub.PlayUp.Domain.Progression;
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
    /// Applies progression for a single fixture onto already-loaded competition stages.
    /// </summary>
    /// <param name="sourceStage">Stage that owns the fixture (membership resolved via <paramref name="competitionStages"/>).</param>
    /// <param name="fixtureId">Fixture whose outcome drives progression.</param>
    /// <param name="match">Already-loaded single-leg match bound to the fixture.</param>
    /// <param name="competitionStages">All competition stages (canonical instances for mutations).</param>
    /// <param name="clock">Clock for domain events.</param>
    /// <returns>Applied progression instructions; empty when no path targets the fixture.</returns>
    public static IReadOnlyList<SlotAssignmentInstruction> Execute(
        StageAggregate sourceStage,
        FixtureId fixtureId,
        Match match,
        IReadOnlyList<StageAggregate> competitionStages,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(sourceStage);
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(competitionStages);
        ArgumentNullException.ThrowIfNull(clock);

        var canonicalSource = ResolveCanonicalStage(sourceStage.Id, competitionStages);
        var fixture = canonicalSource.GetFixture(fixtureId);
        EnsureFixtureMatchCoherence(canonicalSource, fixture, match);

        var paths = canonicalSource.Regulation.ProgressionRules?.Paths
            .Where(p => p.SourceFixtureId.Equals(fixtureId))
            .ToArray() ?? [];

        if (paths.Length == 0)
        {
            return [];
        }

        var snapshot = FixtureOutcomeSnapshotAssembler.Assemble(fixtureId, match);
        var outcome = FixtureOutcomeResolver.Resolve(snapshot);

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

    private static void EnsureFixtureMatchCoherence(
        StageAggregate canonicalSource,
        Fixture fixture,
        Match match)
    {
        if (fixture.MatchIds.Count != 1
            || !match.Id.Equals(fixture.MatchIds[0])
            || !match.StageId.Equals(canonicalSource.Id)
            || !match.CompetitionId.Equals(canonicalSource.CompetitionId))
        {
            throw new ApplicationFailureException(
                $"Fixture '{fixture.Id}' is not coherently bound to a single V1 match.",
                ApplicationErrorCodes.FixtureInvalid);
        }
    }
}

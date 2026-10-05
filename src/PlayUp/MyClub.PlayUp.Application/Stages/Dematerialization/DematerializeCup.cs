// -----------------------------------------------------------------------
// <copyright file="DematerializeCup.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: remove all attached Cup materialization on a stage.
/// </summary>
/// <remarks>
/// Restores pre-materialization topology: rounds, slots, and bracket pairs unchanged; no fixtures.
/// Caller commits via a single unit-of-work save.
/// </remarks>
public static class DematerializeCup
{
    /// <summary>
    /// Dematerializes every fixture/match attached on cup rounds.
    /// </summary>
    public static DematerializeCupResult Execute(
        Competition competition,
        Stage stage,
        IReadOnlyList<Match> stageMatches,
        IMatchRepository matchRepository,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(stageMatches);
        ArgumentNullException.ThrowIfNull(matchRepository);
        ArgumentNullException.ThrowIfNull(clock);

        if (!stage.CompetitionId.Equals(competition.Id))
        {
            throw new ApplicationFailureException(
                $"Stage '{stage.Id}' does not belong to competition '{competition.Id}'.",
                ApplicationErrorCodes.StageNotInCompetition);
        }

        DematerializationPreflight.EnsureCupMutable(competition, stage);
        DematerializationPreflight.EnsureNoOrphanMatches(stage, stageMatches);

        var scope = CupMaterializationScope.Collect(stage, stageMatches);
        DematerializationScopeGuard.EnsureAllAttachedMatchesLoaded(
            scope.AttachedMatchIds,
            scope.Matches);
        if (scope.Matches.Count == 0)
        {
            return new DematerializeCupResult(RemovedMatchCount: 0);
        }

        DematerializationPreflight.EnsureNonePlayed(scope.Matches);

        foreach (var fixtureId in scope.FixtureIds)
        {
            stage.RemoveFixture(fixtureId, clock);
        }

        foreach (var match in scope.Matches)
        {
            matchRepository.Remove(match);
        }

        return new DematerializeCupResult(scope.Matches.Count);
    }
}

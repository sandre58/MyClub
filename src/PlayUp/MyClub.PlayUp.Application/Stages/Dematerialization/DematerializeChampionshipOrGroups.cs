// -----------------------------------------------------------------------
// <copyright file="DematerializeChampionshipOrGroups.cs" company="Stéphane ANDRE">
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
/// Application use case: remove all attached Championship / Groups materialization on a stage.
/// </summary>
/// <remarks>
/// Restores pre-materialization topology: one empty matchday (number 1), structure unchanged.
/// Caller commits via a single unit-of-work save.
/// </remarks>
public static class DematerializeChampionshipOrGroups
{
    /// <summary>
    /// Dematerializes the entire attached phase (all matchdays/fixtures/matches), then trims matchdays to the skeleton.
    /// </summary>
    public static DematerializeChampionshipOrGroupsResult Execute(
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

        DematerializationPreflight.EnsureChampionshipOrGroupsMutable(competition, stage);
        DematerializationPreflight.EnsureNoOrphanMatches(stage, stageMatches);

        var scope = ChampionshipOrGroupsMaterializationScope.Collect(stage, stageMatches);
        if (scope.Matches.Count == 0)
        {
            return new DematerializeChampionshipOrGroupsResult(RemovedMatchCount: 0);
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

        var matchdaysToRemove = stage.Matchdays
            .Where(matchday => matchday.Number > 1)
            .OrderByDescending(matchday => matchday.Number)
            .Select(matchday => matchday.Id)
            .ToList();

        foreach (var matchdayId in matchdaysToRemove)
        {
            stage.RemoveMatchday(matchdayId, clock);
        }

        return new DematerializeChampionshipOrGroupsResult(scope.Matches.Count);
    }
}

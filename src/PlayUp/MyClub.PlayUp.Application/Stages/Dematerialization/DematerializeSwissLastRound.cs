// -----------------------------------------------------------------------
// <copyright file="DematerializeSwissLastRound.cs" company="Stéphane ANDRE">
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
/// Application use case: dematerialize the last Swiss round only.
/// </summary>
public static class DematerializeSwissLastRound
{
    /// <summary>
    /// Removes the highest-numbered Swiss matchday, its fixtures/matches, and optional bye.
    /// </summary>
    public static DematerializeSwissLastRoundResult Execute(
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

        DematerializationPreflight.EnsureSwissDematerializeMutable(competition, stage);
        DematerializationPreflight.EnsureNoOrphanMatches(stage, stageMatches);
        DematerializationPreflight.EnsureContiguousSwissMatchdays(stage);

        var scope = SwissLastRoundMaterializationScope.TryCollectLastRound(stage, stageMatches);
        if (scope is null)
        {
            return new DematerializeSwissLastRoundResult(RemovedMatchCount: 0, RemovedRoundNumber: null);
        }

        DematerializationScopeGuard.EnsureAllAttachedMatchesLoaded(
            scope.AttachedMatchIds,
            scope.Matches);

        if (scope.Matches.Count == 0)
        {
            if (scope.HasBye)
            {
                throw new ApplicationFailureException(
                    "Swiss round has a bye record without fixture materialization.",
                    ApplicationErrorCodes.MaterializationStateInconsistent);
            }

            var lastMatchday = stage.Matchdays.First(matchday => matchday.Number == scope.RoundNumber);
            if (lastMatchday.Fixtures.Count > 0)
            {
                throw new ApplicationFailureException(
                    "Swiss round has fixtures without attached matches.",
                    ApplicationErrorCodes.MaterializationStateInconsistent);
            }

            throw new ApplicationFailureException(
                "Swiss round exists without materialized fixtures.",
                ApplicationErrorCodes.MaterializationStateInconsistent);
        }

        DematerializationPreflight.EnsureNonePlayed(scope.Matches);

        var removedIds = stage.RemoveSwissRoundMaterialization(scope.RoundNumber, clock);
        if (removedIds.Count != scope.Matches.Count)
        {
            throw new ApplicationFailureException(
                "Swiss round materialization could not be removed consistently.",
                ApplicationErrorCodes.MaterializationStateInconsistent);
        }

        foreach (var match in scope.Matches)
        {
            matchRepository.Remove(match);
        }

        return new DematerializeSwissLastRoundResult(
            scope.Matches.Count,
            scope.RoundNumber);
    }
}

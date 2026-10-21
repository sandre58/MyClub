// -----------------------------------------------------------------------
// <copyright file="ChampionshipOrGroupsMaterializationScope.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Attached materialization on Championship / Groups stages (Matchday → Fixture → Match).
/// </summary>
internal static class ChampionshipOrGroupsMaterializationScope
{
    /// <summary>
    /// Collects fixtures and matches currently attached via matchdays on <paramref name="stage"/>.
    /// </summary>
    public static ChampionshipOrGroupsScope Collect(Stage stage, IReadOnlyList<Match> stageMatches)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(stageMatches);

        var fixtureIds = new List<FixtureId>();
        var matchIds = new HashSet<MatchId>();
        foreach (var matchday in stage.Matchdays)
        {
            foreach (var fixture in matchday.Fixtures)
            {
                fixtureIds.Add(fixture.Id);
                foreach (var matchId in fixture.MatchIds)
                {
                    matchIds.Add(matchId);
                }
            }
        }

        var matchesById = stageMatches.ToDictionary(match => match.Id);
        var matches = matchIds
            .Select(matchesById.GetValueOrDefault)
            .Where(match => match is not null)
            .Cast<Match>()
            .ToList();

        return new ChampionshipOrGroupsScope(fixtureIds, matchIds, matches);
    }
}

/// <summary>
/// Snapshot of attached Championship / Groups materialization on a stage.
/// </summary>
/// <param name="FixtureIds">Fixtures to remove from matchdays.</param>
/// <param name="AttachedMatchIds">Match identities referenced on fixtures.</param>
/// <param name="Matches">Match aggregates to delete from persistence.</param>
internal sealed record ChampionshipOrGroupsScope(
    IReadOnlyList<FixtureId> FixtureIds,
    IReadOnlyCollection<MatchId> AttachedMatchIds,
    IReadOnlyList<Match> Matches);

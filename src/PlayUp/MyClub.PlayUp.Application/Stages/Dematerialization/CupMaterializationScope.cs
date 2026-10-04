// -----------------------------------------------------------------------
// <copyright file="CupMaterializationScope.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Attached materialization on Cup stages (Round → Fixture → Match).
/// </summary>
internal static class CupMaterializationScope
{
    /// <summary>
    /// Collects fixtures and matches currently attached via rounds on <paramref name="stage"/>.
    /// </summary>
    public static CupMaterializationSnapshot Collect(Stage stage, IReadOnlyList<Match> stageMatches)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(stageMatches);

        var fixtureIds = new List<FixtureId>();
        var matchIds = new HashSet<MatchId>();
        foreach (var round in stage.Rounds)
        {
            foreach (var fixture in round.Fixtures)
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

        return new CupMaterializationSnapshot(fixtureIds, matches);
    }
}

/// <summary>
/// Snapshot of attached Cup materialization on a stage.
/// </summary>
/// <param name="FixtureIds">Fixtures to remove from rounds.</param>
/// <param name="Matches">Match aggregates to delete from persistence.</param>
internal sealed record CupMaterializationSnapshot(
    IReadOnlyList<FixtureId> FixtureIds,
    IReadOnlyList<Match> Matches);

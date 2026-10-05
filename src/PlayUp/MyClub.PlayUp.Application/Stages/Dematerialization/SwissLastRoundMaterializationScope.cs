// -----------------------------------------------------------------------
// <copyright file="SwissLastRoundMaterializationScope.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Last Swiss round materialization (highest matchday number on the stage).
/// </summary>
internal static class SwissLastRoundMaterializationScope
{
    /// <summary>
    /// Collects the last round when present; otherwise <see langword="null"/>.
    /// </summary>
    public static SwissLastRoundScope? TryCollectLastRound(Stage stage, IReadOnlyList<Match> stageMatches)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(stageMatches);

        if (stage.Matchdays.Count == 0)
        {
            return null;
        }

        var lastMatchday = stage.Matchdays.MaxBy(matchday => matchday.Number)!;
        var matchIds = lastMatchday.Fixtures
            .SelectMany(fixture => fixture.MatchIds)
            .Distinct()
            .ToHashSet();

        var matchesById = stageMatches.ToDictionary(match => match.Id);
        var matches = matchIds
            .Select(matchesById.GetValueOrDefault)
            .Where(match => match is not null)
            .Cast<Match>()
            .ToList();

        var hasBye = stage.SwissByeHistory.Any(bye => bye.RoundIndex == lastMatchday.Number);
        return new SwissLastRoundScope(lastMatchday.Number, matchIds, matches, hasBye);
    }
}

/// <summary>
/// Snapshot of the last Swiss round to dematerialize.
/// </summary>
/// <param name="RoundNumber">Swiss round / matchday number.</param>
/// <param name="AttachedMatchIds">Match identities referenced on that round.</param>
/// <param name="Matches">Matches attached on that round.</param>
/// <param name="HasBye">Whether a Swiss bye exists for this round index.</param>
internal sealed record SwissLastRoundScope(
    int RoundNumber,
    IReadOnlyCollection<MatchId> AttachedMatchIds,
    IReadOnlyList<Match> Matches,
    bool HasBye);

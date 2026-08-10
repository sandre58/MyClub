// -----------------------------------------------------------------------
// <copyright file="StandingMatchAssembler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Standing;
using MatchAggregate = MyClub.PlayUp.Domain.Match.Match;

namespace MyClub.PlayUp.Application.Standing;

/// <summary>
/// Assembles standing match snapshots from Match aggregates (Finished only).
/// </summary>
public static class StandingMatchAssembler
{
    /// <summary>
    /// Converts finished matches to standing snapshots; skips non-finished matches.
    /// </summary>
    /// <param name="matches">Already-loaded matches.</param>
    /// <returns>Standing match snapshots.</returns>
    public static IReadOnlyList<StandingMatch> Assemble(IEnumerable<MatchAggregate> matches)
    {
        ArgumentNullException.ThrowIfNull(matches);

        var result = new List<StandingMatch>();
        foreach (var match in matches)
        {
            if (match.Status != MatchStatus.Finished || match.Result is null)
            {
                continue;
            }

            result.Add(new StandingMatch(
                match.HomeEntryId,
                match.AwayEntryId,
                match.Result.Score.HomeGoals,
                match.Result.Score.AwayGoals));
        }

        return result;
    }
}

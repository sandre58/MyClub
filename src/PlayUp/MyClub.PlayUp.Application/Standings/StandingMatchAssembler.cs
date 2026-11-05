// -----------------------------------------------------------------------
// <copyright file="StandingMatchAssembler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Standings;

namespace MyClub.PlayUp.Application.Standings;

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
    public static IReadOnlyList<StandingMatch> Assemble(IEnumerable<Match> matches)
    {
        ArgumentNullException.ThrowIfNull(matches);

        var result = new List<StandingMatch>();
        foreach (var match in matches)
        {
            if (!TryFinishedParticipants(match.Status, match.Result, match.HomeEntryId, match.AwayEntryId, out var home, out var away))
            {
                continue;
            }

            result.Add(new StandingMatch(
                home,
                away,
                match.Result!.Score.HomeGoals,
                match.Result.Score.AwayGoals));
        }

        return result;
    }

    /// <summary>
    /// Converts finished attention slices to standing snapshots; skips non-finished slices
    /// and matches that still have an unknown side.
    /// </summary>
    public static IReadOnlyList<StandingMatch> Assemble(IEnumerable<MatchAttentionSlice> matches)
    {
        ArgumentNullException.ThrowIfNull(matches);

        var result = new List<StandingMatch>();
        foreach (var match in matches)
        {
            if (!TryFinishedParticipants(match.Status, match.Result, match.HomeEntryId, match.AwayEntryId, out var home, out var away))
            {
                continue;
            }

            result.Add(new StandingMatch(
                home,
                away,
                match.Result!.Score.HomeGoals,
                match.Result.Score.AwayGoals));
        }

        return result;
    }

    /// <summary>
    /// Converts finished summary rows to standing snapshots; skips non-finished rows
    /// and matches that still have an unknown side.
    /// </summary>
    public static IReadOnlyList<StandingMatch> Assemble(IEnumerable<MatchSummaryRow> matches)
    {
        ArgumentNullException.ThrowIfNull(matches);

        var result = new List<StandingMatch>();
        foreach (var match in matches)
        {
            if (!TryFinishedParticipants(match.Status, match.Result, match.HomeEntryId, match.AwayEntryId, out var home, out var away))
            {
                continue;
            }

            result.Add(new StandingMatch(
                home,
                away,
                match.Result!.Score.HomeGoals,
                match.Result.Score.AwayGoals));
        }

        return result;
    }

    /// <summary>
    /// Finished matches with an unknown side are excluded from standings and the statistics they carry.
    /// </summary>
    private static bool TryFinishedParticipants(
        MatchStatus status,
        MatchResult? result,
        EntryId? homeEntryId,
        EntryId? awayEntryId,
        out EntryId home,
        out EntryId away)
    {
        home = default;
        away = default;
        if (status != MatchStatus.Finished || result is null || homeEntryId is not { } knownHome || awayEntryId is not { } knownAway)
        {
            return false;
        }

        home = knownHome;
        away = knownAway;
        return true;
    }

    /// <summary>
    /// Entry identities that may feed a standing. An unknown side excludes the whole match.
    /// </summary>
    public static IEnumerable<EntryId> StandingParticipantIds(EntryId? home, EntryId? away)
    {
        if (home is not { } knownHome || away is not { } knownAway)
        {
            yield break;
        }

        yield return knownHome;
        yield return knownAway;
    }
}

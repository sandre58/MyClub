// -----------------------------------------------------------------------
// <copyright file="MatchAttentionSlice.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Application.Abstractions;

/// <summary>
/// Minimal match read shape for Needs Attention and completion status checks.
/// </summary>
/// <param name="Id">Match identity.</param>
/// <param name="StageId">Owning stage.</param>
/// <param name="Status">Lifecycle status.</param>
/// <param name="HomeEntryId">Home entry.</param>
/// <param name="AwayEntryId">Away entry.</param>
/// <param name="Result">Result when finished; otherwise <see langword="null"/>.</param>
public sealed record MatchAttentionSlice(
    MatchId Id,
    StageId StageId,
    MatchStatus Status,
    EntryId HomeEntryId,
    EntryId AwayEntryId,
    MatchResult? Result)
{
    /// <summary>Maps a loaded aggregate to the attention read slice (tests and older call sites).</summary>
    public static MatchAttentionSlice FromMatch(Match match)
    {
        ArgumentNullException.ThrowIfNull(match);
        return new MatchAttentionSlice(
            match.Id,
            match.StageId,
            match.Status,
            match.HomeEntryId,
            match.AwayEntryId,
            match.Result);
    }

    /// <summary>Maps a projected summary row to the attention read slice.</summary>
    public static MatchAttentionSlice FromSummaryRow(MatchSummaryRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        return new MatchAttentionSlice(
            row.Id,
            row.StageId,
            row.Status,
            row.HomeEntryId,
            row.AwayEntryId,
            row.Result);
    }

    /// <summary>Maps summary rows keyed by stage.</summary>
    public static IReadOnlyDictionary<StageId, IReadOnlyList<MatchAttentionSlice>> FromSummaryRows(
        IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>> matchesByStage)
    {
        ArgumentNullException.ThrowIfNull(matchesByStage);
        return matchesByStage.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<MatchAttentionSlice>)[
                .. pair.Value.Select(FromSummaryRow)]);
    }

    /// <summary>Maps full aggregates keyed by stage.</summary>
    public static IReadOnlyDictionary<StageId, IReadOnlyList<MatchAttentionSlice>> FromMatchesByStage(
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>> matchesByStage)
    {
        ArgumentNullException.ThrowIfNull(matchesByStage);
        return matchesByStage.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<MatchAttentionSlice>)[
                .. pair.Value.Select(FromMatch)]);
    }
}

// -----------------------------------------------------------------------
// <copyright file="ProgressionImpactMatchSummaries.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages.ProgressionImpact;

internal static class ProgressionImpactMatchSummaries
{
    internal static IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>> ForUpstreamStage(
        Stage upstream,
        IReadOnlyList<Match>? upstreamMatches)
    {
        if (upstreamMatches is null || upstreamMatches.Count == 0)
        {
            return new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>();
        }

        var rows = upstreamMatches
            .Where(match => match.StageId.Equals(upstream.Id))
            .Select(ToRow)
            .ToArray();

        return rows.Length == 0
            ? new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>()
            : new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>> { [upstream.Id] = rows };
    }

    internal static IReadOnlyList<Match> MatchesForStage(
        StageId stageId,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>>? matchesByStage)
    {
        if (matchesByStage is null || !matchesByStage.TryGetValue(stageId, out var matches))
        {
            return [];
        }

        return matches;
    }

    private static MatchSummaryRow ToRow(Match match) =>
        new(
            match.Id,
            match.StageId,
            match.Status,
            match.HomeEntryId,
            match.AwayEntryId,
            match.Result);
}

// -----------------------------------------------------------------------
// <copyright file="StageAttachedProduction.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Shared predicate: match production attached to the stage structure graph.
/// </summary>
internal static class StageAttachedProduction
{
    internal static bool HasAttachedMatches(Stage stage, IReadOnlyList<Match> stageMatches)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(stageMatches);

        return stageMatches.Any(match => stage.HasMatch(match.Id));
    }

    internal static bool HasAttachedMatches(Stage stage, IReadOnlyList<MatchSummaryRow> matchSummaries)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(matchSummaries);

        return matchSummaries
            .Any(row => row.StageId.Equals(stage.Id) && stage.HasMatch(row.Id))
;
    }
}

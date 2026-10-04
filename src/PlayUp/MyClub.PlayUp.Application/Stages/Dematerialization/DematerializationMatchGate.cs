// -----------------------------------------------------------------------
// <copyright file="DematerializationMatchGate.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Minimal match projection for dematerialization capability and preflight gates.
/// </summary>
/// <param name="Id">Match identity.</param>
/// <param name="Status">Lifecycle status.</param>
/// <param name="HasObservedLive">Whether Live was observed on this match.</param>
internal readonly record struct DematerializationMatchGate(
    MatchId Id,
    MatchStatus Status,
    bool HasObservedLive)
{
    public static DematerializationMatchGate FromMatch(Match match)
    {
        ArgumentNullException.ThrowIfNull(match);
        return new DematerializationMatchGate(match.Id, match.Status, match.HasObservedLive);
    }

    public static DematerializationMatchGate FromSummary(MatchSummaryRow row) =>
        new(row.Id, row.Status, row.HasObservedLive);
}

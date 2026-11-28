// -----------------------------------------------------------------------
// <copyright file="MatchResultEntryShape.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Application.Matches;

/// <summary>
/// Validated HTTP match result entry (shape only — no confrontation eligibility).
/// </summary>
public sealed record MatchResultEntryShape(
    ResultType Type,
    Score Score,
    bool ExtraTimePlayed,
    PenaltyShootoutScore? Shootout)
{
    /// <summary>
    /// Gets a value indicating whether this entry may require confrontation tie eligibility (unequal play score and shootout).
    /// </summary>
    public bool RequiresConfrontationTieEligibility =>
        Shootout is not null && Score.HomeGoals != Score.AwayGoals;
}

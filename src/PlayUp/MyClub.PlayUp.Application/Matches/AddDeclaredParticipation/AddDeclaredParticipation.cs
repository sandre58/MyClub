// -----------------------------------------------------------------------
// <copyright file="AddDeclaredParticipation.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Application.Matches;

/// <summary>
/// Application use case: add a player to the match composition sheet.
/// </summary>
/// <remarks>
/// Domain owns sheet invariants and lifecycle gates. Application enforces player eligibility (C1).
/// </remarks>
public static class AddDeclaredParticipation
{
    /// <summary>
    /// Adds an eligible player to the match sheet.
    /// </summary>
    public static DeclaredParticipation Execute(
        Match match,
        Competition competition,
        MemberId memberId,
        Side side,
        CompositionStatus compositionStatus,
        IClock clock,
        int? jerseyNumber = null)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(clock);
        MatchSheetEligibility.EnsureDefinedSide(side);
        MatchSheetEligibility.EnsureDefinedCompositionStatus(compositionStatus);
        MatchSheetEligibility.EnsurePlayerEligible(match, competition, memberId, side);
        return match.AddDeclaredParticipation(memberId, side, compositionStatus, clock, jerseyNumber);
    }
}

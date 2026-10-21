// -----------------------------------------------------------------------
// <copyright file="ChangeDeclaredParticipationCompositionStatus.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Application.Matches;

/// <summary>
/// Application use case: change starter/bench status on the match sheet.
/// </summary>
public static class ChangeDeclaredParticipationCompositionStatus
{
    /// <summary>
    /// Changes the composition status of an existing sheet line.
    /// </summary>
    public static void Execute(
        Match match,
        MemberId memberId,
        CompositionStatus compositionStatus,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(clock);
        MatchSheetEligibility.EnsureDefinedCompositionStatus(compositionStatus);
        match.ChangeDeclaredParticipationCompositionStatus(memberId, compositionStatus, clock);
    }
}

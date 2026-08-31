// -----------------------------------------------------------------------
// <copyright file="CorrectRecordedGoal.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Application.Matches;

/// <summary>
/// Application use case: correct a nominative goal attribution.
/// </summary>
public static class CorrectRecordedGoal
{
    /// <summary>
    /// Corrects an existing recorded goal.
    /// </summary>
    public static void Execute(
        Match match,
        GoalId goalId,
        MemberId scorerMemberId,
        Side creditedSide,
        IClock clock,
        MemberId? assisterMemberId = null)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(clock);
        MatchSheetEligibility.EnsureDefinedSide(creditedSide);
        match.CorrectRecordedGoal(goalId, scorerMemberId, creditedSide, clock, assisterMemberId);
    }
}

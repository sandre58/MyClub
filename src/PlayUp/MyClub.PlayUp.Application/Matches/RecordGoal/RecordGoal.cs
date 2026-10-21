// -----------------------------------------------------------------------
// <copyright file="RecordGoal.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Application.Matches;

/// <summary>
/// Application use case: record a nominative goal attribution.
/// </summary>
public static class RecordGoal
{
    /// <summary>
    /// Records a goal on the match journal. Does not mutate running or official score.
    /// </summary>
    public static RecordedGoal Execute(
        Match match,
        MemberId scorerMemberId,
        Side creditedSide,
        IClock clock,
        MemberId? assisterMemberId = null)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(clock);
        MatchSheetEligibility.EnsureDefinedSide(creditedSide);
        return match.RecordGoal(scorerMemberId, creditedSide, clock, assisterMemberId);
    }
}

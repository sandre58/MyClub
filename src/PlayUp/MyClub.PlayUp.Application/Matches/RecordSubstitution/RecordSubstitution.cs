// -----------------------------------------------------------------------
// <copyright file="RecordSubstitution.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Application.Matches;

/// <summary>
/// Application use case: record an ordered substitution fact.
/// </summary>
public static class RecordSubstitution
{
    /// <summary>
    /// Records a substitution on the match journal.
    /// </summary>
    public static RecordedSubstitution Execute(
        Match match,
        MemberId outMemberId,
        MemberId inMemberId,
        Side side,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(clock);
        MatchSheetEligibility.EnsureDefinedSide(side);
        return match.RecordSubstitution(outMemberId, inMemberId, side, clock);
    }
}

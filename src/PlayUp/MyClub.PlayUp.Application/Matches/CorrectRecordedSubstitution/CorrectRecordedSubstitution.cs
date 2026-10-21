// -----------------------------------------------------------------------
// <copyright file="CorrectRecordedSubstitution.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Application.Matches;

/// <summary>
/// Application use case: correct an ordered substitution fact.
/// </summary>
public static class CorrectRecordedSubstitution
{
    /// <summary>
    /// Corrects an existing recorded substitution.
    /// </summary>
    public static void Execute(
        Match match,
        SubstitutionId substitutionId,
        MemberId outMemberId,
        MemberId inMemberId,
        Side side,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(clock);
        MatchSheetEligibility.EnsureDefinedSide(side);
        match.CorrectRecordedSubstitution(substitutionId, outMemberId, inMemberId, side, clock);
    }
}

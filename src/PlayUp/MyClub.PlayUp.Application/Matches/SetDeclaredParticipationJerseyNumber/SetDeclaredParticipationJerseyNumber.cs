// -----------------------------------------------------------------------
// <copyright file="SetDeclaredParticipationJerseyNumber.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Application.Matches;

/// <summary>
/// Application use case: set or clear a jersey number on the match sheet.
/// </summary>
public static class SetDeclaredParticipationJerseyNumber
{
    /// <summary>
    /// Sets or clears the jersey number for a sheet line.
    /// </summary>
    public static void Execute(Match match, MemberId memberId, int? jerseyNumber, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(clock);
        match.SetDeclaredParticipationJerseyNumber(memberId, jerseyNumber, clock);
    }
}

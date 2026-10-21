// -----------------------------------------------------------------------
// <copyright file="RemoveDeclaredParticipation.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Application.Matches;

/// <summary>
/// Application use case: remove a declared participation from the match sheet.
/// </summary>
public static class RemoveDeclaredParticipation
{
    /// <summary>
    /// Removes a participation from the match sheet.
    /// </summary>
    public static void Execute(Match match, MemberId memberId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(clock);
        match.RemoveDeclaredParticipation(memberId, clock);
    }
}

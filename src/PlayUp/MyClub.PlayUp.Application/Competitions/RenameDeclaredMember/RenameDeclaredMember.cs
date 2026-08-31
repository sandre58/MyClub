// -----------------------------------------------------------------------
// <copyright file="RenameDeclaredMember.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Application use case: rename a declared member display name.
/// </summary>
public static class RenameDeclaredMember
{
    /// <summary>
    /// Renames a declared member on the entry roster.
    /// </summary>
    public static void Execute(
        Competition competition,
        EntryId entryId,
        MemberId memberId,
        string displayName,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(clock);
        competition.RenameDeclaredMember(entryId, memberId, displayName, clock);
    }
}

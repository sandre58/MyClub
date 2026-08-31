// -----------------------------------------------------------------------
// <copyright file="ChangeDeclaredMemberRole.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Application use case: change a declared member role within an entry roster.
/// </summary>
public static class ChangeDeclaredMemberRole
{
    /// <summary>
    /// Changes the declared member role.
    /// </summary>
    public static void Execute(
        Competition competition,
        EntryId entryId,
        MemberId memberId,
        DeclaredMemberRole role,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(clock);
        EnsureDefinedRole(role);
        competition.ChangeDeclaredMemberRole(entryId, memberId, role, clock);
    }

    private static void EnsureDefinedRole(DeclaredMemberRole role)
    {
        if (!Enum.IsDefined(role))
        {
            throw new ApplicationFailureException(
                $"Unknown declared member role '{role}'.",
                ApplicationErrorCodes.InvalidDeclaredMemberRole);
        }
    }
}

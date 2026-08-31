// -----------------------------------------------------------------------
// <copyright file="AddDeclaredMember.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Application use case: add a declared member to an entry roster.
/// </summary>
/// <remarks>
/// Domain owns competition/entry gates. Application validates the inbound <see cref="DeclaredMemberRole"/>.
/// </remarks>
public static class AddDeclaredMember
{
    /// <summary>
    /// Adds a declared member to the entry roster.
    /// </summary>
    /// <param name="competition">Target competition.</param>
    /// <param name="entryId">Entry identity.</param>
    /// <param name="displayName">Member display name.</param>
    /// <param name="role">Declared role (Player or Staff).</param>
    /// <param name="clock">Clock for domain events.</param>
    /// <returns>The created declared member.</returns>
    public static DeclaredMember Execute(
        Competition competition,
        EntryId entryId,
        string displayName,
        DeclaredMemberRole role,
        IClock clock) =>
        Execute(competition, entryId, displayName, role, MemberId.New(), clock);

    /// <summary>
    /// Adds a declared member with an explicit member identity.
    /// </summary>
    public static DeclaredMember Execute(
        Competition competition,
        EntryId entryId,
        string displayName,
        DeclaredMemberRole role,
        MemberId memberId,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(clock);
        EnsureDefinedRole(role);
        return competition.AddDeclaredMember(entryId, displayName, role, memberId, clock);
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

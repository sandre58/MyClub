// -----------------------------------------------------------------------
// <copyright file="CompetitionDeclaredMemberRoleChanged.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Competitions.Events;

/// <summary>
/// Raised when a declared member role is changed.
/// </summary>
public sealed record CompetitionDeclaredMemberRoleChanged : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompetitionDeclaredMemberRoleChanged"/> class.
    /// </summary>
    public CompetitionDeclaredMemberRoleChanged(
        CompetitionId competitionId,
        EntryId entryId,
        MemberId memberId,
        DeclaredMemberRole role,
        IClock clock)
        : base(clock)
    {
        CompetitionId = competitionId;
        EntryId = entryId;
        MemberId = memberId;
        Role = role;
    }

    /// <summary>Gets the competition identity.</summary>
    public CompetitionId CompetitionId { get; }

    /// <summary>Gets the entry identity.</summary>
    public EntryId EntryId { get; }

    /// <summary>Gets the declared member identity.</summary>
    public MemberId MemberId { get; }

    /// <summary>Gets the new role.</summary>
    public DeclaredMemberRole Role { get; }
}

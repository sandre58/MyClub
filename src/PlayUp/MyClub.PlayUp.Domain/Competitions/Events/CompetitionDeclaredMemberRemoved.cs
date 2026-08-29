// -----------------------------------------------------------------------
// <copyright file="CompetitionDeclaredMemberRemoved.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Competitions.Events;

/// <summary>
/// Raised when a declared member is removed from a competition entry.
/// </summary>
public sealed record CompetitionDeclaredMemberRemoved : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompetitionDeclaredMemberRemoved"/> class.
    /// </summary>
    public CompetitionDeclaredMemberRemoved(
        CompetitionId competitionId,
        EntryId entryId,
        MemberId memberId,
        IClock clock)
        : base(clock)
    {
        CompetitionId = competitionId;
        EntryId = entryId;
        MemberId = memberId;
    }

    /// <summary>Gets the competition identity.</summary>
    public CompetitionId CompetitionId { get; }

    /// <summary>Gets the entry identity.</summary>
    public EntryId EntryId { get; }

    /// <summary>Gets the removed member identity.</summary>
    public MemberId MemberId { get; }
}

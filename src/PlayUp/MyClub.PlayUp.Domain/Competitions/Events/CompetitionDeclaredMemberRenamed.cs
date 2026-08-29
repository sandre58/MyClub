// -----------------------------------------------------------------------
// <copyright file="CompetitionDeclaredMemberRenamed.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Competitions.Events;

/// <summary>
/// Raised when a declared member display name is changed.
/// </summary>
public sealed record CompetitionDeclaredMemberRenamed : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompetitionDeclaredMemberRenamed"/> class.
    /// </summary>
    public CompetitionDeclaredMemberRenamed(
        CompetitionId competitionId,
        EntryId entryId,
        MemberId memberId,
        string displayName,
        IClock clock)
        : base(clock)
    {
        CompetitionId = competitionId;
        EntryId = entryId;
        MemberId = memberId;
        DisplayName = displayName;
    }

    /// <summary>Gets the competition identity.</summary>
    public CompetitionId CompetitionId { get; }

    /// <summary>Gets the entry identity.</summary>
    public EntryId EntryId { get; }

    /// <summary>Gets the declared member identity.</summary>
    public MemberId MemberId { get; }

    /// <summary>Gets the new display name.</summary>
    public string DisplayName { get; }
}

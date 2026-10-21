// -----------------------------------------------------------------------
// <copyright file="CompetitionEntryRenamed.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Competitions.Events;

/// <summary>
/// Raised when an entry display name is changed.
/// </summary>
public sealed record CompetitionEntryRenamed : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompetitionEntryRenamed"/> class.
    /// </summary>
    /// <param name="competitionId">The competition identity.</param>
    /// <param name="entryId">The entry identity.</param>
    /// <param name="displayName">The new display name.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public CompetitionEntryRenamed(CompetitionId competitionId, EntryId entryId, string displayName, IClock clock)
        : base(clock)
    {
        CompetitionId = competitionId;
        EntryId = entryId;
        DisplayName = displayName;
    }

    /// <summary>
    /// Gets the competition identity.
    /// </summary>
    public CompetitionId CompetitionId { get; }

    /// <summary>
    /// Gets the entry identity.
    /// </summary>
    public EntryId EntryId { get; }

    /// <summary>
    /// Gets the new display name.
    /// </summary>
    public string DisplayName { get; }
}

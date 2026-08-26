// -----------------------------------------------------------------------
// <copyright file="CompetitionEntryPresentationUpdated.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Competitions.Events;

/// <summary>
/// Raised when entry presentation metadata is updated.
/// </summary>
public sealed record CompetitionEntryPresentationUpdated : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompetitionEntryPresentationUpdated"/> class.
    /// </summary>
    /// <param name="competitionId">The competition identity.</param>
    /// <param name="entryId">The entry identity.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public CompetitionEntryPresentationUpdated(CompetitionId competitionId, EntryId entryId, IClock clock)
        : base(clock)
    {
        CompetitionId = competitionId;
        EntryId = entryId;
    }

    /// <summary>
    /// Gets the competition identity.
    /// </summary>
    public CompetitionId CompetitionId { get; }

    /// <summary>
    /// Gets the entry identity.
    /// </summary>
    public EntryId EntryId { get; }
}

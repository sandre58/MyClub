// -----------------------------------------------------------------------
// <copyright file="CompetitionEntryAdded.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Competitions.Events;

/// <summary>
/// Raised when an entry is added to a competition.
/// </summary>
public sealed record CompetitionEntryAdded : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompetitionEntryAdded"/> class.
    /// </summary>
    /// <param name="competitionId">The competition identity.</param>
    /// <param name="entryId">The entry identity.</param>
    /// <param name="teamId">The team identity.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public CompetitionEntryAdded(CompetitionId competitionId, EntryId entryId, TeamId teamId, IClock clock)
        : base(clock)
    {
        CompetitionId = competitionId;
        EntryId = entryId;
        TeamId = teamId;
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
    /// Gets the team identity.
    /// </summary>
    public TeamId TeamId { get; }
}

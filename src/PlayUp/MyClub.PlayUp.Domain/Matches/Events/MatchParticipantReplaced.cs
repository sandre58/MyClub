// -----------------------------------------------------------------------
// <copyright file="MatchParticipantReplaced.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Matches.Events;

/// <summary>
/// Raised when a known match side is replaced by another entry.
/// </summary>
public sealed record MatchParticipantReplaced : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MatchParticipantReplaced"/> class.
    /// </summary>
    /// <param name="matchId">The match identity.</param>
    /// <param name="side">The side that changed.</param>
    /// <param name="previousEntryId">The entry that left that side.</param>
    /// <param name="entryId">The entry that now occupies that side.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public MatchParticipantReplaced(
        MatchId matchId,
        Side side,
        EntryId previousEntryId,
        EntryId entryId,
        IClock clock)
        : base(clock)
    {
        MatchId = matchId;
        Side = side;
        PreviousEntryId = previousEntryId;
        EntryId = entryId;
    }

    /// <summary>Gets the match identity.</summary>
    public MatchId MatchId { get; }

    /// <summary>Gets the side that changed.</summary>
    public Side Side { get; }

    /// <summary>Gets the entry that left that side.</summary>
    public EntryId PreviousEntryId { get; }

    /// <summary>Gets the entry that now occupies that side.</summary>
    public EntryId EntryId { get; }
}

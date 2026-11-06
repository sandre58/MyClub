// -----------------------------------------------------------------------
// <copyright file="MatchParticipantResolved.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Matches.Events;

/// <summary>
/// Raised when a previously unknown match side receives its entry.
/// </summary>
public sealed record MatchParticipantResolved : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MatchParticipantResolved"/> class.
    /// </summary>
    /// <param name="matchId">The match identity.</param>
    /// <param name="side">The side that was unknown.</param>
    /// <param name="entryId">The entry now occupying that side.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public MatchParticipantResolved(MatchId matchId, Side side, EntryId entryId, IClock clock)
        : base(clock)
    {
        MatchId = matchId;
        Side = side;
        EntryId = entryId;
    }

    /// <summary>Gets the match identity.</summary>
    public MatchId MatchId { get; }

    /// <summary>Gets the side that was filled.</summary>
    public Side Side { get; }

    /// <summary>Gets the entry now occupying that side.</summary>
    public EntryId EntryId { get; }
}

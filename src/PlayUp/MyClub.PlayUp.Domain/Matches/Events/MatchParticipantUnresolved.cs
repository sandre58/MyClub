// -----------------------------------------------------------------------
// <copyright file="MatchParticipantUnresolved.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Matches.Events;

/// <summary>
/// Raised when a known match side becomes unknown again.
/// </summary>
public sealed record MatchParticipantUnresolved : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MatchParticipantUnresolved"/> class.
    /// </summary>
    /// <param name="matchId">The match identity.</param>
    /// <param name="side">The side that was cleared.</param>
    /// <param name="entryId">The entry that left that side.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public MatchParticipantUnresolved(MatchId matchId, Side side, EntryId entryId, IClock clock)
        : base(clock)
    {
        MatchId = matchId;
        Side = side;
        EntryId = entryId;
    }

    /// <summary>Gets the match identity.</summary>
    public MatchId MatchId { get; }

    /// <summary>Gets the side that was cleared.</summary>
    public Side Side { get; }

    /// <summary>Gets the entry that left that side.</summary>
    public EntryId EntryId { get; }
}

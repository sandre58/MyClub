// -----------------------------------------------------------------------
// <copyright file="MatchCancelled.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Match.Events;

/// <summary>
/// Raised when a match is cancelled.
/// </summary>
public sealed record MatchCancelled : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MatchCancelled"/> class.
    /// </summary>
    /// <param name="matchId">The match identity.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public MatchCancelled(MatchId matchId, IClock clock)
        : base(clock) => MatchId = matchId;

    /// <summary>
    /// Gets the match identity.
    /// </summary>
    public MatchId MatchId { get; }
}

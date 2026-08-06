// -----------------------------------------------------------------------
// <copyright file="MatchStarted.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Match.Events;

/// <summary>
/// Raised when a match starts (Scheduled to Live).
/// </summary>
public sealed record MatchStarted : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MatchStarted"/> class.
    /// </summary>
    /// <param name="matchId">The match identity.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public MatchStarted(MatchId matchId, IClock clock)
        : base(clock) => MatchId = matchId;

    /// <summary>
    /// Gets the match identity.
    /// </summary>
    public MatchId MatchId { get; }
}

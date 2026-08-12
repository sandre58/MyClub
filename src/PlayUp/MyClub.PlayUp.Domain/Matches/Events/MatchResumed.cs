// -----------------------------------------------------------------------
// <copyright file="MatchResumed.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Matches.Events;

/// <summary>
/// Raised when a postponed match returns to Scheduled.
/// </summary>
public sealed record MatchResumed : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MatchResumed"/> class.
    /// </summary>
    /// <param name="matchId">The match identity.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public MatchResumed(MatchId matchId, IClock clock)
        : base(clock) => MatchId = matchId;

    /// <summary>
    /// Gets the match identity.
    /// </summary>
    public MatchId MatchId { get; }
}

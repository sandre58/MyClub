// -----------------------------------------------------------------------
// <copyright file="MatchFinishedResultInvalidated.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Matches.Events;

/// <summary>
/// Raised when a finished match returns to not played and its result and player sheet are dropped.
/// </summary>
public sealed record MatchFinishedResultInvalidated : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MatchFinishedResultInvalidated"/> class.
    /// </summary>
    /// <param name="matchId">The match identity.</param>
    /// <param name="clock">The clock providing the occurrence timestamp.</param>
    public MatchFinishedResultInvalidated(MatchId matchId, IClock clock)
        : base(clock) =>
        MatchId = matchId;

    /// <summary>Gets the match identity.</summary>
    public MatchId MatchId { get; }
}

// -----------------------------------------------------------------------
// <copyright file="MatchDeclaredParticipationRemoved.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Matches.Events;

/// <summary>
/// Raised when a declared participation is removed from a match composition.
/// </summary>
public sealed record MatchDeclaredParticipationRemoved : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MatchDeclaredParticipationRemoved"/> class.
    /// </summary>
    public MatchDeclaredParticipationRemoved(MatchId matchId, MemberId memberId, IClock clock)
        : base(clock)
    {
        MatchId = matchId;
        MemberId = memberId;
    }

    /// <summary>Gets the match identity.</summary>
    public MatchId MatchId { get; }

    /// <summary>Gets the member identity.</summary>
    public MemberId MemberId { get; }
}

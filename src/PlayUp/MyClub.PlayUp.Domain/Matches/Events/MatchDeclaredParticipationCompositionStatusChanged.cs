// -----------------------------------------------------------------------
// <copyright file="MatchDeclaredParticipationCompositionStatusChanged.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Matches.Events;

/// <summary>
/// Raised when a declared participation composition status changes.
/// </summary>
public sealed record MatchDeclaredParticipationCompositionStatusChanged : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MatchDeclaredParticipationCompositionStatusChanged"/> class.
    /// </summary>
    public MatchDeclaredParticipationCompositionStatusChanged(
        MatchId matchId,
        MemberId memberId,
        CompositionStatus compositionStatus,
        IClock clock)
        : base(clock)
    {
        MatchId = matchId;
        MemberId = memberId;
        CompositionStatus = compositionStatus;
    }

    /// <summary>Gets the match identity.</summary>
    public MatchId MatchId { get; }

    /// <summary>Gets the member identity.</summary>
    public MemberId MemberId { get; }

    /// <summary>Gets the new composition status.</summary>
    public CompositionStatus CompositionStatus { get; }
}

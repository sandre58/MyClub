// -----------------------------------------------------------------------
// <copyright file="MatchDeclaredParticipationAdded.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Matches.Events;

/// <summary>
/// Raised when a declared participation is added to a match composition.
/// </summary>
public sealed record MatchDeclaredParticipationAdded : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MatchDeclaredParticipationAdded"/> class.
    /// </summary>
    public MatchDeclaredParticipationAdded(
        MatchId matchId,
        MemberId memberId,
        Side side,
        CompositionStatus compositionStatus,
        int? jerseyNumber,
        IClock clock)
        : base(clock)
    {
        MatchId = matchId;
        MemberId = memberId;
        Side = side;
        CompositionStatus = compositionStatus;
        JerseyNumber = jerseyNumber;
    }

    /// <summary>Gets the match identity.</summary>
    public MatchId MatchId { get; }

    /// <summary>Gets the member identity.</summary>
    public MemberId MemberId { get; }

    /// <summary>Gets the match side.</summary>
    public Side Side { get; }

    /// <summary>Gets the composition status.</summary>
    public CompositionStatus CompositionStatus { get; }

    /// <summary>Gets the optional jersey number.</summary>
    public int? JerseyNumber { get; }
}

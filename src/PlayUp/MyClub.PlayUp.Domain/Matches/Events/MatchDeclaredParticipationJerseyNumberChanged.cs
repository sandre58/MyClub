// -----------------------------------------------------------------------
// <copyright file="MatchDeclaredParticipationJerseyNumberChanged.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Matches.Events;

/// <summary>
/// Raised when a declared participation jersey number changes.
/// </summary>
public sealed record MatchDeclaredParticipationJerseyNumberChanged : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MatchDeclaredParticipationJerseyNumberChanged"/> class.
    /// </summary>
    public MatchDeclaredParticipationJerseyNumberChanged(
        MatchId matchId,
        MemberId memberId,
        int? jerseyNumber,
        IClock clock)
        : base(clock)
    {
        MatchId = matchId;
        MemberId = memberId;
        JerseyNumber = jerseyNumber;
    }

    /// <summary>Gets the match identity.</summary>
    public MatchId MatchId { get; }

    /// <summary>Gets the member identity.</summary>
    public MemberId MemberId { get; }

    /// <summary>Gets the new optional jersey number.</summary>
    public int? JerseyNumber { get; }
}

// -----------------------------------------------------------------------
// <copyright file="MatchRecordedGoalAdded.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Matches.Events;

/// <summary>
/// Raised when a nominative goal attribution is recorded on a match.
/// Does not imply any change to <see cref="RunningScore"/> or <see cref="MatchResult.Score"/>.
/// </summary>
public sealed record MatchRecordedGoalAdded : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MatchRecordedGoalAdded"/> class.
    /// </summary>
    public MatchRecordedGoalAdded(
        MatchId matchId,
        GoalId goalId,
        MemberId scorerMemberId,
        Side creditedSide,
        MemberId? assisterMemberId,
        IClock clock)
        : base(clock)
    {
        MatchId = matchId;
        GoalId = goalId;
        ScorerMemberId = scorerMemberId;
        CreditedSide = creditedSide;
        AssisterMemberId = assisterMemberId;
    }

    /// <summary>Gets the match identity.</summary>
    public MatchId MatchId { get; }

    /// <summary>Gets the recorded goal identity.</summary>
    public GoalId GoalId { get; }

    /// <summary>Gets the scorer member identity.</summary>
    public MemberId ScorerMemberId { get; }

    /// <summary>Gets the side credited with the goal.</summary>
    public Side CreditedSide { get; }

    /// <summary>Gets the optional assister member identity.</summary>
    public MemberId? AssisterMemberId { get; }
}

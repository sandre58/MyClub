// -----------------------------------------------------------------------
// <copyright file="MatchRecordedGoalRemoved.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Matches.Events;

/// <summary>
/// Raised when a nominative goal attribution is removed from a match.
/// Does not imply any change to <see cref="RunningScore"/> or <see cref="MatchResult.Score"/>.
/// </summary>
public sealed record MatchRecordedGoalRemoved : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MatchRecordedGoalRemoved"/> class.
    /// </summary>
    public MatchRecordedGoalRemoved(MatchId matchId, GoalId goalId, IClock clock)
        : base(clock)
    {
        MatchId = matchId;
        GoalId = goalId;
    }

    /// <summary>Gets the match identity.</summary>
    public MatchId MatchId { get; }

    /// <summary>Gets the removed recorded goal identity.</summary>
    public GoalId GoalId { get; }
}

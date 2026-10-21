// -----------------------------------------------------------------------
// <copyright file="MatchRunningScoreChanged.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Matches.Events;

/// <summary>
/// Raised when a match running score changes to a different value (not an audit trail).
/// </summary>
public sealed record MatchRunningScoreChanged : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MatchRunningScoreChanged"/> class.
    /// </summary>
    public MatchRunningScoreChanged(MatchId matchId, RunningScore runningScore, IClock clock)
        : base(clock)
    {
        MatchId = matchId;
        RunningScore = runningScore;
    }

    /// <summary>Gets the match identity.</summary>
    public MatchId MatchId { get; }

    /// <summary>Gets the new running score.</summary>
    public RunningScore RunningScore { get; }
}

// -----------------------------------------------------------------------
// <copyright file="SetRunningScore.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Application.Matches;

/// <summary>
/// Application use case: replace the observed Live running score on a match.
/// </summary>
/// <remarks>
/// Domain owns Live-only mutation and non-negative invariants on <see cref="RunningScore"/>.
/// Distinct from <see cref="MatchResult.Score"/> and from nominative <see cref="RecordedGoal"/> facts.
/// </remarks>
public static class SetRunningScore
{
    /// <summary>
    /// Sets the absolute running score while the match is Live.
    /// </summary>
    /// <param name="match">Live match aggregate.</param>
    /// <param name="homeGoals">Current home goals (≥ 0).</param>
    /// <param name="awayGoals">Current away goals (≥ 0).</param>
    /// <param name="clock">Clock for domain events.</param>
    public static void Execute(Match match, int homeGoals, int awayGoals, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(clock);
        match.SetRunningScore(new RunningScore(homeGoals, awayGoals), clock);
    }
}

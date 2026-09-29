// -----------------------------------------------------------------------
// <copyright file="RankingCriterion.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Ordered standing ranking criterion. FairPlay is out of scope.
/// </summary>
public enum RankingCriterion
{
    /// <summary>
    /// Total points.
    /// </summary>
    Points = 0,

    /// <summary>
    /// Goal difference (goals for − goals against).
    /// </summary>
    GoalDifference = 1,

    /// <summary>
    /// Goals scored.
    /// </summary>
    GoalsFor = 2,

    /// <summary>
    /// Goals conceded.
    /// </summary>
    GoalsAgainst = 3,

    /// <summary>
    /// Number of wins.
    /// </summary>
    Wins = 4,

    /// <summary>
    /// Head-to-head results among tied teams.
    /// </summary>
    HeadToHead = 5
}

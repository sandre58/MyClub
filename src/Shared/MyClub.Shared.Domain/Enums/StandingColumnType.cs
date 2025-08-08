// -----------------------------------------------------------------------
// <copyright file="StandingColumnType.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.Shared.Domain.Enums;

/// <summary>
/// Enumeration representing the different types of columns that can be displayed in a standings table.
/// These columns represent various match statistics used to rank teams in competitions.
/// </summary>
public enum StandingColumnType
{
    /// <summary>
    /// Total number of games played by the team.
    /// This includes all completed matches in the competition.
    /// </summary>
    GamesPlayed,

    /// <summary>
    /// Number of games won in regular time.
    /// This counts victories achieved without requiring extra time or shootouts.
    /// </summary>
    GamesWon,

    /// <summary>
    /// Number of games won after shootouts.
    /// This counts victories that were decided by penalty shootouts after a draw in regular time.
    /// </summary>
    GamesWonAfterShootouts,

    /// <summary>
    /// Number of games that ended in a draw.
    /// This counts matches where both teams finished with the same score.
    /// </summary>
    GamesDrawn,

    /// <summary>
    /// Number of games lost in regular time.
    /// This counts defeats suffered without extra time or shootouts.
    /// </summary>
    GamesLost,

    /// <summary>
    /// Number of games lost after shootouts.
    /// This counts defeats that occurred in penalty shootouts after a draw in regular time.
    /// </summary>
    GamesLostAfterShootouts,

    /// <summary>
    /// Number of games where the team withdrew or forfeited.
    /// This counts matches where the team did not complete the game due to withdrawal or disqualification.
    /// </summary>
    GamesWithdrawn,

    /// <summary>
    /// Total number of goals scored by the team.
    /// This is the offensive statistic showing the team's scoring ability.
    /// </summary>
    GoalsFor,

    /// <summary>
    /// Total number of goals conceded by the team.
    /// This is the defensive statistic showing goals allowed by the team.
    /// </summary>
    GoalsAgainst,

    /// <summary>
    /// Goal difference (goals for minus goals against).
    /// This calculated statistic shows the net goal balance and is often used as a tiebreaker in standings.
    /// </summary>
    GoalsDifference
}

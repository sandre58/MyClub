// -----------------------------------------------------------------------
// <copyright file="GoalType.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.Shared.Domain.Enums;

/// <summary>
/// Enumeration representing the type of goal scored in a match.
/// This categorizes goals based on how they were scored, which is important for match statistics and analysis.
/// </summary>
public enum GoalType
{
    /// <summary>
    /// A regular goal scored during normal play.
    /// This is the most common type of goal scored from open play situations.
    /// </summary>
    Regular,

    /// <summary>
    /// An own goal scored by a player into their own team's net.
    /// This occurs when a player accidentally or inadvertently scores against their own team.
    /// </summary>
    OwnGoal,

    /// <summary>
    /// A goal scored from a penalty kick.
    /// This is awarded when a foul is committed in the penalty area, resulting in a direct shot on goal.
    /// </summary>
    Penalty,

    /// <summary>
    /// A goal scored directly from a free kick.
    /// This occurs when a player scores directly from a free kick situation without the ball touching another player.
    /// </summary>
    FreeKick
}

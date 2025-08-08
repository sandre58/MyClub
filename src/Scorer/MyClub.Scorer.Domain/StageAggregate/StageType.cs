// -----------------------------------------------------------------------
// <copyright file="StageType.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.Scorer.Domain.StageAggregate;

/// <summary>
/// Defines the types of stages supported in tournament competitions.
/// Each type represents a different format and behavior for organizing teams and matches within a tournament phase.
/// </summary>
public enum StageType
{
    /// <summary>
    /// Represents a knockout stage format.
    /// Teams compete in elimination matches where losers are eliminated from the competition.
    /// Common in cup competitions and playoff systems where a decisive winner must be determined.
    /// Examples: Champions League knockout rounds, World Cup knockout stage.
    /// </summary>
    Knockout,

    /// <summary>
    /// Represents a championship stage format.
    /// Teams compete in a league-style format with standings, where all teams play against each other
    /// and are ranked based on points, goal difference, and other criteria.
    /// Examples: Champions League final tournament phase, some playoff formats.
    /// </summary>
    Championship,

    /// <summary>
    /// Represents a group stage format.
    /// Teams are divided into multiple groups, with each group functioning as a mini-league.
    /// Teams within each group play against each other, and progression is typically based on group standings.
    /// Examples: Champions League group stage, World Cup group stage, Europa League group stage.
    /// </summary>
    Groups
}

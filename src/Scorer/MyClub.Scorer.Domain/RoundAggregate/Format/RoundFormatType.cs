// -----------------------------------------------------------------------
// <copyright file="RoundFormatType.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.Scorer.Domain.RoundAggregate.Format;

/// <summary>
/// Defines the types of round formats supported in knockout tournament competitions.
/// Each type represents a different structure for organizing matches within a tournament round,
/// affecting how teams compete and progress through the elimination phases.
/// </summary>
public enum RoundFormatType
{
    /// <summary>
    /// Single match elimination format.
    /// Teams play one match, and the winner advances while the loser is eliminated.
    /// Most common format for knockout tournaments, providing clear and quick progression.
    /// Used in: FA Cup later rounds, World Cup knockout stage, most domestic cup competitions.
    /// </summary>
    Single,

    /// <summary>
    /// Home-and-away two-legged format.
    /// Teams play twice (once at each venue) with the aggregate score determining the winner.
    /// Reduces home advantage and provides more balanced competition over two matches.
    /// Used in: Champions League knockout rounds, Europa League.
    /// </summary>
    HomeAndAway,

    /// <summary>
    /// Best-of-X series format.
    /// Teams play multiple matches until one team wins a majority (e.g., first to win 3 out of 5).
    /// Provides the most comprehensive test of team strength but requires more time.
    /// Used in: Some playoff systems, specialized tournaments, American sports playoffs.
    /// </summary>
    BestOf,

    /// <summary>
    /// Single match with replay if drawn format.
    /// If the initial match ends in a draw, a replay match is scheduled.
    /// Traditional format that maintains single-match efficiency while ensuring a winner.
    /// Used in: FA Cup early rounds (historically), some domestic cups with replay traditions.
    /// </summary>
    Replay
}

// -----------------------------------------------------------------------
// <copyright file="MatchOutcome.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.Shared.Domain.Enums;

/// <summary>
/// Enumeration representing the possible outcomes of a match for a team.
/// This extends beyond simple win/draw/loss to include special situations like withdrawals.
/// </summary>
public enum MatchOutcome
{
    /// <summary>
    /// No outcome has been determined or the match has not been played.
    /// This is the default state for scheduled matches.
    /// </summary>
    None,

    /// <summary>
    /// The team won the match.
    /// This indicates a victory in regular time, extra time, or penalty shootouts.
    /// </summary>
    Win,

    /// <summary>
    /// The match ended in a draw.
    /// This means both teams scored the same number of goals and no winner was determined.
    /// </summary>
    Draw,

    /// <summary>
    /// The team lost the match.
    /// This indicates a defeat in regular time, extra time, or penalty shootouts.
    /// </summary>
    Loss
}

// -----------------------------------------------------------------------
// <copyright file="MatchResultType.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.Shared.Domain.Enums;

/// <summary>
/// Enumeration representing the detailed result type of match for a team.
/// This provides more granular result information than MatchOutcome, including
/// special cases like shootout victories and withdrawals.
/// </summary>
public enum MatchResultType
{
    /// <summary>
    /// No result has been determined or the match has not been played.
    /// This is the default state for scheduled matches.
    /// </summary>
    None,

    /// <summary>
    /// The team won the match in regular time.
    /// This indicates a victory achieved without requiring extra time or penalty shootouts.
    /// </summary>
    Win,

    /// <summary>
    /// The match ended in a draw.
    /// This means both teams scored the same number of goals and no winner was determined.
    /// </summary>
    Draw,

    /// <summary>
    /// The team lost the match in regular time.
    /// This indicates a defeat suffered without extra time or penalty shootouts.
    /// </summary>
    Loss,

    /// <summary>
    /// The team withdrew from the match.
    /// This indicates that the team forfeited or was disqualified from the match.
    /// </summary>
    Withdraw,

    /// <summary>
    /// The team won the match after a penalty shootout.
    /// This indicates that the match was tied after regular time (and possibly extra time)
    /// and was decided in favor of this team through a penalty shootout.
    /// </summary>
    WinAfterShootouts,

    /// <summary>
    /// The team lost the match after a penalty shootout.
    /// This indicates that the match was tied after regular time (and possibly extra time)
    /// and was decided against this team through a penalty shootout.
    /// </summary>
    LossAfterShootouts
}

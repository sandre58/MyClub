// -----------------------------------------------------------------------
// <copyright file="MatchStatus.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.Shared.Domain.Enums;

/// <summary>
/// Enumeration representing the current status of a match in the competition system.
/// This tracks the lifecycle of a match from scheduling to completion.
/// </summary>
public enum MatchStatus
{
    /// <summary>
    /// The match has no specific status set.
    /// This is typically used for newly created matches or as a default state.
    /// </summary>
    None,

    /// <summary>
    /// The match is currently being played.
    /// This indicates that the match has started but has not yet concluded.
    /// </summary>
    InProgress,

    /// <summary>
    /// The match has been temporarily suspended.
    /// This could be due to weather conditions, technical issues, or other circumstances requiring a pause.
    /// </summary>
    Suspended,

    /// <summary>
    /// The match has been completed and a result has been recorded.
    /// This is the final status for matches that have concluded normally.
    /// </summary>
    Played,

    /// <summary>
    /// The match has been postponed to a later date.
    /// This indicates that the match was scheduled but has been moved to a different time/date.
    /// </summary>
    Postponed,

    /// <summary>
    /// The match has been cancelled and will not be played.
    /// This indicates that the match will not take place and typically no result will be recorded.
    /// </summary>
    Cancelled
}

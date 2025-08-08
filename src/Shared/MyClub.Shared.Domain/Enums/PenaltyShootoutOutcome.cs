// -----------------------------------------------------------------------
// <copyright file="PenaltyShootoutOutcome.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.Shared.Domain.Enums;

/// <summary>
/// Enumeration representing the outcome of an individual penalty shot in a penalty shootout.
/// This tracks whether each penalty attempt was successful or not.
/// </summary>
public enum PenaltyShootoutOutcome
{
    /// <summary>
    /// No outcome has been recorded for this penalty shot.
    /// This is the default state before the penalty is taken.
    /// </summary>
    None,

    /// <summary>
    /// The penalty shot was successful (goal scored).
    /// The ball entered the goal and a point was awarded to the shooting team.
    /// </summary>
    Succeeded,

    /// <summary>
    /// The penalty shot failed (no goal scored).
    /// This includes shots that were saved, missed the target, or hit the post/crossbar.
    /// </summary>
    Failed
}

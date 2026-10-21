// -----------------------------------------------------------------------
// <copyright file="CompetitionStatus.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Common;

/// <summary>
/// Lifecycle status of a Competition aggregate.
/// </summary>
public enum CompetitionStatus
{
    /// <summary>
    /// Competition is being configured.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Configuration is valid and the competition can start.
    /// </summary>
    Ready = 1,

    /// <summary>
    /// Competition is in progress; structural rules are locked.
    /// </summary>
    Running = 2,

    /// <summary>
    /// Competition is temporarily suspended.
    /// </summary>
    Suspended = 3,

    /// <summary>
    /// Competition has finished.
    /// </summary>
    Completed = 4,

    /// <summary>
    /// Competition is archived and read-only.
    /// </summary>
    Archived = 5
}

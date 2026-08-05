// -----------------------------------------------------------------------
// <copyright file="MatchStatus.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Common;

/// <summary>
/// Lifecycle status of a Match aggregate.
/// </summary>
public enum MatchStatus
{
    /// <summary>
    /// Match is scheduled but not started.
    /// </summary>
    Scheduled = 0,

    /// <summary>
    /// Match is currently being played.
    /// </summary>
    Live = 1,

    /// <summary>
    /// Match has finished and has a result.
    /// </summary>
    Finished = 2,

    /// <summary>
    /// Match has been postponed.
    /// </summary>
    Postponed = 3,

    /// <summary>
    /// Match has been cancelled.
    /// </summary>
    Cancelled = 4
}

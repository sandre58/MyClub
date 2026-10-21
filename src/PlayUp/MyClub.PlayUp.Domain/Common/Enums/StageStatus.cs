// -----------------------------------------------------------------------
// <copyright file="StageStatus.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Common;

/// <summary>
/// Lifecycle status of a Stage aggregate.
/// </summary>
public enum StageStatus
{
    /// <summary>
    /// Stage structure is being configured.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Structure is valid and draw is published when required.
    /// </summary>
    Ready = 1,

    /// <summary>
    /// Stage is in progress; structure is locked.
    /// </summary>
    Running = 2,

    /// <summary>
    /// Stage is temporarily suspended.
    /// </summary>
    Suspended = 3,

    /// <summary>
    /// Stage has finished.
    /// </summary>
    Completed = 4
}

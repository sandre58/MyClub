// -----------------------------------------------------------------------
// <copyright file="DrawStatus.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Common;

/// <summary>
/// Lifecycle status of a Draw entity.
/// </summary>
public enum DrawStatus
{
    /// <summary>
    /// Draw is being prepared.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Draw has been published and is immutable.
    /// </summary>
    Published = 1,

    /// <summary>
    /// Draw has been cancelled; a new draw is required for a redo.
    /// </summary>
    Cancelled = 2
}

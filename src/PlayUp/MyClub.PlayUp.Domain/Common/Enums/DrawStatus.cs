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
    /// Draw proposal is ready to publish.
    /// </summary>
    Ready = 1,

    /// <summary>
    /// Draw has been published and is immutable.
    /// </summary>
    Published = 2,

    /// <summary>
    /// Draw has been cancelled; a new draw is required for a redo.
    /// </summary>
    Cancelled = 3
}

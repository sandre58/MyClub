// -----------------------------------------------------------------------
// <copyright file="StageInsertPosition.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Where a newly born stage is placed relative to an existing anchor stage.
/// </summary>
public enum StageInsertPosition
{
    /// <summary>Immediately before the anchor in <c>Competition.StageIds</c>.</summary>
    Before = 0,

    /// <summary>Immediately after the anchor in <c>Competition.StageIds</c>.</summary>
    After = 1
}

// -----------------------------------------------------------------------
// <copyright file="DrawResolutionState.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Stage;

/// <summary>
/// Outcome of the draw resolution process (orthogonal to <see cref="Common.DrawStatus"/> lifecycle).
/// </summary>
public enum DrawResolutionState
{
    /// <summary>
    /// No resolution recorded yet.
    /// </summary>
    NotResolved = 0,

    /// <summary>
    /// A valid resolution was recorded.
    /// </summary>
    Resolved = 1,

    /// <summary>
    /// Resolution attempt found no admissible solution (≠ Cancelled).
    /// </summary>
    NoSolution = 2
}

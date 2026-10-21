// -----------------------------------------------------------------------
// <copyright file="DrawInputsIntent.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application intent when building default <see cref="Domain.Stages.DrawInputs"/>.
/// </summary>
/// <remarks>
/// <see cref="Default"/> applies Encoding F (composition + Fixed* from form occupancy).
/// <see cref="Rerun"/> is an explicit full redraw: CompositionEntries only, Fixed* empty —
/// does not clear stage placements; Cancel remains lifecycle-only.
/// </remarks>
public enum DrawInputsIntent
{
    /// <summary>Encoding F — Fixed* from current group/slot occupancy.</summary>
    Default = 0,

    /// <summary>Full re-draw — ignore occupancy-derived Fixed*; reshuffle the free pool.</summary>
    Rerun = 1
}

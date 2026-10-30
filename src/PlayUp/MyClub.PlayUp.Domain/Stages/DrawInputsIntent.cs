// -----------------------------------------------------------------------
// <copyright file="DrawInputsIntent.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Intent recorded when configuring draw inputs on a Draft draw.
/// </summary>
/// <remarks>
/// <see cref="Default"/> uses the composition pool and Fixed* from current form occupancy.
/// <see cref="Rerun"/> is an explicit full redraw: CompositionEntries only, Fixed* empty.
/// <see cref="Partial"/> draws only unplaced teams into free slots. Occupants stay outside the pool.
/// </remarks>
public enum DrawInputsIntent
{
    /// <summary>Fixed* from current group and slot occupancy.</summary>
    Default = 0,

    /// <summary>Full re-draw — ignore occupancy-derived Fixed*; reshuffle the free pool.</summary>
    Rerun = 1,

    /// <summary>Slot draw of unplaced teams into free slots. Existing occupants are not in the pool.</summary>
    Partial = 2,
}

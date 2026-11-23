// -----------------------------------------------------------------------
// <copyright file="SlotCupRedoPath.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Shared Cup Slot redo routing (C2-ter voie P vs A'2 Rerun). One criterion for
/// <see cref="DrawActionEvaluator"/>, <see cref="RedoDraw"/>, and Partial generation targets.
/// </summary>
internal static class SlotCupRedoPath
{
    /// <summary>
    /// Partial voie P: relaunch after release with <see cref="DrawInputsIntent.Partial"/>.
    /// Applies when the applied draw is Partial, or when it has no Default fixed slots (legacy full grid).
    /// </summary>
    public static bool UsesPartialVoieP(Draw draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        if (draw.Kind != DrawResolutionKind.Slot)
        {
            return false;
        }

        if (draw.InputsIntent == DrawInputsIntent.Partial)
        {
            return true;
        }

        return draw.Inputs is not { FixedSlots.Count: > 0 };
    }

    /// <summary>
    /// A'2: Default / Rerun with fixed slots — full <see cref="DrawInputsIntent.Rerun"/> after release.
    /// </summary>
    public static bool UsesDefaultRerun(Draw draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        return draw.Kind == DrawResolutionKind.Slot
            && draw.InputsIntent != DrawInputsIntent.Partial
            && draw.Inputs is { FixedSlots.Count: > 0 };
    }

    /// <summary>
    /// Draft or applied draw configured as Partial (generation narrows targets to free slots).
    /// </summary>
    public static bool UsesPartialGeneration(Draw draw) =>
        draw.Kind == DrawResolutionKind.Slot
        && draw.InputsIntent == DrawInputsIntent.Partial;
}

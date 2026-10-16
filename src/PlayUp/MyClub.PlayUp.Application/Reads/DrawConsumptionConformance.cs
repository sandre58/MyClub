// -----------------------------------------------------------------------
// <copyright file="DrawConsumptionConformance.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// C-ENF-Detect — whether a draw resolution is obsolete for consumption (W-inputs + W-pool; §11C C4).
/// </summary>
/// <remarks>
/// Distinct from <see cref="DrawAppliedState"/> (occupancy) and from G-B-3 pool-change refuse.
/// W-rules comparison is explicitly out of scope for Detect.
/// </remarks>
public static class DrawConsumptionConformance
{
    /// <summary>
    /// Returns whether <paramref name="draw"/> is obsolete for Apply/Materialize consumption.
    /// </summary>
    public static bool IsObsoleteForConsumption(Draw draw, Stage stage)
    {
        ArgumentNullException.ThrowIfNull(draw);
        ArgumentNullException.ThrowIfNull(stage);

        if (draw.Status is DrawStatus.Cancelled or DrawStatus.Draft)
        {
            return false;
        }

        if (draw.Resolution.State is DrawResolutionState.NoSolution)
        {
            return false;
        }

        if (draw.Status != DrawStatus.Published)
        {
            return false;
        }

        if (draw.Resolution.State != DrawResolutionState.Resolved)
        {
            return false;
        }

        if (DrawAppliedState.IsApplied(draw, stage))
        {
            return false;
        }

        if (draw.Inputs is null)
        {
            return true;
        }

        foreach (var placement in draw.Inputs.FixedGroups)
        {
            if (stage.FindGroup(placement.GroupId) is null)
            {
                return true;
            }
        }

        foreach (var placement in draw.Inputs.FixedSlots)
        {
            if (stage.FindSlot(placement.SlotKey) is null)
            {
                return true;
            }
        }

        var effectivePool = EffectiveEntryPool(stage);

        var resolutionEntries = draw.Kind switch
        {
            DrawResolutionKind.Group => draw.Resolution.GroupResults.Select(p => p.EntryId),
            DrawResolutionKind.Slot => draw.Resolution.SlotResults.Select(p => p.EntryId),
            _ => [],
        };

        foreach (var entry in resolutionEntries)
        {
            if (!draw.Inputs.Entries.Contains(entry))
            {
                return true;
            }

            if (!effectivePool.Contains(entry))
            {
                return true;
            }
        }

        return false;
    }

    private static HashSet<EntryId> EffectiveEntryPool(Stage stage)
    {
        var affectation = stage.AffectationAuthoring.Select(entry => entry.EntryId).ToHashSet();
        var composition = stage.CompositionEntries.Select(entry => entry.EntryId).ToHashSet();
        affectation.IntersectWith(composition);
        return affectation;
    }
}

// -----------------------------------------------------------------------
// <copyright file="DrawConsumptionConformance.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Whether a draw resolution is obsolete for consumption (stored inputs and the effective entry pool).
/// </summary>
/// <remarks>
/// Distinct from <see cref="DrawAppliedState"/> (occupancy) and from refusing pool changes while a draw is Applied.
/// Rules comparison is out of scope for this predicate.
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

        return ResolutionEntriesDiverge(draw, stage);
    }

    /// <summary>
    /// Pool and fixed-placement divergence, ignoring draw status.
    /// Publish-and-apply evaluates this on a Draft draw because Apply runs only after Publish.
    /// </summary>
    internal static bool ResolutionEntriesDiverge(Draw draw, Stage stage)
    {
        ArgumentNullException.ThrowIfNull(draw);
        ArgumentNullException.ThrowIfNull(stage);

        if (draw.Inputs is null)
        {
            return true;
        }

        if (draw.Inputs.FixedGroups.Any(placement => stage.FindGroup(placement.GroupId) is null))
        {
            return true;
        }

        if (draw.Inputs.FixedSlots.Any(placement => stage.FindSlot(placement.SlotKey) is null))
        {
            return true;
        }

        var effectivePool = EffectiveEntryPool(stage);

        var resolutionEntries = draw.Kind switch
        {
            DrawResolutionKind.Group => draw.Resolution.GroupResults.Select(p => p.EntryId),
            DrawResolutionKind.Slot => draw.Resolution.SlotResults.Select(p => p.EntryId),
            _ => []
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
        if (affectation.Count == 0)
        {
            return composition;
        }

        affectation.IntersectWith(composition);
        return affectation;
    }
}

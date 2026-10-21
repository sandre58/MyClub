// -----------------------------------------------------------------------
// <copyright file="DrawAppliedState.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Application-derived draw “Applied” state (Publish ≠ Apply).
/// </summary>
/// <remarks>
/// Domain has no Applied status. Applied means a <see cref="DrawStatus.Published"/> draw's
/// resolution occupancy already matches the stage — Publish ≠ Apply.
/// Draft/Cancelled draws never report Applied even if stage occupancy still matches a prior Apply
/// (Cancel does not unwind materialization).
/// </remarks>
public static class DrawAppliedState
{
    /// <summary>
    /// Returns whether a published resolved draw appears already applied on the stage.
    /// </summary>
    /// <param name="draw">Draw entity.</param>
    /// <param name="stage">Owning stage.</param>
    /// <returns><see langword="true"/> when the draw is Published and placements match.</returns>
    public static bool IsApplied(Draw draw, Stage stage)
    {
        ArgumentNullException.ThrowIfNull(draw);
        ArgumentNullException.ThrowIfNull(stage);

        return draw is { Status: DrawStatus.Published, Resolution.State: DrawResolutionState.Resolved } && draw.Kind switch
        {
            DrawResolutionKind.Slot => IsSlotApplied(draw, stage),
            DrawResolutionKind.Group => IsGroupApplied(draw, stage),
            _ => false
        };
    }

    private static bool IsGroupApplied(Draw draw, Stage stage)
    {
        var placements = draw.Resolution.GroupResults;
        return placements.Count != 0 && !(from placement in placements let @group = stage.FindGroup(placement.GroupId) where @group?.EntryIds.Contains(placement.EntryId) != true select placement).Any();
    }

    private static bool IsSlotApplied(Draw draw, Stage stage)
    {
        var placements = draw.Resolution.SlotResults;
        if (placements.Count == 0)
        {
            return false;
        }

        var byKey = stage.Slots.ToDictionary(slot => slot.SlotKey, StringComparer.Ordinal);
        foreach (var placement in placements)
        {
            if (!byKey.TryGetValue(placement.SlotKey, out var slot)
                || slot.EntryId is null
                || !slot.EntryId.Equals(placement.EntryId))
            {
                return false;
            }
        }

        return true;
    }
}

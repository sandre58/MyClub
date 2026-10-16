// -----------------------------------------------------------------------
// <copyright file="StructureAppliedDrawSlotPlacementGate.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// G-OPS3 B1: refuse manual Cup slot mutations that break placements guaranteed by an Applied draw.
/// </summary>
internal static class StructureAppliedDrawSlotPlacementGate
{
    private const string BlockMessage =
        "Slot assignment is protected by the applied draw on this stage.";

    internal static void EnsureAssignAllowed(Stage stage, string slotKey, EntryId proposedEntryId)
    {
        ArgumentNullException.ThrowIfNull(stage);
        if (!TryGetProtectedEntry(stage, slotKey, out var protectedEntry))
        {
            return;
        }

        if (!protectedEntry.Equals(proposedEntryId))
        {
            ThrowProtected();
        }
    }

    internal static void EnsureClearAllowed(Stage stage, string slotKey)
    {
        ArgumentNullException.ThrowIfNull(stage);

        if (TryGetProtectedEntry(stage, slotKey, out _))
        {
            ThrowProtected();
        }
    }

    private static void ThrowProtected() =>
        throw new ApplicationFailureException(
            BlockMessage,
            ApplicationErrorCodes.AppliedDrawSlotPlacementProtected);

    /// <summary>
    /// Returns whether <paramref name="slotKey"/> is protected by an Applied Slot draw (R0–R2).
    /// </summary>
    /// <remarks>
    /// When multiple Applied draws disagree on the same slot key, returns <see langword="false"/> (R4) so B1 does not refuse globally.
    /// With valid domain data this path is unreachable: <see cref="DrawResolution.ResolvedSlots"/> enforces unique slot keys,
    /// and two non-cancelled Applied draws cannot disagree on occupancy. The branch is defensive for corrupt persistence.
    /// </remarks>
    private static bool TryGetProtectedEntry(
        Stage stage,
        string slotKey,
        out EntryId? protectedEntry)
    {
        protectedEntry = null;
        var slot = stage.FindSlot(slotKey);
        if (slot is null)
        {
            return false;
        }

        var canonicalKey = slot.SlotKey;
        EntryId? sole = null;

        foreach (var draw in stage.Draws)
        {
            if (draw.Status == DrawStatus.Cancelled)
            {
                continue;
            }

            if (!DrawAppliedState.IsApplied(draw, stage) || draw.Kind != DrawResolutionKind.Slot)
            {
                continue;
            }

            foreach (var placement in draw.Resolution.SlotResults)
            {
                if (!PlacementTargetsSlot(stage, placement.SlotKey, canonicalKey))
                {
                    continue;
                }

                if (sole is null)
                {
                    sole = placement.EntryId;
                }
                else if (!sole.Equals(placement.EntryId))
                {
                    return false;
                }
            }
        }

        if (sole is null)
        {
            return false;
        }

        protectedEntry = sole;
        return true;
    }

    /// <summary>
    /// Aligns resolution slot keys with stage slots the same way occupancy is resolved for draws.
    /// </summary>
    private static bool PlacementTargetsSlot(Stage stage, string placementSlotKey, string canonicalSlotKey) =>
        stage.FindSlot(placementSlotKey) is { SlotKey: var resolved }
        && string.Equals(resolved, canonicalSlotKey, StringComparison.Ordinal);
}

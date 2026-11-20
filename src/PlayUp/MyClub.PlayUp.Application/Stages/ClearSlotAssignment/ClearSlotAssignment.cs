// -----------------------------------------------------------------------
// <copyright file="ClearSlotAssignment.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: clear a Cup <see cref="DirectAssignment"/> (and synced occupant).
/// </summary>
public static class ClearSlotAssignment
{
    /// <summary>
    /// Removes the direct assignment on <paramref name="slotKey"/> when the resolved occupant
    /// is still <paramref name="expectedEntryId"/>. Returns <see langword="false"/> when the slot
    /// is already free, or when no direct assignment changes the occupant.
    /// </summary>
    /// <remarks>
    /// A stale expectation is refused before placement guards, including an Applied draw gate.
    /// <paramref name="expectedEntryId"/> <see langword="null"/> means a free slot was expected.
    /// </remarks>
    public static bool Execute(
        Stage stage,
        string slotKey,
        EntryId? expectedEntryId,
        IReadOnlyList<Match>? stageMatches = null)
    {
        ArgumentNullException.ThrowIfNull(stage);

        if (string.IsNullOrWhiteSpace(slotKey))
        {
            throw new ApplicationFailureException(
                "Slot key must be non-empty.",
                ApplicationErrorCodes.InvalidStructureIntent);
        }

        var slot = stage.FindSlot(slotKey)
                   ?? throw new DomainException(
                       $"Slot '{slotKey.Trim()}' was not found.",
                       StageErrorCodes.SlotNotFound);

        if (slot.EntryId != expectedEntryId)
        {
            throw new ApplicationFailureException(
                "The expected slot occupant is no longer current.",
                ApplicationErrorCodes.SlotAssignmentStale);
        }

        if (slot.EntryId is null)
        {
            return false;
        }

        StructureOperationalExitEditGuard.Ensure(stage, stageMatches, "Slot assignment");
        StructureAppliedDrawSlotPlacementGate.EnsureClearAllowed(stage, slotKey);

        var before = slot.EntryId;
        stage.ClearSlotAssignment(
            slotKey,
            MaterializedParticipantSnapshots.FromMatches(stageMatches ?? []));
        return stage.FindSlot(slot.SlotKey)!.EntryId != before;
    }
}

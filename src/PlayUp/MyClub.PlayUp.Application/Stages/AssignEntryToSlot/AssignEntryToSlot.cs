// -----------------------------------------------------------------------
// <copyright file="AssignEntryToSlot.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: assign an entry to a Cup slot via <see cref="DirectAssignment"/>.
/// </summary>
public static class AssignEntryToSlot
{
    /// <summary>
    /// Pins <paramref name="entryId"/> on <paramref name="slotKey"/> when the resolved occupant
    /// is still <paramref name="expectedEntryId"/>. Returns <see langword="false"/> when the
    /// requested entry is already that occupant (no persistence).
    /// </summary>
    /// <remarks>
    /// A stale expectation is refused before placement guards, including an Applied draw gate.
    /// </remarks>
    public static bool Execute(
        Stage stage,
        string slotKey,
        EntryId entryId,
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

        if (slot.EntryId == entryId)
        {
            return false;
        }

        StructureOperationalExitEditGuard.Ensure(stage, stageMatches, "Slot assignment");
        StructureAppliedDrawSlotPlacementGate.EnsureAssignAllowed(stage, slotKey, entryId);

        stage.AssignEntryToSlot(
            slotKey,
            entryId,
            MaterializedParticipantSnapshots.FromMatches(stageMatches ?? []));
        return true;
    }
}

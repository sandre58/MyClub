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
    /// Pins <paramref name="entryId"/> on <paramref name="slotKey"/> (configuration + sync resolution).
    /// Domain enforces population membership, feed conflicts, and Cup opponent coherence.
    /// </summary>
    public static void Execute(
        Stage stage,
        string slotKey,
        EntryId entryId,
        IReadOnlyList<Match>? stageMatches = null)
    {
        ArgumentNullException.ThrowIfNull(stage);

        StructureOperationalExitEditGuard.Ensure(stage, stageMatches, "Slot assignment");

        if (string.IsNullOrWhiteSpace(slotKey))
        {
            throw new ApplicationFailureException(
                "Slot key must be non-empty.",
                ApplicationErrorCodes.InvalidStructureIntent);
        }

        StructureAppliedDrawSlotPlacementGate.EnsureAssignAllowed(stage, slotKey, entryId);

        stage.AssignEntryToSlot(
            slotKey,
            entryId,
            MaterializedParticipantSnapshots.FromMatches(stageMatches ?? []));
    }
}

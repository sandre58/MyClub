// -----------------------------------------------------------------------
// <copyright file="ClearSlotAssignment.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: clear a Cup <see cref="DirectAssignment"/> (and synced occupant).
/// </summary>
public static class ClearSlotAssignment
{
    /// <summary>
    /// Removes the direct assignment on <paramref name="slotKey"/> when present.
    /// Domain no-ops when no DirectAssignment owns the slot (does not wipe Draw/Qual occupants).
    /// </summary>
    public static void Execute(
        Stage stage,
        string slotKey,
        IReadOnlyList<Match>? stageMatches = null)
    {
        ArgumentNullException.ThrowIfNull(stage);

        StageStructureEditGuard.EnsureEditable(stage);

        if (string.IsNullOrWhiteSpace(slotKey))
        {
            throw new ApplicationFailureException(
                "Slot key must be non-empty.",
                ApplicationErrorCodes.InvalidStructureIntent);
        }

        stage.ClearSlotAssignment(
            slotKey,
            MaterializedParticipantSnapshots.FromMatches(stageMatches ?? []));
    }
}

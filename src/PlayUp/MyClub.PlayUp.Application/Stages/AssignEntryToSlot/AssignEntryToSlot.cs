// -----------------------------------------------------------------------
// <copyright file="AssignEntryToSlot.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: assign an entry to a Cup slot via <see cref="DirectAssignment"/>.
/// </summary>
public static class AssignEntryToSlot
{
    /// <summary>
    /// Pins <paramref name="entryId"/> on <paramref name="slotKey"/> (configuration + sync resolution).
    /// Domain enforces population membership and feed conflicts.
    /// </summary>
    public static void Execute(Stage stage, string slotKey, EntryId entryId)
    {
        ArgumentNullException.ThrowIfNull(stage);

        if (stage.Status is StageStatus.Running or StageStatus.Suspended or StageStatus.Completed)
        {
            throw new ApplicationFailureException(
                $"Slot assignments cannot change while stage status is '{stage.Status}'.",
                ApplicationErrorCodes.StructureNotMutable);
        }

        if (string.IsNullOrWhiteSpace(slotKey))
        {
            throw new ApplicationFailureException(
                "Slot key must be non-empty.",
                ApplicationErrorCodes.InvalidStructureIntent);
        }

        stage.AssignEntryToSlot(slotKey, entryId);
    }
}

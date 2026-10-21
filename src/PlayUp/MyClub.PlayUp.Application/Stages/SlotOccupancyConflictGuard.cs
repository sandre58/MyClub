// -----------------------------------------------------------------------
// <copyright file="SlotOccupancyConflictGuard.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application F8 preflight: refuse silent overwrite of a dynamic slot occupant.
/// </summary>
/// <remarks>
/// Same Entry → allowed (idempotent Domain no-op). Empty slot → allowed.
/// Different Entry → <see cref="ApplicationErrorCodes.SlotOccupancyConflict"/> before mutation.
/// </remarks>
public static class SlotOccupancyConflictGuard
{
    /// <summary>
    /// Ensures applying <paramref name="instruction"/> would not overwrite another occupant.
    /// </summary>
    /// <param name="destination">Destination stage owning the slot.</param>
    /// <param name="instruction">Resolved assignment instruction.</param>
    public static void EnsureCompatible(Stage destination, SlotAssignmentInstruction instruction)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(instruction);

        var slot = destination.FindSlot(instruction.SlotKey)
            ?? throw new ApplicationFailureException(
                $"Destination slot '{instruction.SlotKey}' was not found on stage '{destination.Id}'.",
                ApplicationErrorCodes.DanglingFeedTarget);

        // DirectAssignment conflicts remain Domain SlotFeedConflict — F8 covers dynamic occupants only.
        if (destination.DirectAssignments.Any(assignment =>
                string.Equals(assignment.SlotKey, instruction.SlotKey, StringComparison.Ordinal)))
        {
            return;
        }

        if (slot.EntryId is null || slot.EntryId.Equals(instruction.EntryId))
        {
            return;
        }

        throw new ApplicationFailureException(
            $"Slot '{instruction.SlotKey}' on stage '{destination.Id}' is occupied by '{slot.EntryId}' "
            + $"and cannot be overwritten by '{instruction.EntryId}'.",
            ApplicationErrorCodes.SlotOccupancyConflict);
    }
}

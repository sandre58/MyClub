// -----------------------------------------------------------------------
// <copyright file="AddStageSlot.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: add a positional Slot to a Stage.
/// </summary>
public static class AddStageSlot
{
    /// <summary>
    /// Adds a slot key to the stage.
    /// </summary>
    public static Slot Execute(Stage stage, string slotKey, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(clock);

        if (stage.Status is StageStatus.Running or StageStatus.Suspended or StageStatus.Completed)
        {
            throw new ApplicationFailureException(
                $"Slots cannot be added while stage status is '{stage.Status}'.",
                ApplicationErrorCodes.OrganisationNotMutable);
        }

        if (string.IsNullOrWhiteSpace(slotKey))
        {
            throw new ApplicationFailureException(
                "Slot key must be non-empty.",
                ApplicationErrorCodes.InvalidStructureIntent);
        }

        return stage.AddSlot(slotKey, clock);
    }
}

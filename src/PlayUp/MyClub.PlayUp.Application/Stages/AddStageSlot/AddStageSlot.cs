// -----------------------------------------------------------------------
// <copyright file="AddStageSlot.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Competitions;
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
    public static Slot Execute(Stage stage, Competition competition, string slotKey)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competition);

        StageStructureEditGuard.EnsureEditable(stage, competition);

        return string.IsNullOrWhiteSpace(slotKey)
            ? throw new ApplicationFailureException(
                "Slot key must be non-empty.",
                ApplicationErrorCodes.InvalidStructureIntent)
            : stage.AddSlot(slotKey);
    }
}

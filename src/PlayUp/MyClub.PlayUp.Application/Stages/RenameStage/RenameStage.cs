// -----------------------------------------------------------------------
// <copyright file="RenameStage.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: rename a stage (locale, non-destructive).
/// </summary>
public static class RenameStage
{
    /// <summary>
    /// Renames the stage when mutable.
    /// </summary>
    public static void Execute(Stage stage, string name)
    {
        ArgumentNullException.ThrowIfNull(stage);
        if (stage.Status is StageStatus.Running or StageStatus.Suspended or StageStatus.Completed)
        {
            throw new ApplicationFailureException(
                $"Stage '{stage.Id}' cannot be renamed while status is '{stage.Status}'.",
                ApplicationErrorCodes.StructureNotMutable);
        }

        stage.Rename(new StageName(name));
    }
}

// -----------------------------------------------------------------------
// <copyright file="ReplaceStageSwissSettings.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: set Swiss planned round count K (locale).
/// </summary>
public static class ReplaceStageSwissSettings
{
    /// <summary>
    /// Sets Swiss settings to the given planned round count.
    /// </summary>
    public static void Execute(Stage stage, int roundCount)
    {
        ArgumentNullException.ThrowIfNull(stage);

        if (stage.Status is StageStatus.Running or StageStatus.Suspended or StageStatus.Completed)
        {
            throw new ApplicationFailureException(
                $"Swiss settings cannot be changed while stage status is '{stage.Status}'.",
                ApplicationErrorCodes.StructureNotMutable);
        }

        if (roundCount < 1)
        {
            throw new ApplicationFailureException(
                "Swiss round count must be at least 1.",
                ApplicationErrorCodes.InvalidStructureIntent);
        }

        stage.SetSwissSettings(new SwissSettings(roundCount));
    }
}

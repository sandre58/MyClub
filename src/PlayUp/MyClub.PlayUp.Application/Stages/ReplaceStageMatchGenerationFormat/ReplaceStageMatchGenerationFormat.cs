// -----------------------------------------------------------------------
// <copyright file="ReplaceStageMatchGenerationFormat.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: set Championship / Groups match generation format (locale).
/// </summary>
public static class ReplaceStageMatchGenerationFormat
{
    /// <summary>
    /// Sets the match generation format when the stage is mutable.
    /// </summary>
    public static void Execute(Stage stage, MatchGenerationFormat format)
    {
        ArgumentNullException.ThrowIfNull(stage);

        if (stage.Status is StageStatus.Running or StageStatus.Suspended or StageStatus.Completed)
        {
            throw new ApplicationFailureException(
                $"Match generation format cannot be changed while stage status is '{stage.Status}'.",
                ApplicationErrorCodes.StructureNotMutable);
        }

        if (!Enum.IsDefined(format))
        {
            throw new ApplicationFailureException(
                $"Unknown match generation format '{format}'.",
                ApplicationErrorCodes.InvalidStructureIntent);
        }

        stage.SetMatchGenerationFormat(format);
    }
}

// -----------------------------------------------------------------------
// <copyright file="ReplaceStageSwissSettings.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Competitions;
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
    public static void Execute(Stage stage, Competition competition, int roundCount)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competition);

        StageStructureEditGuard.EnsureEditable(stage, competition);

        if (roundCount < 1)
        {
            throw new ApplicationFailureException(
                "Swiss round count must be at least 1.",
                ApplicationErrorCodes.InvalidStructureIntent);
        }

        stage.SetSwissSettings(new SwissSettings(roundCount));
    }
}

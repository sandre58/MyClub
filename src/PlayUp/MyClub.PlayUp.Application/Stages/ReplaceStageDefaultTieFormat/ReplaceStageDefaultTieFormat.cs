// -----------------------------------------------------------------------
// <copyright file="ReplaceStageDefaultTieFormat.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: replace or clear the stage default TieFormat.
/// </summary>
public static class ReplaceStageDefaultTieFormat
{
    /// <summary>
    /// Replaces the stage default tie format (<see langword="null"/> clears).
    /// </summary>
    public static void Execute(Stage stage, TieFormat? tieFormat, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(clock);

        stage.ReplaceDefaultTieFormat(tieFormat, clock);
    }
}

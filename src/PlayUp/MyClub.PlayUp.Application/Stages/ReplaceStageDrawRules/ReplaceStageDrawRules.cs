// -----------------------------------------------------------------------
// <copyright file="ReplaceStageDrawRules.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: replace or clear DrawRules on a stage.
/// </summary>
public static class ReplaceStageDrawRules
{
    /// <summary>
    /// Replaces stage draw rules (<see langword="null"/> clears).
    /// </summary>
    public static void Execute(Stage stage, DrawRules? drawRules, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(clock);

        stage.ReplaceDrawRules(drawRules, clock);
    }
}

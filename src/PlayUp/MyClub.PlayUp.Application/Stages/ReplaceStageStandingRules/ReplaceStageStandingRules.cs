// -----------------------------------------------------------------------
// <copyright file="ReplaceStageStandingRules.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: replace StandingRules on a classifying stage.
/// Allowed after Start (calculation ≠ structure); Domain rejects non-classifying / Completed.
/// Does not recalculate standings — reads recompute on demand.
/// </summary>
public static class ReplaceStageStandingRules
{
    /// <summary>
    /// Replaces stage standing rules (points + ranking criteria).
    /// </summary>
    /// <param name="stage">Target stage.</param>
    /// <param name="standingRules">Replacement standing rules.</param>
    /// <param name="clock">Clock for domain events.</param>
    public static void Execute(Stage stage, StandingRules standingRules, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(standingRules);
        ArgumentNullException.ThrowIfNull(clock);

        stage.ReplaceStandingRules(standingRules, clock);
    }
}

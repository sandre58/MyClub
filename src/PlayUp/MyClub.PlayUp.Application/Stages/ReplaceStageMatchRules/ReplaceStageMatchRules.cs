// -----------------------------------------------------------------------
// <copyright file="ReplaceStageMatchRules.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: specialize MatchRules on a stage (unbinds changed heritable parts).
/// </summary>
public static class ReplaceStageMatchRules
{
    /// <summary>
    /// Replaces stage match rules.
    /// </summary>
    public static void Execute(Stage stage, MatchRules matchRules, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(matchRules);
        ArgumentNullException.ThrowIfNull(clock);

        stage.ReplaceMatchRules(matchRules, clock);
    }
}

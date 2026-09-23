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
    /// When replacing, existing constraints are preserved if the incoming
    /// <see cref="DrawRules"/> carries none — the write API has no constraints
    /// surface yet, so a pots/seeds-only update must not wipe them.
    /// </summary>
    public static void Execute(Stage stage, DrawRules? drawRules, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(clock);

        if (drawRules is not null)
        {
            var existing = stage.Regulation.DrawRules;
            if (existing is not null)
            {
                // Write API has no constraints / seeds surface — preserve what the editor
                // cannot express so pots/mode-only updates do not wipe them.
                var constraints =
                    drawRules.Constraints.Count == 0 && existing.Constraints.Count > 0
                        ? existing.Constraints
                        : drawRules.Constraints;
                var seeding =
                    drawRules.SeedingRules is null && existing.SeedingRules is not null
                        ? existing.SeedingRules
                        : drawRules.SeedingRules;

                if (!ReferenceEquals(constraints, drawRules.Constraints)
                    || !ReferenceEquals(seeding, drawRules.SeedingRules))
                {
                    drawRules = new DrawRules(
                        drawRules.Mode,
                        seeding,
                        drawRules.PotRules,
                        constraints);
                }
            }
        }

        stage.ReplaceDrawRules(drawRules, clock);
    }
}

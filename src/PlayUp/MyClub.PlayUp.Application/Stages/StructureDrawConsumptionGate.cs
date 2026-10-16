// -----------------------------------------------------------------------
// <copyright file="StructureDrawConsumptionGate.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// C-ENF consumption gates — obsolete draw resolution must not be consumed as valid.
/// </summary>
internal static class StructureDrawConsumptionGate
{
    internal static void EnsureDrawConsumable(Draw draw, Stage stage)
    {
        ArgumentNullException.ThrowIfNull(draw);
        ArgumentNullException.ThrowIfNull(stage);

        if (!DrawConsumptionConformance.IsObsoleteForConsumption(draw, stage))
        {
            return;
        }

        throw new ApplicationFailureException(
            $"Draw '{draw.Id}' is obsolete for consumption on stage '{stage.Id}'.",
            ApplicationErrorCodes.DrawObsoleteForConsumption);
    }

    internal static void EnsureNoObsoleteDrawsOnStage(Stage stage)
    {
        ArgumentNullException.ThrowIfNull(stage);

        foreach (var draw in stage.Draws.Where(draw => draw.Status != DrawStatus.Cancelled))
        {
            EnsureDrawConsumable(draw, stage);
        }
    }
}

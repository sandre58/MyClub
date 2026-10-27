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
/// Obsolete draw resolution must not be consumed as valid.
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

    internal static bool StageHasObsoleteDrawForConsumption(Stage stage)
    {
        ArgumentNullException.ThrowIfNull(stage);

        return stage.Draws.Any(draw => draw.Status != DrawStatus.Cancelled && DrawConsumptionConformance.IsObsoleteForConsumption(draw, stage));
    }

    internal static void EnsureNoObsoleteDrawsOnStage(Stage stage)
    {
        if (!StageHasObsoleteDrawForConsumption(stage))
        {
            return;
        }

        throw new ApplicationFailureException(
            $"Stage '{stage.Id}' has a draw that is obsolete for consumption.",
            ApplicationErrorCodes.DrawObsoleteForConsumption);
    }
}

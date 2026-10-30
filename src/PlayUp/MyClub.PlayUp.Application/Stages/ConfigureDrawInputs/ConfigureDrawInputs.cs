// -----------------------------------------------------------------------
// <copyright file="ConfigureDrawInputs.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: configure inputs on a Draft Draw.
/// </summary>
public static class ConfigureDrawInputs
{
    /// <summary>
    /// Configures draw inputs (resets resolution to NotResolved).
    /// </summary>
    /// <param name="stage">Owning stage.</param>
    /// <param name="drawId">Draw identity.</param>
    /// <param name="inputs">Concrete inputs.</param>
    /// <param name="configureIntent">
    /// <see cref="DrawInputsIntent.Rerun"/> enforces a vacant launch grid.
    /// <see cref="DrawInputsIntent.Partial"/> requires the unplaced pool to match free slots.
    /// </param>
    /// <param name="competitionStages">Required for Rerun and Partial feed classification.</param>
    /// <param name="stageMatches">Matches of stage.</param>
    public static void Execute(
        Stage stage,
        DrawId drawId,
        DrawInputs inputs,
        DrawInputsIntent configureIntent = DrawInputsIntent.Default,
        IReadOnlyList<Stage>? competitionStages = null,
        IReadOnlyList<Match>? stageMatches = null)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(inputs);
        StructureOperationalExitEditGuard.Ensure(stage, stageMatches, "Draw");

        var draw = stage.GetDraw(drawId);
        if (draw.Status != DrawStatus.Draft)
        {
            throw new ApplicationFailureException(
                $"Draw '{drawId}' must be Draft to configure inputs (status is '{draw.Status}').",
                ApplicationErrorCodes.DrawGenerationFailure);
        }

        inputs.EnsureCompatibleWith(draw.Kind);

        if (configureIntent == DrawInputsIntent.Rerun)
        {
            if (competitionStages is null)
            {
                throw new ArgumentNullException(
                    nameof(competitionStages),
                    "Competition stages are required to validate Rerun draw inputs.");
            }

            StageDrawRerunLaunchGuard.Ensure(stage, competitionStages, draw.Kind);
        }
        else if (configureIntent == DrawInputsIntent.Partial)
        {
            EnsurePartialSlotInputs(stage, draw, inputs, competitionStages);
        }

        stage.ConfigureDrawInputs(drawId, inputs, configureIntent);
    }

    private static void EnsurePartialSlotInputs(
        Stage stage,
        Draw draw,
        DrawInputs inputs,
        IReadOnlyList<Stage>? competitionStages)
    {
        if (draw.Kind != DrawResolutionKind.Slot)
        {
            throw new ApplicationFailureException(
                $"Partial draw supports Slot only (kind is '{draw.Kind}').",
                ApplicationErrorCodes.DrawKindNotSupported);
        }

        if (competitionStages is null)
        {
            throw new ArgumentNullException(
                nameof(competitionStages),
                "Competition stages are required to validate partial draw inputs.");
        }

        if (inputs.FixedSlots.Count > 0 || inputs.FixedGroups.Count > 0)
        {
            throw new ApplicationFailureException(
                "Partial draw inputs must not fix existing occupants.",
                ApplicationErrorCodes.DrawGenerationFailure);
        }

        var scope = SlotPartialDrawPlanner.Compose(stage, competitionStages);
        SlotPartialDrawPlanner.EnsureDrawable(scope);
        if (!SlotPartialDrawPlanner.EntriesMatch(inputs.Entries, scope.UnplacedEntries))
        {
            throw new ApplicationFailureException(
                "Partial draw inputs must be exactly the unplaced teams.",
                ApplicationErrorCodes.DrawGenerationFailure);
        }
    }
}

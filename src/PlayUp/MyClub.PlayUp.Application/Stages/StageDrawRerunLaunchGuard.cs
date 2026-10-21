// -----------------------------------------------------------------------
// <copyright file="StageDrawRerunLaunchGuard.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Refuses <see cref="DrawInputsIntent.Rerun"/> when the launch grid is not vacant (SPA parity).
/// </summary>
public static class StageDrawRerunLaunchGuard
{
    /// <summary>
    /// Ensures Rerun input configuration is allowed on the current stage snapshot.
    /// </summary>
    /// <param name="stage">Stage that owns the draw (tracked snapshot).</param>
    /// <param name="competitionStages">All competition stages for feed resolution.</param>
    /// <param name="drawKind">Draw resolution kind being configured.</param>
    /// <remarks>
    /// Precedence matches SPA <c>resolveDrawCreateGate</c>: direct assignment before occupied places.
    /// Must run before mutating draw inputs.
    /// </remarks>
    public static void Ensure(
        Stage stage,
        IReadOnlyList<Stage> competitionStages,
        DrawResolutionKind drawKind)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competitionStages);

        if (stage.DirectAssignments.Count > 0)
        {
            throw new ApplicationFailureException(
                "Draw rerun inputs require a stage without direct slot assignments.",
                ApplicationErrorCodes.DrawLaunchDirectAssignmentPresent);
        }

        var facts = StageDrawBracketLaunchClassifier.ClassifyForDrawKind(
            stage,
            competitionStages,
            drawKind);

        if (facts.BlockingPlaceCount > 0)
        {
            throw new ApplicationFailureException(
                "Draw rerun inputs require vacant bracket places (no occupants or structural reservations).",
                ApplicationErrorCodes.DrawLaunchGridNotVacant);
        }
    }
}

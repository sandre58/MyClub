// -----------------------------------------------------------------------
// <copyright file="StructurePrepareAttributionGate.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Prepare gate: structural PlacementAward inconsistency on non-classifying (Cup/KO) phases only.
/// </summary>
/// <remarks>
/// Policy uses <see cref="CompetitionFlowAnalyzer.AuthoredAwardUnreachable"/> only — not other RESULT codes.
/// Lifecycle read exposes <see cref="StructureStageLifecycleAssembler.ChecklistAttribution"/>; Analyzer keeps RESULT diagnostics for the fiche.
/// </remarks>
internal static class StructurePrepareAttributionGate
{
    internal static bool BlocksPrepare(StructureStageLifecycleInput input) =>
        BlocksPrepare(input.Stage, input.StructureIssues);

    internal static bool BlocksPrepare(Stage stage, IReadOnlyList<string>? structureIssues) =>
        StageClassification.IsNonClassifyingPhase(stage)
        && structureIssues?.Contains(CompetitionFlowAnalyzer.AuthoredAwardUnreachable, StringComparer.Ordinal) == true;

    internal static void Ensure(Stage stage)
    {
        if (!StageClassification.IsNonClassifyingPhase(stage))
        {
            return;
        }

        var hasUnreachable = CompetitionFlowAnalyzer.AnalyzeResult([stage])
            .Any(diagnostic =>
                diagnostic.StageId == stage.Id.Value
                && diagnostic.Code == CompetitionFlowAnalyzer.AuthoredAwardUnreachable);

        if (!hasUnreachable)
        {
            return;
        }

        throw new ApplicationFailureException(
            $"Stage '{stage.Id}' prepare refused: placement award paths are unreachable.",
            ApplicationErrorCodes.AttributionUnreachable);
    }
}

// -----------------------------------------------------------------------
// <copyright file="StructurePreparePlacesGate.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Prepare gate: composition (Champ / Groups root) or schematic cup/group places before Draft → Ready.
/// </summary>
internal static class StructurePreparePlacesGate
{
    internal static bool BlocksPrepare(StructureStageLifecycleInput input) =>
        HasCompositionGap(input) || HasSchematicPlaceGap(input);

    internal static void Ensure(Competition competition, Stage stage, IReadOnlyList<Stage> competitionStages)
    {
        if (HasCompositionGap(competition, stage))
        {
            throw new ApplicationFailureException(
                $"Stage '{stage.Id}' prepare refused: places not filled.",
                ApplicationErrorCodes.SlotFeedsInvalid);
        }

        var issues = new List<string>();
        var hasDrawRules = stage.Regulation.DrawRules is not null;
        var badge = StructureViewAssembler.GetDrawExecutionBadge(stage, hasDrawRules);
        var placesN = StructureViewAssembler.GetCompositionCapacity(competition, stage);
        StructurePlaceOccupancyIssues.AppendSchematicPlaceOccupancyIssue(
            issues,
            competition,
            stage,
            competitionStages,
            hasDrawRules,
            badge,
            placesN);

        if (issues.Contains(CompetitionFlowAnalyzer.SlotFeedMissing, StringComparer.Ordinal))
        {
            throw new ApplicationFailureException(
                $"Stage '{stage.Id}' prepare refused: {CompetitionFlowAnalyzer.SlotFeedMissing}.",
                ApplicationErrorCodes.SlotFeedsInvalid);
        }
    }

    private static bool HasCompositionGap(StructureStageLifecycleInput input) =>
        HasCompositionGap(input.Competition, input.Stage);

    private static bool HasCompositionGap(Competition competition, Stage stage)
    {
        // Cup entry places are slot-fed (qualification / progression / draw), not CompositionEntries.
        if (StructureSkeleton.InferFormat(stage) is StructureFormatKind.Cup)
        {
            return false;
        }

        var capacity = StructureViewAssembler.GetCompositionCapacity(competition, stage);
        return capacity is > 0 && stage.CompositionEntries.Count < capacity.Value;
    }

    private static bool HasSchematicPlaceGap(StructureStageLifecycleInput input) =>
        input.StructureIssues?.Contains(CompetitionFlowAnalyzer.SlotFeedMissing, StringComparer.Ordinal) == true;
}

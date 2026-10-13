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
/// Prepare gate: Groups composition pool; Cup/Groups/Champ/Swiss schematic places (Champ/Swiss allow unresolved feeders).
/// </summary>
internal static class StructurePreparePlacesGate
{
    internal static bool BlocksPrepare(StructureStageLifecycleInput input) =>
        HasCompositionGap(input) || HasSchematicPlaceGap(input);

    internal static void Ensure(Competition competition, Stage stage, IReadOnlyList<Stage> competitionStages)
    {
        if (HasCompositionGap(competition, stage, competitionStages))
        {
            throw new ApplicationFailureException(
                $"Stage '{stage.Id}' prepare refused: places not filled.",
                ApplicationErrorCodes.SlotFeedsInvalid);
        }

        var issues = new List<string>();
        var hasDrawRules = stage.Regulation.DrawRules is not null;
        var badge = StructureViewAssembler.GetDrawExecutionBadge(stage, hasDrawRules);
        var placesN = StructureViewAssembler.GetCompositionCapacity(competition, stage, competitionStages);
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
        HasCompositionGap(input.Competition, input.Stage, input.CompetitionStages);

    private static bool HasCompositionGap(
        Competition competition,
        Stage stage,
        IReadOnlyList<Stage> competitionStages)
    {
        var format = StructureSkeleton.InferFormat(stage);

        // Cup: slot-fed. Championship/Swiss: structural occupancy (Prepare); composition gates Materialize only.
        if (format is StructureFormatKind.Cup
            or StructureFormatKind.Championship
            or StructureFormatKind.Swiss)
        {
            return false;
        }

        var capacity = StructureViewAssembler.GetCompositionCapacity(competition, stage, competitionStages);
        return capacity is > 0 && stage.CompositionEntries.Count < capacity.Value;
    }

    private static bool HasSchematicPlaceGap(StructureStageLifecycleInput input) =>
        input.StructureIssues?.Contains(CompetitionFlowAnalyzer.SlotFeedMissing, StringComparer.Ordinal) == true;
}

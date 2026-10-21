// -----------------------------------------------------------------------
// <copyright file="StructurePlaceOccupancyIssues.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Projects schematic place occupancy gaps onto <see cref="StructureViewAssembler.IssueSlotFeedMissing"/>.
/// </summary>
internal static class StructurePlaceOccupancyIssues
{
    /// <summary>
    /// Empty places are expected until draw results are applied (matches Host <c>isSlotFeedAwaitingDraw</c>).
    /// </summary>
    internal static bool IsSlotFeedAwaitingDraw(
        bool hasDrawRules,
        StructureDrawExecutionBadge? badge) =>
        hasDrawRules && badge is not StructureDrawExecutionBadge.ToApply
            and not StructureDrawExecutionBadge.Applied;

    /// <summary>
    /// Incomplete group pools are expected until a published draw is applied (Groups + draw rules).
    /// </summary>
    internal static bool DeferGroupsNotReadyForDraw(
        bool hasDrawRules,
        StructureDrawExecutionBadge? drawExecutionBadge) =>
        hasDrawRules && drawExecutionBadge is not StructureDrawExecutionBadge.Applied;

    internal static void AppendSchematicPlaceOccupancyIssue(
        IList<string> issues,
        Competition competition,
        Stage stage,
        IReadOnlyList<Stage> competitionStages,
        bool hasDrawRules,
        StructureDrawExecutionBadge? drawExecutionBadge,
        int? placesN)
    {
        ArgumentNullException.ThrowIfNull(issues);
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competitionStages);

        if (IsSlotFeedAwaitingDraw(hasDrawRules, drawExecutionBadge))
        {
            return;
        }

        if (issues.Contains(StructureViewAssembler.IssueSlotFeedMissing, StringComparer.Ordinal))
        {
            return;
        }

        if (placesN is null or <= 0)
        {
            return;
        }

        var format = StructureSkeleton.InferFormat(stage);
        if (format is not (
            StructureFormatKind.Groups
            or StructureFormatKind.Cup
            or StructureFormatKind.Championship
            or StructureFormatKind.Swiss))
        {
            return;
        }

        var schematic = StageSchematicAssembler.Assemble(stage, competition, competitionStages);
        var occupied = CountOccupiedPlaces(schematic, format.Value);
        if (occupied >= placesN.Value)
        {
            return;
        }

        issues.Add(StructureViewAssembler.IssueSlotFeedMissing);
    }

    internal static int CountOccupiedPlaces(StageSchematicDto schematic, StructureFormatKind format) =>
        format switch
        {
            StructureFormatKind.Cup => CountCupEntryRoundOccupied(schematic.Cases),
            StructureFormatKind.Groups => schematic.Cases.Count(c =>
                c.FormPosition.Kind == StageSchematicAssembler.FormKindGroupPlace
                && CasePlaceIsOccupied(c)),
            StructureFormatKind.Championship or StructureFormatKind.Swiss => schematic.Cases.Count(c =>
                c.FormPosition.Kind == StageSchematicAssembler.FormKindRosterPlace
                && CasePlaceIsOccupied(c)),
            _ => 0
        };

    private static int CountCupEntryRoundOccupied(IReadOnlyList<SchematicCaseDto> cases)
    {
        var cupCases = cases
            .Where(c => c.FormPosition.Kind == StageSchematicAssembler.FormKindCupSlot)
            .ToList();
        if (cupCases.Count == 0)
        {
            return 0;
        }

        var entryRound = cupCases.Min(c => c.FormPosition.RoundOrder ?? 0);
        return cupCases.Count(c =>
            (c.FormPosition.RoundOrder ?? 0) == entryRound && CasePlaceIsOccupied(c));
    }

    internal static bool IsSchematicCaseOccupied(SchematicCaseDto place) =>
        (place.Entry?.EntryId is { } entryId && entryId != Guid.Empty)
        || (place.Assignment?.EntryId is { } assignmentEntryId && assignmentEntryId != Guid.Empty)
        || place.FeedOrigin is not null;

    private static bool CasePlaceIsOccupied(SchematicCaseDto place) => IsSchematicCaseOccupied(place);
}

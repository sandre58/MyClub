// -----------------------------------------------------------------------
// <copyright file="StageDrawDisplay.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Read-time cup draw grid and hero metrics. Origin is derived for display; it is not a stored provenance.
/// </summary>
internal static class StageDrawDisplay
{
    /// <summary>
    /// Current partial-draw counts. Null when the stage has no slots.
    /// </summary>
    public static StageDrawDisplayMetricsDto? MetricsForStage(
        Stage stage,
        IReadOnlyList<Stage> competitionStages)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competitionStages);
        return stage.Slots.Count == 0 ? null : Metrics(stage, competitionStages);
    }

    /// <summary>
    /// Projects metrics and cells for one draw. Group draws and non-slot kinds carry neither.
    /// Published and Cancelled cells use only that draw's resolution.
    /// </summary>
    public static (StageDrawDisplayMetricsDto? Metrics, IReadOnlyList<StageDrawSlotDisplayCellDto>? Cells) Project(
        Draw draw,
        Stage stage,
        IReadOnlyList<Stage> competitionStages,
        IReadOnlyDictionary<EntryId, CompetitionEntry> entries,
        StageDrawDisplayMetricsDto? stageMetrics)
    {
        ArgumentNullException.ThrowIfNull(draw);
        ArgumentNullException.ThrowIfNull(stage);

        if (draw.Kind != DrawResolutionKind.Slot || stage.Slots.Count == 0)
        {
            return (null, null);
        }

        if (draw.Status != DrawStatus.Draft)
        {
            return (null, HistoricalCells(draw, stage, entries));
        }

        return (stageMetrics, DraftCells(draw, stage, competitionStages, entries));
    }

    private static StageDrawDisplayMetricsDto Metrics(
        Stage stage,
        IReadOnlyList<Stage> competitionStages)
    {
        var snapshot = SlotFeedSnapshotAssembler.Assemble(stage, competitionStages);
        var scope = SlotPartialDraw.Compose(stage, snapshot);
        var alreadyPlaced = SlotPartialDraw.Classify(stage, snapshot)
            .Count(fact => fact.Occupancy == SlotDrawPlaceOccupancy.AlreadyPlaced);

        return new StageDrawDisplayMetricsDto(
            scope.UnplacedEntries.Count,
            scope.VacantSlotKeys.Count,
            scope.VacantSlotKeys,
            alreadyPlaced,
            stage.CompositionEntries.Count,
            stage.Slots.Count);
    }

    private static StageDrawSlotDisplayCellDto[] DraftCells(
        Draw draw,
        Stage stage,
        IReadOnlyList<Stage> competitionStages,
        IReadOnlyDictionary<EntryId, CompetitionEntry> entries)
    {
        var snapshot = SlotFeedSnapshotAssembler.Assemble(stage, competitionStages);
        var facts = SlotPartialDraw.Classify(stage, snapshot);
        var resolution = ResolutionBySlot(draw);
        var cells = new StageDrawSlotDisplayCellDto[facts.Count];
        for (var index = 0; index < facts.Count; index++)
        {
            var fact = facts[index];
            if (resolution.TryGetValue(fact.SlotKey, out var placement))
            {
                cells[index] = FromPlacement(
                    placement,
                    SlotDrawCellState.Drawn,
                    DrawPlacementOrigin.Draw,
                    entries);
                continue;
            }

            cells[index] = fact.Occupancy switch
            {
                SlotDrawPlaceOccupancy.Free => WithoutTeam(fact.SlotKey, SlotDrawCellState.Free, origin: null),
                SlotDrawPlaceOccupancy.StructuralFeed => FromOccupant(
                    fact,
                    SlotDrawCellState.Reserved,
                    DrawPlacementOrigin.Feed,
                    entries),
                SlotDrawPlaceOccupancy.Withdrawn => FromOccupant(
                    fact,
                    SlotDrawCellState.Withdrawn,
                    DrawPlacementOrigin.Manual,
                    entries),
                SlotDrawPlaceOccupancy.AlreadyPlaced => FromOccupant(
                    fact,
                    SlotDrawCellState.AlreadyPlaced,
                    DrawPlacementOrigin.Manual,
                    entries),
                _ => throw new InvalidOperationException(
                    $"Slot occupancy '{fact.Occupancy}' is not a draft cell."),
            };
        }

        return cells;
    }

    /// <summary>
    /// Resolution placements only. Structural slots absent from the resolution are outside this draw.
    /// Does not read slot occupants or the current feed snapshot.
    /// </summary>
    private static StageDrawSlotDisplayCellDto[] HistoricalCells(
        Draw draw,
        Stage stage,
        IReadOnlyDictionary<EntryId, CompetitionEntry> entries)
    {
        var resolution = ResolutionBySlot(draw);
        var cells = new StageDrawSlotDisplayCellDto[stage.Slots.Count];
        for (var index = 0; index < stage.Slots.Count; index++)
        {
            var slotKey = stage.Slots[index].SlotKey;
            cells[index] = resolution.TryGetValue(slotKey, out var placement)
                ? FromPlacement(placement, SlotDrawCellState.Drawn, DrawPlacementOrigin.Draw, entries)
                : WithoutTeam(slotKey, SlotDrawCellState.OutsideDraw, origin: null);
        }

        return cells;
    }

    private static Dictionary<string, SlotDrawPlacement> ResolutionBySlot(Draw draw)
    {
        if (draw.Resolution.State != DrawResolutionState.Resolved)
        {
            return [];
        }

        return draw.Resolution.SlotResults.ToDictionary(
            placement => placement.SlotKey,
            StringComparer.Ordinal);
    }

    private static StageDrawSlotDisplayCellDto FromPlacement(
        SlotDrawPlacement placement,
        SlotDrawCellState state,
        DrawPlacementOrigin origin,
        IReadOnlyDictionary<EntryId, CompetitionEntry> entries)
    {
        var side = EntryDisplayNames.ToSide(entries, placement.EntryId);
        return new StageDrawSlotDisplayCellDto(
            placement.SlotKey,
            state,
            side.EntryId,
            side.DisplayName,
            side.ShortName,
            side.LogoMediaId,
            side.PrimaryColor,
            origin);
    }

    private static StageDrawSlotDisplayCellDto FromOccupant(
        SlotDrawPlaceFact fact,
        SlotDrawCellState state,
        DrawPlacementOrigin origin,
        IReadOnlyDictionary<EntryId, CompetitionEntry> entries)
    {
        if (fact.EntryId is not { } entryId)
        {
            return WithoutTeam(fact.SlotKey, state, origin);
        }

        var side = EntryDisplayNames.ToSide(entries, entryId);
        return new StageDrawSlotDisplayCellDto(
            fact.SlotKey,
            state,
            side.EntryId,
            side.DisplayName,
            side.ShortName,
            side.LogoMediaId,
            side.PrimaryColor,
            origin);
    }

    private static StageDrawSlotDisplayCellDto WithoutTeam(
        string slotKey,
        SlotDrawCellState state,
        DrawPlacementOrigin? origin) =>
        new(slotKey, state, null, null, null, null, null, origin);
}

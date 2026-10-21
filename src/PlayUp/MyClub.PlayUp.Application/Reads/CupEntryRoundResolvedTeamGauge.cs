// -----------------------------------------------------------------------
// <copyright file="CupEntryRoundResolvedTeamGauge.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Cup form — entry-round slots with a resolved <see cref="Slot.EntryId"/> (not placement-only feeds).
/// </summary>
internal static class CupEntryRoundResolvedTeamGauge
{
    internal static (int Resolved, int Capacity)? TryMeasure(
        StructureMatchProductionPresentationAssembler.Input input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.FormatKind != StructureFormatKind.Cup
            || input.CompositionCapacity is not { } capacity
            || capacity <= 0)
        {
            return null;
        }

        var resolved = Math.Min(ResolveResolvedTeamCount(input), capacity);
        return (resolved, capacity);
    }

    internal static int CountResolvedEntryRoundTeams(
        Competition competition,
        Stage stage,
        IReadOnlyList<Stage> competitionStages,
        IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>>? matchSummariesByStage = null)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competitionStages);

        IReadOnlyList<MatchSummaryRow> destinationRows = [];
        if (matchSummariesByStage?.TryGetValue(stage.Id, out var rows) == true)
        {
            destinationRows = rows;
        }

        var schematic = StageSchematicAssembler.Assemble(
            stage,
            competition,
            competitionStages,
            destinationRows,
            matchSummariesByStage);
        var cupSlots = schematic.Cases
            .Where(place => place.FormPosition.Kind == StageSchematicAssembler.FormKindCupSlot)
            .ToList();
        if (cupSlots.Count == 0)
        {
            return stage.Slots.Count(slot => slot.EntryId is not null);
        }

        var entryRound = cupSlots.Min(place => place.FormPosition.RoundOrder ?? 0);
        return cupSlots.Count(place =>
            (place.FormPosition.RoundOrder ?? 0) == entryRound
            && IsCupSlotTeamResolved(stage, place));
    }

    private static bool IsCupSlotTeamResolved(Stage stage, SchematicCaseDto place)
    {
        if (place.Entry?.EntryId is { } entryId && entryId != Guid.Empty)
        {
            return true;
        }

        if (place.Assignment?.EntryId is { } assignmentId && assignmentId != Guid.Empty)
        {
            return true;
        }

        var slotKey = place.FormPosition.SlotKey;
        return (!string.IsNullOrEmpty(slotKey) && stage.FindSlot(slotKey)?.EntryId is not null) || (place.FeedOrigin is { Kind: FeedKind.Direct, ConfiguredEntryId: { } directId }
            && directId != Guid.Empty);
    }

    private static int ResolveResolvedTeamCount(StructureMatchProductionPresentationAssembler.Input input)
    {
        var fromSlots = CountResolvedEntryRoundTeams(
            input.Competition,
            input.Stage,
            input.CompetitionStages,
            input.MatchSummariesByStage);

        if (input.InboundComposition is not { ConfiguredVolume: > 0 } inbound)
        {
            return fromSlots;
        }

        var capacity = input.CompositionCapacity ?? int.MaxValue;
        return Math.Min(capacity, Math.Max(fromSlots, inbound.ResolvedCount));
    }
}

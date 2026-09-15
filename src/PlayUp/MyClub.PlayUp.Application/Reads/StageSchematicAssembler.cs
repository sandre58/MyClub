// -----------------------------------------------------------------------
// <copyright file="StageSchematicAssembler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Assembles <see cref="StageSchematicDto"/> — form units + placed entries (no Composition k projection).
/// </summary>
public static class StageSchematicAssembler
{
    public const string FormKindCupSlot = "CupSlot";
    public const string FormKindGroupPlace = "GroupPlace";
    public const string FormKindRosterPlace = "RosterPlace";

    /// <summary>
    /// Maps stage form + placement into the schematic read model.
    /// </summary>
    /// <param name="stage">Target stage (full structure).</param>
    /// <param name="competition">Owning competition (entry presentation).</param>
    /// <param name="competitionStages">All competition stages (inbound WhoFeeds).</param>
    /// <param name="matchRows">Stage match rows (pairing-draw cup sides); empty when unavailable.</param>
    /// <returns>The assembled schematic.</returns>
    public static StageSchematicDto Assemble(
        Stage stage,
        Competition competition,
        IReadOnlyList<Stage> competitionStages,
        IReadOnlyList<MatchSummaryRow>? matchRows = null)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(competitionStages);

        var format = InferFormat(stage);
        var entries = EntryDisplayNames.ToEntries(competition);

        return format switch
        {
            StructureFormatKind.Cup => AssembleCup(stage, competitionStages, entries, matchRows ?? []),
            StructureFormatKind.Groups => AssembleGroups(stage, entries),
            StructureFormatKind.Championship or StructureFormatKind.Swiss =>
                AssembleRosterCapacity(stage, competition, format.Value, entries),
            _ => Empty(stage, format),
        };
    }

    private static StageSchematicDto Empty(Stage stage, StructureFormatKind? format) =>
        new(
            stage.Id.Value,
            stage.CompetitionId.Value,
            stage.Name.Value,
            stage.Status,
            format,
            [],
            [],
            SwissRoundCount: stage.SwissSettings?.RoundCount,
            CupRoundCount: format == StructureFormatKind.Cup ? stage.Rounds.Count : null);

    private static StageSchematicDto AssembleCup(
        Stage stage,
        IReadOnlyList<Stage> competitionStages,
        IReadOnlyDictionary<EntryId, CompetitionEntry> entries,
        IReadOnlyList<MatchSummaryRow> matchRows)
    {
        var feedBySlot = ResolveFeedsTolerant(stage, competitionStages);

        var slotCases = stage.Slots
            .Select(slot =>
            {
                var feed = feedBySlot.GetValueOrDefault(slot.SlotKey);
                return new SchematicCaseDto(
                    new SchematicFormPositionDto(FormKindCupSlot, SlotKey: slot.SlotKey),
                    MapFeedOrigin(feed, slot.SlotKey, competitionStages),
                    MapEntry(slot.EntryId, entries),
                    MapAssignment(slot.EntryId, entries));
            })
            .ToArray();

        var rowsByMatch = matchRows.ToDictionary(row => row.Id);
        var connections = new List<SchematicConnectionDto>();
        var pairingCases = new List<SchematicCaseDto>();
        for (var roundOrder = 0; roundOrder < stage.Rounds.Count; roundOrder++)
        {
            var round = stage.Rounds[roundOrder];
            var fixtures = OrderedFixtures(round);

            // Stable Match #n: EF collection order is not deterministic across loads.
            for (var fixtureIndex = 0; fixtureIndex < fixtures.Count; fixtureIndex++)
            {
                var fixture = fixtures[fixtureIndex];
                if (fixture.SlotAKey is not null && fixture.SlotBKey is not null)
                {
                    connections.Add(
                        new SchematicConnectionDto(
                            fixture.Id.Value,
                            roundOrder,
                            fixture.SlotAKey,
                            fixture.SlotBKey,
                            fixtureIndex + 1));
                    continue;
                }

                // Pairing-draw fixture: no slot binding; the placed sides live on the real match.
                var row = FirstLegRow(fixture, rowsByMatch);
                if (row is null)
                {
                    continue;
                }

                connections.Add(
                    new SchematicConnectionDto(
                        fixture.Id.Value,
                        roundOrder,
                        SlotAKey: null,
                        SlotBKey: null,
                        fixtureIndex + 1));
                if (roundOrder == 0)
                {
                    pairingCases.Add(PairingCase(fixture.Id, "A", row.HomeEntryId, entries));
                    pairingCases.Add(PairingCase(fixture.Id, "B", row.AwayEntryId, entries));
                }
            }
        }

        // A published pairing draw fills the bracket without binding slots: the materialized
        // pairs are the truthful occupation; keeping the unbound empty slots would double capacity.
        var cases = pairingCases.Count > 0 && stage.Slots.All(slot => slot.EntryId is null)
            ? pairingCases
            : (IReadOnlyList<SchematicCaseDto>)slotCases;

        return new StageSchematicDto(
            stage.Id.Value,
            stage.CompetitionId.Value,
            stage.Name.Value,
            stage.Status,
            StructureFormatKind.Cup,
            cases,
            connections,
            CupRoundCount: stage.Rounds.Count);
    }

    /// <summary>
    /// Read-model resolve: an invalid feed graph (e.g. dangling qualification target)
    /// degrades to "no feed info" instead of failing the schematic.
    /// </summary>
    private static Dictionary<string, SlotFeedResolution> ResolveFeedsTolerant(
        Stage stage,
        IReadOnlyList<Stage> competitionStages)
    {
        try
        {
            var snapshot = SlotFeedSnapshotAssembler.Assemble(stage, competitionStages);
            return SlotFeedResolver.ResolveAll(snapshot)
                .ToDictionary(resolution => resolution.SlotKey, StringComparer.Ordinal);
        }
        catch (ApplicationFailureException)
        {
            return new Dictionary<string, SlotFeedResolution>(StringComparer.Ordinal);
        }
        catch (DomainException)
        {
            return new Dictionary<string, SlotFeedResolution>(StringComparer.Ordinal);
        }
    }

    private static SchematicCaseDto PairingCase(
        FixtureId fixtureId,
        string side,
        EntryId entryId,
        IReadOnlyDictionary<EntryId, CompetitionEntry> entries) =>
        new(
            new SchematicFormPositionDto(FormKindCupSlot, FixtureId: fixtureId.Value, Side: side),
            FeedOrigin: null,
            MapEntry(entryId, entries),
            MapAssignment(entryId, entries));

    private static MatchSummaryRow? FirstLegRow(
        Fixture fixture,
        IReadOnlyDictionary<MatchId, MatchSummaryRow> rowsByMatch)
    {
        var attachment = fixture.Attachments.OrderBy(a => a.LegIndex).FirstOrDefault();
        return attachment is null ? null : rowsByMatch.GetValueOrDefault(attachment.MatchId);
    }

    private static StageSchematicDto AssembleGroups(
        Stage stage,
        IReadOnlyDictionary<EntryId, CompetitionEntry> entries)
    {
        var perGroup = stage.PlacesPerGroup
            ?? stage.Regulation.DrawRules?.PotRules?.NumberOfPots;
        if (stage.Groups.Count == 0 || perGroup is null or < 1)
        {
            return Empty(stage, StructureFormatKind.Groups);
        }

        var cases = new List<SchematicCaseDto>(stage.Groups.Count * perGroup.Value);
        foreach (var group in stage.Groups)
        {
            for (var index = 1; index <= perGroup.Value; index++)
            {
                EntryId? placed = index <= group.EntryIds.Count
                    ? group.EntryIds[index - 1]
                    : null;
                cases.Add(
                    new SchematicCaseDto(
                        new SchematicFormPositionDto(
                            FormKindGroupPlace,
                            GroupId: group.Id.Value,
                            GroupName: group.Name,
                            Index: index),
                        FeedOrigin: null,
                        MapEntry(placed, entries),
                        MapAssignment(placed, entries)));
            }
        }

        return new StageSchematicDto(
            stage.Id.Value,
            stage.CompetitionId.Value,
            stage.Name.Value,
            stage.Status,
            StructureFormatKind.Groups,
            cases,
            []);
    }

    /// <summary>
    /// Championship / Swiss: RosterPlace 1..N with Composition entries placed in order.
    /// These formats have no separate placement mechanism — the constituted set is the roster.
    /// Surplus k beyond N stays in the Entrées rail only.
    /// </summary>
    private static StageSchematicDto AssembleRosterCapacity(
        Stage stage,
        Competition competition,
        StructureFormatKind format,
        IReadOnlyDictionary<EntryId, CompetitionEntry> entries)
    {
        var places = ResolvePlaces(competition, stage, format);
        if (places is null or < 1)
        {
            return Empty(stage, format);
        }

        var placed = stage.CompositionEntries
            .Select(compositionEntry => compositionEntry.EntryId)
            .ToArray();

        var cases = Enumerable
            .Range(1, places.Value)
            .Select(index =>
            {
                EntryId? entryId = index <= placed.Length ? placed[index - 1] : null;
                return new SchematicCaseDto(
                    new SchematicFormPositionDto(FormKindRosterPlace, Index: index),
                    FeedOrigin: null,
                    MapEntry(entryId, entries),
                    MapAssignment(entryId, entries));
            })
            .ToArray();

        return new StageSchematicDto(
            stage.Id.Value,
            stage.CompetitionId.Value,
            stage.Name.Value,
            stage.Status,
            format,
            cases,
            [],
            SwissRoundCount: stage.SwissSettings?.RoundCount);
    }

    /// <summary>
    /// Stable 1-based Match #n within the fixture's round (ordered by FixtureId).
    /// </summary>
    private static int? FindFixtureNumber(
        StageId sourceStageId,
        FixtureId fixtureId,
        IReadOnlyList<Stage> competitionStages)
    {
        var source = competitionStages.FirstOrDefault(s => s.Id.Equals(sourceStageId));
        if (source is null)
        {
            return null;
        }

        foreach (var round in source.Rounds)
        {
            var fixtures = OrderedFixtures(round);
            for (var index = 0; index < fixtures.Count; index++)
            {
                if (fixtures[index].Id.Equals(fixtureId))
                {
                    return index + 1;
                }
            }
        }

        return null;
    }

    private static IReadOnlyList<Fixture> OrderedFixtures(Round round) =>
        [.. round.Fixtures.OrderBy(fixture => fixture.Id.Value)];

    private static SchematicFeedOriginDto? MapFeedOrigin(
        SlotFeedResolution? resolution,
        string slotKey,
        IReadOnlyList<Stage> competitionStages)
    {
        if (resolution is null ||
            resolution.Status != FeedResolutionStatus.Unique ||
            resolution.Source is null)
        {
            return null;
        }

        var source = resolution.Source;
        return source.Kind switch
        {
            FeedKind.Direct => new SchematicFeedOriginDto(
                FeedKind.Direct,
                ConfiguredEntryId: source.Direct!.ConfiguredEntryId.Value,
                SlotKey: slotKey),
            FeedKind.Draw => new SchematicFeedOriginDto(
                FeedKind.Draw,
                DrawId: source.Draw!.DrawId.Value,
                SlotKey: slotKey),
            FeedKind.Progression => new SchematicFeedOriginDto(
                FeedKind.Progression,
                SourceStageId: source.Progression!.SourceStageId.Value,
                SourceFixtureId: source.Progression.SourceFixtureId.Value,
                SourceFixtureNumber: FindFixtureNumber(
                    source.Progression.SourceStageId,
                    source.Progression.SourceFixtureId,
                    competitionStages),
                Outcome: source.Progression.Outcome,
                SlotKey: slotKey),
            FeedKind.Qualification => MapQualificationOrigin(
                source.Qualification!,
                slotKey,
                competitionStages),
            _ => null,
        };
    }

    private static SchematicFeedOriginDto MapQualificationOrigin(
        QualificationFeedRef qualification,
        string slotKey,
        IReadOnlyList<Stage> competitionStages)
    {
        var sourceStage = competitionStages.FirstOrDefault(s => s.Id.Equals(qualification.SourceStageId));
        var path = sourceStage?.Regulation.QualificationRules?.Paths
            .FirstOrDefault(p => p.Order == qualification.PathOrder);

        string? groupName = null;
        if (path?.Source.GroupId is { } groupId && sourceStage is not null)
        {
            groupName = sourceStage.Groups.FirstOrDefault(g => g.Id.Equals(groupId))?.Name;
        }

        return new SchematicFeedOriginDto(
            FeedKind.Qualification,
            SourceStageId: qualification.SourceStageId.Value,
            PathOrder: qualification.PathOrder,
            SelectionMode: path?.Selection.Mode,
            SelectionValue: path?.Selection.Value,
            SelectionEndValue: path?.Selection.EndValue,
            RankingScope: path?.Source.Scope,
            GroupId: path?.Source.GroupId?.Value,
            GroupName: groupName,
            AcrossGroupsPosition: path?.Source.AcrossGroupsPosition,
            SlotKey: slotKey);
    }

    private static SchematicEntryRefDto? MapEntry(
        EntryId? entryId,
        IReadOnlyDictionary<EntryId, CompetitionEntry> entries)
    {
        if (entryId is not { } id)
        {
            return null;
        }

        var name = entries.TryGetValue(id, out var entry) ? entry.DisplayName : null;
        return new SchematicEntryRefDto(id.Value, name);
    }

    private static SchematicParticipantRefDto? MapAssignment(
        EntryId? entryId,
        IReadOnlyDictionary<EntryId, CompetitionEntry> entries)
    {
        if (entryId is not { } id)
        {
            return null;
        }

        var side = EntryDisplayNames.ToSide(entries, id);
        return new SchematicParticipantRefDto(
            side.EntryId,
            side.DisplayName,
            side.ShortName,
            side.LogoMediaId,
            side.PrimaryColor,
            side.SecondaryColor);
    }

    private static int? ResolvePlaces(
        Competition competition,
        Stage stage,
        StructureFormatKind format) =>
        format switch
        {
            StructureFormatKind.Cup => ResolveCupEntryPlaces(stage),
            StructureFormatKind.Championship or StructureFormatKind.Swiss =>
                competition.Entries.Count(entry => entry.Status == EntryStatus.Active) is var n and > 0
                    ? n
                    : null,
            StructureFormatKind.Groups => ResolveGroupsPlaces(stage),
            _ => null,
        };

    /// <summary>
    /// Cup Places N = entry places (see StructureViewAssembler). Used only when Cup needs
    /// a places figure; schematic cases remain all form units (<c>slotCount</c>).
    /// </summary>
    private static int? ResolveCupEntryPlaces(Stage stage)
    {
        var slotCount = stage.Slots.Count;
        if (slotCount == 0)
        {
            return null;
        }

        var roundCount = stage.Rounds.Count;
        if (roundCount <= 1)
        {
            return slotCount;
        }

        var fullTreeSlots = (1 << (roundCount + 1)) - 2;
        if (slotCount == fullTreeSlots)
        {
            return 1 << roundCount;
        }

        var firstRoundSlots = 1 << roundCount;
        if (slotCount == firstRoundSlots)
        {
            return firstRoundSlots;
        }

        if (slotCount >= 2 && (slotCount + 2) % 2 == 0)
        {
            var entryPlaces = (slotCount + 2) / 2;
            if (entryPlaces >= 2 && (entryPlaces & (entryPlaces - 1)) == 0)
            {
                return entryPlaces;
            }
        }

        return null;
    }

    private static int? ResolveGroupsPlaces(Stage stage)
    {
        if (stage.Groups.Count == 0)
        {
            return null;
        }

        var perGroup = stage.PlacesPerGroup
            ?? stage.Regulation.DrawRules?.PotRules?.NumberOfPots;
        return perGroup is null or < 1 ? null : stage.Groups.Count * perGroup.Value;
    }

    private static StructureFormatKind? InferFormat(Stage stage) =>
        stage.IsSwiss
            ? StructureFormatKind.Swiss
            : stage.Rounds.Count > 0
                ? StructureFormatKind.Cup
                : stage.Groups.Count > 0
                    ? StructureFormatKind.Groups
                    : stage.Matchdays.Count > 0
                        ? StructureFormatKind.Championship
                        : null;
}

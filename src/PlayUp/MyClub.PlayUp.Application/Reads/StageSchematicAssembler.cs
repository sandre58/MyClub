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
            StructureFormatKind.Groups => AssembleGroups(stage, competitionStages, entries),
            StructureFormatKind.Championship or StructureFormatKind.Swiss =>
                AssembleRosterCapacity(stage, competition, competitionStages, format.Value, entries),
            _ => Empty(stage, format)
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
        var addressBySlot = new Dictionary<string, CupPlaceAddress>(StringComparer.Ordinal);

        // A1: topology first (stable Place ordinals). Fixture binding only attaches FixtureId.
        FillTopologyCupAddresses(stage, addressBySlot);

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
                var pairOrdinal = pairOrdinalOrNull(fixtureIndex);
                if (fixture.SlotAKey is not null && fixture.SlotBKey is not null)
                {
                    connections.Add(
                        new SchematicConnectionDto(
                            fixture.Id.Value,
                            roundOrder,
                            fixture.SlotAKey,
                            fixture.SlotBKey,
                            fixtureIndex + 1));
                    AttachFixtureToCupAddress(
                        addressBySlot,
                        fixture.SlotAKey,
                        fixture.Id.Value,
                        roundOrder,
                        round.Name,
                        pairOrdinal,
                        "A");
                    AttachFixtureToCupAddress(
                        addressBySlot,
                        fixture.SlotBKey,
                        fixture.Id.Value,
                        roundOrder,
                        round.Name,
                        pairOrdinal,
                        "B");
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
                if (roundOrder != 0) continue;
                pairingCases.Add(
                    PairingCase(
                        fixture.Id,
                        "A",
                        row.HomeEntryId,
                        entries,
                        roundOrder,
                        round.Name,
                        pairOrdinal));
                pairingCases.Add(
                    PairingCase(
                        fixture.Id,
                        "B",
                        row.AwayEntryId,
                        entries,
                        roundOrder,
                        round.Name,
                        pairOrdinal));
            }

            continue;

            // U4: omit pair ordinal when the round has a single fixture (e.g. Finale).
            int? pairOrdinalOrNull(int fixtureIndex) =>
                fixtures.Count > 1 ? fixtureIndex + 1 : null;
        }

        var slotCases = stage.Slots
            .Select(slot =>
            {
                var feed = feedBySlot.GetValueOrDefault(slot.SlotKey);
                var hasAddress = addressBySlot.TryGetValue(slot.SlotKey, out var address);
                return new SchematicCaseDto(
                    new SchematicFormPositionDto(
                        FormKindCupSlot,
                        SlotKey: slot.SlotKey,
                        FixtureId: hasAddress ? address.FixtureId : null,
                        Side: hasAddress ? address.Side : null,
                        RoundOrder: hasAddress ? address.RoundOrder : null,
                        RoundName: hasAddress ? address.RoundName : null,
                        PairOrdinal: hasAddress ? address.PairOrdinal : null),
                    MapFeedOrigin(feed, slot.SlotKey, competitionStages),
                    MapEntry(slot.EntryId, entries),
                    MapAssignment(slot.EntryId, entries));
            })
            .ToArray();

        // A published pairing draw fills the bracket without binding slots: the materialized
        // pairs are the truthful occupation; keeping the unbound empty slots would double capacity.
        IReadOnlyList<SchematicCaseDto> cases = pairingCases.Count > 0 && stage.Slots.All(slot => slot.EntryId is null)
            ? pairingCases
            : slotCases;

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

    private readonly record struct CupPlaceAddress(
        int RoundOrder,
        string RoundName,
        int? PairOrdinal,
        string Side,
        Guid? FixtureId);

    private static void RememberCupAddress(
        Dictionary<string, CupPlaceAddress> addressBySlot,
        string slotKey,
        CupPlaceAddress address) =>
        addressBySlot.TryAdd(slotKey, address);

    private static void AttachFixtureToCupAddress(
        Dictionary<string, CupPlaceAddress> addressBySlot,
        string slotKey,
        Guid fixtureId,
        int roundOrder,
        string roundName,
        int? pairOrdinal,
        string side)
    {
        if (addressBySlot.TryGetValue(slotKey, out var existing))
        {
            // Keep topology Round/Side/PairOrdinal; only attach fixture id.
            addressBySlot[slotKey] = existing with { FixtureId = fixtureId };
            return;
        }

        addressBySlot[slotKey] = new CupPlaceAddress(
            roundOrder,
            roundName,
            pairOrdinal,
            side,
            fixtureId);
    }

    /// <summary>
    /// U4 A1 — derive Place address from form topology when fixtures do not bind slots yet.
    /// Matches SPA bracket column layout (classic KO tree or single-round adjacent pairs).
    /// </summary>
    private static void FillTopologyCupAddresses(
        Stage stage,
        Dictionary<string, CupPlaceAddress> addressBySlot)
    {
        if (stage.Rounds.Count == 0 || stage.Slots.Count == 0)
        {
            return;
        }

        var keys = stage.Slots.Select(slot => slot.SlotKey).ToArray();
        var roundCount = stage.Rounds.Count;

        if (roundCount == 1)
        {
            FillAdjacentPairsInRound(stage, roundOrder: 0, keys, offset: 0, length: keys.Length, addressBySlot);
            return;
        }

        var firstRoundSlots = 1 << roundCount;
        var allRoundsSlots = (2 * firstRoundSlots) - 2;

        if (keys.Length == allRoundsSlots)
        {
            var offset = 0;
            var size = firstRoundSlots;
            for (var roundOrder = 0; roundOrder < roundCount; roundOrder++)
            {
                FillAdjacentPairsInRound(stage, roundOrder, keys, offset, size, addressBySlot);
                offset += size;
                size /= 2;
            }

            return;
        }

        if (keys.Length == firstRoundSlots)
        {
            FillAdjacentPairsInRound(stage, roundOrder: 0, keys, offset: 0, length: keys.Length, addressBySlot);
            return;
        }

        // Non-classic slot counts: still label as adjacent pairs on round 0.
        FillAdjacentPairsInRound(stage, roundOrder: 0, keys, offset: 0, length: keys.Length, addressBySlot);
    }

    private static void FillAdjacentPairsInRound(
        Stage stage,
        int roundOrder,
        string[] keys,
        int offset,
        int length,
        Dictionary<string, CupPlaceAddress> addressBySlot)
    {
        if (roundOrder < 0 || roundOrder >= stage.Rounds.Count || length < 2)
        {
            return;
        }

        var round = stage.Rounds[roundOrder];
        var pairCount = length / 2;
        for (var pairIndex = 0; pairIndex < pairCount; pairIndex++)
        {
            var i = offset + (pairIndex * 2);
            int? pairOrdinal = pairCount > 1 ? pairIndex + 1 : null;
            RememberCupAddress(
                addressBySlot,
                keys[i],
                new CupPlaceAddress(roundOrder, round.Name, pairOrdinal, "A", FixtureId: null));
            RememberCupAddress(
                addressBySlot,
                keys[i + 1],
                new CupPlaceAddress(roundOrder, round.Name, pairOrdinal, "B", FixtureId: null));
        }
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
        IReadOnlyDictionary<EntryId, CompetitionEntry> entries,
        int roundOrder,
        string roundName,
        int? pairOrdinal) =>
        new(
            new SchematicFormPositionDto(
                FormKindCupSlot,
                FixtureId: fixtureId.Value,
                Side: side,
                RoundOrder: roundOrder,
                RoundName: roundName,
                PairOrdinal: pairOrdinal),
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
        IReadOnlyList<Stage> competitionStages,
        IReadOnlyDictionary<EntryId, CompetitionEntry> entries)
    {
        var perGroup = stage.PlacesPerGroup
                       ?? stage.Regulation.DrawRules?.PotRules?.NumberOfPots;
        if (stage.Groups.Count == 0 || perGroup is null or < 1)
        {
            return Empty(stage, StructureFormatKind.Groups);
        }

        var pendingByGroup = CollectGroupPendingFeeds(stage, competitionStages);
        var cases = new List<SchematicCaseDto>(stage.Groups.Count * perGroup.Value);
        foreach (var group in stage.Groups)
        {
            var pending = pendingByGroup.GetValueOrDefault(group.Id.Value) ?? [];
            var pendingIndex = 0;
            for (var index = 1; index <= perGroup.Value; index++)
            {
                EntryId? placed = index <= group.EntryIds.Count
                    ? group.EntryIds[index - 1]
                    : null;

                // Empty seats absorb pending ForGroup WhoFeeds (same bag idea as Champ/Swiss).
                // Occupied seats stay occupancy-only — no Path→Place k address.
                SchematicFeedOriginDto? feedOrigin = null;
                if (placed is null && pendingIndex < pending.Count)
                {
                    feedOrigin = pending[pendingIndex++];
                }

                cases.Add(
                    new SchematicCaseDto(
                        new SchematicFormPositionDto(
                            FormKindGroupPlace,
                            GroupId: group.Id.Value,
                            GroupName: group.Name,
                            Index: index),
                        FeedOrigin: feedOrigin,
                        MapEntry(placed, entries),
                        MapAssignment(placed, entries)));
            }
        }

        // GroupFeeds kept for API compat; Structure SPA paints WhoFeeds on cases only.
        var groupFeeds = ResolveGroupFeeds(pendingByGroup);

        return new StageSchematicDto(
            stage.Id.Value,
            stage.CompetitionId.Value,
            stage.Name.Value,
            stage.Status,
            StructureFormatKind.Groups,
            cases,
            [],
            GroupFeeds: groupFeeds);
    }

    /// <summary>
    /// Inbound Qual/Prog ForGroup destinations, ordered per group (all paths — one seat each when pending).
    /// </summary>
    private static Dictionary<Guid, List<SchematicFeedOriginDto>> CollectGroupPendingFeeds(
        Stage target,
        IReadOnlyList<Stage> competitionStages)
    {
        var candidates = new Dictionary<Guid, List<SchematicFeedOriginDto>>();

        foreach (var stage in competitionStages)
        {
            if (stage.Regulation.QualificationRules is { } qualification)
            {
                foreach (var path in qualification.Paths)
                {
                    if (!path.Destination.StageId.Equals(target.Id) || !path.Destination.TargetsGroup)
                    {
                        continue;
                    }

                    var groupId = path.Destination.GroupId!.Value.Value;
                    var origin = MapQualificationPathOrigin(path, stage, groupId);
                    AddGroupFeedCandidate(candidates, groupId, origin);
                }
            }

            if (stage.Regulation.ProgressionRules is not { } progression)
            {
                continue;
            }

            foreach (var path in progression.Paths)
            {
                if (!path.Destination.StageId.Equals(target.Id) || !path.Destination.TargetsGroup)
                {
                    continue;
                }

                var groupId = path.Destination.GroupId!.Value.Value;
                var origin = new SchematicFeedOriginDto(
                    FeedKind.Progression,
                    SourceStageId: stage.Id.Value,
                    SourceStageName: stage.Name.Value,
                    SourceFixtureId: path.SourceFixtureId.Value,
                    SourceFixtureNumber: FindFixtureNumber(stage.Id, path.SourceFixtureId, competitionStages),
                    Outcome: path.Outcome,
                    DestinationGroupId: groupId);
                AddGroupFeedCandidate(candidates, groupId, origin);
            }
        }

        var result = new Dictionary<Guid, List<SchematicFeedOriginDto>>();
        foreach (var (groupId, origins) in candidates)
        {
            var ordered = origins
                .OrderBy(o => o.PathOrder ?? int.MaxValue)
                .ThenBy(o => o.SourceFixtureId ?? Guid.Empty)
                .ThenBy(o => o.SourceStageId ?? Guid.Empty)
                .ToList();
            result[groupId] = ordered;
        }

        return result;
    }

    /// <summary>
    /// One representative origin per group when a single Qual/Prog mechanism feeds that poule.
    /// </summary>
    private static List<SchematicGroupFeedDto> ResolveGroupFeeds(
        Dictionary<Guid, List<SchematicFeedOriginDto>> pendingByGroup)
    {
        var result = new List<SchematicGroupFeedDto>();
        foreach (var (groupId, origins) in pendingByGroup)
        {
            var byMechanism = origins
                .GroupBy(o => (o.Kind, o.SourceStageId))
                .ToArray();
            if (byMechanism.Length != 1)
            {
                continue;
            }

            result.Add(new SchematicGroupFeedDto(groupId, byMechanism[0].First()));
        }

        return result;
    }

    private static void AddGroupFeedCandidate(
        Dictionary<Guid, List<SchematicFeedOriginDto>> candidates,
        Guid groupId,
        SchematicFeedOriginDto origin)
    {
        if (!candidates.TryGetValue(groupId, out var list))
        {
            list = [];
            candidates[groupId] = list;
        }

        list.Add(origin);
    }

    private static SchematicFeedOriginDto MapQualificationPathOrigin(
        Domain.Rules.QualificationPath path,
        Stage sourceStage,
        Guid? destinationGroupId)
    {
        string? groupName = null;
        if (path.Source.GroupId is { } sourceGroupId)
        {
            groupName = sourceStage.Groups.FirstOrDefault(g => g.Id.Equals(sourceGroupId))?.Name;
        }

        return new SchematicFeedOriginDto(
            FeedKind.Qualification,
            SourceStageId: sourceStage.Id.Value,
            SourceStageName: sourceStage.Name.Value,
            PathOrder: path.Order,
            SelectionMode: path.Selection.Mode,
            SelectionValue: path.Selection.Value,
            SelectionEndValue: path.Selection.EndValue,
            RankingScope: path.Source.Scope,
            GroupId: path.Source.GroupId?.Value,
            GroupName: groupName,
            AcrossGroupsPosition: path.Source.AcrossGroupsPosition,
            DestinationGroupId: destinationGroupId);
    }

    /// <summary>
    /// Championship / Swiss: RosterPlace 1..N as a bag projection of
    /// <see cref="StageSchematicDto.ExpectedFormParticipants"/> (non-addressing order).
    /// Pending ForForm intentions carry FeedOrigin on the case for Structure chrome only —
    /// never a Path→RosterPlace address.
    /// </summary>
    private static StageSchematicDto AssembleRosterCapacity(
        Stage stage,
        Competition competition,
        IReadOnlyList<Stage> competitionStages,
        StructureFormatKind format,
        IReadOnlyDictionary<EntryId, CompetitionEntry> entries)
    {
        var expected = ResolveExpectedFormParticipants(stage, competitionStages, entries);
        var places = ResolvePlaces(competition, stage, format);
        if (places is null or < 1)
        {
            return Empty(stage, format) with { ExpectedFormParticipants = expected };
        }

        var bag = new List<(EntryId? EntryId, SchematicFeedOriginDto? PendingOrigin)>(
            expected.Resolved.Count + expected.Pending.Count);
        bag.AddRange(stage.CompositionEntries.Select(compositionEntry => ((EntryId? EntryId, SchematicFeedOriginDto? PendingOrigin))(compositionEntry.EntryId, null)));

        bag.AddRange(expected.Pending.Select(pending => ((EntryId? EntryId, SchematicFeedOriginDto? PendingOrigin))(null, pending)));

        var cases = Enumerable
            .Range(1, places.Value)
            .Select(index =>
            {
                if (index > bag.Count)
                {
                    return new SchematicCaseDto(
                        new SchematicFormPositionDto(FormKindRosterPlace, Index: index),
                        FeedOrigin: null,
                        Entry: null,
                        Assignment: null);
                }

                var (entryId, pendingOrigin) = bag[index - 1];
                return new SchematicCaseDto(
                    new SchematicFormPositionDto(FormKindRosterPlace, Index: index),
                    FeedOrigin: pendingOrigin,
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
            SwissRoundCount: stage.SwissSettings?.RoundCount,
            ExpectedFormParticipants: expected);
    }

    /// <summary>
    /// Resolved = Composition; Pending = inbound ForForm paths without resolution provenance.
    /// </summary>
    private static ExpectedFormParticipantsDto ResolveExpectedFormParticipants(
        Stage stage,
        IReadOnlyList<Stage> competitionStages,
        IReadOnlyDictionary<EntryId, CompetitionEntry> entries)
    {
        var resolved = stage.CompositionEntries
            .Select(compositionEntry =>
            {
                var entryId = compositionEntry.EntryId;
                return new ExpectedResolvedFormParticipantDto(
                    MapEntry(entryId, entries)!,
                    MapAssignment(entryId, entries));
            })
            .ToArray();

        var applied = stage.FormPathResolutions
            .Select(resolution => resolution.PathFingerprint)
            .ToHashSet(StringComparer.Ordinal);

        var pending = new List<SchematicFeedOriginDto>();
        foreach (var source in competitionStages)
        {
            if (source.Regulation.QualificationRules is { } qualification)
            {
                pending.AddRange(from path in qualification.Paths where path.Destination.StageId.Equals(stage.Id) && path.Destination.TargetsForm let fingerprint = FormPathResolutionKey.FromQualification(source.Id, path) where !applied.Contains(fingerprint) select MapQualificationPathOrigin(path, source, destinationGroupId: null));
            }

            if (source.Regulation.ProgressionRules is not { } progression)
            {
                continue;
            }

            pending.AddRange(from path in progression.Paths where path.Destination.StageId.Equals(stage.Id) && path.Destination.TargetsForm let fingerprint = FormPathResolutionKey.FromProgression(source.Id, path) where !applied.Contains(fingerprint) select new SchematicFeedOriginDto(FeedKind.Progression, SourceStageId: source.Id.Value, SourceStageName: source.Name.Value, SourceFixtureId: path.SourceFixtureId.Value, SourceFixtureNumber: FindFixtureNumber(source.Id, path.SourceFixtureId, competitionStages), Outcome: path.Outcome));
        }

        var orderedPending =
            pending
                .OrderBy(o => o.PathOrder ?? int.MaxValue)
                .ThenBy(o => o.SourceFixtureId ?? Guid.Empty)
                .ThenBy(o => o.SourceStageId ?? Guid.Empty)
                .ToArray();

        return new ExpectedFormParticipantsDto(resolved, orderedPending);
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
                SourceStageName: FindStageName(
                    source.Progression.SourceStageId,
                    competitionStages),
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
            _ => null
        };
    }

    private static string? FindStageName(
        StageId stageId,
        IReadOnlyList<Stage> competitionStages) =>
        competitionStages.FirstOrDefault(s => s.Id.Equals(stageId))?.Name.Value;

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
            SourceStageName: sourceStage?.Name.Value,
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
            _ => null
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

        if (slotCount < 2 || (slotCount + 2) % 2 != 0) return null;
        var entryPlaces = (slotCount + 2) / 2;
        return entryPlaces >= 2 && (entryPlaces & (entryPlaces - 1)) == 0 ? entryPlaces : null;
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

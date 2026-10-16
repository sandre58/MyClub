// -----------------------------------------------------------------------
// <copyright file="StructureInboundCompositionAssembler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Hub projection for Composition Qualifiés — inbound feed resolution and placement (not SPA schematic matching).
/// </summary>
public static class StructureInboundCompositionAssembler
{
    /// <summary>
    /// Builds inbound composition for a destination stage.
    /// </summary>
    public static StructureInboundCompositionDto? Assemble(
        Competition competition,
        Stage destination,
        IReadOnlyList<Stage> competitionStages,
        IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>>? matchSummariesByStage = null)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(competitionStages);

        if (StructureStageInboundTopology.IsRootComposition(destination, competitionStages))
        {
            return null;
        }

        IReadOnlyList<MatchSummaryRow> destinationMatchRows = [];
        if (matchSummariesByStage?.TryGetValue(destination.Id, out var rows) == true)
        {
            destinationMatchRows = rows;
        }

        var schematic = StageSchematicAssembler.Assemble(
            destination,
            competition,
            competitionStages,
            destinationMatchRows);
        var entriesById = competition.Entries.ToDictionary(entry => entry.Id);

        var rules = new List<StructureInboundFeedRuleDto>();
        foreach (var source in competitionStages)
        {
            if (source.Regulation.QualificationRules is { } qualification)
            {
                rules.AddRange(from path in qualification.Paths where path.Destination.StageId.Equals(destination.Id) let feedKey = $"q-{source.Id.Value}-{path.Order}" let volume = StructurePopulationVolume.QualificationPathVolume(path) select BuildRule(feedKey, source.Id.Value, volume, path, null, destination, source, entriesById, matchSummariesByStage, schematic, competitionStages));
            }

            if (source.Regulation.ProgressionRules is not { } progression)
            {
                continue;
            }

            rules.AddRange(from path in progression.Paths where path.Destination.StageId.Equals(destination.Id) let feedKey = $"p-{source.Id.Value}-{path.SourcePairKey}-{path.Outcome}" select BuildRule(feedKey, source.Id.Value, 1, null, path, destination, source, entriesById, matchSummariesByStage, schematic, competitionStages));
        }

        rules.Sort(static (a, b) => string.CompareOrdinal(a.FeedKey, b.FeedKey));
        var configuredVolume = rules.Sum(rule => rule.Seats.Count);
        var resolvedCount = rules.Sum(rule => rule.Seats.Count(seat => seat.EntryId is not null));

        return new StructureInboundCompositionDto(configuredVolume, resolvedCount, rules);
    }

    private static StructureInboundFeedRuleDto BuildRule(
        string feedKey,
        Guid sourceStageId,
        int volume,
        QualificationPath? qualPath,
        ProgressionPath? progPath,
        Stage destination,
        Stage sourceStage,
        IReadOnlyDictionary<EntryId, CompetitionEntry> entriesById,
        IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>>? matchSummariesByStage,
        StageSchematicDto schematic,
        IReadOnlyList<Stage> competitionStages)
    {
        var seats = new List<StructureInboundFeedSeatDto>(volume);
        var usedSchematicCases = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < volume; i++)
        {
            seats.Add(ResolveSeat(
                qualPath,
                progPath,
                destination,
                sourceStage,
                entriesById,
                matchSummariesByStage,
                schematic,
                usedSchematicCases,
                competitionStages));
        }

        return new StructureInboundFeedRuleDto(feedKey, sourceStageId, seats);
    }

    private static StructureInboundFeedSeatDto ResolveSeat(
        QualificationPath? qualPath,
        ProgressionPath? progPath,
        Stage destination,
        Stage sourceStage,
        IReadOnlyDictionary<EntryId, CompetitionEntry> entriesById,
        IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>>? matchSummariesByStage,
        StageSchematicDto schematic,
        HashSet<string> usedSchematicCases,
        IReadOnlyList<Stage> competitionStages)
    {
        EntryId? entryId = null;

        if (qualPath is not null)
        {
            entryId = ResolveQualificationSeat(qualPath, destination, sourceStage);
        }
        else if (progPath is not null)
        {
            entryId = InboundFeedSeatResolver.ResolveProgressionEntry(
                progPath,
                destination,
                sourceStage,
                matchSummariesByStage);
        }

        entryId ??= ResolveFromSchematicInboundCase(
            qualPath,
            progPath,
            sourceStage,
            schematic,
            usedSchematicCases);

        var placed = InboundFeedSeatResolver.IsInboundSeatPlaced(
            destination,
            sourceStage,
            qualPath,
            progPath,
            entryId,
            competitionStages);

        if (entryId is null)
        {
            return new StructureInboundFeedSeatDto(null, null, placed);
        }

        var resolvedId = entryId.Value;
        entriesById.TryGetValue(resolvedId, out var entry);
        var displayName = entry?.DisplayName;
        return new StructureInboundFeedSeatDto(resolvedId.Value, displayName, placed);
    }

    private static EntryId? ResolveQualificationSeat(
        QualificationPath path,
        Stage destination,
        Stage sourceStage)
    {
        if (path.Destination.TargetsForm)
        {
            var fingerprint = FormPathResolutionKey.FromQualification(sourceStage.Id, path);
            return destination.FormPathResolutions
                .FirstOrDefault(resolution => resolution.PathFingerprint == fingerprint)
                ?.EntryId;
        }

        if (path.Destination.TargetsPopulation || path.Destination.TargetsGroup)
        {
            return null;
        }

        var slotKey = path.Destination.SlotKey;
        return string.IsNullOrWhiteSpace(slotKey) ? null : destination.FindSlot(slotKey)?.EntryId;
    }

    private static EntryId? ResolveFromSchematicInboundCase(
        QualificationPath? qualPath,
        ProgressionPath? progPath,
        Stage sourceStage,
        StageSchematicDto schematic,
        HashSet<string> usedSchematicCases)
    {
        foreach (var place in schematic.Cases)
        {
            var caseKey = CaseKey(place);
            if (usedSchematicCases.Contains(caseKey))
            {
                continue;
            }

            var origin = place.FeedOrigin;
            if (origin is null)
            {
                continue;
            }

            if (qualPath is not null)
            {
                if (origin.Kind != FeedKind.Qualification
                    || origin.SourceStageId != sourceStage.Id.Value)
                {
                    continue;
                }

                if (origin.PathOrder is { } order && order != qualPath.Order)
                {
                    continue;
                }

                usedSchematicCases.Add(caseKey);
                return EntryIdFromCase(place);
            }

            if (progPath is null) continue;
            if (origin.Kind != FeedKind.Progression
                || origin.SourceStageId != sourceStage.Id.Value)
            {
                continue;
            }

            if (!string.Equals(origin.SourcePairKey, progPath.SourcePairKey, StringComparison.Ordinal)
                || origin.Outcome != progPath.Outcome)
            {
                continue;
            }

            usedSchematicCases.Add(caseKey);
            return EntryIdFromCase(place);
        }

        return null;
    }

    private static string CaseKey(SchematicCaseDto place) =>
        $"{place.FormPosition.Kind}|{place.FormPosition.SlotKey}|{place.FormPosition.GroupId}|{place.FormPosition.Index}";

    private static EntryId? EntryIdFromCase(SchematicCaseDto place) =>
        place.Assignment is not null
            ? new EntryId(place.Assignment.EntryId)
            : place.Entry is not null ? new EntryId(place.Entry.EntryId) : null;
}

/// <summary>Inbound Qual/Prog composition projection for the Structure hub.</summary>
/// <param name="ConfiguredVolume">Total inbound seats configured toward this phase.</param>
/// <param name="ResolvedCount">Seats with a resolved entry identity.</param>
/// <param name="Rules">One row per inbound feed rule (aligned with SPA feed keys).</param>
public sealed record StructureInboundCompositionDto(
    int ConfiguredVolume,
    int ResolvedCount,
    IReadOnlyList<StructureInboundFeedRuleDto> Rules);

/// <summary>One inbound feed rule and its seat resolutions.</summary>
public sealed record StructureInboundFeedRuleDto(
    string FeedKey,
    Guid SourceStageId,
    IReadOnlyList<StructureInboundFeedSeatDto> Seats);

/// <summary>One seat within an inbound feed rule.</summary>
public sealed record StructureInboundFeedSeatDto(
    Guid? EntryId,
    string? DisplayName,
    bool Placed);

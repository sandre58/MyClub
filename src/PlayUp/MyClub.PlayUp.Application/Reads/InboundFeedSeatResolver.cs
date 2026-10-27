// -----------------------------------------------------------------------
// <copyright file="InboundFeedSeatResolver.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Progression;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Resolves inbound Qual/Prog seats for Composition hub (domain + fixture outcomes, not SPA schematic).
/// </summary>
internal static class InboundFeedSeatResolver
{
    internal static EntryId? ResolveQualificationEntry(
        QualificationPath path,
        Stage destination,
        Stage sourceStage,
        IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>>? matchSummariesByStage)
    {
        if (path.Destination.TargetsForm || path.Destination.TargetsPopulation)
        {
            var fingerprint = FormPathResolutionKey.FromQualification(sourceStage.Id, path);
            var fromWitness = destination.FormPathResolutions
                .FirstOrDefault(resolution => resolution.PathFingerprint == fingerprint)
                ?.EntryId;
            return fromWitness ?? InboundQualificationSeatProjection.TryResolveSeat(
                path,
                sourceStage,
                destination,
                matchSummariesByStage);
        }

        if (path.Destination.TargetsGroup)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(path.Destination.SlotKey))
        {
            return InboundQualificationSeatProjection.TryResolveSeat(
                path,
                sourceStage,
                destination,
                matchSummariesByStage);
        }

        var occupant = destination.FindSlot(path.Destination.SlotKey)?.EntryId;
        return occupant ?? InboundQualificationSeatProjection.TryResolveSeat(
            path,
            sourceStage,
            destination,
            matchSummariesByStage);
    }

    internal static EntryId? ResolveProgressionEntry(
        ProgressionPath path,
        Stage destination,
        Stage sourceStage,
        IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>>? matchSummariesByStage)
    {
        if (!string.IsNullOrWhiteSpace(path.Destination.SlotKey))
        {
            var occupant = destination.FindSlot(path.Destination.SlotKey)?.EntryId;
            if (occupant is not null)
            {
                return occupant;
            }
        }

        if (!path.Destination.TargetsForm)
            return ProjectProgressionEntryFromSourceFixture(path, sourceStage, matchSummariesByStage);
        var fingerprint = FormPathResolutionKey.FromProgression(sourceStage.Id, path);
        var resolved = destination.FormPathResolutions
            .FirstOrDefault(r => r.PathFingerprint == fingerprint)
            ?.EntryId;
        return resolved ?? ProjectProgressionEntryFromSourceFixture(path, sourceStage, matchSummariesByStage);
    }

    /// <summary>
    /// Inbound seat placed on destination (resolved entry or structural slot/form feed).
    /// </summary>
    internal static bool IsInboundSeatPlaced(
        Stage destination,
        Stage sourceStage,
        QualificationPath? qualPath,
        ProgressionPath? progPath,
        EntryId? entryId,
        IReadOnlyList<Stage> competitionStages) =>
        entryId is not null
            ? IsPlacedOnDestination(
                destination,
                sourceStage.Id,
                qualPath,
                progPath,
                entryId.Value)
            : IsInboundPathPlacedWithoutResolvedEntry(
                destination,
                sourceStage,
                qualPath,
                progPath,
                competitionStages);

    internal static bool IsPlacedOnDestination(
        Stage destination,
        StageId sourceStageId,
        QualificationPath? qualPath,
        ProgressionPath? progPath,
        EntryId entryId) =>
        progPath is not null
            ? IsPlacedForProgression(destination, sourceStageId, progPath, entryId)
            : qualPath is not null && IsPlacedForQualification(destination, sourceStageId, qualPath, entryId);

    private static bool IsInboundPathPlacedWithoutResolvedEntry(
        Stage destination,
        Stage sourceStage,
        QualificationPath? qualPath,
        ProgressionPath? progPath,
        IReadOnlyList<Stage> competitionStages)
    {
        if (qualPath is not null)
        {
            if (qualPath.Destination.TargetsSlot
                && !string.IsNullOrWhiteSpace(qualPath.Destination.SlotKey))
            {
                return SlotFeedMatchesQualification(
                    destination,
                    competitionStages,
                    sourceStage.Id,
                    qualPath);
            }

            if (qualPath.Destination.TargetsForm)
            {
                var fingerprint = FormPathResolutionKey.FromQualification(sourceStage.Id, qualPath);
                return destination.FormPathResolutions
                    .Any(resolution => resolution.PathFingerprint == fingerprint);
            }
        }

        if (progPath is null) return false;
        if (progPath.Destination.TargetsSlot
            && !string.IsNullOrWhiteSpace(progPath.Destination.SlotKey))
        {
            return SlotFeedMatchesProgression(
                destination,
                competitionStages,
                sourceStage.Id,
                progPath);
        }

        if (!progPath.Destination.TargetsForm) return false;
        var fingerprint1 = FormPathResolutionKey.FromProgression(sourceStage.Id, progPath);
        return destination.FormPathResolutions
            .Any(resolution => resolution.PathFingerprint == fingerprint1);
    }

    private static bool SlotFeedMatchesQualification(
        Stage destination,
        IReadOnlyList<Stage> competitionStages,
        StageId sourceStageId,
        QualificationPath path)
    {
        var slotKey = path.Destination.SlotKey;
        if (string.IsNullOrWhiteSpace(slotKey))
        {
            return false;
        }

        var snapshot = SlotFeedSnapshotAssembler.Assemble(destination, competitionStages);
        var resolution = SlotFeedResolver.Resolve(snapshot, slotKey);
        if (resolution.Status != FeedResolutionStatus.Unique || resolution.Source is null)
        {
            return false;
        }

        if (resolution.Source.Kind != FeedKind.Qualification)
        {
            return false;
        }

        var qualification = resolution.Source.Qualification;
        return qualification?.SourceStageId.Equals(sourceStageId) == true
            && qualification.PathOrder == path.Order;
    }

    internal static bool SlotFeedMatchesProgression(
        Stage destination,
        IReadOnlyList<Stage> competitionStages,
        StageId sourceStageId,
        ProgressionPath path)
    {
        var slotKey = path.Destination.SlotKey;
        if (string.IsNullOrWhiteSpace(slotKey))
        {
            return false;
        }

        var snapshot = SlotFeedSnapshotAssembler.Assemble(destination, competitionStages);
        var resolution = SlotFeedResolver.Resolve(snapshot, slotKey);
        if (resolution.Status != FeedResolutionStatus.Unique || resolution.Source is null)
        {
            return false;
        }

        if (resolution.Source.Kind != FeedKind.Progression)
        {
            return false;
        }

        var progression = resolution.Source.Progression;
        return progression?.SourceStageId.Equals(sourceStageId) == true
            && string.Equals(progression.SourcePairKey, path.SourcePairKey, StringComparison.Ordinal)
            && progression.Outcome == path.Outcome;
    }

    private static bool IsPlacedForProgression(
        Stage destination,
        StageId sourceStageId,
        ProgressionPath path,
        EntryId entryId)
    {
        if (string.IsNullOrWhiteSpace(path.Destination.SlotKey))
        {
            return DestinationSatisfaction.EvaluateProgression(
                       destination,
                       sourceStageId,
                       path,
                       entryId)
                   == DestinationSatisfaction.Kind.Satisfied;
        }

        var occupant = destination.FindSlot(path.Destination.SlotKey)?.EntryId;
        return occupant?.Equals(entryId) == true;
    }

    private static bool IsPlacedForQualification(
        Stage destination,
        StageId sourceStageId,
        QualificationPath path,
        EntryId entryId)
    {
        if (string.IsNullOrWhiteSpace(path.Destination.SlotKey))
        {
            return DestinationSatisfaction.EvaluateQualification(
                       destination,
                       sourceStageId,
                       path,
                       entryId)
                   == DestinationSatisfaction.Kind.Satisfied;
        }

        var occupant = destination.FindSlot(path.Destination.SlotKey)?.EntryId;
        return occupant?.Equals(entryId) == true;
    }

    /// <summary>
    /// Replays upstream fixture outcome for consumption checks (no slot occupant shortcut).
    /// </summary>
    internal static EntryId? ProjectProgressionEntryFromSourceFixture(
        ProgressionPath path,
        Stage sourceStage,
        IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>>? matchSummariesByStage)
    {
        if (matchSummariesByStage is null
            || !matchSummariesByStage.TryGetValue(sourceStage.Id, out var rows)
            || rows.Count == 0)
        {
            return null;
        }

        var fixture = sourceStage.FindFixtureByBracketPairKey(path.SourcePairKey);
        if (fixture is null)
        {
            return null;
        }

        var slices = rows
            .Where(row => fixture.MatchIds.Contains(row.Id))
            .Select(ToAttentionSlice)
            .ToArray();
        if (slices.Length == 0)
        {
            return null;
        }

        var round = sourceStage.Rounds.FirstOrDefault(candidate =>
            candidate.Fixtures.Any(f => f.Id.Equals(fixture.Id)));
        if (round is null)
        {
            return null;
        }

        try
        {
            var snapshot = FixtureConfrontationSnapshotAssembler.Assemble(fixture, slices);
            var outcome = FixtureOutcomeResolver.Resolve(
                TieFormat.OrDefaultOneLeg(round.TieFormat),
                snapshot);
            var instruction = ProgressionApplier.Apply(path, path.SourcePairKey, outcome);
            return instruction.EntryId;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static MatchAttentionSlice ToAttentionSlice(MatchSummaryRow row) =>
        new(
            row.Id,
            row.StageId,
            row.Status,
            row.HomeEntryId,
            row.AwayEntryId,
            row.Result);
}

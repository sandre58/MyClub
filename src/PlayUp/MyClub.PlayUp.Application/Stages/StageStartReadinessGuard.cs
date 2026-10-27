// -----------------------------------------------------------------------
// <copyright file="StageStartReadinessGuard.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application guard: Ready → Running Start readiness.
/// </summary>
internal static class StageStartReadinessGuard
{
    /// <summary>Read-side disabled reason when eligible confrontations remain unmaterialized.</summary>
    internal const string DisabledUnmaterializedConfrontations = "UnmaterializedConfrontations";

    internal const string DisabledDrawObsoleteForConsumption = "DrawObsoleteForConsumption";

    internal const string DisabledIncompleteEntryRound = CupCanonicalEntryRoundStartGate.DisabledReasonCode;

    internal const string DisabledNoExploitableInitialConfrontations = "NoExploitableInitialConfrontations";

    internal const string DisabledPhaseRosterIncomplete =
        StructureMaterializeDisabledReasons.PhaseRosterIncomplete;

    internal const string DisabledGroupsNotReady = StructureMaterializeDisabledReasons.GroupsNotReady;

    internal const string DisabledNoDeterminedConfrontations =
        StructureMaterializeDisabledReasons.NoDeterminedConfrontations;

    private const string DisabledInitialRoundNotPlayable = "InitialRoundNotPlayable";

    internal static void EnsureReadyToStart(
        Stage stage,
        Competition competition,
        IReadOnlyList<MatchSummaryRow> matchSummaries,
        IReadOnlyList<Stage>? competitionStages = null)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(matchSummaries);

        var stages = competitionStages ?? [stage];
        if (CupEntryRoundPairPlacementGauge.HasIncompleteStartedPairPlacement(competition, stage, stages))
        {
            throw new ApplicationFailureException(
                "Start is blocked because canonical Cup entry-round pairs are not all placement-complete.",
                ApplicationErrorCodes.StartBlockedIncompleteEntryRound);
        }

        if (TryGetProductionStartGap(competition, stage, matchSummaries, competitionStages) is { } productionGap)
        {
            ThrowStartBlockedForProductionGap(productionGap);
        }

        if (CupEntryRoundPairPlacementGauge.HasRemainingCanonicalEntryRoundPairs(competition, stage, stages))
        {
            throw new ApplicationFailureException(
                "Start is blocked because canonical Cup entry-round pairs are not all placement-complete.",
                ApplicationErrorCodes.StartBlockedIncompleteEntryRound);
        }

        if (StageConfrontationMaterializationState.LacksExploitableInitialConfrontations(
                competition,
                stage,
                competitionStages))
        {
            throw new ApplicationFailureException(
                "Start is blocked because the phase requires initial confrontations but none can be established.",
                ApplicationErrorCodes.StartBlockedNoExploitableInitialConfrontations);
        }

        if (CupReadyLacksDeterminedConfrontations(stage, matchSummaries))
        {
            throw new ApplicationFailureException(
                "Start is blocked because no confrontation is determined for this Cup phase.",
                ApplicationErrorCodes.StartBlockedNoDeterminedConfrontations);
        }

        StructureDrawConsumptionGate.EnsureNoObsoleteDrawsOnStage(stage);
    }

    internal static string? TryGetDisabledReasonCode(
        Stage stage,
        Competition competition,
        IReadOnlyList<MatchSummaryRow> matchSummaries,
        IReadOnlyList<Stage>? competitionStages = null)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(matchSummaries);

        var stages = competitionStages ?? [stage];
        if (CupEntryRoundPairPlacementGauge.HasIncompleteStartedPairPlacement(competition, stage, stages))
        {
            return DisabledIncompleteEntryRound;
        }

        if (TryGetProductionStartGap(competition, stage, matchSummaries, competitionStages) is { } productionGap)
        {
            return productionGap;
        }

        if (CupEntryRoundPairPlacementGauge.HasRemainingCanonicalEntryRoundPairs(competition, stage, stages))
        {
            return DisabledIncompleteEntryRound;
        }

        if (StageConfrontationMaterializationState.LacksExploitableInitialConfrontations(
                competition,
                stage,
                competitionStages))
        {
            return DisabledNoExploitableInitialConfrontations;
        }

        if (StructureDrawConsumptionGate.StageHasObsoleteDrawForConsumption(stage))
        {
            return DisabledDrawObsoleteForConsumption;
        }

        if (CupReadyLacksDeterminedConfrontations(stage, matchSummaries))
        {
            return DisabledNoDeterminedConfrontations;
        }

        var format = StructureSkeleton.InferFormat(stage);
        return format == StructureFormatKind.Cup && !IsInitialRoundPlayable(stage)
            ? DisabledInitialRoundNotPlayable
            : null;
    }

    private static bool CupReadyLacksDeterminedConfrontations(
        Stage stage,
        IReadOnlyList<MatchSummaryRow> matchSummaries)
    {
        if (StructureSkeleton.InferFormat(stage) != StructureFormatKind.Cup
            || stage.Status is not StageStatus.Ready)
        {
            return false;
        }

        if (stage.BracketPairs.Count == 0)
        {
            return false;
        }

        if (matchSummaries.Any(row => row.StageId.Equals(stage.Id)))
        {
            return false;
        }

        foreach (var pair in stage.BracketPairs)
        {
            var slotA = stage.FindSlot(pair.SlotAKey);
            var slotB = stage.FindSlot(pair.SlotBKey);
            if (slotA?.EntryId is { } home
                && slotB?.EntryId is { } away
                && !home.Equals(away))
            {
                return false;
            }
        }

        return true;
    }

    private static string? TryGetProductionStartGap(
        Competition competition,
        Stage stage,
        IReadOnlyList<MatchSummaryRow> matchSummaries,
        IReadOnlyList<Stage>? competitionStages)
    {
        if (stage.Status is not StageStatus.Ready)
        {
            return null;
        }

        var stages = competitionStages ?? [stage];
        var format = StructureSkeleton.InferFormat(stage);
        return (format == StructureFormatKind.Groups && GroupsPlacementReadiness.BlocksProduction(stage)) || StageConfrontationMaterializationState.CountEligibleUnmaterialized(competition, stage, matchSummaries, competitionStages) > 0 || CupReadyEntryRoundMatchProductionIncomplete(stage) ? DisabledUnmaterializedConfrontations : format switch { StructureFormatKind.Championship when !ChampionshipSwissPhasePlaces.IsRosterComplete(competition, stage, stages) => DisabledPhaseRosterIncomplete, _ => null };
    }

    private static bool BracketPairPlacementStarted(Stage stage, BracketPair pair)
    {
        var slotA = stage.FindSlot(pair.SlotAKey)?.EntryId;
        var slotB = stage.FindSlot(pair.SlotBKey)?.EntryId;
        return slotA is not null || slotB is not null;
    }

    private static bool CupReadyEntryRoundMatchProductionIncomplete(Stage stage)
    {
        if (StructureSkeleton.InferFormat(stage) != StructureFormatKind.Cup
            || stage.Status is not StageStatus.Ready
            || stage.BracketPairs.Count == 0)
        {
            return false;
        }

        var legs = TieFormat.OrDefaultOneLeg(stage.Regulation.TieFormat).NumberOfLegs;
        if (legs < 1)
        {
            legs = 1;
        }

        var startedPairCount = stage.BracketPairs.Count(pair => BracketPairPlacementStarted(stage, pair));
        if (startedPairCount == 0)
        {
            return false;
        }

        var targetMatchSlots = startedPairCount * legs;
        return StructureSkeleton.CountAttachedMatches(stage) < targetMatchSlots;
    }

    private static void ThrowStartBlockedForProductionGap(string productionGap) =>
        throw productionGap switch
        {
            DisabledUnmaterializedConfrontations => new ApplicationFailureException(
                "Start is blocked because eligible confrontations are not fully materialized.",
                ApplicationErrorCodes.StartBlockedUnmaterialized),
            DisabledPhaseRosterIncomplete => new ApplicationFailureException(
                "Start is blocked because the phase composition roster is incomplete.",
                ApplicationErrorCodes.StartBlockedPhaseRosterIncomplete),
            DisabledGroupsNotReady => new ApplicationFailureException(
                "Start is blocked because groups are not ready for this phase.",
                ApplicationErrorCodes.StartBlockedGroupsNotReady),
            _ => new ApplicationFailureException(
                "Start is blocked because production prerequisites are not met.",
                ApplicationErrorCodes.StartBlockedUnmaterialized)
        };

    internal static IReadOnlyList<MatchSummaryRow> ToSummaryRows(IReadOnlyList<Match> stageMatches) =>
    [
        .. stageMatches
            .Select(match => new MatchSummaryRow(
                match.Id,
                match.StageId,
                match.Status,
                match.HomeEntryId,
                match.AwayEntryId,
                match.Result,
                match.HasObservedLive))
    ];

    private static bool IsInitialRoundPlayable(Stage stage)
    {
        if (stage.Rounds.Count == 0)
        {
            return true;
        }

        var hasSlottedFixtures = stage.Rounds
            .SelectMany(round => round.Fixtures)
            .Any(fixture => fixture.SlotAKey is not null || fixture.SlotBKey is not null);
        if (!hasSlottedFixtures)
        {
            return true;
        }

        var initialRound = stage.Rounds[0];
        return initialRound.Fixtures.Any(fixture => IsFixturePlayable(stage, fixture));
    }

    private static bool IsFixturePlayable(Stage stage, Fixture fixture)
    {
        if (fixture.SlotAKey is null || fixture.SlotBKey is null)
        {
            return false;
        }

        var slotA = stage.Slots.FirstOrDefault(slot => slot.SlotKey == fixture.SlotAKey);
        var slotB = stage.Slots.FirstOrDefault(slot => slot.SlotKey == fixture.SlotBKey);
        return slotA?.EntryId is not null && slotB?.EntryId is not null;
    }
}

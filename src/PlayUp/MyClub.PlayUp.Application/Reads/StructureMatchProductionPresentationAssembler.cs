// -----------------------------------------------------------------------
// <copyright file="StructureMatchProductionPresentationAssembler.cs" company="Stéphane ANDRE">
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
/// Presents Matchs production for Structure workspace (counts, severity, CTAs) from existing materialize reads.
/// </summary>
internal static class StructureMatchProductionPresentationAssembler
{
    internal const string GaugeModeMaterialized = "materialized";
    internal const string GaugeModeDetermined = "determined";

    internal const string MessageKindInfo = "info";
    internal const string MessageKindSuccess = "success";
    internal const string MessageKindAttention = "attention";
    internal const string MessageKindDanger = "danger";

    internal sealed record Input(
        Competition Competition,
        Stage Stage,
        IReadOnlyList<Stage> CompetitionStages,
        StructureFormatKind? FormatKind,
        StructureMaterializeDto? Materialize,
        StructureDematerializeDto? Dematerialize,
        int AttachedMatchCount,
        int? NumberOfLegs,
        StructureTieFormatSummaryDto? DefaultTieFormat,
        int? CompositionCapacity,
        int CompositionBandResolvedCount,
        PhaseRosterPresentationKind? PhaseRosterPresentation,
        StructureStageLifecycleDto? Lifecycle,
        bool HasDrawRules,
        StructureDrawExecutionBadge? DrawExecutionBadge,
        StructureInboundCompositionDto? InboundComposition,
        IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>>? MatchSummariesByStage = null);

    internal static StructureMatchProductionPresentationDto? Assemble(Input input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Competition);
        ArgumentNullException.ThrowIfNull(input.Stage);
        ArgumentNullException.ThrowIfNull(input.CompetitionStages);

        if (input.FormatKind == StructureFormatKind.Swiss)
        {
            return null;
        }

        if (input.Materialize is null && input.Dematerialize is null)
        {
            return null;
        }

        var presentation = ClassifyMaterializePresentation(
            input.Materialize,
            input.Competition.Status,
            input.Stage.Status);
        var productionVisible = ResolveProductionVisible(input, presentation);
        if (!productionVisible)
        {
            return null;
        }

        var legs = StructureMatchProductionCapacity.ResolveLegsPerConfrontation(
            input.DefaultTieFormat,
            input.NumberOfLegs);
        var materialize = input.Materialize;
        var rosterBlocks = materialize is not null
                           && StructureMatchProductionCapacity.PhaseRosterBlocksProduction(
                               materialize,
                               input.FormatKind,
                               input.PhaseRosterPresentation,
                               input.Stage.Status);

        var (primary, primaryMode) = ResolvePrimaryGauge(
            input,
            materialize,
            legs,
            rosterBlocks,
            presentation);
        var prerequisite = ResolvePrerequisiteGauge(input, materialize, rosterBlocks);
        var followUp = ResolveFollowUp(input.FormatKind, input.Stage, materialize, presentation);
        var materializeAction = ResolveMaterializeAction(input, materialize, legs, presentation);
        var dematerializeAction = ResolveDematerializeAction(input);
        var (showPrimaryGauge, frozenPartial, materializePresentation, dematerializePresentation) =
            ApplyExploitationPresentation(
                input,
                presentation,
                materialize,
                primary,
                materializeAction,
                dematerializeAction);

        return new StructureMatchProductionPresentationDto(
            productionVisible,
            showPrimaryGauge,
            primary,
            primaryMode,
            prerequisite,
            frozenPartial,
            followUp,
            materializePresentation,
            dematerializePresentation);
    }

    private static (
        bool ShowPrimaryGauge,
        StructureMatchProductionFrozenPartialDto? FrozenPartial,
        StructureMatchProductionMaterializeActionDto Materialize,
        StructureMatchProductionDematerializeActionDto Dematerialize) ApplyExploitationPresentation(
            Input input,
            MaterializePresentationKind presentation,
            StructureMaterializeDto? materialize,
            StructureMatchProductionGaugeDto primary,
            StructureMatchProductionMaterializeActionDto materializeAction,
            StructureMatchProductionDematerializeActionDto dematerializeAction)
    {
        if (input.Stage.Status is not (StageStatus.Running or StageStatus.Suspended))
        {
            return (true, null, materializeAction, dematerializeAction);
        }

        var frozenPartial = TryResolveFrozenPartial(presentation, materialize, primary);
        return (
            false,
            frozenPartial,
            materializeAction with { Visible = false },
            dematerializeAction with { Visible = false });
    }

    private static StructureMatchProductionFrozenPartialDto? TryResolveFrozenPartial(
        MaterializePresentationKind presentation,
        StructureMaterializeDto? materialize,
        StructureMatchProductionGaugeDto primary) =>
        materialize is null
        || presentation != MaterializePresentationKind.Frozen
        || materialize.DisabledReason != StructureMaterializeDisabledReasons.StructureNotMutable
        || primary.Capacity <= 0
        || primary.Current >= primary.Capacity
            ? null
            : new StructureMatchProductionFrozenPartialDto(
                primary.Current,
                primary.Capacity,
                StructureMatchProductionMessageCodes.FrozenPartial,
                MessageKindAttention);

    private static bool ResolveProductionVisible(Input input, MaterializePresentationKind presentation)
    {
        if (input.Stage.Status is StageStatus.Draft or StageStatus.Completed)
        {
            return false;
        }

        if (input.Materialize is null && input.Dematerialize is null)
        {
            return false;
        }

        if (input.Stage.Status == StageStatus.Ready)
        {
            return true;
        }

        if (input.Stage.Status is not (StageStatus.Running or StageStatus.Suspended))
            return presentation == MaterializePresentationKind.Actionable || IsDematerializePresentationVisible(input);
        if (input.AttachedMatchCount > 0)
        {
            return true;
        }

        if (presentation == MaterializePresentationKind.Actionable)
        {
            return true;
        }

        if (IsDematerializePresentationVisible(input))
        {
            return true;
        }

        var materialize = input.Materialize;
        return materialize is not null
               && (materialize.CommandEnabled
                   || materialize.MaterializedCount > 0
                   || materialize.DeterminedCount > materialize.MaterializedCount);
    }

    private static bool IsDematerializePresentationVisible(Input input) =>
        input.AttachedMatchCount > 0
        || input.Dematerialize?.Enabled == true;

    private static (StructureMatchProductionGaugeDto Gauge, string Mode) ResolvePrimaryGauge(
        Input input,
        StructureMaterializeDto? materialize,
        int legs,
        bool rosterBlocks,
        MaterializePresentationKind presentation)
    {
        if (materialize is null)
        {
            return (
                new StructureMatchProductionGaugeDto(
                    input.AttachedMatchCount,
                    0,
                    StructureMatchProductionMessageCodes.Unavailable,
                    MessageKindInfo),
                GaugeModeMaterialized);
        }

        var capacity = StructureMatchProductionCapacity.ResolveProductionCapacityMatchSlots(
            materialize,
            input.FormatKind,
            legs,
            input.Competition,
            input.Stage,
            input.CompetitionStages);
        var current = input.AttachedMatchCount;
        var (code, kind) = ResolveMaterializedMessage(
            input,
            materialize,
            current,
            capacity,
            rosterBlocks,
            presentation);
        return (
            new StructureMatchProductionGaugeDto(current, capacity, code, kind),
            GaugeModeMaterialized);
    }

    private static (string Code, string Kind) ResolveMaterializedMessage(
        Input input,
        StructureMaterializeDto materialize,
        int current,
        int capacity,
        bool rosterBlocks,
        MaterializePresentationKind presentation)
    {
        if (IsAwaitingInboundFeedResolution(input, materialize) && current <= 0)
        {
            return (
                StructureMatchProductionMessageCodes.NoDeterminedConfrontations,
                MessageKindInfo);
        }

        if (materialize.DisabledReason == StructureMaterializeDisabledReasons.NoDeterminedConfrontations
            && current <= 0)
        {
            return (
                StructureMatchProductionMessageCodes.NoDeterminedConfrontations,
                ResolveNoDeterminedConfrontationsMessageKind(input));
        }

        switch (capacity)
        {
            case <= 0 when current <= 0:
                {
                    if (presentation == MaterializePresentationKind.Blocked
                        && materialize.DisabledReason is { } blocked)
                    {
                        return blocked == StructureMaterializeDisabledReasons.DrawObsoleteForConsumption
                            ? (blocked, MessageKindDanger)
                            : (blocked, rosterBlocks ? MessageKindInfo : MessageKindAttention);
                    }

                    return (StructureMatchProductionMessageCodes.StatusUnknown, MessageKindInfo);
                }

            case > 0 when current >= capacity:
                return (StructureMatchProductionMessageCodes.Complete, MessageKindSuccess);
        }

        if (StructureMatchProductionCapacity.IsGroupsPoolPlacementIncomplete(materialize)
            && !IsAwaitingInboundFeedResolution(input, materialize))
        {
            var groupsBlockedKind =
                StructurePlaceOccupancyIssues.DeferGroupsNotReadyForDraw(
                    input.HasDrawRules,
                    input.DrawExecutionBadge)
                    ? MessageKindInfo
                    : MessageKindAttention;
            return (
                StructureMatchProductionMessageCodes.MatchesBlockedByGroupsPools,
                groupsBlockedKind);
        }

        if (rosterBlocks)
        {
            return (StructureMatchProductionMessageCodes.Partial, MessageKindInfo);
        }

        switch (presentation)
        {
            case MaterializePresentationKind.Blocked
                when materialize.DisabledReason == StructureMaterializeDisabledReasons.DrawObsoleteForConsumption:
                return (materialize.DisabledReason, MessageKindDanger);
            case MaterializePresentationKind.Blocked
                when materialize.DisabledReason == StructureMaterializeDisabledReasons.StructureNotMutable:
                return (materialize.DisabledReason, MessageKindAttention);
        }

        if (materialize.CommandEnabled)
        {
            return (
                StructureMatchProductionMessageCodes.Partial,
                ResolvePartialMessageKind(input, current, capacity, materialize));
        }

        if (presentation == MaterializePresentationKind.Blocked && materialize.DisabledReason is { } reason)
        {
            return (reason, MessageKindAttention);
        }

        return (
            StructureMatchProductionMessageCodes.Partial,
            ResolvePartialMessageKind(input, current, capacity, materialize));
    }

    private static string ResolvePartialMessageKind(
        Input input,
        int current,
        int capacity,
        StructureMaterializeDto? materialize = null)
    {
        if (capacity <= current || input is
            {
                HasDrawRules: true, DrawExecutionBadge: StructureDrawExecutionBadge.ToLaunch
                or StructureDrawExecutionBadge.InProgress
                or StructureDrawExecutionBadge.ToApply
            })
        {
            return MessageKindInfo;
        }

        if (input is { HasDrawRules: true, DrawExecutionBadge: StructureDrawExecutionBadge.Applied })
        {
            return MessageKindDanger;
        }

        materialize ??= input.Materialize;
        return StructurePlaceOccupancyIssues.IsSlotFeedAwaitingDraw(
                input.HasDrawRules,
                input.DrawExecutionBadge)
            && input.FormatKind == StructureFormatKind.Cup
            ? MessageKindInfo
            : input is { FormatKind: StructureFormatKind.Cup, Stage.Status: StageStatus.Ready }
            && materialize?.CommandCode == OverviewAssembler.ActionMaterializeFromOccupiedSlots
            && capacity > current
            ? MessageKindDanger
            : current == 0 ? MessageKindInfo : MessageKindAttention;
    }

    private static string ResolveNoDeterminedConfrontationsMessageKind(Input input) =>
        input is
        {
            HasDrawRules: true, DrawExecutionBadge: StructureDrawExecutionBadge.ToLaunch
            or StructureDrawExecutionBadge.InProgress
            or StructureDrawExecutionBadge.ToApply
        }
        || (input.FormatKind == StructureFormatKind.Groups
        && StructurePlaceOccupancyIssues.DeferGroupsNotReadyForDraw(
            input.HasDrawRules,
            input.DrawExecutionBadge))
            ? MessageKindInfo
            : input is
            {
                FormatKind: StructureFormatKind.Cup, Stage.Status: StageStatus.Ready, Lifecycle.Transition:
                {
                    Enabled: false,
                    DisabledReasonCode: StructureMaterializeDisabledReasons.NoDeterminedConfrontations
                }
            }

                ? MessageKindDanger
                : IsAwaitingInboundFeedResolution(input, input.Materialize) ? MessageKindInfo : MessageKindAttention;

    private static bool IsAwaitingInboundFeedResolution(
        Input input,
        StructureMaterializeDto? materialize) =>
        MatchProductionInboundFeedDeferral.IsAwaitingResolution(
            input.InboundComposition,
            materialize?.DeterminedCount ?? 0);

    private static StructureMatchProductionGaugeDto? ResolvePrerequisiteGauge(
        Input input,
        StructureMaterializeDto? materialize,
        bool rosterBlocks)
    {
        if (materialize is null)
        {
            return null;
        }

        if (TryResolveCupEntryRoundPrerequisite(input, materialize) is { } cupEntryRound)
        {
            return cupEntryRound;
        }

        if (rosterBlocks
            && input.CompositionCapacity is > 0)
        {
            var resolved = Math.Min(input.CompositionBandResolvedCount, input.CompositionCapacity.Value);
            return new StructureMatchProductionGaugeDto(
                resolved,
                input.CompositionCapacity.Value,
                StructureMatchProductionMessageCodes.PrerequisiteComposition,
                MessageKindAttention);
        }

        if (input.FormatKind == StructureFormatKind.Groups
            && IsAwaitingInboundFeedResolution(input, materialize)
            && input.InboundComposition is { ConfiguredVolume: > 0 } inbound)
        {
            var complete = inbound.ResolvedCount >= inbound.ConfiguredVolume;
            return new StructureMatchProductionGaugeDto(
                inbound.ResolvedCount,
                inbound.ConfiguredVolume,
                StructureMatchProductionMessageCodes.PrerequisiteInboundFeed,
                complete ? MessageKindSuccess : MessageKindInfo);
        }

        if (!StructureMatchProductionCapacity.IsGroupsPoolPlacementIncomplete(materialize)
            || IsAwaitingInboundFeedResolution(input, materialize))
        {
            return null;
        }

        var capacity = materialize.GroupsPlacementCapacityCount;
        var filled = materialize.GroupsPlacementFilledCount;
        if (capacity <= 0) return null;
        var awaitingDraw = StructurePlaceOccupancyIssues.DeferGroupsNotReadyForDraw(
            input.HasDrawRules,
            input.DrawExecutionBadge);
        return new StructureMatchProductionGaugeDto(
            filled,
            capacity,
            StructureMatchProductionMessageCodes.PrerequisiteGroupsPlacement,
            awaitingDraw ? MessageKindInfo : MessageKindAttention);
    }

    private static StructureMatchProductionGaugeDto? TryResolveCupEntryRoundPrerequisite(
        Input input,
        StructureMaterializeDto materialize)
    {
        if (input.FormatKind != StructureFormatKind.Cup
            || input.Stage.Status != StageStatus.Ready
            || materialize.CommandCode != OverviewAssembler.ActionMaterializeFromOccupiedSlots
            || TryCupEntryRoundGauge(input, materialize) is not { } entryRound)
        {
            return null;
        }

        var pairsComplete = entryRound.Filled >= entryRound.Capacity;
        if (pairsComplete)
        {
            return CupReadyEntryRoundMatchesFullyMaterialized(input, materialize)
                ? null
                : TryResolveCupFormResolvedTeamsPrerequisite(input);
        }

        var awaitingDraw = StructurePlaceOccupancyIssues.IsSlotFeedAwaitingDraw(
            input.HasDrawRules,
            input.DrawExecutionBadge);
        var incompleteKind = awaitingDraw
            ? MessageKindInfo
            : MessageKindDanger;
        return new StructureMatchProductionGaugeDto(
            entryRound.Filled,
            entryRound.Capacity,
            StructureMatchProductionMessageCodes.PrerequisiteCupEntryRound,
            incompleteKind,
            CupEntryRoundBlocksPhaseStart(input));
    }

    private static StructureMatchProductionGaugeDto? TryResolveCupFormResolvedTeamsPrerequisite(
        Input input) =>
        CupEntryRoundResolvedTeamGauge.TryMeasure(input) is not { Resolved: var resolved, Capacity: var capacity }
        || resolved >= capacity
            ? null
            : new StructureMatchProductionGaugeDto(
                resolved,
                capacity,
                StructureMatchProductionMessageCodes.PrerequisiteComposition,
                MessageKindInfo);

    private static bool CupReadyEntryRoundMatchesFullyMaterialized(
        Input input,
        StructureMaterializeDto materialize)
    {
        var legs = StructureMatchProductionCapacity.ResolveLegsPerConfrontation(
            input.DefaultTieFormat,
            input.NumberOfLegs);
        var capacity = StructureMatchProductionCapacity.ResolveProductionCapacityMatchSlots(
            materialize,
            input.FormatKind,
            legs,
            input.Competition,
            input.Stage,
            input.CompetitionStages);
        return capacity > 0 && input.AttachedMatchCount >= capacity;
    }

    private static bool CupEntryRoundBlocksPhaseStart(Input input)
    {
        if (StructurePlaceOccupancyIssues.IsSlotFeedAwaitingDraw(
                input.HasDrawRules,
                input.DrawExecutionBadge))
        {
            return false;
        }

        if (input.Stage.Status != StageStatus.Ready)
        {
            return false;
        }

        var transition = input.Lifecycle?.Transition;
        return transition is
        {
            Enabled: false, DisabledReasonCode: StructureStageLifecycleAssembler.DisabledIncompleteEntryRound
            or StructureStageLifecycleAssembler.DisabledNoDeterminedConfrontations
        };
    }

    private static (int Filled, int Capacity)? TryCupEntryRoundGauge(
        Input input,
        StructureMaterializeDto materialize)
    {
        if (CupEntryRoundPairPlacementGauge.TryGetCanonicalEntryRoundDisplayProgress(
                input.Competition,
                input.Stage,
                input.CompetitionStages,
                out var canonicalPairs,
                out var incompletePairs)
            && canonicalPairs > 0)
        {
            return (canonicalPairs - incompletePairs, canonicalPairs);
        }

        if (CupEntryRoundPairPlacementGauge.TryMeasure(
                input.Competition,
                input.Stage,
                input.CompetitionStages) is { } placement)
        {
            return placement;
        }

        if (materialize.Confrontations.Count == 0) return null;
        var pairCapacity = materialize.Confrontations.Count;
        var filled = materialize.Confrontations.Count(row => row.Determined);
        return (filled, pairCapacity);
    }

    private static StructureMatchProductionFollowUpDto? ResolveFollowUp(
        StructureFormatKind? formatKind,
        Stage stage,
        StructureMaterializeDto? materialize,
        MaterializePresentationKind presentation) =>
        formatKind != StructureFormatKind.Cup
        || materialize is null
        || presentation != MaterializePresentationKind.Terminal
            ? null
            : stage.Rounds.Count <= 1
                ? null
                : materialize.LabelVariant != StructureMaterializeAssembler.LabelVariantExhausted
                    ? null
                    : materialize.EligibleCount > 0
                        ? null
                        : new StructureMatchProductionFollowUpDto(
                            StructureMatchProductionMessageCodes.FollowUpExhausted,
                            MessageKindInfo);

    private static StructureMatchProductionMaterializeActionDto ResolveMaterializeAction(
        Input input,
        StructureMaterializeDto? materialize,
        int legs,
        MaterializePresentationKind presentation)
    {
        if (materialize is null)
        {
            return new StructureMatchProductionMaterializeActionDto(
                false,
                false,
                OverviewAssembler.ActionMaterializeMatches,
                StructureMaterializeAssembler.LabelVariantIdle);
        }

        var frozenRemaining = Math.Max(0, materialize.DeterminedCount - materialize.MaterializedCount);
        var confrontationCapacity = ResolveConfrontationCapacityForCta(
            input,
            materialize,
            legs);
        var totalConfrontationCapacity = ResolveTotalConfrontationCapacity(
            input,
            materialize,
            legs);
        var visible = frozenRemaining > 0
                      || (presentation == MaterializePresentationKind.Blocked
                          && StructureMatchProductionCapacity.IsGroupsPoolPlacementIncomplete(materialize))
                      || confrontationCapacity > materialize.MaterializedCount
                      || totalConfrontationCapacity > materialize.MaterializedCount
                      || (materialize.CommandEnabled && confrontationCapacity > 0);

        if (!visible && materialize.CommandEnabled)
        {
            visible = true;
        }

        return new StructureMatchProductionMaterializeActionDto(
            visible,
            materialize.CommandEnabled,
            materialize.CommandCode,
            materialize.LabelVariant,
            materialize.DisabledReason);
    }

    private static StructureMatchProductionDematerializeActionDto ResolveDematerializeAction(Input input)
    {
        var dematerialize = input.Dematerialize;
        return !IsDematerializePresentationVisible(input)
            ? new StructureMatchProductionDematerializeActionDto(false, false, null, null)
            : dematerialize is null
            ? new StructureMatchProductionDematerializeActionDto(true, false, null, null)
            : new StructureMatchProductionDematerializeActionDto(
            true,
            dematerialize.Enabled,
            dematerialize.DisabledReason,
            dematerialize.Scope);
    }

    private enum MaterializePresentationKind
    {
        Actionable,
        Terminal,
        Frozen,
        Blocked
    }

    private static MaterializePresentationKind ClassifyMaterializePresentation(
        StructureMaterializeDto? materialize,
        CompetitionStatus competitionStatus,
        StageStatus stageStatus)
    {
        if (materialize is null)
        {
            return MaterializePresentationKind.Blocked;
        }

        if (materialize.CommandEnabled)
        {
            return MaterializePresentationKind.Actionable;
        }

        var target = materialize.DeterminedCount;
        var materialized = materialize.MaterializedCount;
        return target > 0 && materialized >= target
            ? MaterializePresentationKind.Terminal
            : target > 0
            && materialized < target
            && !IsMaterializeStructureMutable(materialize.CommandCode, competitionStatus, stageStatus)
            ? MaterializePresentationKind.Frozen
            : MaterializePresentationKind.Blocked;
    }

    private static int ResolveConfrontationCapacityForCta(
        Input input,
        StructureMaterializeDto materialize,
        int legs)
    {
        var determined = materialize.DeterminedCount;
        return determined > 0 ? determined : ResolveProspectiveConfrontationCapacity(input, materialize, legs);
    }

    private static int ResolveTotalConfrontationCapacity(
        Input input,
        StructureMaterializeDto materialize,
        int legs) =>
        materialize.Confrontations.Count > 0 ? materialize.Confrontations.Count : ResolveProspectiveConfrontationCapacity(input, materialize, legs);

    private static int ResolveProspectiveConfrontationCapacity(
        Input input,
        StructureMaterializeDto materialize,
        int legs)
    {
        var matchSlots = StructureMatchProductionCapacity.ResolveProductionCapacityMatchSlots(
            materialize,
            input.FormatKind,
            legs,
            input.Competition,
            input.Stage,
            input.CompetitionStages);
        return legs > 0 ? matchSlots / legs : matchSlots;
    }

    private static bool IsMaterializeStructureMutable(
        string commandCode,
        CompetitionStatus competitionStatus,
        StageStatus stageStatus) =>
        commandCode == OverviewAssembler.ActionMaterializeFromOccupiedSlots
            ? competitionStatus is not (
                  CompetitionStatus.Suspended
                  or CompetitionStatus.Completed
                  or CompetitionStatus.Archived)
              && stageStatus is StageStatus.Draft or StageStatus.Ready
            : competitionStatus is CompetitionStatus.Draft or CompetitionStatus.Ready
              && stageStatus is StageStatus.Draft or StageStatus.Ready;
}

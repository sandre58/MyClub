// -----------------------------------------------------------------------
// <copyright file="StructureMatchProductionPresentationAssembler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

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
        StructureDrawExecutionBadge? DrawExecutionBadge);

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
        StructureMatchProductionGaugeDto primary)
    {
        if (materialize is null
            || presentation != MaterializePresentationKind.Frozen
            || materialize.DisabledReason != StructureMaterializeDisabledReasons.StructureNotMutable
            || primary.Capacity <= 0
            || primary.Current >= primary.Capacity)
        {
            return null;
        }

        return new StructureMatchProductionFrozenPartialDto(
            primary.Current,
            primary.Capacity,
            StructureMatchProductionMessageCodes.FrozenPartial,
            MessageKindAttention);
    }

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

        if (input.Stage.Status is StageStatus.Running or StageStatus.Suspended)
        {
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
            if (materialize is not null
                && (materialize.CommandEnabled
                    || materialize.MaterializedCount > 0
                    || materialize.DeterminedCount > materialize.MaterializedCount))
            {
                return true;
            }

            return false;
        }

        if (presentation == MaterializePresentationKind.Actionable)
        {
            return true;
        }

        if (IsDematerializePresentationVisible(input))
        {
            return true;
        }

        return false;
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
        if (materialize.DisabledReason == StructureMaterializeDisabledReasons.NoDeterminedConfrontations
            && current <= 0)
        {
            return (
                StructureMatchProductionMessageCodes.NoDeterminedConfrontations,
                ResolveNoDeterminedConfrontationsMessageKind(input));
        }

        if (capacity <= 0 && current <= 0)
        {
            if (presentation == MaterializePresentationKind.Blocked
                && materialize.DisabledReason is { } blocked)
            {
                if (blocked == StructureMaterializeDisabledReasons.DrawObsoleteForConsumption)
                {
                    return (blocked, MessageKindDanger);
                }

                return (blocked, rosterBlocks ? MessageKindInfo : MessageKindAttention);
            }

            return (StructureMatchProductionMessageCodes.StatusUnknown, MessageKindInfo);
        }

        if (capacity > 0 && current >= capacity)
        {
            return (StructureMatchProductionMessageCodes.Complete, MessageKindSuccess);
        }

        if (StructureMatchProductionCapacity.IsGroupsPoolPlacementIncomplete(materialize))
        {
            return (
                StructureMatchProductionMessageCodes.MatchesBlockedByGroupsPools,
                MessageKindAttention);
        }

        if (rosterBlocks)
        {
            return (StructureMatchProductionMessageCodes.Partial, MessageKindInfo);
        }

        if (presentation == MaterializePresentationKind.Blocked
            && materialize.DisabledReason == StructureMaterializeDisabledReasons.DrawObsoleteForConsumption)
        {
            return (materialize.DisabledReason, MessageKindDanger);
        }

        if (presentation == MaterializePresentationKind.Blocked
            && materialize.DisabledReason == StructureMaterializeDisabledReasons.StructureNotMutable)
        {
            return (materialize.DisabledReason, MessageKindAttention);
        }

        if (materialize.CommandEnabled)
        {
            return (
                StructureMatchProductionMessageCodes.Partial,
                ResolvePartialMessageKind(input, current, capacity));
        }

        if (presentation == MaterializePresentationKind.Blocked && materialize.DisabledReason is { } reason)
        {
            return (reason, MessageKindAttention);
        }

        return (StructureMatchProductionMessageCodes.Partial, MessageKindAttention);
    }

    private static string ResolvePartialMessageKind(Input input, int current, int capacity)
    {
        if (capacity <= current)
        {
            return MessageKindInfo;
        }

        if (input.HasDrawRules
            && input.DrawExecutionBadge is StructureDrawExecutionBadge.ToLaunch
                or StructureDrawExecutionBadge.InProgress
                or StructureDrawExecutionBadge.ToApply)
        {
            return MessageKindInfo;
        }

        if (input.HasDrawRules
            && input.DrawExecutionBadge == StructureDrawExecutionBadge.Applied)
        {
            return MessageKindDanger;
        }

        return current == 0 ? MessageKindInfo : MessageKindAttention;
    }

    private static string ResolveNoDeterminedConfrontationsMessageKind(Input input)
    {
        if (input.HasDrawRules
            && input.DrawExecutionBadge is StructureDrawExecutionBadge.ToLaunch
                or StructureDrawExecutionBadge.InProgress
                or StructureDrawExecutionBadge.ToApply)
        {
            return MessageKindInfo;
        }

        if (input.FormatKind == StructureFormatKind.Groups
            && StructurePlaceOccupancyIssues.DeferGroupsNotReadyForDraw(
                input.HasDrawRules,
                input.DrawExecutionBadge))
        {
            return MessageKindInfo;
        }

        return MessageKindAttention;
    }

    private static StructureMatchProductionGaugeDto? ResolvePrerequisiteGauge(
        Input input,
        StructureMaterializeDto? materialize,
        bool rosterBlocks)
    {
        if (materialize is null)
        {
            return null;
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

        if (StructureMatchProductionCapacity.IsGroupsPoolPlacementIncomplete(materialize))
        {
            var capacity = materialize.GroupsPlacementCapacityCount;
            var filled = materialize.GroupsPlacementFilledCount;
            if (capacity > 0)
            {
                return new StructureMatchProductionGaugeDto(
                    filled,
                    capacity,
                    StructureMatchProductionMessageCodes.PrerequisiteGroupsPlacement,
                    MessageKindAttention);
            }
        }

        if (materialize.CommandCode == OverviewAssembler.ActionMaterializeFromOccupiedSlots
            && !materialize.CommandEnabled
            && TryCupEntryRoundGauge(materialize) is { } entryRound)
        {
            return new StructureMatchProductionGaugeDto(
                entryRound.Filled,
                entryRound.Capacity,
                StructureMatchProductionMessageCodes.PrerequisiteCupEntryRound,
                MessageKindAttention,
                CupEntryRoundBlocksPhaseStart(input));
        }

        return null;
    }

    private static bool CupEntryRoundBlocksPhaseStart(Input input)
    {
        if (input.Stage.Status != StageStatus.Ready)
        {
            return false;
        }

        var transition = input.Lifecycle?.Transition;
        if (transition is { Enabled: false, DisabledReasonCode: var code }
            && code == StructureStageLifecycleAssembler.DisabledIncompleteEntryRound)
        {
            return true;
        }

        return false;
    }

    private static (int Filled, int Capacity)? TryCupEntryRoundGauge(StructureMaterializeDto materialize)
    {
        var capacity = materialize.CanonicalEntryPairCount;
        var undetermined = materialize.UndeterminedCanonicalEntryPairCount;
        if (capacity > 0 && undetermined > 0)
        {
            return (capacity - undetermined, capacity);
        }

        if (materialize.Confrontations.Count > 0 && materialize.DeterminedCount <= 0 && capacity > 0)
        {
            return (0, capacity);
        }

        return null;
    }

    private static StructureMatchProductionFollowUpDto? ResolveFollowUp(
        StructureFormatKind? formatKind,
        Stage stage,
        StructureMaterializeDto? materialize,
        MaterializePresentationKind presentation)
    {
        if (formatKind != StructureFormatKind.Cup
            || materialize is null
            || presentation != MaterializePresentationKind.Terminal)
        {
            return null;
        }

        if (stage.Rounds.Count <= 1)
        {
            return null;
        }

        if (materialize.LabelVariant != StructureMaterializeAssembler.LabelVariantExhausted)
        {
            return null;
        }

        if (materialize.EligibleCount > 0)
        {
            return null;
        }

        return new StructureMatchProductionFollowUpDto(
            StructureMatchProductionMessageCodes.FollowUpExhausted,
            MessageKindInfo);
    }

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
        var visible = frozenRemaining > 0
                      || (presentation == MaterializePresentationKind.Blocked
                          && StructureMatchProductionCapacity.IsGroupsPoolPlacementIncomplete(materialize))
                      || confrontationCapacity > materialize.MaterializedCount
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
        if (!IsDematerializePresentationVisible(input))
        {
            return new StructureMatchProductionDematerializeActionDto(false, false, null, null);
        }

        if (dematerialize is null)
        {
            return new StructureMatchProductionDematerializeActionDto(true, false, null, null);
        }

        return new StructureMatchProductionDematerializeActionDto(
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
        if (target > 0 && materialized >= target)
        {
            return MaterializePresentationKind.Terminal;
        }

        if (target > 0
            && materialized < target
            && !IsMaterializeStructureMutable(materialize.CommandCode, competitionStatus, stageStatus))
        {
            return MaterializePresentationKind.Frozen;
        }

        return MaterializePresentationKind.Blocked;
    }

    private static int ResolveConfrontationCapacityForCta(
        Input input,
        StructureMaterializeDto materialize,
        int legs)
    {
        var determined = materialize.DeterminedCount;
        if (determined > 0)
        {
            return determined;
        }

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
        StageStatus stageStatus)
    {
        if (commandCode == OverviewAssembler.ActionMaterializeFromOccupiedSlots)
        {
            return competitionStatus is not (
                    CompetitionStatus.Suspended
                    or CompetitionStatus.Completed
                    or CompetitionStatus.Archived)
                   && stageStatus is StageStatus.Draft or StageStatus.Ready;
        }

        return competitionStatus is CompetitionStatus.Draft or CompetitionStatus.Ready
               && stageStatus is StageStatus.Draft or StageStatus.Ready;
    }
}

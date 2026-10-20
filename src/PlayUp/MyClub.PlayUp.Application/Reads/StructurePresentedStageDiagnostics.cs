// -----------------------------------------------------------------------
// <copyright file="StructurePresentedStageDiagnostics.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Canonical presented diagnostic codes per Structure hub phase (Gate 3 parity).
/// Consumed by <see cref="StructureViewAssembler"/>, <see cref="StructurePresentedAttentionCollector"/>,
/// and the Structure SPA via <c>presentedIssueCodes</c>.
/// </summary>
public static class StructurePresentedStageDiagnostics
{
    /// <summary>
    /// Informative presented code when roster gap is solely upstream qual flux not yet produced (A3).
    /// Replaces <see cref="StructureStageLifecycleAssembler.DisabledPhaseRosterIncomplete"/> in
    /// <c>presentedIssueCodes</c> only — Start <c>disabledReasonCode</c> stays PhaseRosterIncomplete.
    /// </summary>
    public const string PresentedAwaitingInboundQualification = "AwaitingInboundQualification";

    private static readonly StringComparer CodeComparer = StringComparer.Ordinal;

    /// <summary>
    /// Ordered distinct presented codes for a phase (empty when <paramref name="stage"/> is completed).
    /// </summary>
    public static IReadOnlyList<string> CollectPresentedIssueCodes(
        StructureViewDto view,
        StructureStageHubSummaryDto stage)
    {
        if (stage.Status == StageStatus.Completed)
        {
            return [];
        }

        var codes = new List<string>(ProjectPresentedStageIssueCodesFromStructure(view, stage));

        var transition = stage.Lifecycle?.Transition;
        if (transition is { Enabled: false, Code: OverviewAssembler.ActionStartStage })
        {
            foreach (var reason in EnumeratePresentedStartTransitionBlockers(stage, transition.DisabledReasonCode))
            {
                AddDistinct(codes, reason);
            }
        }

        if (ReportsCupEntryRoundPairGap(stage))
        {
            AddDistinct(codes, StructureStageLifecycleAssembler.DisabledIncompleteEntryRound);
        }

        if (ReportsGroupsMatchProductionAttentionGap(stage))
        {
            AddDistinct(
                codes,
                StructureStageLifecycleAssembler.DisabledUnmaterializedConfrontations);
        }

        foreach (var detail in view.Readiness.BlockerDetails)
        {
            if (detail.StageId is not { } ownerId || !ownerId.Equals(stage.StageId))
            {
                continue;
            }

            AddDistinct(codes, detail.Code);
        }

        ApplyPhaseRosterPresentationSubstitution(codes, stage);

        return codes;
    }

    private static void ApplyPhaseRosterPresentationSubstitution(
        List<string> codes,
        StructureStageHubSummaryDto stage)
    {
        if (stage.PhaseRosterPresentation != PhaseRosterPresentationKind.AwaitingInboundQualification)
        {
            return;
        }

        codes.RemoveAll(code =>
            code == StructureStageLifecycleAssembler.DisabledPhaseRosterIncomplete);
        AddDistinct(codes, PresentedAwaitingInboundQualification);
    }

    /// <summary>SPA: <c>stagePresentedIssueCodes</c> structure slice (structureIssues + U1).</summary>
    public static IReadOnlyList<string> ProjectPresentedStageIssueCodesFromStructure(
        StructureViewDto view,
        StructureStageHubSummaryDto stage)
    {
        var raw = stage.StructureIssues?.ToList() ?? [];
        if (StructurePopulationVolume.ShouldSynthesizePopulationShortfall(view, stage, raw))
        {
            raw.Add(CompetitionFlowAnalyzer.PopulationShortfall);
        }

        return ProjectPresentedStageIssueCodes(raw, stage);
    }

    /// <summary>SPA: <c>projectPresentedStageIssueCodes</c>.</summary>
    public static IReadOnlyList<string> ProjectPresentedStageIssueCodes(
        IReadOnlyList<string> codes,
        StructureStageHubSummaryDto stage)
    {
        var filtered = codes.ToList();
        if (IsSlotFeedAwaitingDraw(stage))
        {
            filtered.RemoveAll(code => code == CompetitionFlowAnalyzer.SlotFeedMissing);
        }

        if (DeferGroupsNotReadyForDraw(stage))
        {
            filtered.RemoveAll(code =>
                code == StructureStageLifecycleAssembler.DisabledGroupsNotReady);
        }

        if (IsSlotFeedAwaitingDraw(stage))
        {
            filtered.RemoveAll(SuppressCupProductionBlockerForAwaitingDraw);
        }

        if (IsAwaitingInboundFeedResolution(stage))
        {
            filtered.RemoveAll(SuppressMatchProductionBlockerForAwaitingInbound);
        }

        if (filtered.Contains(CompetitionFlowAnalyzer.PopulationShortfall, CodeComparer))
        {
            filtered.RemoveAll(code => code == CompetitionFlowAnalyzer.NonRootUnfed);
        }

        return ProjectPresentedIssueCodes(filtered);
    }

    /// <summary>SPA: <c>projectPresentedIssueCodes</c>.</summary>
    public static IReadOnlyList<string> ProjectPresentedIssueCodes(IReadOnlyList<string> codes) =>
        codes.Contains(CompetitionFlowAnalyzer.AuthoredAwardUnreachable, CodeComparer)
            ? [.. codes.Where(code => code != CompetitionFlowAnalyzer.ExpectedRankUnreachable)]
            : codes;

    /// <summary>SPA: <c>isSlotFeedAwaitingDraw</c>.</summary>
    public static bool IsSlotFeedAwaitingDraw(StructureStageHubSummaryDto stage) =>
        stage is
        {
            HasDrawRules: true, DrawExecutionBadge: not (
            StructureDrawExecutionBadge.ToApply or StructureDrawExecutionBadge.Applied)
        };

    public static bool DeferGroupsNotReadyForDraw(StructureStageHubSummaryDto stage) =>
        StructurePlaceOccupancyIssues.DeferGroupsNotReadyForDraw(
            stage.HasDrawRules,
            stage.DrawExecutionBadge);

    /// <summary>SPA: match production / start blockers deferred while Qual/Prog inbound seats are unresolved.</summary>
    public static bool IsAwaitingInboundFeedResolution(StructureStageHubSummaryDto stage) =>
        MatchProductionInboundFeedDeferral.IsAwaitingResolution(
            stage.InboundComposition,
            stage.Materialize?.DeterminedCount ?? 0);

    /// <summary>SPA: <c>stageHasEngagedMissingPots</c>.</summary>
    public static bool StageHasEngagedMissingPots(StructureStageHubSummaryDto stage) =>
        stage is { FormatKind: StructureFormatKind.Groups, HasDrawRules: true, NumberOfPots: null };

    private static void AddDistinct(List<string> codes, string code)
    {
        if (!codes.Contains(code, CodeComparer))
        {
            codes.Add(code);
        }
    }

    private static bool ReportsGroupsMatchProductionAttentionGap(StructureStageHubSummaryDto stage)
    {
        if (IsAwaitingInboundFeedResolution(stage))
        {
            return false;
        }

        if (stage.Status is not StageStatus.Ready || stage.FormatKind is not StructureFormatKind.Groups)
        {
            return false;
        }

        var materialize = stage.Materialize;
        if (materialize?.CommandEnabled != false)
        {
            return false;
        }

        var capacity = materialize.GroupsPlacementCapacityCount;
        var filled = materialize.GroupsPlacementFilledCount;
        if (capacity <= 0 || filled >= capacity)
        {
            return false;
        }

        var transition = stage.Lifecycle?.Transition;
        return transition is not { Enabled: false, Code: OverviewAssembler.ActionStartStage };
    }

    private static IEnumerable<string> EnumeratePresentedStartTransitionBlockers(
        StructureStageHubSummaryDto stage,
        string? primaryDisabledReasonCode)
    {
        if (!string.IsNullOrWhiteSpace(primaryDisabledReasonCode)
            && primaryDisabledReasonCode is
                StructureStageLifecycleAssembler.DisabledIncompleteEntryRound
                or StructureStageLifecycleAssembler.DisabledNoDeterminedConfrontations
                or StructureStageLifecycleAssembler.DisabledUnmaterializedConfrontations
                or StructureStageLifecycleAssembler.DisabledNoExploitableInitialConfrontations
                or StructureStageLifecycleAssembler.DisabledPhaseRosterIncomplete
                or StructureStageLifecycleAssembler.DisabledGroupsNotReady)
        {
            var suppressForDraw = IsSlotFeedAwaitingDraw(stage)
                                  && SuppressCupProductionBlockerForAwaitingDraw(
                                      primaryDisabledReasonCode);
            var suppressForInbound = IsAwaitingInboundFeedResolution(stage)
                                     && SuppressMatchProductionBlockerForAwaitingInbound(
                                         primaryDisabledReasonCode);
            if (!suppressForDraw
                && !suppressForInbound
                && (primaryDisabledReasonCode
                        != StructureStageLifecycleAssembler.DisabledGroupsNotReady
                    || !DeferGroupsNotReadyForDraw(stage)))
            {
                yield return primaryDisabledReasonCode;
            }
        }

        if (ReportsCupEntryRoundPairGap(stage))
        {
            yield return StructureStageLifecycleAssembler.DisabledIncompleteEntryRound;
        }

        if (ReportsUnmaterializedConfrontationsStartGap(stage, primaryDisabledReasonCode))
        {
            yield return StructureStageLifecycleAssembler.DisabledUnmaterializedConfrontations;
        }
    }

    private static bool SuppressCupProductionBlockerForAwaitingDraw(string code) =>
        code is StructureStageLifecycleAssembler.DisabledIncompleteEntryRound
            or StructureStageLifecycleAssembler.DisabledUnmaterializedConfrontations
            or StructureStageLifecycleAssembler.DisabledNoDeterminedConfrontations;

    private static bool SuppressMatchProductionBlockerForAwaitingInbound(string code) =>
        code is StructureStageLifecycleAssembler.DisabledUnmaterializedConfrontations
            or StructureStageLifecycleAssembler.DisabledNoDeterminedConfrontations
            or StructureStageLifecycleAssembler.DisabledGroupsNotReady;

    private static bool ReportsCupEntryRoundPairGap(StructureStageHubSummaryDto stage)
    {
        if (IsSlotFeedAwaitingDraw(stage))
        {
            return false;
        }

        if (stage.Status is not StageStatus.Ready)
        {
            return false;
        }

        var materialize = stage.Materialize;
        if (materialize?.CommandCode != OverviewAssembler.ActionMaterializeFromOccupiedSlots)
        {
            return false;
        }

        var canonical = materialize.CanonicalEntryPairCount;
        var placementIncomplete = materialize.UndeterminedCanonicalEntryPairCount;
        if (canonical > 0)
        {
            return placementIncomplete > 0;
        }

        return materialize.Confrontations.Any(row => !row.Determined);
    }

    private static bool ReportsUnmaterializedConfrontationsStartGap(
        StructureStageHubSummaryDto stage,
        string? primaryDisabledReasonCode)
    {
        if (IsSlotFeedAwaitingDraw(stage) || IsAwaitingInboundFeedResolution(stage))
        {
            return false;
        }

        switch (primaryDisabledReasonCode)
        {
            case StructureStageLifecycleAssembler.DisabledPhaseRosterIncomplete
                or StructureStageLifecycleAssembler.DisabledGroupsNotReady:
                return false;
            case StructureStageLifecycleAssembler.DisabledUnmaterializedConfrontations:
                return true;
        }

        if (stage.Status is not StageStatus.Ready)
        {
            return false;
        }

        var materialize = stage.Materialize;
        if (materialize is null)
        {
            return false;
        }

        if (materialize.CommandCode == OverviewAssembler.ActionMaterializeFromOccupiedSlots
            && stage.MatchProductionPresentation?.PrimaryGauge is { } primary
            && primary.Capacity > primary.Current)
        {
            return true;
        }

        return (materialize.EligibleCount != 0
                || materialize.MaterializedCount < materialize.DeterminedCount)
               && (materialize.EligibleCount > 0
                   || materialize.DeterminedCount > materialize.MaterializedCount);
    }
}

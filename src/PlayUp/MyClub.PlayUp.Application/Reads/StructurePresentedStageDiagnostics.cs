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

        return codes;
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
                or StructureStageLifecycleAssembler.DisabledUnmaterializedConfrontations
                or StructureStageLifecycleAssembler.DisabledNoExploitableInitialConfrontations
                or StructureStageLifecycleAssembler.DisabledPhaseRosterIncomplete
                or StructureStageLifecycleAssembler.DisabledGroupsNotReady)
        {
            if (primaryDisabledReasonCode
                    != StructureStageLifecycleAssembler.DisabledGroupsNotReady
                || !DeferGroupsNotReadyForDraw(stage))
            {
                yield return primaryDisabledReasonCode;
            }
        }

        if (ReportsCanonicalEntryRoundStartGap(stage))
        {
            yield return StructureStageLifecycleAssembler.DisabledIncompleteEntryRound;
        }

        if (ReportsUnmaterializedConfrontationsStartGap(stage, primaryDisabledReasonCode))
        {
            yield return StructureStageLifecycleAssembler.DisabledUnmaterializedConfrontations;
        }
    }

    private static bool ReportsCanonicalEntryRoundStartGap(StructureStageHubSummaryDto stage)
    {
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
        var undetermined = materialize.UndeterminedCanonicalEntryPairCount;
        return canonical > 0 && undetermined > 0;
    }

    private static bool ReportsUnmaterializedConfrontationsStartGap(
        StructureStageHubSummaryDto stage,
        string? primaryDisabledReasonCode)
    {
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
        return materialize is not null && (materialize.EligibleCount != 0
                                            || materialize.MaterializedCount < materialize.DeterminedCount) && (materialize.EligibleCount > 0
            || materialize.DeterminedCount > materialize.MaterializedCount);
    }
}

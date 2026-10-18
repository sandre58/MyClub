// -----------------------------------------------------------------------
// <copyright file="StructurePresentedAttentionCollector.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Projects Structure hub diagnostics onto <c>GET /attention</c> using the same
/// presented rules as the Structure PageHead (SPA <c>stagePresentedIssueCodes</c>).
/// Includes U1 synthetic <c>PopulationShortfall</c> via <see cref="StructurePopulationVolume"/>.
/// </summary>
public static class StructurePresentedAttentionCollector
{
    private static readonly StringComparer CodeComparer = StringComparer.Ordinal;

    /// <summary>Collects presented structure anomalies for the attention catalogue (D1 revision).</summary>
    public static void Collect(StructureViewDto structureView, IList<NeedsAttentionItemDto> items)
    {
        ArgumentNullException.ThrowIfNull(structureView);
        ArgumentNullException.ThrowIfNull(items);

        if (structureView.Stages.Count == 0)
        {
            return;
        }

        foreach (var stage in structureView.Stages)
        {
            if (stage.Status == StageStatus.Completed)
            {
                continue;
            }

            StagePresentedIssueCodes(structureView, stage)
                .Where(code => !IsExcludedSource(code) && ShouldEmitAttention(code, stage, structureView))
                .Select(code => CreateStageItem(code, stage, structureView)).ToList().ForEach(items.Add);

            if (StageHasEngagedMissingPots(stage))
            {
                items.Add(CreateStageItem(
                    StructureViewAssembler.BlockerMissingPotRules,
                    stage,
                    structureView,
                    NeedsAttentionAssembler.SeverityBlocking));
            }

            CollectStartTransitionBlockers(stage, items);
        }

        if (structureView.Readiness.Blockers.Contains(StructureViewAssembler.BlockerCupBracketInvalid))
        {
            items.Add(new NeedsAttentionItemDto(
                StructureViewAssembler.BlockerCupBracketInvalid,
                NeedsAttentionAssembler.SeverityBlocking,
                "Competition",
                structureView.CompetitionId.ToString(),
                new Dictionary<string, string>
                {
                    ["stageId"] = structureView.Format.PrimaryStageId?.ToString()
                        ?? string.Empty
                }));
        }
    }

    private static bool ShouldEmitAttention(
        string code,
        StructureStageHubSummaryDto stage,
        StructureViewDto view) =>
        StructureDiagnosticPresentation.ResolveSeverity(code, stage, view)
            != StructureDiagnosticPresentation.PresentationSeverity.Hidden;

    private static void CollectStartTransitionBlockers(
        StructureStageHubSummaryDto stage,
        IList<NeedsAttentionItemDto> items)
    {
        var transition = stage.Lifecycle?.Transition;
        if (transition is not { Enabled: false, Code: OverviewAssembler.ActionStartStage })
        {
            return;
        }

        var emitted = new HashSet<string>(StringComparer.Ordinal);
        foreach (var reason in EnumeratePresentedStartTransitionBlockers(stage, transition.DisabledReasonCode))
        {
            if (!emitted.Add(reason))
            {
                continue;
            }

            items.Add(
                new NeedsAttentionItemDto(
                    reason,
                    NeedsAttentionAssembler.SeverityBlocking,
                    "Stage",
                    stage.StageId.ToString(),
                    new Dictionary<string, string> { ["stageId"] = stage.StageId.ToString() },
                    Problem: OrganizerProblemSemanticsResolver.TryFromStartTransitionBlocker(reason)));
        }
    }

    private static IEnumerable<string> EnumeratePresentedStartTransitionBlockers(
        StructureStageHubSummaryDto stage,
        string? primaryDisabledReasonCode)
    {
        if (!string.IsNullOrWhiteSpace(primaryDisabledReasonCode)
            && primaryDisabledReasonCode is
                StructureStageLifecycleAssembler.DisabledIncompleteEntryRound
                or StructureStageLifecycleAssembler.DisabledUnmaterializedConfrontations
                or StructureStageLifecycleAssembler.DisabledNoExploitableInitialConfrontations)
        {
            yield return primaryDisabledReasonCode;
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
        if (primaryDisabledReasonCode
            == StructureStageLifecycleAssembler.DisabledUnmaterializedConfrontations)
        {
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

        return materialize.EligibleCount > 0
               || materialize.DeterminedCount > materialize.MaterializedCount;
    }

    private static bool IsExcludedSource(string code) =>
        code is StructureViewAssembler.BlockerMissingStage
            or StructureViewAssembler.BlockerMissingStructure
            or StructureViewAssembler.BlockerStructureGraphInvalid;

    private static IReadOnlyList<string> StagePresentedIssueCodes(
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
    internal static IReadOnlyList<string> ProjectPresentedStageIssueCodes(
        IReadOnlyList<string> codes,
        StructureStageHubSummaryDto stage)
    {
        var filtered = codes.ToList();
        if (IsSlotFeedAwaitingDraw(stage))
        {
            filtered.RemoveAll(code => code == CompetitionFlowAnalyzer.SlotFeedMissing);
        }

        return ProjectPresentedIssueCodes(filtered);
    }

    /// <summary>SPA: <c>projectPresentedIssueCodes</c>.</summary>
    internal static IReadOnlyList<string> ProjectPresentedIssueCodes(IReadOnlyList<string> codes) => codes.Contains(CompetitionFlowAnalyzer.AuthoredAwardUnreachable, CodeComparer) ? [.. codes.Where(code => code != CompetitionFlowAnalyzer.ExpectedRankUnreachable)] : codes;

    /// <summary>SPA: <c>isSlotFeedAwaitingDraw</c>.</summary>
    internal static bool IsSlotFeedAwaitingDraw(StructureStageHubSummaryDto stage) =>
        stage is
        {
            HasDrawRules: true, DrawExecutionBadge: not (
            StructureDrawExecutionBadge.ToApply or StructureDrawExecutionBadge.Applied)
        };

    /// <summary>SPA: <c>stageHasEngagedMissingPots</c>.</summary>
    internal static bool StageHasEngagedMissingPots(StructureStageHubSummaryDto stage) =>
        stage is { FormatKind: StructureFormatKind.Groups, HasDrawRules: true, NumberOfPots: null };

    private static NeedsAttentionItemDto CreateStageItem(
        string source,
        StructureStageHubSummaryDto stage,
        StructureViewDto view,
        string? severityOverride = null)
    {
        var severity = severityOverride
            ?? StructureDiagnosticPresentation.MapSeverityToAttention(
                StructureDiagnosticPresentation.ResolveSeverity(source, stage, view));

        return new NeedsAttentionItemDto(
            source,
            severity,
            "Stage",
            stage.StageId.ToString(),
            new Dictionary<string, string>
            {
                ["stageId"] = stage.StageId.ToString()
            },
            Problem: OrganizerProblemSemanticsResolver.TryFromStructureIssue(source));
    }
}

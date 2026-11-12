// -----------------------------------------------------------------------
// <copyright file="StructurePresentedAttentionCollector.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Projects Structure hub diagnostics onto <c>GET /attention</c> using
/// <see cref="StructurePresentedStageDiagnostics"/> (same codes as <c>presentedIssueCodes</c>).
/// </summary>
public static class StructurePresentedAttentionCollector
{
    /// <summary>Collects presented structure anomalies for the attention catalogue.</summary>
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

            foreach (var code in stage.PresentedIssueCodes
                         ?? StructurePresentedStageDiagnostics.CollectPresentedIssueCodes(structureView, stage))
            {
                if (IsExcludedSource(code) || !ShouldEmitAttention(code, stage, structureView))
                {
                    continue;
                }

                items.Add(CreateStageItem(code, stage, structureView));
            }

            CollectResolutionFreshness(stage, items);
        }
    }

    private static void CollectResolutionFreshness(
        StructureStageHubSummaryDto stage,
        IList<NeedsAttentionItemDto> items)
    {
        var occupants = stage.ResolutionOccupants;
        if (occupants is null || occupants.Count == 0)
        {
            return;
        }

        var perime = 0;
        var indetermine = 0;
        foreach (var occupant in occupants)
        {
            switch (occupant.State)
            {
                case StructureResolutionFreshness.Perime:
                    perime++;
                    break;
                case StructureResolutionFreshness.Indetermine:
                    indetermine++;
                    break;
            }
        }

        if (perime > 0)
        {
            items.Add(FreshnessItem(NeedsAttentionAssembler.SourceResolutionPerime, stage, perime));
        }

        if (indetermine > 0)
        {
            items.Add(FreshnessItem(NeedsAttentionAssembler.SourceResolutionIndetermine, stage, indetermine));
        }
    }

    private static NeedsAttentionItemDto FreshnessItem(
        string source,
        StructureStageHubSummaryDto stage,
        int count) =>
        new(
            source,
            NeedsAttentionAssembler.SeverityWarning,
            "Stage",
            stage.StageId.ToString(),
            new Dictionary<string, string>
            {
                ["stageId"] = stage.StageId.ToString(),
                ["count"] = count.ToString(System.Globalization.CultureInfo.InvariantCulture),
            });

    private static bool ShouldEmitAttention(
        string code,
        StructureStageHubSummaryDto stage,
        StructureViewDto view) =>
        code != StructurePresentedStageDiagnostics.PresentedAwaitingInboundQualification && StructureDiagnosticPresentation.ResolveSeverity(code, stage, view)
        != StructureDiagnosticPresentation.PresentationSeverity.Hidden;

    private static bool IsExcludedSource(string code) =>
        code is StructureViewAssembler.BlockerMissingStage
            or StructureViewAssembler.BlockerMissingStructure
            or StructureViewAssembler.BlockerStructureGraphInvalid;

    /// <summary>SPA parity — delegates to <see cref="StructurePresentedStageDiagnostics"/>.</summary>
    internal static IReadOnlyList<string> StagePresentedIssueCodes(
        StructureViewDto view,
        StructureStageHubSummaryDto stage) =>
        StructurePresentedStageDiagnostics.ProjectPresentedStageIssueCodesFromStructure(view, stage);

    /// <summary>SPA: <c>projectPresentedStageIssueCodes</c>.</summary>
    internal static IReadOnlyList<string> ProjectPresentedStageIssueCodes(
        IReadOnlyList<string> codes,
        StructureStageHubSummaryDto stage) =>
        StructurePresentedStageDiagnostics.ProjectPresentedStageIssueCodes(codes, stage);

    /// <summary>SPA: <c>projectPresentedIssueCodes</c>.</summary>
    internal static IReadOnlyList<string> ProjectPresentedIssueCodes(IReadOnlyList<string> codes) =>
        StructurePresentedStageDiagnostics.ProjectPresentedIssueCodes(codes);

    /// <summary>SPA: <c>isSlotFeedAwaitingDraw</c>.</summary>
    internal static bool IsSlotFeedAwaitingDraw(StructureStageHubSummaryDto stage) =>
        StructurePresentedStageDiagnostics.IsSlotFeedAwaitingDraw(stage);

    internal static bool DeferGroupsNotReadyForDraw(StructureStageHubSummaryDto stage) =>
        StructurePresentedStageDiagnostics.DeferGroupsNotReadyForDraw(stage);

    /// <summary>
    /// Same shape as <see cref="StructurePresentedStageDiagnostics.StageHasEngagedMissingPots"/>.
    /// The Structure SPA reads MissingPotRules from <c>presentedIssueCodes</c>.
    /// </summary>
    internal static bool StageHasEngagedMissingPots(StructureStageHubSummaryDto stage) =>
        StructurePresentedStageDiagnostics.StageHasEngagedMissingPots(stage);

    private static NeedsAttentionItemDto CreateStageItem(
        string source,
        StructureStageHubSummaryDto stage,
        StructureViewDto view,
        string? severityOverride = null)
    {
        var severity = severityOverride
            ?? StructureDiagnosticPresentation.MapSeverityToAttention(
                StructureDiagnosticPresentation.ResolveSeverity(source, stage, view));

        var problem = OrganizerProblemSemanticsResolver.TryFromStructureIssue(source)
            ?? OrganizerProblemSemanticsResolver.TryFromStartTransitionBlocker(source);

        return new NeedsAttentionItemDto(
            source,
            severity,
            "Stage",
            stage.StageId.ToString(),
            new Dictionary<string, string>
            {
                ["stageId"] = stage.StageId.ToString()
            },
            Problem: problem);
    }
}

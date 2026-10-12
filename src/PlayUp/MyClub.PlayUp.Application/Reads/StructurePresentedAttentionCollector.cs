// -----------------------------------------------------------------------
// <copyright file="StructurePresentedAttentionCollector.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;

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
            StagePresentedIssueCodes(structureView, stage)
                .Where(code => !IsExcludedSource(code))
                .Select(code => CreateStageItem(code, stage.StageId)).ToList().ForEach(items.Add);

            if (StageHasEngagedMissingPots(stage))
            {
                items.Add(CreateStageItem(
                    StructureViewAssembler.BlockerMissingPotRules,
                    stage.StageId,
                    NeedsAttentionAssembler.SeverityBlocking));
            }
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
        Guid stageId,
        string? severityOverride = null) =>
        new(
            source,
            severityOverride ?? ResolveSeverity(source),
            "Stage",
            stageId.ToString(),
            new Dictionary<string, string>
            {
                ["stageId"] = stageId.ToString()
            },
            Problem: OrganizerProblemSemanticsResolver.TryFromStructureIssue(source));

    private static string ResolveSeverity(string source) =>
        SeverityBySource.GetValueOrDefault(source, NeedsAttentionAssembler.SeverityBlocking);

    /// <summary>Workshop default matrix (D1 revision Annex A4 PROPOSED).</summary>
    private static readonly Dictionary<string, string> SeverityBySource =
        new(CodeComparer)
        {
            [CompetitionFlowAnalyzer.OverCapacityEdge] = NeedsAttentionAssembler.SeverityWarning,
            [CompetitionFlowAnalyzer.NonRootUnfed] = NeedsAttentionAssembler.SeverityWarning,
            [CompetitionFlowAnalyzer.AuthoredAwardUnreachable] = NeedsAttentionAssembler.SeverityWarning,
            [CompetitionFlowAnalyzer.ExpectedRankUnreachable] = NeedsAttentionAssembler.SeverityWarning
        };
}

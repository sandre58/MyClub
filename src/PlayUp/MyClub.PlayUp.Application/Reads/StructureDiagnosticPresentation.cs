// -----------------------------------------------------------------------
// <copyright file="StructureDiagnosticPresentation.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Structure diagnostic presentation policy (parity with SPA
/// <c>structureDiagnosticPresentationPolicy.ts</c>). Presentation only — not authorization.
/// </summary>
public static class StructureDiagnosticPresentation
{
    public enum PresentationPhaseBucket
    {
        Draft,
        Ready,
        Running,
        Hidden
    }

    public enum PresentationSeverity
    {
        Hidden,
        Warning,
        Blocking
    }

    public static PresentationPhaseBucket ResolvePhaseBucket(StageStatus status) =>
        status switch
        {
            StageStatus.Ready => PresentationPhaseBucket.Ready,
            StageStatus.Running or StageStatus.Suspended => PresentationPhaseBucket.Running,
            StageStatus.Completed => PresentationPhaseBucket.Hidden,
            _ => PresentationPhaseBucket.Draft
        };

    public static PresentationSeverity ResolveSeverity(
        string code,
        StructureStageHubSummaryDto stage,
        StructureViewDto view)
    {
        var bucket = ResolvePhaseBucket(stage.Status);
        if (bucket == PresentationPhaseBucket.Hidden)
        {
            return PresentationSeverity.Hidden;
        }

        var cell = ResolveMatrixCell(code, bucket, stage, view);
        return cell switch
        {
            MatrixCell.Danger => PresentationSeverity.Blocking,
            _ => PresentationSeverity.Warning
        };
    }

    public static string MapSeverityToAttention(PresentationSeverity severity) =>
        severity == PresentationSeverity.Blocking
            ? NeedsAttentionAssembler.SeverityBlocking
            : NeedsAttentionAssembler.SeverityWarning;

    private enum MatrixCell
    {
        Danger,
        Attention,
        Progress
    }

    private static MatrixCell ResolveMatrixCell(
        string code,
        PresentationPhaseBucket bucket,
        StructureStageHubSummaryDto stage,
        StructureViewDto view) =>
        code is CompetitionFlowAnalyzer.OverCapacityEdge
            or CompetitionFlowAnalyzer.ExpectedRankUnreachable
            ? MatrixCell.Attention
            : code is CompetitionFlowAnalyzer.PopulationShortfall
                or CompetitionFlowAnalyzer.SourcePopulationShortfall
                ? bucket == PresentationPhaseBucket.Ready
                    ? MatrixCell.Danger
                    : MatrixCell.Attention
                : code == CompetitionFlowAnalyzer.SlotFeedMissing
                    ? bucket == PresentationPhaseBucket.Draft
                        ? MatrixCell.Attention
                        : MatrixCell.Danger
                    : code != CompetitionFlowAnalyzer.NonRootUnfed
                    ? code == CompetitionFlowAnalyzer.AuthoredAwardUnreachable
                        ? stage.FormatKind == StructureFormatKind.Cup
                            ? MatrixCell.Danger
                            : MatrixCell.Attention
                        : MatrixCell.Danger
                    : bucket == PresentationPhaseBucket.Draft
                      && ResolveIncompletenessExpectedNonRootUnfed(stage, view)
                        ? MatrixCell.Progress
                        : MatrixCell.Attention;

    internal static bool ResolveIncompletenessExpectedNonRootUnfed(
        StructureStageHubSummaryDto stage,
        StructureViewDto view)
    {
        if (stage.Status != StageStatus.Draft)
        {
            return false;
        }

        var baselineId = view.Format.PrimaryStageId ?? view.Stages.FirstOrDefault()?.StageId;
        return (baselineId is null || !stage.StageId.Equals(baselineId)) && !StageHasInboundQualProgFeed(view, stage.StageId) && stage.AffectationEntryCount <= 0;
    }

    internal static bool StageHasInboundQualProgFeed(StructureViewDto view, Guid destinationStageId)
    {
        foreach (var source in view.Stages)
        {
            if (source.QualificationIntents?.Any(intent => intent.DestinationStageId == destinationStageId) == true)
            {
                return true;
            }

            if (source.ProgressionIntents?.Any(intent => intent.DestinationStageId == destinationStageId) == true)
            {
                return true;
            }
        }

        return false;
    }
}

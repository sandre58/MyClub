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

    private static readonly HashSet<string> DangerMatrixCodes = new(StringComparer.Ordinal)
    {
        CompetitionFlowAnalyzer.CycleDetected,
        CompetitionFlowAnalyzer.RoutingNondeterminism,
        CompetitionFlowAnalyzer.OverExtraction,
        CompetitionFlowAnalyzer.ConfrontationUndecidable,
        CompetitionFlowAnalyzer.SlotFeedMissing,
        CompetitionFlowAnalyzer.SlotFeedInvalid,
        CompetitionFlowAnalyzer.SlotFeedMultiple,
        "DanglingQualificationTarget",
        "DanglingProgressionTarget",
        "MissingQualificationDestinationSlot",
        "MissingQualificationDestinationGroup",
        "MissingProgressionDestinationSlot",
        "MissingProgressionDestinationGroup"
    };

    public static PresentationPhaseBucket ResolvePhaseBucket(StageStatus status) =>
        status switch
        {
            StageStatus.Ready => PresentationPhaseBucket.Ready,
            StageStatus.Running => PresentationPhaseBucket.Running,
            StageStatus.Suspended => PresentationPhaseBucket.Running,
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
        StructureViewDto view)
    {
        if (code is CompetitionFlowAnalyzer.OverCapacityEdge
            or CompetitionFlowAnalyzer.ExpectedRankUnreachable)
        {
            return MatrixCell.Attention;
        }

        if (code is CompetitionFlowAnalyzer.PopulationShortfall
            or CompetitionFlowAnalyzer.SourcePopulationShortfall)
        {
            return bucket == PresentationPhaseBucket.Ready
                ? MatrixCell.Danger
                : MatrixCell.Attention;
        }

        if (code == CompetitionFlowAnalyzer.NonRootUnfed)
        {
            if (bucket == PresentationPhaseBucket.Draft
                && ResolveIncompletenessExpectedNonRootUnfed(stage, view))
            {
                return MatrixCell.Progress;
            }

            return MatrixCell.Attention;
        }

        if (code == CompetitionFlowAnalyzer.AuthoredAwardUnreachable)
        {
            return stage.FormatKind == StructureFormatKind.Cup
                ? MatrixCell.Danger
                : MatrixCell.Attention;
        }

        if (DangerMatrixCodes.Contains(code))
        {
            return MatrixCell.Danger;
        }

        return MatrixCell.Danger;
    }

    internal static bool ResolveIncompletenessExpectedNonRootUnfed(
        StructureStageHubSummaryDto stage,
        StructureViewDto view)
    {
        if (stage.Status != StageStatus.Draft)
        {
            return false;
        }

        var baselineId = view.Format.PrimaryStageId ?? view.Stages.FirstOrDefault()?.StageId;
        if (baselineId is not null && stage.StageId.Equals(baselineId))
        {
            return false;
        }

        if (StageHasInboundQualProgFeed(view, stage.StageId))
        {
            return false;
        }

        if (stage.AffectationEntryCount > 0)
        {
            return false;
        }

        return true;
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

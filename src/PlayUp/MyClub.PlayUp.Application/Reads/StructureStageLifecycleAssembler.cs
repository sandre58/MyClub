// -----------------------------------------------------------------------
// <copyright file="StructureStageLifecycleAssembler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Projects Structure phase lifecycle (transition, match progress, optional revert).
/// </summary>
public static class StructureStageLifecycleAssembler
{
    /// <summary>Prepare checklist — competition entries / affectation.</summary>
    public const string ChecklistEntries = "Entries";

    /// <summary>Prepare checklist — Places N / composition pool.</summary>
    public const string ChecklistPlaces = "Places";

    /// <summary>Prepare checklist — draw execution when DrawRules engaged.</summary>
    public const string ChecklistDraw = "Draw";

    /// <summary>Prepare checklist — match materialization workspace.</summary>
    public const string ChecklistMatches = "Matches";

    /// <summary>Prepare checklist — qualification / progression authoring.</summary>
    public const string ChecklistExits = "Exits";

    /// <summary>Prepare checklist — root affectation / population authoring.</summary>
    public const string ChecklistAttribution = "Attribution";

    /// <summary>Lifecycle disabled — competition Completed / Archived.</summary>
    public const string DisabledCompetitionClosed = "CompetitionClosed";

    /// <summary>Lifecycle disabled — competition Running while phase remains Draft (construction frozen).</summary>
    public const string DisabledConstructionFrozen = "ConstructionFrozen";

    /// <summary>Lifecycle disabled — Cup initial round lacks playable fixtures.</summary>
    public const string DisabledInitialRoundNotPlayable = "InitialRoundNotPlayable";

    /// <summary>Lifecycle disabled — local structure not ready for Prepare.</summary>
    public const string DisabledStageNotReady = "StageNotReady";

    /// <summary>Lifecycle disabled — structure graph invalid for Prepare.</summary>
    public const string DisabledStructureGraphInvalid = "StructureGraphInvalid";

    /// <summary>Revert disabled — matches attached to the stage.</summary>
    public const string DisabledProductionPresent = "ProductionPresent";

    /// <summary>Start disabled — eligible confrontations not fully materialized.</summary>
    public const string DisabledUnmaterializedConfrontations = "UnmaterializedConfrontations";

    /// <summary>Revert disabled — match sportively engaged.</summary>
    public const string DisabledMatchLifecycleStarted = "MatchLifecycleStarted";

    public const string DisabledMatchLiveInProgress = "MatchLiveInProgress";

    /// <summary>
    /// Assembles lifecycle projection for one stage hub row.
    /// </summary>
    public static StructureStageLifecycleDto Assemble(StructureStageLifecycleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var matchProgress = BuildMatchProgress(input.Stage, input.MatchSummaries);
        var transition = ResolveTransition(input, matchProgress);
        var revert = ResolveRevertTransition(input);
        var cancelStart = ResolveCancelStartTransition(input);

        return new StructureStageLifecycleDto(transition, matchProgress, revert, cancelStart);
    }

    private static StructureStageLifecycleTransitionDto ResolveTransition(
        StructureStageLifecycleInput input,
        StructureStageLifecycleMatchProgressDto? matchProgress) =>
        input.Competition.Status is CompetitionStatus.Completed or CompetitionStatus.Archived
            ? BlockedTransition(
                ResolveTransitionCode(input.Stage.Status),
                DisabledCompetitionClosed,
                input,
                remediationCode: DisabledCompetitionClosed)
            : input.Stage.Status == StageStatus.Draft
                ? ResolvePrepareTransition(input)
                : input.Competition.Status == CompetitionStatus.Running
                  && input.Stage.Status == StageStatus.Ready
                  && IsConstructionFrozenWhileCompetitionRunning(input)
                    ? BlockedTransition(
                        OverviewAssembler.ActionStartStage,
                        DisabledConstructionFrozen,
                        input,
                        DisabledConstructionFrozen)
                    : input.Stage.Status switch
                    {
                        StageStatus.Ready => ResolveStartTransition(input),
                        StageStatus.Running or StageStatus.Suspended => ResolveCompleteTransition(input, matchProgress),
                        StageStatus.Completed or StageStatus.Draft => new StructureStageLifecycleTransitionDto(null, false),
                        _ => new StructureStageLifecycleTransitionDto(null, false)
                    };

    /// <summary>
    /// Freezes Prepare and Start on a lone Draft/Ready phase left behind when the competition went live.
    /// Downstream phases (e.g. SF after QF is Running) stay lifecycle-eligible.
    /// </summary>
    private static bool IsConstructionFrozenWhileCompetitionRunning(StructureStageLifecycleInput input) =>
        !input.CompetitionStages.Any(peer =>
            !peer.Id.Equals(input.Stage.Id)
            && peer.Status is StageStatus.Running or StageStatus.Suspended or StageStatus.Completed);

    private static StructureStageLifecycleTransitionDto ResolvePrepareTransition(
        StructureStageLifecycleInput input)
    {
        const string code = OverviewAssembler.ActionPrepareStage;

        if (input.Competition.Status == CompetitionStatus.Running
            && IsConstructionFrozenWhileCompetitionRunning(input))
        {
            return BlockedTransition(
                code,
                DisabledConstructionFrozen,
                input,
                DisabledConstructionFrozen);
        }

        if (!DomainAllowsPrepare(input.Stage, input.FormatKind))
        {
            return BlockedTransition(code, DisabledStageNotReady, input, DisabledStageNotReady);
        }

        if (HasStructureGraphGate(input.StructureIssues))
        {
            return BlockedTransition(
                code,
                DisabledStructureGraphInvalid,
                input,
                input.StructureIssues!.First(CompetitionFlowAnalyzer.IsStructureGraphGateIssue));
        }

        if (StructurePreparePlacesGate.BlocksPrepare(input))
        {
            return BlockedTransition(
                code,
                ChecklistPlaces,
                input,
                ChecklistPlaces);
        }

        if (StructurePrepareAttributionGate.BlocksPrepare(input))
        {
            return BlockedTransition(
                code,
                ChecklistAttribution,
                input,
                ChecklistAttribution);
        }

        var prepareGate = CollectPrepareGateCodes(input.Stage, input.CompetitionStages);
        return prepareGate.Length > 0 ? BlockedTransition(code, prepareGate[0], input, prepareGate[0]) : new StructureStageLifecycleTransitionDto(code, true);
    }

    private static StructureStageLifecycleTransitionDto ResolveStartTransition(StructureStageLifecycleInput input)
    {
        const string code = OverviewAssembler.ActionStartStage;

        return StageStartReadinessGuard.TryGetDisabledReasonCode(
                input.Stage,
                input.Competition,
                input.MatchSummaries,
                input.CompetitionStages) is { } disabledReason
            ? BlockedTransition(code, disabledReason, input, disabledReason)
            : new StructureStageLifecycleTransitionDto(code, true);
    }

    private static StructureStageLifecycleTransitionDto ResolveCompleteTransition(
        StructureStageLifecycleInput input,
        StructureStageLifecycleMatchProgressDto? matchProgress)
    {
        const string code = OverviewAssembler.ActionCompleteStage;
        return matchProgress is { CanComplete: true }
            ? new StructureStageLifecycleTransitionDto(code, true)
            : AttachProblem(
                new StructureStageLifecycleTransitionDto(
                    code,
                    false,
                    CompleteStage.ReasonOpenMatches),
                input);
    }

    private static StructureStageLifecycleTransitionDto? ResolveRevertTransition(StructureStageLifecycleInput input) => input.Competition.Status is CompetitionStatus.Completed or CompetitionStatus.Archived
            ? null
            : input.Stage.Status switch
            {
                StageStatus.Ready => ResolveRevertToDraftTransition(input),
                StageStatus.Running => ResolveSuspendTransition(input),
                StageStatus.Suspended => ResolveResumeTransition(),
                StageStatus.Draft => null,
                StageStatus.Completed => null,
                _ => null
            };

    private static StructureStageLifecycleTransitionDto ResolveRevertToDraftTransition(
        StructureStageLifecycleInput input)
    {
        const string code = OverviewAssembler.ActionRevertToDraftStage;
        return StageAttachedProduction.HasAttachedMatches(input.Stage, input.MatchSummaries)
            ? new StructureStageLifecycleTransitionDto(code, false, DisabledProductionPresent)
            : new StructureStageLifecycleTransitionDto(code, true);
    }

    private static StructureStageLifecycleTransitionDto ResolveSuspendTransition(
        StructureStageLifecycleInput input)
    {
        const string code = OverviewAssembler.ActionSuspendStage;
        var gates = input.MatchSummaries
            .Where(row => row.StageId.Equals(input.Stage.Id))
            .Select(DematerializationMatchGate.FromSummary)
            .ToArray();

        return DematerializationCapability.TrySuspendBlockedByLiveReason(gates) is not null
            ? new StructureStageLifecycleTransitionDto(code, false, DisabledMatchLiveInProgress)
            : new StructureStageLifecycleTransitionDto(code, true);
    }

    private static StructureStageLifecycleTransitionDto ResolveResumeTransition()
    {
        const string code = OverviewAssembler.ActionResumeStage;
        return new StructureStageLifecycleTransitionDto(code, true);
    }

    private static StructureStageLifecycleTransitionDto? ResolveCancelStartTransition(
        StructureStageLifecycleInput input)
    {
        if (input.Stage.Status is not (StageStatus.Running or StageStatus.Suspended))
        {
            return null;
        }

        var gates = input.MatchSummaries
            .Where(row => row.StageId.Equals(input.Stage.Id))
            .Select(DematerializationMatchGate.FromSummary)
            .ToArray();

        return DematerializationCapability.TryMatchLifecycleReason(gates) is not null
            ? new StructureStageLifecycleTransitionDto(
                OverviewAssembler.ActionRevertToReadyStage,
                false,
                DisabledMatchLifecycleStarted)
            : new StructureStageLifecycleTransitionDto(OverviewAssembler.ActionRevertToReadyStage, true);
    }

    private static StructureStageLifecycleTransitionDto BlockedTransition(
        string? code,
        string disabledReasonCode,
        StructureStageLifecycleInput input,
        string remediationCode) =>
        AttachProblem(
            new StructureStageLifecycleTransitionDto(
                code,
                false,
                disabledReasonCode,
                ResolveRemediation(input.Competition.Id, input.Stage.Id, remediationCode)),
            input);

    private static StructureStageLifecycleTransitionDto AttachProblem(
        StructureStageLifecycleTransitionDto dto,
        StructureStageLifecycleInput input)
    {
        var problem = OrganizerProblemSemanticsResolver.TryFromLifecycleTransition(
            dto.Code,
            dto.DisabledReasonCode,
            input.StructureIssues);
        return problem is null ? dto : dto with { Problem = problem };
    }

    private static string? ResolveTransitionCode(StageStatus status) =>
        status switch
        {
            StageStatus.Draft => OverviewAssembler.ActionPrepareStage,
            StageStatus.Ready => OverviewAssembler.ActionStartStage,
            StageStatus.Running or StageStatus.Suspended => OverviewAssembler.ActionCompleteStage,
            StageStatus.Completed => null,
            _ => null
        };

    private static StructureStageLifecycleMatchProgressDto? BuildMatchProgress(
        Stage stage,
        IReadOnlyList<MatchSummaryRow> summaries)
    {
        if (stage.Status is not (StageStatus.Running or StageStatus.Suspended or StageStatus.Completed))
        {
            return null;
        }

        var finished = summaries.Count(row => row.Status == MatchStatus.Finished);
        var cancelled = summaries.Count(row => row.Status == MatchStatus.Cancelled);
        var total = summaries.Count;
        var open = total - finished - cancelled;
        var canComplete = stage.Status is StageStatus.Running or StageStatus.Suspended && open == 0;

        return new StructureStageLifecycleMatchProgressDto(total, finished, cancelled, open, canComplete);
    }

    private static bool HasStructureGraphGate(IReadOnlyList<string>? structureIssues) =>
        structureIssues is { Count: > 0 }
        && structureIssues.Any(CompetitionFlowAnalyzer.IsStructureGraphGateIssue);

    private static string[] CollectPrepareGateCodes(Stage target, IReadOnlyList<Stage> competitionStages)
    {
        var execution = CompetitionFlowAnalyzer.AnalyzeExecution([target]);
        var capacity = CompetitionFlowAnalyzer.AnalyzeCapacity(competitionStages);
        var topology = CompetitionFlowAnalyzer.AnalyzeTopology(competitionStages);

        return
        [
            .. execution
                .Concat(capacity)
                .Concat(topology)
                .Where(diagnostic =>
                    diagnostic.StageId == target.Id.Value
                    && CompetitionFlowAnalyzer.IsPrepareGateIssue(diagnostic.Code))
                .Select(diagnostic => diagnostic.Code)
                .Distinct(StringComparer.Ordinal)
        ];
    }

    private static bool DomainAllowsPrepare(Stage stage, StructureFormatKind? formatKind) =>
        formatKind == StructureFormatKind.Swiss
            ? stage.SwissSettings is not null
            : stage.Groups.Count > 0
                ? stage.Matchdays.Count > 0
                : stage.Rounds.Count > 0 || stage.Matchdays.Count > 0;

    private static (string? Section, string? Focus, bool Manual, bool OpenDraw) MapRemediationDeepLink(
        string remediationCode)
    {
        switch (remediationCode)
        {
            case ChecklistDraw or StructureViewAssembler.BlockerMissingPotRules:
                return ("tirage", null, false, true);
            case ChecklistPlaces or DisabledInitialRoundNotPlayable:
                return (null, "operational-placement", true, false);
            case ChecklistMatches or DisabledUnmaterializedConfrontations:
                return ("matchs", null, false, false);
            case ChecklistExits or StructureViewAssembler.IssueConfrontationUndecidable:
                return ("qualification", null, false, false);
            case ChecklistAttribution:
                return (null, "rail-attribution", false, false);
            case ChecklistEntries:
                return (null, "rail-entries", false, false);
            case DisabledStructureGraphInvalid:
                return ("construction", null, false, false);
            case DisabledConstructionFrozen or DisabledCompetitionClosed:
                return (null, null, false, false);
            default:
                break;
        }

        if (remediationCode.Contains("Qualification", StringComparison.Ordinal))
        {
            return ("qualification", null, false, false);
        }

        if (remediationCode.Contains("Progression", StringComparison.Ordinal))
        {
            return ("progression", null, false, false);
        }

        return ("construction", null, false, false);
    }

    private static StructureStageLifecycleRemediationDto? ResolveRemediation(
        CompetitionId competitionId,
        StageId stageId,
        string remediationCode)
    {
        var (section, focus, manual, openDraw) = MapRemediationDeepLink(remediationCode);

        if (section is null && focus is null && !manual && !openDraw
            && remediationCode is DisabledConstructionFrozen or DisabledCompetitionClosed)
        {
            return null;
        }

        var href = StructureLifecycleDeepLinks.Build(
            competitionId,
            stageId,
            section,
            focus,
            manual,
            openDraw);
        return new StructureStageLifecycleRemediationDto(href, remediationCode);
    }
}

/// <summary>
/// Inputs for <see cref="StructureStageLifecycleAssembler"/> (computed by <see cref="StructureViewAssembler"/>).
/// </summary>
public sealed record StructureStageLifecycleInput(
    Competition Competition,
    Stage Stage,
    IReadOnlyList<Stage> CompetitionStages,
    IReadOnlyList<string>? StructureIssues,
    IReadOnlyList<MatchSummaryRow> MatchSummaries,
    StructureFormatKind? FormatKind,
    bool HasDrawRules,
    bool HasQualificationRules,
    bool HasProgressionRules,
    bool IsRootComposition,
    int CompositionEntryCount,
    int? CompositionCapacity,
    int AffectationEntryCount,
    StructureDrawExecutionBadge? DrawExecutionBadge,
    StructureMaterializeDto? Materialize);

/// <summary>
/// Relative Structure hub URLs aligned with the SPA deep-link contract.
/// </summary>
internal static class StructureLifecycleDeepLinks
{
    internal static string Build(
        CompetitionId competitionId,
        StageId stageId,
        string? section,
        string? focus,
        bool manual,
        bool openDraw)
    {
        var query = new List<string> { $"stage={stageId.Value}" };
        if (!string.IsNullOrEmpty(section))
        {
            query.Add($"section={section}");
        }

        if (!string.IsNullOrEmpty(focus))
        {
            query.Add($"focus={focus}");
        }

        if (manual)
        {
            query.Add("manual=1");
        }

        if (openDraw)
        {
            query.Add("draw=1");
        }

        return $"/competitions/{competitionId.Value}/structure?{string.Join('&', query)}";
    }
}

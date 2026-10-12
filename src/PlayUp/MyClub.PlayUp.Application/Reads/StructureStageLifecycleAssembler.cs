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
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Projects Structure phase lifecycle (transition, Prepare checklist, match progress).
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

    /// <summary>
    /// Assembles lifecycle projection for one stage hub row.
    /// </summary>
    public static StructureStageLifecycleDto Assemble(StructureStageLifecycleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var matchProgress = BuildMatchProgress(input.Stage, input.MatchSummaries);
        var checklist = input.Stage.Status == StageStatus.Draft
            ? BuildPrepareChecklist(input)
            : null;
        var transition = ResolveTransition(input, matchProgress);

        return new StructureStageLifecycleDto(transition, checklist, matchProgress);
    }

    private static StructureStageLifecycleTransitionDto ResolveTransition(
        StructureStageLifecycleInput input,
        StructureStageLifecycleMatchProgressDto? matchProgress)
    {
        if (input.Competition.Status is CompetitionStatus.Completed or CompetitionStatus.Archived)
        {
            return BlockedTransition(
                ResolveTransitionCode(input.Stage.Status),
                DisabledCompetitionClosed,
                input,
                remediationCode: DisabledCompetitionClosed);
        }

        if (input.Competition.Status == CompetitionStatus.Running
            && input.Stage.Status is StageStatus.Draft or StageStatus.Ready)
        {
            var code = input.Stage.Status == StageStatus.Draft
                ? OverviewAssembler.ActionPrepareStage
                : OverviewAssembler.ActionStartStage;
            return BlockedTransition(code, DisabledConstructionFrozen, input, DisabledConstructionFrozen);
        }

        return input.Stage.Status switch
        {
            StageStatus.Draft => ResolvePrepareTransition(input),
            StageStatus.Ready => ResolveStartTransition(input),
            StageStatus.Running or StageStatus.Suspended => ResolveCompleteTransition(matchProgress),
            StageStatus.Completed => new StructureStageLifecycleTransitionDto(null, false),
            _ => new StructureStageLifecycleTransitionDto(null, false)
        };
    }

    private static StructureStageLifecycleTransitionDto ResolvePrepareTransition(
        StructureStageLifecycleInput input)
    {
        const string code = OverviewAssembler.ActionPrepareStage;

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

        var prepareGate = CollectPrepareGateCodes(input.Stage, input.CompetitionStages);
        if (prepareGate.Length > 0)
        {
            return BlockedTransition(code, prepareGate[0], input, prepareGate[0]);
        }

        return new StructureStageLifecycleTransitionDto(code, true);
    }

    private static StructureStageLifecycleTransitionDto ResolveStartTransition(StructureStageLifecycleInput input)
    {
        const string code = OverviewAssembler.ActionStartStage;

        if (!IsInitialRoundPlayable(input.Stage))
        {
            return BlockedTransition(code, DisabledInitialRoundNotPlayable, input, DisabledInitialRoundNotPlayable);
        }

        return new StructureStageLifecycleTransitionDto(code, true);
    }

    private static StructureStageLifecycleTransitionDto ResolveCompleteTransition(
        StructureStageLifecycleMatchProgressDto? matchProgress)
    {
        const string code = OverviewAssembler.ActionCompleteStage;
        if (matchProgress is { CanComplete: true })
        {
            return new StructureStageLifecycleTransitionDto(code, true);
        }

        return new StructureStageLifecycleTransitionDto(
            code,
            false,
            CompleteStage.ReasonOpenMatches);
    }

    private static StructureStageLifecycleTransitionDto BlockedTransition(
        string? code,
        string disabledReasonCode,
        StructureStageLifecycleInput input,
        string remediationCode) =>
        new(
            code,
            false,
            disabledReasonCode,
            ResolveRemediation(input.Competition.Id, input.Stage.Id, remediationCode));

    private static string? ResolveTransitionCode(StageStatus status) =>
        status switch
        {
            StageStatus.Draft => OverviewAssembler.ActionPrepareStage,
            StageStatus.Ready => OverviewAssembler.ActionStartStage,
            StageStatus.Running or StageStatus.Suspended => OverviewAssembler.ActionCompleteStage,
            _ => null
        };

    private static StructureStageLifecyclePrepareChecklistDto BuildPrepareChecklist(
        StructureStageLifecycleInput input)
    {
        var items = new List<StructureStageLifecyclePrepareChecklistItemDto>();
        var minimumTeams = input.Competition.Regulation.EntryRules.MinimumTeams;
        var activeEntries = input.Competition.Entries.Count(entry => entry.Status == EntryStatus.Active);

        var entriesApplicable = input.IsRootComposition;
        var entriesSatisfied = activeEntries >= minimumTeams;
        items.Add(new StructureStageLifecyclePrepareChecklistItemDto(
            ChecklistEntries,
            entriesApplicable,
            entriesSatisfied));

        var placesN = input.CompositionCapacity is > 0
            ? input.CompositionCapacity
            : StructurePopulationVolume.ResolvePlacesCapacity(input.Stage);
        var placesApplicable = placesN is > 0;
        var placesSatisfied = !placesApplicable
                              || EvaluatePlacesSatisfied(input, placesN!.Value);
        items.Add(new StructureStageLifecyclePrepareChecklistItemDto(
            ChecklistPlaces,
            placesApplicable,
            placesSatisfied));

        var drawApplicable = input.HasDrawRules;
        var drawSatisfied = !drawApplicable
                            || input.DrawExecutionBadge == StructureDrawExecutionBadge.Applied;
        items.Add(new StructureStageLifecyclePrepareChecklistItemDto(
            ChecklistDraw,
            drawApplicable,
            drawSatisfied));

        var exitsApplicable = input.HasQualificationRules || input.HasProgressionRules;
        var exitsSatisfied = !exitsApplicable || !HasExitStructureIssues(input.StructureIssues);
        items.Add(new StructureStageLifecyclePrepareChecklistItemDto(
            ChecklistExits,
            exitsApplicable,
            exitsSatisfied));

        var attributionApplicable = input.IsRootComposition
                                    && input.FormatKind is StructureFormatKind.Groups or StructureFormatKind.Cup;
        var attributionSatisfied = !attributionApplicable
                                   || input.AffectationEntryCount > 0
                                   || (input.CompositionCapacity is > 0
                                       && input.CompositionEntryCount >= input.CompositionCapacity.Value);
        items.Add(new StructureStageLifecyclePrepareChecklistItemDto(
            ChecklistAttribution,
            attributionApplicable,
            attributionSatisfied));

        var allSatisfied = items.Where(item => item.Applicable).All(item => item.Satisfied);
        return new StructureStageLifecyclePrepareChecklistDto(items, allSatisfied);
    }

    /// <summary>
    /// Places row — root: expected occupancy (affectation + inbound) vs Places N (Équipes meter).
    /// Non-root: composition pool fill vs capacity.
    /// </summary>
    private static bool EvaluatePlacesSatisfied(
        StructureStageLifecycleInput input,
        int placesN)
    {
        if (input.IsRootComposition)
        {
            var expected = StructurePopulationVolume.ExpectedOccupancy(
                input.Stage,
                input.CompetitionStages);
            return expected >= placesN;
        }

        return input.CompositionEntryCount >= placesN;
    }

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

    private static bool HasExitStructureIssues(IReadOnlyList<string>? structureIssues)
    {
        if (structureIssues is not { Count: > 0 })
        {
            return false;
        }

        return structureIssues.Any(code =>
            code.Contains("Qualification", StringComparison.Ordinal)
            || code.Contains("Progression", StringComparison.Ordinal)
            || string.Equals(code, StructureViewAssembler.IssueConfrontationUndecidable, StringComparison.Ordinal));
    }

    private static string[] CollectPrepareGateCodes(Stage target, IReadOnlyList<Stage> competitionStages)
    {
        var execution = CompetitionFlowAnalyzer.AnalyzeExecution([target]);
        var capacity = CompetitionFlowAnalyzer.AnalyzeCapacity(competitionStages);
        var topology = CompetitionFlowAnalyzer.AnalyzeTopology(competitionStages);

        return execution
            .Concat(capacity)
            .Concat(topology)
            .Where(diagnostic =>
                diagnostic.StageId == target.Id.Value
                && CompetitionFlowAnalyzer.IsPrepareGateIssue(diagnostic.Code))
            .Select(diagnostic => diagnostic.Code)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static bool DomainAllowsPrepare(Stage stage, StructureFormatKind? formatKind)
    {
        if (formatKind == StructureFormatKind.Swiss)
        {
            return stage.SwissSettings is not null;
        }

        if (stage.Groups.Count > 0)
        {
            return stage.Matchdays.Count > 0 && stage.Groups.Any(group => group.EntryIds.Count > 0);
        }

        if (stage.Rounds.Count > 0)
        {
            return true;
        }

        return stage.Matchdays.Count > 0;
    }

    private static bool IsInitialRoundPlayable(Stage stage)
    {
        if (stage.Rounds.Count == 0)
        {
            return true;
        }

        var hasSlottedFixtures = stage.Rounds
            .SelectMany(round => round.Fixtures)
            .Any(fixture => fixture.SlotAKey is not null || fixture.SlotBKey is not null);
        if (!hasSlottedFixtures)
        {
            return true;
        }

        var initialRound = stage.Rounds[0];
        return initialRound.Fixtures.Any(fixture => IsFixturePlayable(stage, fixture));
    }

    private static bool IsFixturePlayable(Stage stage, Fixture fixture)
    {
        if (fixture.SlotAKey is null || fixture.SlotBKey is null)
        {
            return false;
        }

        var slotA = stage.Slots.FirstOrDefault(slot => slot.SlotKey == fixture.SlotAKey);
        var slotB = stage.Slots.FirstOrDefault(slot => slot.SlotKey == fixture.SlotBKey);
        return slotA?.EntryId is not null && slotB?.EntryId is not null;
    }

    private static (string? Section, string? Focus, bool Manual, bool OpenDraw) MapRemediationDeepLink(
        string remediationCode)
    {
        if (remediationCode is ChecklistDraw or StructureViewAssembler.BlockerMissingPotRules)
        {
            return ("tirage", null, false, true);
        }

        if (remediationCode is ChecklistPlaces or DisabledInitialRoundNotPlayable)
        {
            return (null, "operational-placement", true, false);
        }

        if (remediationCode == ChecklistMatches)
        {
            return ("matchs", null, false, false);
        }

        if (remediationCode is ChecklistExits or StructureViewAssembler.IssueConfrontationUndecidable)
        {
            return ("qualification", null, false, false);
        }

        if (remediationCode == ChecklistAttribution)
        {
            return (null, "rail-entries", false, true);
        }

        if (remediationCode == ChecklistEntries)
        {
            return (null, "rail-entries", false, false);
        }

        if (remediationCode == DisabledStructureGraphInvalid)
        {
            return ("construction", null, false, false);
        }

        if (remediationCode is DisabledConstructionFrozen or DisabledCompetitionClosed)
        {
            return (null, null, false, false);
        }

        if (remediationCode.Contains("Qualification", StringComparison.Ordinal))
        {
            return ("qualification", null, false, false);
        }

        if (remediationCode.Contains("Progression", StringComparison.Ordinal))
        {
            return ("progression", null, false, false);
        }

        if (remediationCode.Contains("SlotFeed", StringComparison.Ordinal))
        {
            return ("construction", null, false, false);
        }

        return ("construction", null, false, false);
    }

    private static StructureStageLifecycleRemediationDto? ResolveRemediation(
        CompetitionId competitionId,
        StageId stageId,
        string remediationCode)
    {
        var link = MapRemediationDeepLink(remediationCode);
        var section = link.Section;
        var focus = link.Focus;
        var manual = link.Manual;
        var openDraw = link.OpenDraw;

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

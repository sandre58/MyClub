// -----------------------------------------------------------------------
// <copyright file="OverviewAssembler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Assembles the Overview Read projection from competition state.
/// </summary>
/// <remarks>
/// Composes existing Application diagnostics — does not re-implement Domain invariants.
/// Competition Prepare/Start are Host-executable and projected as available actions
/// opportunities when Domain preconditions appear satisfied (R19 — not execution guarantees).
/// They are intentional lifecycle transitions (L7) — not automatically elevated to naturalProgression.
/// </remarks>
public static class OverviewAssembler
{
    /// <summary>Cycle reading: construction (Draft/Ready).</summary>
    public const string CycleConstruction = "Construction";

    /// <summary>Cycle reading: competition in progress (Running/Suspended).</summary>
    public const string CycleInProgress = "InProgress";

    /// <summary>Cycle reading: completed.</summary>
    public const string CycleCompleted = "Completed";

    /// <summary>Cycle reading: archived.</summary>
    public const string CycleArchived = "Archived";

    /// <summary>Preparation focus: configuration / structure work.</summary>
    public const string PreparationFocusSetup = "Setup";

    /// <summary>Preparation focus: Championship calendar generated, ready to start.</summary>
    public const string PreparationFocusGeneratedCalendar = "GeneratedCalendar";

    /// <summary>Result presentation: hero winner (Cup KO without Top-3).</summary>
    public const string OutcomePresentationWinner = "Winner";

    /// <summary>Result presentation: Top-3 podium (Championship / Swiss / PlacementAwards 1–3).</summary>
    public const string OutcomePresentationPodium = "Podium";

    /// <summary>Max matchdays in calendar overview preview.</summary>
    public const int CalendarPreviewMatchdayLimit = 3;

    /// <summary>Situation nature: blocking attention.</summary>
    public const string NatureBlocking = "Blocking";

    /// <summary>Situation nature: informational.</summary>
    public const string NatureInformational = "Informational";

    /// <summary>Impact: construction / structure progress blocked.</summary>
    public const string ImpactBlocksConstruction = "BlocksConstruction";

    /// <summary>Impact: draw pipeline cannot produce a usable result.</summary>
    public const string ImpactBlocksDraw = "BlocksDraw";

    /// <summary>Impact: sporting progression / qualification cannot advance.</summary>
    public const string ImpactBlocksProgression = "BlocksProgression";

    /// <summary>Transition readiness: materialize matches path.</summary>
    public const string TransitionMaterializeMatches = "MaterializeMatches";

    /// <summary>Transition readiness: Cup from occupied slots (distinct from skeleton MaterializeMatches).</summary>
    public const string TransitionMaterializeFromOccupiedSlots = "MaterializeFromOccupiedSlots";

    /// <summary>Transition readiness: Swiss GenerateNextRound path.</summary>
    public const string TransitionGenerateNextRound = "GenerateNextRound";

    /// <summary>Transition readiness: draw path identifiable (structure/pots/bracket).</summary>
    public const string TransitionDraw = "Draw";

    /// <summary>Blocker: Swiss stage is not Running yet.</summary>
    public const string BlockerSwissStageNotRunning = "SwissStageNotRunning";

    /// <summary>Blocker: previous Swiss round matches are not all Finished.</summary>
    public const string BlockerSwissAwaitingRoundResults = "SwissAwaitingRoundResults";

    /// <summary>Blocker: all planned Swiss rounds already generated.</summary>
    public const string BlockerSwissRoundsComplete = "SwissRoundsComplete";

    /// <summary>Blocker: fewer than two active entries for Swiss pairing.</summary>
    public const string BlockerSwissInsufficientParticipants = "SwissInsufficientParticipants";

    /// <summary>Source: competition suspended (operational, L11).</summary>
    public const string SourceCompetitionSuspended = "CompetitionSuspended";

    /// <summary>Prominence: present.</summary>
    public const string ProminencePresent = "Present";

    /// <summary>Prominence: condensed.</summary>
    public const string ProminenceCondensed = "Condensed";

    /// <summary>Prominence: dominant.</summary>
    public const string ProminenceDominant = "Dominant";

    /// <summary>Prominence: absent.</summary>
    public const string ProminenceAbsent = "Absent";

    /// <summary>Action codes (semantic — Host use cases already exist unless noted).</summary>
    public const string ActionPrepareStage = "PrepareStage";

    /// <summary>Start stage.</summary>
    public const string ActionStartStage = "StartStage";

    /// <summary>Publish draw.</summary>
    public const string ActionPublishDraw = "PublishDraw";

    /// <summary>Happy-path orchestration: Publish then Apply (recovery remains <see cref="ActionApplyDraw"/>).</summary>
    public const string ActionPublishAndApplyDraw = "PublishAndApplyDraw";

    /// <summary>Apply draw.</summary>
    public const string ActionApplyDraw = "ApplyDraw";

    /// <summary>Materialize matches.</summary>
    public const string ActionMaterializeMatches = "MaterializeMatches";

    /// <summary>Materialize Cup confrontations from occupied slots (navigate to Stage pairing UI).</summary>
    public const string ActionMaterializeFromOccupiedSlots = "MaterializeFromOccupiedSlots";

    /// <summary>Generate next Swiss round (pairings + Matchday) while Running.</summary>
    public const string ActionGenerateNextRound = "GenerateNextRound";

    /// <summary>Generate schedule proposal.</summary>
    public const string ActionGenerateSchedule = "GenerateSchedule";

    /// <summary>Apply schedule.</summary>
    public const string ActionApplySchedule = "ApplySchedule";

    /// <summary>Start match.</summary>
    public const string ActionStartMatch = "StartMatch";

    /// <summary>Finish match.</summary>
    public const string ActionFinishMatch = "FinishMatch";

    /// <summary>Apply progression.</summary>
    public const string ActionApplyProgression = "ApplyProgression";

    /// <summary>Apply qualification.</summary>
    public const string ActionApplyQualification = "ApplyQualification";

    /// <summary>Complete competition.</summary>
    public const string ActionCompleteCompetition = "CompleteCompetition";

    /// <summary>Archive competition.</summary>
    public const string ActionArchiveCompetition = "ArchiveCompetition";

    /// <summary>Prepare competition (Draft → Ready).</summary>
    public const string ActionPrepareCompetition = "PrepareCompetition";

    /// <summary>Start competition (Ready → Running).</summary>
    public const string ActionStartCompetition = "StartCompetition";

    /// <summary>
    /// Builds the Overview view.
    /// </summary>
    /// <param name="competition">Loaded competition.</param>
    /// <param name="stages">Stages in competition order.</param>
    /// <param name="matchesByStage">Matches keyed by stage.</param>
    /// <returns>Overview projection DTO.</returns>
    public static OverviewViewDto Assemble(
        Competition competition,
        IReadOnlyList<Stage> stages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>> matchesByStage)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stages);
        ArgumentNullException.ThrowIfNull(matchesByStage);

        var structureView = StructureViewAssembler.Assemble(competition, stages);
        var attention = NeedsAttentionAssembler.Assemble(competition, stages, matchesByStage);
        var completion = competition.Status is CompetitionStatus.Running or CompetitionStatus.Suspended
            ? CompletionAnalyzer.Analyze(competition, stages, matchesByStage)
            : null;

        var fixtureToMatch = BuildFixtureToMatchMap(stages);
        var situations = BuildSituations(competition, structureView, attention, stages, fixtureToMatch);
        var attentionSummary = BuildAttentionSummary(situations);

        var matchCounts = BuildMatchCounts(matchesByStage);
        var cycleReading = BuildCycleReading(competition.Status);
        var preparationFocus = ResolvePreparationFocus(
            cycleReading.Code,
            competition.Status,
            structureView.Format.Kind,
            matchCounts.Total);
        var calendarSummary = preparationFocus == PreparationFocusGeneratedCalendar
            ? BuildCalendarSummary(
                competition,
                stages,
                matchesByStage,
                matchCounts.Total,
                structureView.Format.PrimaryStageId)
            : null;
        var operationalFocus = BuildOperationalFocus(
            competition,
            stages,
            matchesByStage,
            matchCounts,
            structureView.Format.Kind);
        var competitionOutcome = BuildCompetitionOutcome(
            competition,
            stages,
            matchesByStage,
            structureView.Format.Kind);
        var dimensions = BuildDimensions(competition, structureView, stages, matchCounts, matchesByStage);
        var actions = BuildActions(competition, stages, matchesByStage, structureView, attention, completion, fixtureToMatch);
        var fromSlotsOpportunities = stages
            .Where(stage => TryDescribeFromSlotsOpportunity(competition, stage, out _))
            .Select(stage => stage.Id)
            .ToArray();
        var progression = ResolveNaturalProgression(
            competition,
            actions,
            fromSlotsOpportunities.Length > 0);
        var closure = new OverviewClosureHintDto(
            completion?.CanCompleteNormally ?? false,
            completion?.Reasons.Select(reason => reason.Code).ToArray() ?? []);
        var navigation = BuildNavigationHints(competition, situations, stages, fixtureToMatch);

        return new OverviewViewDto(
            competition.Id.Value,
            competition.Name.Value,
            competition.Status,
            competition.CompletionMode,
            BuildPeriod(stages),
            cycleReading,
            preparationFocus,
            calendarSummary,
            competitionOutcome,
            dimensions,
            operationalFocus,
            situations,
            attentionSummary,
            actions,
            progression,
            closure,
            navigation);
    }

    /// <summary>
    /// Host-owned preparation sub-situation — not a cycleReading code.
    /// GeneratedCalendar: Construction + Championship + Ready + matches.total &gt; 0.
    /// </summary>
    internal static string ResolvePreparationFocus(
        string cycleReadingCode,
        CompetitionStatus status,
        StructureFormatKind? formatKind,
        int matchTotal) =>
        cycleReadingCode == CycleConstruction
        && status == CompetitionStatus.Ready
        && formatKind == StructureFormatKind.Championship
        && matchTotal > 0
            ? PreparationFocusGeneratedCalendar
            : PreparationFocusSetup;

    /// <summary>
    /// Calendar overview synthesis for GeneratedCalendar — primary Championship stage (may still be Draft/Ready).
    /// Does not use ReferenceStage (Running/Completed only).
    /// </summary>
    internal static OverviewCalendarSummaryDto BuildCalendarSummary(
        Competition competition,
        IReadOnlyList<Stage> stages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>> matchesByStage,
        int matchTotal,
        Guid? primaryStageId)
    {
        Stage? stage = null;
        if (primaryStageId is { } id)
        {
            stage = stages.FirstOrDefault(candidate => candidate.Id.Value == id);
        }

        stage ??= stages.FirstOrDefault();
        if (stage is null)
        {
            return new OverviewCalendarSummaryDto(0, matchTotal, [], null);
        }

        var names = EntryDisplayNames.ToMap(competition);
        var matches = matchesByStage.TryGetValue(stage.Id, out var list) ? list : [];
        var byId = matches.ToDictionary(match => match.Id);
        var units = EnumerateSportUnits(stage, byId)
            .Where(unit => unit.UnitKind == UnitKindMatchday)
            .ToArray();

        var preview = units
            .Take(CalendarPreviewMatchdayLimit)
            .Select(unit => new OverviewCalendarMatchdayPreviewDto(
                unit.MatchdayNumber ?? unit.Order,
                unit.Matches.Count))
            .ToArray();

        return new OverviewCalendarSummaryDto(
            units.Length,
            matchTotal,
            preview,
            ResolveCalendarNextMatch(stage, units, names));
    }

    private static OverviewCalendarNextMatchDto? ResolveCalendarNextMatch(
        Stage stage,
        IReadOnlyList<SportUnitSlice> units,
        IReadOnlyDictionary<EntryId, string> names)
    {
        var candidates = new List<(OverviewCalendarNextMatchDto Dto, DateTimeOffset? Start)>();
        foreach (var unit in units)
        {
            foreach (var match in unit.Matches)
            {
                if (match.Status != MatchStatus.Scheduled)
                {
                    continue;
                }

                DateTimeOffset? scheduledAt = null;
                if (stage.TryGetMatchPlacement(match.Id, out var placement))
                {
                    scheduledAt = placement.Start;
                }

                candidates.Add((
                    new OverviewCalendarNextMatchDto(
                        match.Id.Value,
                        stage.Id.Value,
                        unit.MatchdayNumber,
                        scheduledAt,
                        EntryDisplayNames.Resolve(names, match.HomeEntryId) ?? match.HomeEntryId.Value.ToString(),
                        EntryDisplayNames.Resolve(names, match.AwayEntryId) ?? match.AwayEntryId.Value.ToString()),
                    scheduledAt));
            }
        }

        return candidates
            .OrderBy(candidate => candidate.Start is null ? 1 : 0)
            .ThenBy(candidate => candidate.Start ?? DateTimeOffset.MaxValue)
            .ThenBy(candidate => candidate.Dto.MatchdayNumber ?? int.MaxValue)
            .Select(candidate => candidate.Dto)
            .FirstOrDefault();
    }

    /// <summary>
    /// Derives operational calendar bounds from match placements (min/max start).
    /// Null when no placement exists — not declared competition season dates.
    /// </summary>
    private static OverviewCompetitionPeriodDto? BuildPeriod(IReadOnlyList<Stage> stages)
    {
        DateTimeOffset? earliest = null;
        DateTimeOffset? latest = null;

        foreach (var stage in stages)
        {
            foreach (var placement in stage.MatchPlacements)
            {
                var start = placement.Start;
                if (earliest is null || start < earliest)
                {
                    earliest = start;
                }

                if (latest is null || start > latest)
                {
                    latest = start;
                }
            }
        }

        return earliest is null && latest is null ? null : new OverviewCompetitionPeriodDto(earliest, latest);
    }

    private static OverviewCycleReadingDto BuildCycleReading(CompetitionStatus status) =>
        status switch
        {
            CompetitionStatus.Draft or CompetitionStatus.Ready => new OverviewCycleReadingDto(CycleConstruction),
            CompetitionStatus.Running or CompetitionStatus.Suspended => new OverviewCycleReadingDto(CycleInProgress),
            CompetitionStatus.Completed => new OverviewCycleReadingDto(CycleCompleted),
            CompetitionStatus.Archived => new OverviewCycleReadingDto(CycleArchived),
            _ => new OverviewCycleReadingDto(CycleConstruction)
        };

    private static OverviewConstructionDimensionsDto BuildDimensions(
        Competition competition,
        StructureViewDto structureView,
        IReadOnlyList<Stage> stages,
        OverviewMatchCountsDto matchCounts,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>> matchesByStage)
    {
        var inConstruction = competition.Status is CompetitionStatus.Draft or CompetitionStatus.Ready;
        var running = competition.Status is CompetitionStatus.Running or CompetitionStatus.Suspended;
        var hasStructureBlockers = structureView.Readiness.Blockers.Count > 0;

        var teamsProminence = inConstruction
            ? hasStructureBlockers &&
              structureView.Readiness.Blockers.Contains(StructureViewAssembler.BlockerInsufficientParticipants)
                ? ProminenceDominant
                : ProminencePresent
            : ProminenceCondensed;

        var structureProminence = inConstruction
            ? structureView.Readiness.Blockers.Any(blocker =>
                blocker is StructureViewAssembler.BlockerMissingStage
                    or StructureViewAssembler.BlockerMissingStructure
                    or StructureViewAssembler.BlockerMissingPotRules
                    or StructureViewAssembler.BlockerCupBracketInvalid)
                ? ProminenceDominant
                : ProminencePresent
            : ProminenceCondensed;

        var regulationProminence = inConstruction ? ProminencePresent : ProminenceCondensed;
        var matchesProminence = matchCounts.Total == 0
            ? inConstruction ? ProminenceCondensed : ProminenceAbsent
            : running
                ? matchCounts.Live > 0 ? ProminenceDominant : ProminencePresent
                : ProminenceCondensed;

        var structureFacts = new Dictionary<string, string>
        {
            ["formatKind"] = structureView.Format.Kind?.ToString() ?? "None",
            ["groupCount"] = structureView.Structure.GroupCount.ToString(CultureInfo.InvariantCulture),
            ["roundCount"] = structureView.Structure.RoundCount.ToString(CultureInfo.InvariantCulture),
            ["matchdayCount"] =
                structureView.Structure.MatchdayCount.ToString(CultureInfo.InvariantCulture),
            ["slotCount"] = structureView.Structure.SlotCount.ToString(CultureInfo.InvariantCulture)
        };
        if (structureView.Format.Kind != StructureFormatKind.Swiss)
        {
            return new OverviewConstructionDimensionsDto(
                new OverviewDimensionDto(
                    teamsProminence,
                    new Dictionary<string, string>
                    {
                        ["activeCount"] = structureView.Participants.ActiveCount.ToString(CultureInfo.InvariantCulture),
                        ["occupyingCount"] =
                            structureView.Participants.OccupyingCount.ToString(CultureInfo.InvariantCulture),
                        ["minimumTeams"] =
                            structureView.Regulation.MinimumTeams.ToString(CultureInfo.InvariantCulture),
                        ["maximumTeams"] =
                            structureView.Regulation.MaximumTeams.ToString(CultureInfo.InvariantCulture)
                    }),
                new OverviewDimensionDto(structureProminence, structureFacts),
                BuildRegulationDimension(
                    competition,
                    structureView,
                    stages,
                    matchesByStage,
                    regulationProminence,
                    inConstruction),
                new OverviewDimensionDto(
                    matchesProminence,
                    new Dictionary<string, string>
                    {
                        ["live"] = matchCounts.Live.ToString(CultureInfo.InvariantCulture),
                        ["scheduled"] = matchCounts.Scheduled.ToString(CultureInfo.InvariantCulture),
                        ["finished"] = matchCounts.Finished.ToString(CultureInfo.InvariantCulture),
                        ["total"] = matchCounts.Total.ToString(CultureInfo.InvariantCulture)
                    }));
        }

        structureFacts["swissRoundCount"] =
            (structureView.Structure.SwissRoundCount ?? 0).ToString(CultureInfo.InvariantCulture);
        structureFacts["swissByeCount"] = stages
            .Where(stage => stage.IsSwiss)
            .Sum(stage => stage.SwissByeHistory.Count)
            .ToString(CultureInfo.InvariantCulture);

        return new OverviewConstructionDimensionsDto(
            new OverviewDimensionDto(
                teamsProminence,
                new Dictionary<string, string>
                {
                    ["activeCount"] = structureView.Participants.ActiveCount.ToString(CultureInfo.InvariantCulture),
                    ["occupyingCount"] =
                        structureView.Participants.OccupyingCount.ToString(CultureInfo.InvariantCulture),
                    ["minimumTeams"] =
                        structureView.Regulation.MinimumTeams.ToString(CultureInfo.InvariantCulture),
                    ["maximumTeams"] =
                        structureView.Regulation.MaximumTeams.ToString(CultureInfo.InvariantCulture)
                }),
            new OverviewDimensionDto(structureProminence, structureFacts),
            BuildRegulationDimension(
                competition,
                structureView,
                stages,
                matchesByStage,
                regulationProminence,
                inConstruction),
            new OverviewDimensionDto(
                matchesProminence,
                new Dictionary<string, string>
                {
                    ["live"] = matchCounts.Live.ToString(CultureInfo.InvariantCulture),
                    ["scheduled"] = matchCounts.Scheduled.ToString(CultureInfo.InvariantCulture),
                    ["finished"] = matchCounts.Finished.ToString(CultureInfo.InvariantCulture),
                    ["total"] = matchCounts.Total.ToString(CultureInfo.InvariantCulture)
                }));
    }

    /// <summary>
    /// Builds the regulation dimension: factual Competition + Stage summaries and transition readiness.
    /// </summary>
    /// <remarks>
    /// Reuses <see cref="StructureViewAssembler"/> readiness — does not invent Domain validation.
    /// PrepareStage / StartStage are status transitions, not regulation content gates — not projected here.
    /// Competition Prepare/Start are projected in <see cref="BuildActions"/>, not as regulation readiness.
    /// </remarks>
    private static OverviewRegulationDimensionDto BuildRegulationDimension(
        Competition competition,
        StructureViewDto structureView,
        IReadOnlyList<Stage> stages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>> matchesByStage,
        string prominence,
        bool inConstruction)
    {
        var stageSummary = BuildStageRegulationSummary(structureView, stages);
        var mutable = competition.Status is CompetitionStatus.Draft or CompetitionStatus.Ready;
        var readiness = inConstruction
            ? BuildRegulationTransitionReadiness(structureView)
            : [];
        AppendFromSlotsTransitionReadiness(competition, stages, readiness);
        AppendSwissGenerateNextRoundReadiness(competition, structureView, stages, matchesByStage, readiness);

        return new OverviewRegulationDimensionDto(
            prominence,
            structureView.Regulation,
            stageSummary,
            mutable,
            readiness);
    }

    private static OverviewStageRegulationSummaryDto? BuildStageRegulationSummary(
        StructureViewDto structureView,
        IReadOnlyList<Stage> stages)
    {
        if (structureView.Format.PrimaryStageId is not { } primaryId)
        {
            return null;
        }

        var stage = stages.FirstOrDefault(candidate => candidate.Id.Value == primaryId);
        if (stage is null)
        {
            return null;
        }

        var regulation = stage.Regulation;
        var qualificationPaths = regulation.QualificationRules?.Paths.Count ?? 0;
        var progressionPaths = regulation.ProgressionRules?.Paths.Count ?? 0;

        return new OverviewStageRegulationSummaryDto(
            stage.Id.Value,
            stage.Name.Value,
            HasDrawRules: regulation.DrawRules is not null,
            NumberOfPots: regulation.DrawRules?.PotRules?.NumberOfPots,
            HasQualificationRules: regulation.QualificationRules is not null,
            QualificationPathCount: qualificationPaths,
            HasProgressionRules: regulation.ProgressionRules is not null,
            ProgressionPathCount: progressionPaths,
            HasTieFormat: regulation.TieFormat is not null);
    }

    private static List<OverviewTransitionReadinessDto> BuildRegulationTransitionReadiness(
        StructureViewDto structureView)
    {
        var blockers = structureView.Readiness.Blockers;
        var readiness = new List<OverviewTransitionReadinessDto>();
        var kind = structureView.Format.Kind;

        // Championship / Swiss never use the draw path — omit Draw readiness.
        if (kind is not StructureFormatKind.Championship and not StructureFormatKind.Swiss)
        {
            readiness.Add(
                new OverviewTransitionReadinessDto(
                    TransitionDraw,
                    structureView.Readiness.ReadyForDraw,
                    structureView.Readiness.ReadyForDraw ? [] : blockers));
        }

        // Swiss uses GenerateNextRound — omit MaterializeMatches (always false with empty blockers).
        if (kind is not StructureFormatKind.Swiss)
        {
            readiness.Add(
                new OverviewTransitionReadinessDto(
                    TransitionMaterializeMatches,
                    structureView.Readiness.ReadyForMaterialization,
                    structureView.Readiness.ReadyForMaterialization ? [] : blockers));
        }

        return readiness;
    }

    private static void AppendSwissGenerateNextRoundReadiness(
        Competition competition,
        StructureViewDto structureView,
        IReadOnlyList<Stage> stages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>> matchesByStage,
        List<OverviewTransitionReadinessDto> readiness)
    {
        if (structureView.Format.Kind is not StructureFormatKind.Swiss)
        {
            return;
        }

        if (competition.Status is CompetitionStatus.Completed or CompetitionStatus.Archived)
        {
            return;
        }

        var evaluation = TryEvaluateSwissGenerateNextRound(
            competition,
            structureView,
            stages,
            matchesByStage,
            out _,
            out var blockers);
        if (evaluation is null)
        {
            return;
        }

        readiness.Add(
            new OverviewTransitionReadinessDto(
                TransitionGenerateNextRound,
                evaluation.Value.Ready,
                blockers));
    }

    /// <summary>
    /// Projects from-slots transition only when an opportunity exists.
    /// Omitting the not-ready row avoids poisoning regulation primaryGap with
    /// <see cref="BlockerInsufficientOccupiedSlots"/> during early Cup construction
    /// (skeleton / draw are separate concepts — D2 readiness audit).
    /// </summary>
    private static void AppendFromSlotsTransitionReadiness(
        Competition competition,
        IReadOnlyList<Stage> stages,
        List<OverviewTransitionReadinessDto> readiness)
    {
        if (competition.Status is not (CompetitionStatus.Draft or CompetitionStatus.Ready or CompetitionStatus.Running))
        {
            return;
        }

        if (!stages.Any(stage => TryDescribeFromSlotsOpportunity(competition, stage, out _))) return;
        readiness.Add(
            new OverviewTransitionReadinessDto(
                TransitionMaterializeFromOccupiedSlots,
                Ready: true,
                []));
    }

    /// <summary>Blocker: Cup stage lacks enough uncovered occupied slots for from-slots materialization.</summary>
    /// <remarks>Retained for clients/tests; no longer attached as a standing regulation gap when absent.</remarks>
    public const string BlockerInsufficientOccupiedSlots = "InsufficientOccupiedSlots";

    /// <summary>Sport unit kind: championship / groups / Swiss matchday.</summary>
    public const string UnitKindMatchday = "Matchday";

    /// <summary>Sport unit kind: cup elimination round.</summary>
    public const string UnitKindRound = "Round";

    private static OverviewOperationalFocusDto BuildOperationalFocus(
        Competition competition,
        IReadOnlyList<Stage> stages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>> matchesByStage,
        OverviewMatchCountsDto matchCounts,
        StructureFormatKind? formatKind)
    {
        var names = EntryDisplayNames.ToMap(competition);
        var stageFocus = stages
            .Select(stage => new OverviewStageFocusDto(stage.Id.Value, stage.Name.Value, stage.Status))
            .ToArray();

        var draws = stages
            .SelectMany(stage => stage.Draws
                .Where(draw => draw.Status != DrawStatus.Cancelled)
                .Select(draw => new OverviewDrawFocusDto(
                    stage.Id.Value,
                    draw.Id.Value,
                    draw.Kind,
                    draw.Status,
                    draw.Resolution.State,
                    DrawAppliedState.IsApplied(draw, stage))))
            .ToArray();

        var swissByes = stages
            .Where(stage => stage.IsSwiss)
            .SelectMany(stage => stage.SwissByeHistory.Select(bye => new OverviewSwissByeDto(
                stage.Id.Value,
                bye.RoundIndex,
                bye.EntryId.Value,
                EntryDisplayNames.Resolve(names, bye.EntryId) ?? bye.EntryId.Value.ToString())))
            .OrderBy(bye => bye.RoundIndex)
            .ThenBy(bye => bye.EntryId)
            .ToArray();

        var (recentUnit, nextUnit) = BuildTemporalSportUnits(competition, stages, matchesByStage, names);

        return new OverviewOperationalFocusDto(
            stageFocus,
            draws,
            matchCounts,
            swissByes,
            recentUnit,
            nextUnit,
            BuildStandingCompact(competition, stages, matchesByStage),
            BuildReferenceStageGameRules(competition, stages, formatKind));
    }

    /// <summary>
    /// Game-rule facts for Overview Regulation — ReferenceStage only.
    /// </summary>
    internal static OverviewReferenceStageGameRulesDto? BuildReferenceStageGameRules(
        Competition competition,
        IReadOnlyList<Stage> stages,
        StructureFormatKind? competitionFormatKind)
    {
        var reference = ResolveReferenceStage(competition, stages);
        if (reference is null)
        {
            return null;
        }

        var match = reference.Regulation.MatchRules;

        // Stage StandingRules when classifying; otherwise competition defaults (A4 seed) for display.
        var standing = (reference.Regulation.StandingRules ?? competition.Regulation.StandingRules).Points;
        var tie = TieFormat.OrDefaultOneLeg(reference.Regulation.TieFormat);
        var formatKind = ResolveGameRulesFormatKind(reference, competitionFormatKind);

        return new OverviewReferenceStageGameRulesDto(
            reference.Id.Value,
            reference.Name.Value,
            formatKind.ToString(),
            standing.WinPoints,
            standing.DrawPoints,
            standing.LossPoints,
            match.Duration.NumberOfPeriods,
            match.Duration.DurationPerPeriod,
            HasExtraTime: match.ExtraTimePolicy is not null,
            HasPenaltyShootout: match.PenaltyShootoutPolicy is not null,
            NumberOfLegs: tie.NumberOfLegs,
            AggregateScoring: tie.AggregateScoring,
            HasTieExtraTime: tie.ExtraTimeRule is not null,
            HasTiePenaltyShootout: tie.PenaltyShootoutRule is not null,
            SwissPlannedRounds: reference.SwissSettings?.RoundCount);
    }

    private static StructureFormatKind ResolveGameRulesFormatKind(
        Stage stage,
        StructureFormatKind? competitionFormatKind) =>
        stage.IsSwiss
            ? StructureFormatKind.Swiss
            : competitionFormatKind ?? (stage.Rounds.Count > 0 && stage.Matchdays.Count == 0
                ? StructureFormatKind.Cup
                : stage.Groups.Count > 0 ? StructureFormatKind.Groups : StructureFormatKind.Championship);

    /// <summary>
    /// Recent / Upcoming on ReferenceStage only — full Matchday or Round units (no caps).
    /// </summary>
    internal static (OverviewSportUnitDto? Recent, OverviewSportUnitDto? Next) BuildTemporalSportUnits(
        Competition competition,
        IReadOnlyList<Stage> stages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>> matchesByStage,
        IReadOnlyDictionary<EntryId, string>? names = null)
    {
        var reference = ResolveReferenceStage(competition, stages);
        if (reference is null)
        {
            return (null, null);
        }

        names ??= EntryDisplayNames.ToMap(competition);
        var matches = matchesByStage.TryGetValue(reference.Id, out var list) ? list : [];
        var byId = matches.ToDictionary(match => match.Id);
        var units = EnumerateSportUnits(reference, byId);
        if (units.Count == 0)
        {
            return (null, null);
        }

        SportUnitSlice? recentSlice = null;
        for (var i = units.Count - 1; i >= 0; i--)
        {
            var slice = units[i];
            if (!slice.Matches.Exists(match => match.Status is MatchStatus.Live or MatchStatus.Finished)) continue;
            recentSlice = slice;
            break;
        }

        var nextSlice = recentSlice is null
            ? units.FirstOrDefault(slice => slice.Matches.Count > 0)
            : units.FirstOrDefault(slice =>
                slice.Order > recentSlice.Order && slice.Matches.Count > 0);

        return (
            recentSlice is null ? null : ProjectSportUnit(reference, recentSlice, names),
            nextSlice is null ? null : ProjectSportUnit(reference, nextSlice, names));
    }

    private static List<SportUnitSlice> EnumerateSportUnits(
        Stage stage,
        IReadOnlyDictionary<MatchId, Match> matchesById)
    {
        if (stage.Matchdays.Count > 0)
        {
            return
            [
                .. stage.Matchdays
                    .OrderBy(matchday => matchday.Number)
                    .Select(matchday => new SportUnitSlice(
                        matchday.Number,
                        UnitKindMatchday,
                        matchday.Number.ToString(CultureInfo.InvariantCulture),
                        matchday.Number,
                        RoundName: null,
                        CollectUnitMatches(matchday.Fixtures, matchesById)))
            ];
        }

        if (stage.Rounds.Count == 0) return [];
        var slices = new List<SportUnitSlice>(stage.Rounds.Count);
        slices.AddRange(stage.Rounds.Select((round, index) => new SportUnitSlice(index + 1, UnitKindRound, round.Id.Value.ToString(), MatchdayNumber: null, round.Name, CollectUnitMatches(round.Fixtures, matchesById))));

        return slices;
    }

    private static List<Match> CollectUnitMatches(
        IReadOnlyList<Fixture> fixtures,
        IReadOnlyDictionary<MatchId, Match> matchesById)
    {
        var collected = new List<Match>();
        foreach (var fixture in fixtures)
        {
            foreach (var matchId in fixture.MatchIds)
            {
                if (matchesById.TryGetValue(matchId, out var match))
                {
                    collected.Add(match);
                }
            }
        }

        return collected;
    }

    private static OverviewSportUnitDto ProjectSportUnit(
        Stage stage,
        SportUnitSlice slice,
        IReadOnlyDictionary<EntryId, string> names)
    {
        var lines = slice.Matches
            .Select(match =>
            {
                DateTimeOffset? scheduledAt = null;
                if (stage.TryGetMatchPlacement(match.Id, out var placement))
                {
                    scheduledAt = placement.Start;
                }

                MatchScoreDto? score = null;
                if (match is { Status: MatchStatus.Finished, Result: not null })
                {
                    score = new MatchScoreDto(
                        match.Result.Score.HomeGoals,
                        match.Result.Score.AwayGoals);
                }

                return new OverviewMatchLineDto(
                    match.Id.Value,
                    stage.Id.Value,
                    match.Status,
                    scheduledAt,
                    EntryDisplayNames.Resolve(names, match.HomeEntryId) ?? match.HomeEntryId.Value.ToString(),
                    EntryDisplayNames.Resolve(names, match.AwayEntryId) ?? match.AwayEntryId.Value.ToString(),
                    score);
            })
            .OrderBy(line => line.Status == MatchStatus.Live ? 0 : 1)
            .ThenBy(line => line.ScheduledAt ?? DateTimeOffset.MaxValue)
            .ThenBy(line => line.MatchId)
            .ToArray();

        return new OverviewSportUnitDto(
            stage.Id.Value,
            stage.Name.Value,
            slice.UnitKind,
            slice.UnitKey,
            slice.MatchdayNumber,
            slice.RoundName,
            lines.Length,
            lines);
    }

    private sealed record SportUnitSlice(
        int Order,
        string UnitKind,
        string UnitKey,
        int? MatchdayNumber,
        string? RoundName,
        List<Match> Matches);

    /// <summary>
    /// Projects CompetitionOutcome for the Completed Result surface.
    /// Includes Host <see cref="CompetitionOutcomeDto.Presentation"/> (Winner | Podium).
    /// Null when Abandoned, no places, or places that cannot conclude a presentable Result
    /// (no unique rank-1 and not a Top-3 podium case).
    /// </summary>
    internal static CompetitionOutcomeDto? BuildCompetitionOutcome(
        Competition competition,
        IReadOnlyList<Stage> stages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>> matchesByStage,
        StructureFormatKind? formatKind)
    {
        if (competition.Status is not (CompetitionStatus.Completed or CompetitionStatus.Archived))
        {
            return null;
        }

        // Abandoned: no final sporting result to project (G7 — Completed ≠ outcome).
        if (competition.CompletionMode == CompletionMode.Abandoned)
        {
            return null;
        }

        var names = EntryDisplayNames.ToMap(competition);

        // Explicit placement awards (Cup / classification / consolation) when rules exist and fixtures are decided.
        var awardPlaces = ProjectPlacementAwardPlaces(stages, matchesByStage, names);
        if (awardPlaces.Count > 0)
        {
            return TryCreateOutcome(awardPlaces, fromStanding: false);
        }

        // Championship RR (and Swiss Overall): final Standing is the competition result → Podium.
        if (formatKind is not (StructureFormatKind.Championship or StructureFormatKind.Swiss)) return null;
        var standing = ProjectStandingOutcomePlaces(competition, stages, matchesByStage);
        return standing is null ? null : TryCreateOutcome(standing, fromStanding: true);

        // Groups-only / Cup without PlacementAwardRules → no inventable competition outcome.
    }

    /// <summary>
    /// Host presentation for Result: Podium (standing or ranks 1–3) vs Winner (unique rank 1 only).
    /// Returns null when neither mode applies — SPA silence (no fake champion).
    /// </summary>
    internal static string? ResolveOutcomePresentation(
        IReadOnlyList<FinalPlacementDto> places,
        bool fromStanding)
    {
        if (places.Count == 0)
        {
            return null;
        }

        if (fromStanding)
        {
            return OutcomePresentationPodium;
        }

        var rank1Count = places.Count(place => place.Rank == 1);
        var hasRank2 = places.Any(place => place.Rank == 2);
        var hasRank3 = places.Any(place => place.Rank == 3);

        return rank1Count switch
        {
            1 when hasRank2 && hasRank3 => OutcomePresentationPodium,
            1 => OutcomePresentationWinner,
            _ => null
        };
    }

    private static CompetitionOutcomeDto? TryCreateOutcome(
        IReadOnlyList<FinalPlacementDto> places,
        bool fromStanding)
    {
        var presentation = ResolveOutcomePresentation(places, fromStanding);
        return presentation is null ? null : new CompetitionOutcomeDto(places, presentation);
    }

    private static IReadOnlyList<FinalPlacementDto> ProjectPlacementAwardPlaces(
        IReadOnlyList<Stage> stages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>> matchesByStage,
        IReadOnlyDictionary<EntryId, string> names)
    {
        var instructions = ResolvePlacementAwards.Execute(stages, matchesByStage);
        return instructions.Count == 0
            ? []
            : [
            .. instructions.Select(instruction =>
            {
                var displayName = EntryDisplayNames.Resolve(names, instruction.EntryId)
                    ?? instruction.EntryId.Value.ToString();
                return new FinalPlacementDto(
                    instruction.Rank,
                    instruction.EntryId.Value,
                    displayName);
            })
        ];
    }

    private static IReadOnlyList<FinalPlacementDto>? ProjectStandingOutcomePlaces(
        Competition competition,
        IReadOnlyList<Stage> stages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>> matchesByStage)
    {
        var reference = ResolveReferenceStage(competition, stages);
        if (reference is null)
        {
            return null;
        }

        var matches = matchesByStage.TryGetValue(reference.Id, out var list) ? list : [];
        var section = ConsultationAssembler.ProjectStandingsForStage(competition, reference, matches);
        if (!section.Applicable)
        {
            return null;
        }

        var overall = section.Tables.FirstOrDefault(table =>
            string.Equals(table.Scope, ConsultationAssembler.ScopeOverall, StringComparison.Ordinal));
        return overall is null || overall.Rows.Count == 0
            ? null
            : [
            .. overall.Rows.Select(row => new FinalPlacementDto(
                row.Position,
                row.EntryId,
                row.DisplayName))
        ];
    }

    private static OverviewStandingCompactDto? BuildStandingCompact(
        Competition competition,
        IReadOnlyList<Stage> stages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>> matchesByStage)
    {
        // Operational In-progress / Completed — not useful during construction.
        if (competition.Status is not (
            CompetitionStatus.Running or
            CompetitionStatus.Suspended or
            CompetitionStatus.Completed or
            CompetitionStatus.Archived))
        {
            return null;
        }

        var reference = ResolveReferenceStage(competition, stages);
        if (reference is null)
        {
            return null;
        }

        var matches = matchesByStage.TryGetValue(reference.Id, out var list) ? list : [];
        var section = ConsultationAssembler.ProjectStandingsForStage(competition, reference, matches);
        return !section.Applicable || section.Tables.Count == 0
            ? null
            : new OverviewStandingCompactDto(
            reference.Id.Value,
            reference.Name.Value,
            [
                .. section.Tables.Select(table => new OverviewStandingCompactTableDto(
                    table.Scope,
                    table.GroupId,
                    table.GroupName,
                    [
                        .. table.Rows.Select(row => new OverviewStandingCompactRowDto(
                            row.Position,
                            row.EntryId,
                            row.DisplayName,
                            row.Played,
                            row.Points))
                    ]))
            ]);
    }

    /// <summary>
    /// Reference stage for compact standing:
    /// first Running or Suspended in StageIds order; else last Completed in StageIds order; else null.
    /// For Overview display, Suspended is treated as Running. Multiple candidates → first wins (no error).
    /// </summary>
    public static Stage? ResolveReferenceStage(
        Competition competition,
        IReadOnlyList<Stage> stages)
    {
        var byId = stages.ToDictionary(stage => stage.Id);
        Stage? firstActive = null;
        Stage? lastCompleted = null;

        foreach (var stageId in competition.StageIds)
        {
            if (!byId.TryGetValue(stageId, out var stage))
            {
                continue;
            }

            if (firstActive is null && stage.Status is StageStatus.Running or StageStatus.Suspended)
            {
                firstActive = stage;
            }

            if (stage.Status == StageStatus.Completed)
            {
                lastCompleted = stage;
            }
        }

        return firstActive ?? lastCompleted;
    }

    private static OverviewMatchCountsDto BuildMatchCounts(
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>> matchesByStage)
    {
        var live = 0;
        var scheduled = 0;
        var finished = 0;
        var postponed = 0;
        var cancelled = 0;

        foreach (var matches in matchesByStage.Values)
        {
            foreach (var match in matches)
            {
                switch (match.Status)
                {
                    case MatchStatus.Live:
                        live++;
                        break;
                    case MatchStatus.Scheduled:
                        scheduled++;
                        break;
                    case MatchStatus.Finished:
                        finished++;
                        break;
                    case MatchStatus.Postponed:
                        postponed++;
                        break;
                    case MatchStatus.Cancelled:
                        cancelled++;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(matchesByStage));
                }
            }
        }

        return new OverviewMatchCountsDto(
            live,
            scheduled,
            finished,
            postponed,
            cancelled,
            live + scheduled + finished + postponed + cancelled);
    }

    /// <summary>
    /// AttentionSummary = Blocking situations only (subset of Situations — not a parallel list).
    /// </summary>
    private static OverviewAttentionSummaryDto BuildAttentionSummary(
        IReadOnlyList<OverviewSituationDto> situations)
    {
        var blocking = situations.Where(situation => situation.Nature == NatureBlocking).ToArray();
        return new OverviewAttentionSummaryDto(blocking.Length, blocking);
    }

    /// <summary>
    /// Builds operational situations from Needs Attention + construction blockers + Suspended.
    /// </summary>
    /// <remarks>
    /// AttentionSummary = Blocking only (not a second calculation).
    /// Structure readiness blockers become situations only during Draft/Ready (construction).
    /// <c>InsufficientParticipants</c> is projected from Needs Attention (single SoT); other org blockers still merge here.
    /// Completion blockers stay on ClosureHint — never merged here.
    /// Identity = Source + TargetType + TargetId; duplicates collapsed.
    /// </remarks>
    private static List<OverviewSituationDto> BuildSituations(
        Competition competition,
        StructureViewDto structureView,
        NeedsAttentionDto attention,
        IReadOnlyList<Stage> stages,
        Dictionary<Guid, Guid> fixtureToMatch)
    {
        var items = attention.Items
            .Select(item =>
            {
                var actionCode = MapAttentionAction(item.Source);
                var parameters = BuildSituationParams(item, structureView);
                return CreateSituation(
                    item.Source,
                    NatureBlocking,
                    item.TargetType,
                    item.TargetId,
                    ResolveMatchIdForAttentionItem(item, stages, fixtureToMatch),
                    actionCode,
                    MapAttentionImpact(item.Source),
                    parameters);
            })
            .ToList();

        switch (competition.Status)
        {
            case CompetitionStatus.Draft or CompetitionStatus.Ready:
                items.AddRange(
                    from blocker in structureView.Readiness.Blockers
                    where blocker != StructureViewAssembler.BlockerInsufficientParticipants
                    let actionCode = MapOrgBlockerAction(blocker)
                    select CreateSituation(
                        blocker,
                        NatureBlocking,
                        "Structure",
                        competition.Id.Value.ToString(),
                        null,
                        actionCode,
                        ImpactBlocksConstruction,
                        new Dictionary<string, string>
                        {
                            ["minimumTeams"] = structureView.Regulation.MinimumTeams.ToString(CultureInfo.InvariantCulture),
                            ["activeCount"] = structureView.Participants.ActiveCount.ToString(CultureInfo.InvariantCulture)
                        }));
                break;
            case CompetitionStatus.Suspended:
                // Domain Resume exists; Host exposure OPEN — informational, not actionable.
                items.Add(CreateSituation(
                    SourceCompetitionSuspended,
                    NatureInformational,
                    "Competition",
                    competition.Id.Value.ToString(),
                    null,
                    null,
                    null,
                    new Dictionary<string, string>()));
                break;
        }

        return DeduplicateSituations(items);
    }

    private static OverviewSituationDto CreateSituation(
        string source,
        string nature,
        string? targetType,
        string? targetId,
        Guid? matchId,
        string? actionCode,
        string? impactCode,
        IReadOnlyDictionary<string, string> parameters) =>
        new(
            source,
            nature,
            targetType,
            targetId,
            matchId,
            Actionable: actionCode is not null,
            actionCode,
            impactCode,
            parameters);

    private static string? MapAttentionAction(string source) =>
        source switch
        {
            NeedsAttentionAssembler.SourceProgressionPending
                or NeedsAttentionAssembler.SourceProgressionConflict => ActionApplyProgression,
            NeedsAttentionAssembler.SourceQualificationPending
                or NeedsAttentionAssembler.SourceQualificationConflict => ActionApplyQualification,
            NeedsAttentionAssembler.SourceInsufficientParticipants =>
                StructureViewAssembler.ActionAddEntry,

            // DrawNoSolution: regenerate/reconfigure lives on Stage — no Host action projected here.
            _ => null
        };

    private static string? MapAttentionImpact(string source) =>
        source switch
        {
            NeedsAttentionAssembler.SourceDrawNoSolution => ImpactBlocksDraw,
            NeedsAttentionAssembler.SourceProgressionPending
                or NeedsAttentionAssembler.SourceProgressionConflict
                or NeedsAttentionAssembler.SourceQualificationPending
                or NeedsAttentionAssembler.SourceQualificationConflict => ImpactBlocksProgression,
            NeedsAttentionAssembler.SourceInsufficientParticipants => ImpactBlocksConstruction,
            _ => null
        };

    private static List<OverviewSituationDto> DeduplicateSituations(List<OverviewSituationDto> items) =>
    [
        .. items
            .GroupBy(situation => (
                situation.Source,
                situation.TargetType ?? string.Empty,
                situation.TargetId ?? string.Empty))
            .Select(group => group.First())
    ];

    private static Dictionary<string, string> BuildSituationParams(
        NeedsAttentionItemDto item,
        StructureViewDto structureView)
    {
        if (item.Source == NeedsAttentionAssembler.SourceInsufficientParticipants)
        {
            // Prefer wire params from Needs Attention when present; else Structure facts.
            return item.Params is { Count: > 0 }
                ? item.Params.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value,
                    StringComparer.Ordinal)
                : new Dictionary<string, string>
            {
                ["minimumTeams"] = structureView.Regulation.MinimumTeams.ToString(CultureInfo.InvariantCulture),
                ["activeCount"] = structureView.Participants.ActiveCount.ToString(CultureInfo.InvariantCulture),
                ["missingCount"] = Math.Max(
                        0,
                        structureView.Regulation.MinimumTeams - structureView.Participants.ActiveCount)
                    .ToString(CultureInfo.InvariantCulture)
            };
        }

        var parameters = new Dictionary<string, string>();
        if (item is not { TargetType: "Slot", TargetId: not null })
        {
            return parameters;
        }

        var parts = item.TargetId.Split(':', 2);
        if (parts.Length != 2)
        {
            return parameters;
        }

        parameters["slotKey"] = parts[1];
        parameters["destinationStageId"] = parts[0];

        return parameters;
    }

    private static string? MapOrgBlockerAction(string blocker) =>
        blocker switch
        {
            StructureViewAssembler.BlockerInsufficientParticipants => StructureViewAssembler.ActionAddEntry,
            StructureViewAssembler.BlockerMissingStage
                or StructureViewAssembler.BlockerMissingStructure
                or StructureViewAssembler.BlockerMissingPotRules
                or StructureViewAssembler.BlockerCupBracketInvalid =>
                StructureViewAssembler.ActionConfigureStructure,
            _ => null
        };

    private static IReadOnlyList<OverviewActionDto> BuildActions(
        Competition competition,
        IReadOnlyList<Stage> stages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>> matchesByStage,
        StructureViewDto structureView,
        NeedsAttentionDto attention,
        CompletionAnalysis? completion,
        Dictionary<Guid, Guid> fixtureToMatch)
    {
        var competitionOpen = competition.Status is not (CompetitionStatus.Completed or CompetitionStatus.Archived);

        var actions = structureView.Actions
            .Select(code => new OverviewActionDto(
                code,
                Guaranteed: false,
                StageId: structureView.Format.PrimaryStageId))
            .ToList();

        if (!competitionOpen)
        {
            if (competition.Status == CompetitionStatus.Completed)
            {
                actions.Add(new OverviewActionDto(ActionArchiveCompetition, Guaranteed: false));
            }

            return DeduplicateActions(actions);
        }

        if (competition is { Status: CompetitionStatus.Draft, StageIds.Count: > 0 }
            && competition.Entries.Any(entry => entry.Status == EntryStatus.Active))
        {
            actions.Add(new OverviewActionDto(ActionPrepareCompetition, Guaranteed: false));
        }

        if (competition.Status == CompetitionStatus.Ready)
        {
            actions.Add(new OverviewActionDto(ActionStartCompetition, Guaranteed: false));
        }

        foreach (var stage in stages)
        {
            var stageParams = new Dictionary<string, string> { ["stageName"] = stage.Name.Value };
            switch (stage.Status)
            {
                case StageStatus.Draft:
                    actions.Add(new OverviewActionDto(
                        ActionPrepareStage,
                        Guaranteed: false,
                        stage.Id.Value,
                        Params: stageParams));
                    break;
                case StageStatus.Ready:
                    actions.Add(new OverviewActionDto(
                        ActionStartStage,
                        Guaranteed: false,
                        stage.Id.Value,
                        Params: stageParams));
                    break;
                case StageStatus.Running:
                case StageStatus.Suspended:
                case StageStatus.Completed:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stages));
            }

            foreach (var draw in stage.Draws.Where(candidate => candidate.Status != DrawStatus.Cancelled))
            {
                if (draw is { Status: DrawStatus.Draft, Resolution.State: DrawResolutionState.Resolved, Kind: DrawResolutionKind.Slot or DrawResolutionKind.Group })
                {
                    actions.Add(new OverviewActionDto(
                        ActionPublishAndApplyDraw,
                        Guaranteed: false,
                        stage.Id.Value,
                        draw.Id.Value));
                }

                if (draw is { Status: DrawStatus.Published, Resolution.State: DrawResolutionState.Resolved, Kind: DrawResolutionKind.Slot or DrawResolutionKind.Group }
                    && !DrawAppliedState.IsApplied(draw, stage))
                {
                    actions.Add(new OverviewActionDto(
                        ActionApplyDraw,
                        Guaranteed: false,
                        stage.Id.Value,
                        draw.Id.Value));
                }
            }
        }

        if (structureView.Readiness.ReadyForMaterialization
            && competition.Status is CompetitionStatus.Draft or CompetitionStatus.Ready
            && structureView.Format.PrimaryStageId is { } materializeStageId)
        {
            actions.Add(new OverviewActionDto(
                ActionMaterializeMatches,
                Guaranteed: false,
                materializeStageId));
        }

        if (TryEvaluateSwissGenerateNextRound(
                competition,
                structureView,
                stages,
                matchesByStage,
                out var swissStageId,
                out _) is { Ready: true }
            && swissStageId is { } generateStageId)
        {
            var stage = stages.First(candidate => candidate.Id.Value == generateStageId);
            var nextRound = stage.Matchdays.Count == 0
                ? 1
                : stage.Matchdays.Max(matchday => matchday.Number) + 1;
            actions.Add(new OverviewActionDto(
                ActionGenerateNextRound,
                Guaranteed: false,
                generateStageId,
                Params: new Dictionary<string, string>
                {
                    ["stageName"] = stage.Name.Value,
                    ["roundIndex"] = nextRound.ToString(CultureInfo.InvariantCulture),
                    ["plannedRounds"] = (stage.SwissSettings?.RoundCount ?? 0)
                        .ToString(CultureInfo.InvariantCulture)
                }));
        }

        foreach (var stage in stages)
        {
            if (!TryDescribeFromSlotsOpportunity(competition, stage, out var occupiedSlotCount))
            {
                continue;
            }

            actions.Add(new OverviewActionDto(
                ActionMaterializeFromOccupiedSlots,
                Guaranteed: false,
                stage.Id.Value,
                Params: new Dictionary<string, string>
                {
                    ["stageName"] = stage.Name.Value,
                    ["occupiedSlotCount"] = occupiedSlotCount.ToString(CultureInfo.InvariantCulture)
                }));
        }

        if (structureView.Readiness.ReadyForSchedule
            && structureView.Format.PrimaryStageId is { } scheduleStageId)
        {
            actions.Add(new OverviewActionDto(
                ActionGenerateSchedule,
                Guaranteed: false,
                scheduleStageId));
            actions.Add(new OverviewActionDto(
                ActionApplySchedule,
                Guaranteed: false,
                scheduleStageId));
        }

        var liveMatches = matchesByStage
            .SelectMany(pair => pair.Value
                .Where(match => match.Status == MatchStatus.Live)
                .Select(match => (StageId: pair.Key.Value, Match: match)))
            .Take(3);
        foreach (var (stageId, match) in liveMatches)
        {
            actions.Add(new OverviewActionDto(
                ActionFinishMatch,
                Guaranteed: false,
                stageId,
                MatchId: match.Id.Value));
        }

        var scheduledMatches = matchesByStage
            .SelectMany(pair => pair.Value
                .Where(match => match.Status == MatchStatus.Scheduled)
                .Select(match => (StageId: pair.Key.Value, Match: match)))
            .Take(3);
        foreach (var (stageId, match) in scheduledMatches)
        {
            actions.Add(new OverviewActionDto(
                ActionStartMatch,
                Guaranteed: false,
                stageId,
                MatchId: match.Id.Value));
        }

        foreach (var item in attention.Items)
        {
            switch (item.Source)
            {
                case NeedsAttentionAssembler.SourceProgressionPending
                    or NeedsAttentionAssembler.SourceProgressionConflict:
                    {
                        var fixtureId = ResolveMaterializedFixtureIdForAttentionItem(item, stages);
                        Guid? matchId = fixtureId is not null &&
                                        fixtureToMatch.TryGetValue(fixtureId.Value, out var mid)
                            ? mid
                            : null;
                        var stageId = ResolveStageIdFromAttention(item, stages);
                        actions.Add(new OverviewActionDto(
                            ActionApplyProgression,
                            Guaranteed: false,
                            stageId,
                            MatchId: matchId,
                            FixtureId: fixtureId));
                        break;
                    }

                case NeedsAttentionAssembler.SourceQualificationPending
                    or NeedsAttentionAssembler.SourceQualificationConflict:
                    {
                        var stageId = ResolveStageIdFromAttention(item, stages);
                        actions.Add(new OverviewActionDto(
                            ActionApplyQualification,
                            Guaranteed: false,
                            stageId));
                        break;
                    }
            }
        }

        if (completion?.CanCompleteNormally == true)
        {
            actions.Add(new OverviewActionDto(ActionCompleteCompetition, Guaranteed: false));
        }

        return DeduplicateActions(actions);
    }

    private static Guid? ResolveStageIdFromAttention(NeedsAttentionItemDto item, IReadOnlyList<Stage> stages)
    {
        if (item.TargetType == "Stage" && Guid.TryParse(item.TargetId, out var stageId))
        {
            return stageId;
        }

        if (item is { TargetType: "Slot", TargetId: not null })
        {
            var prefix = item.TargetId.Split(':', 2)[0];
            if (Guid.TryParse(prefix, out var fromSlot))
            {
                return fromSlot;
            }
        }

        switch (item.TargetType)
        {
            case "Fixture" when Guid.TryParse(item.TargetId, out var fixtureId):
                {
                    foreach (var stage in stages)
                    {
                        if (stage.Rounds.SelectMany(round => round.Fixtures)
                                .Any(fixture => fixture.Id.Value == fixtureId)
                            || stage.Matchdays.SelectMany(matchday => matchday.Fixtures)
                                .Any(fixture => fixture.Id.Value == fixtureId))
                        {
                            return stage.Id.Value;
                        }
                    }

                    break;
                }

            case "Draw" when Guid.TryParse(item.TargetId, out var drawId):
                {
                    foreach (var stage in stages)
                    {
                        if (stage.Draws.Any(draw => draw.Id.Value == drawId))
                        {
                            return stage.Id.Value;
                        }
                    }

                    break;
                }
        }

        return stages.Count > 0 ? stages[0].Id.Value : null;
    }

    private static IReadOnlyList<OverviewActionDto> DeduplicateActions(List<OverviewActionDto> actions) =>
    [
        .. actions
            .GroupBy(action => (
                action.Code,
                action.StageId,
                action.DrawId,
                action.MatchId,
                action.FixtureId))
            .Select(group => group.First())
    ];

    /// <summary>
    /// Preparation structural tip priority — first matching projected action wins.
    /// From-slots is handled separately before this scan (<see cref="ResolveNaturalProgression"/>).
    /// PrepareCompetition / StartCompetition stay in availableActions only — never naturalProgression.
    /// AddEntry is never a tip (Teams / Needs attention).
    /// </summary>
    internal static readonly string[] ConstructionStructuralProgressionPriority =
    [
        ActionPrepareStage,
        ActionStartStage,
        ActionMaterializeMatches,
        ActionPublishAndApplyDraw,
        ActionApplyDraw
    ];

    /// <summary>
    /// In-progress structural tip priority — first matching <see cref="OverviewActionDto.Code"/> wins.
    /// Semantic order (not incidental list order). No consultation / match-hub fallback tip.
    /// </summary>
    internal static readonly string[] InProgressStructuralProgressionPriority =
    [
        ActionMaterializeFromOccupiedSlots,
        ActionGenerateNextRound,
        ActionPublishAndApplyDraw,
        ActionApplyDraw,
        ActionApplyProgression,
        ActionApplyQualification,
        ActionPrepareStage,
        ActionStartStage,
        ActionCompleteCompetition
    ];

    /// <summary>
    /// Natural progression hint: one structural tip, or null when none.
    /// Draft/Ready: from-slots or <see cref="ConstructionStructuralProgressionPriority"/> —
    /// null is a valid calm Construction state (no ContinueStructure fallback).
    /// Running/Suspended: <see cref="InProgressStructuralProgressionPriority"/> —
    /// null is a valid calm-competition outcome (not OpenMatches fallback).
    /// </summary>
    private static OverviewNaturalProgressionDto? ResolveNaturalProgression(
        Competition competition,
        IReadOnlyList<OverviewActionDto> actions,
        bool fromSlotsOpportunity) =>
        competition.Status switch
        {
            // From-slots Cup before championship/groups MaterializeMatches — avoid concurrent
            // "create matches" tips when occupied Cup slots are ready.
            CompetitionStatus.Draft or CompetitionStatus.Ready when fromSlotsOpportunity =>
                new OverviewNaturalProgressionDto(ActionMaterializeFromOccupiedSlots),
            CompetitionStatus.Draft or CompetitionStatus.Ready =>
                ResolveConstructionStructuralProgression(actions),
            CompetitionStatus.Running or CompetitionStatus.Suspended =>
                ResolveInProgressStructuralProgression(actions),
            _ => null
        };

    /// <summary>
    /// Picks the highest-priority structural transition during Preparation (Draft/Ready).
    /// Returns null when none — valid calm Construction state (SPA may still show lifecycle alone).
    /// </summary>
    internal static OverviewNaturalProgressionDto? ResolveConstructionStructuralProgression(
        IReadOnlyList<OverviewActionDto> actions) =>
        (from code in ConstructionStructuralProgressionPriority where actions.Any(action => action.Code == code) select new OverviewNaturalProgressionDto(code)).FirstOrDefault();

    /// <summary>
    /// Picks the highest-priority structural transition among projected actions.
    /// Returns null when none — valid In-progress calm state.
    /// </summary>
    internal static OverviewNaturalProgressionDto? ResolveInProgressStructuralProgression(
        IReadOnlyList<OverviewActionDto> actions) =>
        (from code in InProgressStructuralProgressionPriority where actions.Any(action => action.Code == code) select new OverviewNaturalProgressionDto(code)).FirstOrDefault();

    /// <summary>
    /// Evaluates whether Swiss <see cref="ActionGenerateNextRound"/> is an opportunity.
    /// Mirrors GenerateNextRound preconditions without inventing pairing rules.
    /// </summary>
    private static (bool Ready, Guid StageId)? TryEvaluateSwissGenerateNextRound(
        Competition competition,
        StructureViewDto structureView,
        IReadOnlyList<Stage> stages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>> matchesByStage,
        out Guid? stageId,
        out IReadOnlyList<string> blockers)
    {
        stageId = null;
        blockers = [];
        if (structureView.Format.Kind is not StructureFormatKind.Swiss
            || structureView.Format.PrimaryStageId is not { } primaryId)
        {
            return null;
        }

        var stage = stages.FirstOrDefault(candidate => candidate.Id.Value == primaryId);
        if (stage?.IsSwiss != true || stage.SwissSettings is null)
        {
            return null;
        }

        stageId = stage.Id.Value;
        var codes = new List<string>();
        var activeCount = competition.Entries.Count(entry => entry.Status == EntryStatus.Active);
        if (activeCount < 2)
        {
            codes.Add(BlockerSwissInsufficientParticipants);
        }

        if (stage.Status is not StageStatus.Running)
        {
            codes.Add(BlockerSwissStageNotRunning);
        }

        var planned = stage.SwissSettings.RoundCount;
        var generated = stage.Matchdays.Count;
        if (generated >= planned)
        {
            codes.Add(BlockerSwissRoundsComplete);
        }
        else if (generated > 0)
        {
            var previous = stage.Matchdays.Max(matchday => matchday.Number);
            if (!IsSwissRoundFullyFinished(stage, previous, matchesByStage))
            {
                codes.Add(BlockerSwissAwaitingRoundResults);
            }
        }

        if (competition.Status is CompetitionStatus.Suspended
            or CompetitionStatus.Completed
            or CompetitionStatus.Archived)
        {
            codes.Add(BlockerSwissStageNotRunning);
        }

        var ready = codes.Count == 0;
        blockers = codes;
        return (ready, stage.Id.Value);
    }

    private static bool IsSwissRoundFullyFinished(
        Stage stage,
        int roundIndex,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>> matchesByStage)
    {
        var matchday = stage.Matchdays.FirstOrDefault(candidate => candidate.Number == roundIndex);
        if (matchday is null)
        {
            return false;
        }

        var attached = matchesByStage.TryGetValue(stage.Id, out var list)
            ? list.ToDictionary(match => match.Id)
            : [];

        foreach (var fixture in matchday.Fixtures)
        {
            if (fixture.Attachments.Count == 0)
            {
                return false;
            }

            foreach (var matchId in fixture.MatchIds)
            {
                if (!attached.TryGetValue(matchId, out var match) || match.Status != MatchStatus.Finished)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Opportunity for from-slots materialization: Cup stage Draft/Ready, enough occupied slots
    /// not yet covered by a complete SlotA/B fixture. Does not invent pairing.
    /// </summary>
    private static bool TryDescribeFromSlotsOpportunity(
        Competition competition,
        Stage stage,
        out int occupiedSlotCount)
    {
        occupiedSlotCount = 0;
        if (competition.Status is not (CompetitionStatus.Draft or CompetitionStatus.Ready or CompetitionStatus.Running))
        {
            return false;
        }

        if (stage.Status is not (StageStatus.Draft or StageStatus.Ready))
        {
            return false;
        }

        if (!IsCupStage(stage))
        {
            return false;
        }

        var occupiedKeys = stage.Slots
            .Where(slot => slot.EntryId is not null)
            .Select(slot => slot.SlotKey)
            .ToArray();
        occupiedSlotCount = occupiedKeys.Length;
        if (occupiedSlotCount < 2)
        {
            return false;
        }

        var covered = CupSlotCoverage.GetSlotsCoveredByCompleteFixtures(stage);
        var uncoveredOccupied = occupiedKeys.Count(key => !covered.Contains(key));
        return uncoveredOccupied >= 2;
    }

    private static bool IsCupStage(Stage stage) =>
        stage.Rounds.Count > 0 && stage.Groups.Count == 0 && stage.Matchdays.Count == 0;

    private static IReadOnlyList<OverviewNavigationHintDto> BuildNavigationHints(
        Competition competition,
        IReadOnlyList<OverviewSituationDto> situations,
        IReadOnlyList<Stage> stages,
        Dictionary<Guid, Guid> fixtureToMatch)
    {
        var hints = new List<OverviewNavigationHintDto>
        {
            new("Competition", competition.Id.Value.ToString(), null, null, competition.Id.Value),
            new("Structure", competition.Id.Value.ToString(), null, null, competition.Id.Value)
        };
        hints.AddRange(stages.Select(stage =>
            new OverviewNavigationHintDto("Stage", stage.Id.Value.ToString(), null, stage.Id.Value, competition.Id.Value)));

        foreach (var situation in situations)
        {
            if (situation.TargetType is null || situation.TargetId is null)
            {
                continue;
            }

            Guid? stageId = null;
            switch (situation.TargetType)
            {
                case "Slot":
                    {
                        var prefix = situation.TargetId.Split(':', 2)[0];
                        if (Guid.TryParse(prefix, out var parsed))
                        {
                            stageId = parsed;
                        }

                        break;
                    }

                case "Stage" when Guid.TryParse(situation.TargetId, out var sid):
                    stageId = sid;
                    break;
                case "Fixture" when Guid.TryParse(situation.TargetId, out var fixtureId):
                    stageId = FindStageIdForFixture(stages, fixtureId);
                    break;
            }

            hints.Add(new OverviewNavigationHintDto(
                situation.TargetType,
                situation.TargetId,
                situation.MatchId,
                stageId,
                competition.Id.Value));
        }

        // Explicit Fixture → Match navigation when attachments are known (avoids SPA join).
        foreach (var (fixtureId, matchId) in fixtureToMatch)
        {
            hints.Add(new OverviewNavigationHintDto(
                "Fixture",
                fixtureId.ToString(),
                matchId,
                FindStageIdForFixture(stages, fixtureId),
                competition.Id.Value));
        }

        return
        [
            .. hints
                .GroupBy(hint => (hint.TargetType, hint.TargetId, hint.MatchId))
                .Select(group => group.First())
        ];
    }

    private static Guid? ResolveMatchIdForAttentionItem(
        NeedsAttentionItemDto item,
        IReadOnlyList<Stage> stages,
        Dictionary<Guid, Guid> fixtureToMatch)
    {
        var fixtureId = ResolveMaterializedFixtureIdForAttentionItem(item, stages);
        return fixtureId is not null && fixtureToMatch.TryGetValue(fixtureId.Value, out var matchId) ? matchId : null;
    }

    /// <summary>
    /// UI overlay only: resolves a materialized fixture Guid for a Needs Attention item.
    /// Path identity remains <c>SourcePairKey</c>; FixtureId is never the structural source.
    /// </summary>
    private static Guid? ResolveMaterializedFixtureIdForAttentionItem(
        NeedsAttentionItemDto item,
        IReadOnlyList<Stage> stages)
    {
        if (item is { TargetType: "Fixture", TargetId: not null }
            && Guid.TryParse(item.TargetId, out var fixtureId))
        {
            return fixtureId;
        }

        if (item is not
            {
                TargetType: "Slot", TargetId: not null, Source: NeedsAttentionAssembler.SourceProgressionPending
                or NeedsAttentionAssembler.SourceProgressionConflict
            })
        {
            return null;
        }

        var parts = item.TargetId.Split(':', 2);
        if (parts.Length != 2 || !Guid.TryParse(parts[0], out var destinationStageId)) return null;
        var slotKey = parts[1];
        foreach (var stage in stages)
        {
            var rules = stage.Regulation.ProgressionRules;
            if (rules is null)
            {
                continue;
            }

            foreach (var path in rules.Paths)
            {
                if (path.Destination.StageId.Value == destinationStageId
                    && string.Equals(path.Destination.SlotKey, slotKey, StringComparison.Ordinal))
                {
                    return FindMaterializedFixtureIdByPairKey(stage, path.SourcePairKey);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Post-materialize overlay: PairKey → bound Fixture Guid when present.
    /// </summary>
    private static Guid? FindMaterializedFixtureIdByPairKey(Stage stage, string sourcePairKey) =>
        stage.FindFixtureByBracketPairKey(sourcePairKey)?.Id.Value;

    private static Guid? FindStageIdForFixture(IReadOnlyList<Stage> stages, Guid fixtureId)
    {
        foreach (var stage in stages)
        {
            if (stage.Rounds.SelectMany(round => round.Fixtures).Any(fixture => fixture.Id.Value == fixtureId)
                || stage.Matchdays.SelectMany(matchday => matchday.Fixtures)
                    .Any(fixture => fixture.Id.Value == fixtureId))
            {
                return stage.Id.Value;
            }
        }

        return null;
    }

    private static Dictionary<Guid, Guid> BuildFixtureToMatchMap(IReadOnlyList<Stage> stages)
    {
        var map = new Dictionary<Guid, Guid>();
        foreach (var stage in stages)
        {
            var fixtures = stage.Rounds.SelectMany(round => round.Fixtures)
                .Concat(stage.Matchdays.SelectMany(matchday => matchday.Fixtures));
            foreach (var fixture in fixtures)
            {
                var attachment = fixture.Attachments.OrderBy(item => item.LegIndex).FirstOrDefault();
                if (attachment is not null)
                {
                    map[fixture.Id.Value] = attachment.MatchId.Value;
                }
            }
        }

        return map;
    }
}

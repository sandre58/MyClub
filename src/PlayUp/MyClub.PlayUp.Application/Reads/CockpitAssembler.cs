// -----------------------------------------------------------------------
// <copyright file="CockpitAssembler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Assembles the Cockpit Read projection from competition state (Phase 16.1).
/// </summary>
/// <remarks>
/// Composes existing Application diagnostics — does not re-implement Domain invariants.
/// Competition Prepare/Start are Domain-only today and are intentionally not projected as actions.
/// </remarks>
public static class CockpitAssembler
{
    /// <summary>Cycle reading: construction (Draft/Ready).</summary>
    public const string CycleConstruction = "Construction";

    /// <summary>Cycle reading: competition in progress (Running/Suspended).</summary>
    public const string CycleInProgress = "InProgress";

    /// <summary>Cycle reading: completed.</summary>
    public const string CycleCompleted = "Completed";

    /// <summary>Cycle reading: archived.</summary>
    public const string CycleArchived = "Archived";

    /// <summary>Situation nature: blocking attention.</summary>
    public const string NatureBlocking = "Blocking";

    /// <summary>Situation nature: informational (minimal V1).</summary>
    public const string NatureInformational = "Informational";

    /// <summary>Impact: construction / organisation progress blocked.</summary>
    public const string ImpactBlocksConstruction = "BlocksConstruction";

    /// <summary>Impact: draw pipeline cannot produce a usable result.</summary>
    public const string ImpactBlocksDraw = "BlocksDraw";

    /// <summary>Impact: sporting progression / qualification cannot advance.</summary>
    public const string ImpactBlocksProgression = "BlocksProgression";

    /// <summary>Transition readiness: materialize matches path.</summary>
    public const string TransitionMaterializeMatches = "MaterializeMatches";

    /// <summary>Transition readiness: draw path identifiable (structure/pots/bracket).</summary>
    public const string TransitionDraw = "Draw";

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

    /// <summary>Apply draw.</summary>
    public const string ActionApplyDraw = "ApplyDraw";

    /// <summary>Materialize matches.</summary>
    public const string ActionMaterializeMatches = "MaterializeMatches";

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

    /// <summary>Continue organisation (natural progression).</summary>
    public const string ProgressionContinueOrganisation = "ContinueOrganisation";

    /// <summary>Open matches (natural progression).</summary>
    public const string ProgressionOpenMatches = "OpenMatches";

    /// <summary>Open consultation (natural progression).</summary>
    public const string ProgressionOpenConsultation = "OpenConsultation";

    /// <summary>
    /// Builds the Cockpit view.
    /// </summary>
    /// <param name="competition">Loaded competition.</param>
    /// <param name="stages">Stages in competition order.</param>
    /// <param name="matchesByStage">Matches keyed by stage.</param>
    /// <returns>Cockpit projection DTO.</returns>
    public static CockpitViewDto Assemble(
        Competition competition,
        IReadOnlyList<Stage> stages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>> matchesByStage)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stages);
        ArgumentNullException.ThrowIfNull(matchesByStage);

        var organisation = OrganisationViewAssembler.Assemble(competition, stages);
        var attention = NeedsAttentionAssembler.Assemble(competition, stages, matchesByStage);
        var completion = competition.Status is CompetitionStatus.Running or CompetitionStatus.Suspended
            ? CompletionAnalyzer.Analyze(competition, stages, matchesByStage)
            : null;

        var fixtureToMatch = BuildFixtureToMatchMap(stages);
        var situations = BuildSituations(competition, organisation, attention, stages, fixtureToMatch);
        var attentionSummary = BuildAttentionSummary(situations);

        var matchCounts = BuildMatchCounts(matchesByStage);
        var operationalFocus = BuildOperationalFocus(competition, stages, matchesByStage, matchCounts);
        var dimensions = BuildDimensions(competition, organisation, stages, matchCounts);
        var actions = BuildActions(competition, stages, matchesByStage, organisation, attention, completion, fixtureToMatch);
        var progression = ResolveNaturalProgression(competition, organisation, completion, attentionSummary.Count);
        var closure = new CockpitClosureHintDto(
            completion?.CanCompleteNormally ?? false,
            completion?.Reasons.Select(reason => reason.Code).ToArray() ?? []);
        var navigation = BuildNavigationHints(competition, situations, stages, fixtureToMatch);

        return new CockpitViewDto(
            competition.Id.Value,
            competition.Name.Value,
            competition.Status,
            competition.CompletionMode,
            BuildCycleReading(competition.Status),
            dimensions,
            operationalFocus,
            situations,
            attentionSummary,
            actions,
            progression,
            closure,
            navigation);
    }

    private static CockpitCycleReadingDto BuildCycleReading(CompetitionStatus status) =>
        status switch
        {
            CompetitionStatus.Draft or CompetitionStatus.Ready => new CockpitCycleReadingDto(CycleConstruction),
            CompetitionStatus.Running or CompetitionStatus.Suspended => new CockpitCycleReadingDto(CycleInProgress),
            CompetitionStatus.Completed => new CockpitCycleReadingDto(CycleCompleted),
            CompetitionStatus.Archived => new CockpitCycleReadingDto(CycleArchived),
            _ => new CockpitCycleReadingDto(CycleConstruction)
        };

    private static CockpitConstructionDimensionsDto BuildDimensions(
        Competition competition,
        OrganisationViewDto organisation,
        IReadOnlyList<Stage> stages,
        CockpitMatchCountsDto matchCounts)
    {
        var inConstruction = competition.Status is CompetitionStatus.Draft or CompetitionStatus.Ready;
        var running = competition.Status is CompetitionStatus.Running or CompetitionStatus.Suspended;
        var hasOrgBlockers = organisation.Readiness.Blockers.Count > 0;

        var teamsProminence = inConstruction
            ? hasOrgBlockers &&
              organisation.Readiness.Blockers.Contains(OrganisationViewAssembler.BlockerInsufficientParticipants)
                ? ProminenceDominant
                : ProminencePresent
            : ProminenceCondensed;

        var structureProminence = inConstruction
            ? organisation.Readiness.Blockers.Any(blocker =>
                blocker is OrganisationViewAssembler.BlockerMissingStage
                    or OrganisationViewAssembler.BlockerMissingStructure
                    or OrganisationViewAssembler.BlockerMissingPotRules
                    or OrganisationViewAssembler.BlockerCupBracketInvalid)
                ? ProminenceDominant
                : ProminencePresent
            : ProminenceCondensed;

        var regulationProminence = inConstruction ? ProminencePresent : ProminenceCondensed;
        var matchesProminence = matchCounts.Total == 0
            ? inConstruction ? ProminenceCondensed : ProminenceAbsent
            : running
                ? matchCounts.Live > 0 ? ProminenceDominant : ProminencePresent
                : ProminenceCondensed;

        return new CockpitConstructionDimensionsDto(
            new CockpitDimensionDto(
                teamsProminence,
                new Dictionary<string, string>
                {
                    ["activeCount"] = organisation.Participants.ActiveCount.ToString(CultureInfo.InvariantCulture),
                    ["occupyingCount"] =
                        organisation.Participants.OccupyingCount.ToString(CultureInfo.InvariantCulture),
                    ["minimumTeams"] =
                        organisation.Regulation.MinimumTeams.ToString(CultureInfo.InvariantCulture),
                    ["maximumTeams"] =
                        organisation.Regulation.MaximumTeams.ToString(CultureInfo.InvariantCulture)
                }),
            new CockpitDimensionDto(
                structureProminence,
                new Dictionary<string, string>
                {
                    ["formatKind"] = organisation.Format.Kind?.ToString() ?? "None",
                    ["groupCount"] = organisation.Structure.GroupCount.ToString(CultureInfo.InvariantCulture),
                    ["roundCount"] = organisation.Structure.RoundCount.ToString(CultureInfo.InvariantCulture),
                    ["matchdayCount"] =
                        organisation.Structure.MatchdayCount.ToString(CultureInfo.InvariantCulture),
                    ["slotCount"] = organisation.Structure.SlotCount.ToString(CultureInfo.InvariantCulture)
                }),
            BuildRegulationDimension(competition, organisation, stages, regulationProminence, inConstruction),
            new CockpitDimensionDto(
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
    /// Reuses <see cref="OrganisationViewAssembler"/> readiness — does not invent Domain validation.
    /// PrepareStage / StartStage are status transitions, not regulation content gates — not projected here.
    /// Competition Prepare/Start remain Host-OPEN (gap D) — never claimed executable.
    /// </remarks>
    private static CockpitRegulationDimensionDto BuildRegulationDimension(
        Competition competition,
        OrganisationViewDto organisation,
        IReadOnlyList<Stage> stages,
        string prominence,
        bool inConstruction)
    {
        var stageSummary = BuildStageRegulationSummary(organisation, stages);
        var mutable = competition.Status is CompetitionStatus.Draft or CompetitionStatus.Ready;
        var readiness = inConstruction
            ? BuildRegulationTransitionReadiness(organisation)
            : [];

        return new CockpitRegulationDimensionDto(
            prominence,
            organisation.Regulation,
            stageSummary,
            mutable,
            readiness);
    }

    private static CockpitStageRegulationSummaryDto? BuildStageRegulationSummary(
        OrganisationViewDto organisation,
        IReadOnlyList<Stage> stages)
    {
        if (organisation.Format.PrimaryStageId is not { } primaryId)
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

        return new CockpitStageRegulationSummaryDto(
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

    private static List<CockpitTransitionReadinessDto> BuildRegulationTransitionReadiness(
        OrganisationViewDto organisation)
    {
        var blockers = organisation.Readiness.Blockers;
        var readiness = new List<CockpitTransitionReadinessDto>();

        // Championship never uses the draw path — omit Draw readiness (avoid ready=false with empty blockers).
        if (organisation.Format.Kind is not StructureFormatKind.Championship)
        {
            readiness.Add(
                new CockpitTransitionReadinessDto(
                    TransitionDraw,
                    organisation.Readiness.ReadyForDraw,
                    organisation.Readiness.ReadyForDraw ? [] : blockers));
        }

        readiness.Add(
            new CockpitTransitionReadinessDto(
                TransitionMaterializeMatches,
                organisation.Readiness.ReadyForMaterialization,
                organisation.Readiness.ReadyForMaterialization ? [] : blockers));

        return readiness;
    }

    private static CockpitOperationalFocusDto BuildOperationalFocus(
        Competition competition,
        IReadOnlyList<Stage> stages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>> matchesByStage,
        CockpitMatchCountsDto matchCounts)
    {
        var names = EntryDisplayNames.ToMap(competition);
        var stageFocus = stages
            .Select(stage => new CockpitStageFocusDto(stage.Id.Value, stage.Name.Value, stage.Status))
            .ToArray();

        var draws = stages
            .SelectMany(stage => stage.Draws
                .Where(draw => draw.Status != DrawStatus.Cancelled)
                .Select(draw => new CockpitDrawFocusDto(
                    stage.Id.Value,
                    draw.Id.Value,
                    draw.Kind,
                    draw.Status,
                    draw.Resolution.State,
                    DrawAppliedState.IsApplied(draw, stage))))
            .ToArray();

        var upcoming = matchesByStage
            .SelectMany(pair => pair.Value
                .Where(match => match.Status == MatchStatus.Scheduled)
                .Select(match =>
                {
                    DateTimeOffset? scheduledAt = null;
                    var stage = stages.First(candidate => candidate.Id.Equals(pair.Key));
                    if (stage.TryGetMatchPlacement(match.Id, out var placement))
                    {
                        scheduledAt = placement.Start;
                    }

                    return new CockpitUpcomingMatchDto(
                        match.Id.Value,
                        pair.Key.Value,
                        scheduledAt,
                        EntryDisplayNames.Resolve(names, match.HomeEntryId) ?? match.HomeEntryId.Value.ToString(),
                        EntryDisplayNames.Resolve(names, match.AwayEntryId) ?? match.AwayEntryId.Value.ToString());
                }))
            .OrderBy(item => item.ScheduledAt ?? DateTimeOffset.MaxValue)
            .ThenBy(item => item.MatchId)
            .Take(8)
            .ToArray();

        return new CockpitOperationalFocusDto(stageFocus, draws, matchCounts, upcoming);
    }

    private static CockpitMatchCountsDto BuildMatchCounts(
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

        return new CockpitMatchCountsDto(
            live,
            scheduled,
            finished,
            postponed,
            cancelled,
            live + scheduled + finished + postponed + cancelled);
    }

    /// <summary>
    /// AttentionSummary V1 = Blocking situations only (subset of Situations — not a parallel list).
    /// </summary>
    private static CockpitAttentionSummaryDto BuildAttentionSummary(
        IReadOnlyList<CockpitSituationDto> situations)
    {
        var blocking = situations.Where(situation => situation.Nature == NatureBlocking).ToArray();
        return new CockpitAttentionSummaryDto(blocking.Length, blocking);
    }

    /// <summary>
    /// Builds pilotage situations from Needs Attention + construction blockers + Suspended.
    /// </summary>
    /// <remarks>
    /// AttentionSummary V1 = Blocking only (not a second calculation).
    /// Organisation readiness blockers become situations only during Draft/Ready (construction).
    /// Completion blockers stay on ClosureHint — never merged here.
    /// Identity = Source + TargetType + TargetId; duplicates collapsed.
    /// </remarks>
    private static List<CockpitSituationDto> BuildSituations(
        Competition competition,
        OrganisationViewDto organisation,
        NeedsAttentionDto attention,
        IReadOnlyList<Stage> stages,
        Dictionary<Guid, Guid> fixtureToMatch)
    {
        var items = new List<CockpitSituationDto>();

        foreach (var item in attention.Items)
        {
            var actionCode = MapAttentionAction(item.Source);
            items.Add(CreateSituation(
                item.Source,
                NatureBlocking,
                item.TargetType,
                item.TargetId,
                ResolveMatchIdForAttentionItem(item, stages, fixtureToMatch),
                actionCode,
                MapAttentionImpact(item.Source),
                BuildSituationParams(item)));
        }

        if (competition.Status is CompetitionStatus.Draft or CompetitionStatus.Ready)
        {
            foreach (var blocker in organisation.Readiness.Blockers)
            {
                var actionCode = MapOrgBlockerAction(blocker);
                items.Add(CreateSituation(
                    blocker,
                    NatureBlocking,
                    "Organisation",
                    competition.Id.Value.ToString(),
                    null,
                    actionCode,
                    ImpactBlocksConstruction,
                    new Dictionary<string, string>
                    {
                        ["minimumTeams"] = organisation.Regulation.MinimumTeams.ToString(CultureInfo.InvariantCulture),
                        ["activeCount"] = organisation.Participants.ActiveCount.ToString(CultureInfo.InvariantCulture),
                    }));
            }
        }

        if (competition.Status == CompetitionStatus.Suspended)
        {
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
        }

        return DeduplicateSituations(items);
    }

    private static CockpitSituationDto CreateSituation(
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

            // DrawNoSolution: regenerate/reconfigure lives on Stage — no Host action projected here.
            _ => null,
        };

    private static string? MapAttentionImpact(string source) =>
        source switch
        {
            NeedsAttentionAssembler.SourceDrawNoSolution => ImpactBlocksDraw,
            NeedsAttentionAssembler.SourceProgressionPending
                or NeedsAttentionAssembler.SourceProgressionConflict
                or NeedsAttentionAssembler.SourceQualificationPending
                or NeedsAttentionAssembler.SourceQualificationConflict => ImpactBlocksProgression,
            _ => null,
        };

    private static List<CockpitSituationDto> DeduplicateSituations(List<CockpitSituationDto> items) =>
    [
        .. items
            .GroupBy(situation => (
                situation.Source,
                situation.TargetType ?? string.Empty,
                situation.TargetId ?? string.Empty))
            .Select(group => group.First())
    ];

    private static Dictionary<string, string> BuildSituationParams(NeedsAttentionItemDto item)
    {
        var parameters = new Dictionary<string, string>();
        if (item is not { TargetType: "Slot", TargetId: not null }) return parameters;
        var parts = item.TargetId.Split(':', 2);
        if (parts.Length != 2) return parameters;
        parameters["slotKey"] = parts[1];
        parameters["destinationStageId"] = parts[0];

        return parameters;
    }

    private static string? MapOrgBlockerAction(string blocker) =>
        blocker switch
        {
            OrganisationViewAssembler.BlockerInsufficientParticipants => OrganisationViewAssembler.ActionAddEntry,
            OrganisationViewAssembler.BlockerMissingStage
                or OrganisationViewAssembler.BlockerMissingStructure
                or OrganisationViewAssembler.BlockerMissingPotRules
                or OrganisationViewAssembler.BlockerCupBracketInvalid =>
                OrganisationViewAssembler.ActionConfigureStructure,
            _ => null
        };

    private static IReadOnlyList<CockpitActionDto> BuildActions(
        Competition competition,
        IReadOnlyList<Stage> stages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>> matchesByStage,
        OrganisationViewDto organisation,
        NeedsAttentionDto attention,
        CompletionAnalysis? completion,
        Dictionary<Guid, Guid> fixtureToMatch)
    {
        var competitionOpen = competition.Status is not (CompetitionStatus.Completed or CompetitionStatus.Archived);

        var actions = organisation.Actions
            .Select(code => new CockpitActionDto(
                code,
                Guaranteed: false,
                StageId: organisation.Format.PrimaryStageId))
            .ToList();

        if (!competitionOpen)
        {
            if (competition.Status == CompetitionStatus.Completed)
            {
                actions.Add(new CockpitActionDto(ActionArchiveCompetition, Guaranteed: false));
            }

            return DeduplicateActions(actions);
        }

        foreach (var stage in stages)
        {
            var stageParams = new Dictionary<string, string> { ["stageName"] = stage.Name.Value };
            switch (stage.Status)
            {
                case StageStatus.Draft:
                    actions.Add(new CockpitActionDto(
                        ActionPrepareStage,
                        Guaranteed: false,
                        stage.Id.Value,
                        Params: stageParams));
                    break;
                case StageStatus.Ready:
                    actions.Add(new CockpitActionDto(
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
                if (draw is { Status: DrawStatus.Draft, Resolution.State: DrawResolutionState.Resolved })
                {
                    actions.Add(new CockpitActionDto(
                        ActionPublishDraw,
                        Guaranteed: false,
                        stage.Id.Value,
                        draw.Id.Value));
                }

                if (draw is { Status: DrawStatus.Published, Resolution.State: DrawResolutionState.Resolved, Kind: DrawResolutionKind.Slot or DrawResolutionKind.Pairing }
                    && !DrawAppliedState.IsApplied(draw, stage))
                {
                    actions.Add(new CockpitActionDto(
                        ActionApplyDraw,
                        Guaranteed: false,
                        stage.Id.Value,
                        draw.Id.Value));
                }
            }
        }

        if (organisation.Readiness.ReadyForMaterialization
            && competition.Status is CompetitionStatus.Draft or CompetitionStatus.Ready
            && organisation.Format.PrimaryStageId is { } materializeStageId)
        {
            actions.Add(new CockpitActionDto(
                ActionMaterializeMatches,
                Guaranteed: false,
                materializeStageId));
        }

        if (organisation.Readiness.ReadyForSchedule
            && organisation.Format.PrimaryStageId is { } scheduleStageId)
        {
            actions.Add(new CockpitActionDto(
                ActionGenerateSchedule,
                Guaranteed: false,
                scheduleStageId));
            actions.Add(new CockpitActionDto(
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
            actions.Add(new CockpitActionDto(
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
            actions.Add(new CockpitActionDto(
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
                        var fixtureId = ResolveSourceFixtureIdForAttentionItem(item, stages);
                        Guid? matchId = fixtureId is not null &&
                                        fixtureToMatch.TryGetValue(fixtureId.Value, out var mid)
                            ? mid
                            : null;
                        var stageId = ResolveStageIdFromAttention(item, stages);
                        actions.Add(new CockpitActionDto(
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
                        actions.Add(new CockpitActionDto(
                            ActionApplyQualification,
                            Guaranteed: false,
                            stageId));
                        break;
                    }
            }
        }

        if (completion?.CanCompleteNormally == true)
        {
            actions.Add(new CockpitActionDto(ActionCompleteCompetition, Guaranteed: false));
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

    private static IReadOnlyList<CockpitActionDto> DeduplicateActions(List<CockpitActionDto> actions) =>
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

    private static CockpitNaturalProgressionDto? ResolveNaturalProgression(
        Competition competition,
        OrganisationViewDto organisation,
        CompletionAnalysis? completion,
        int attentionCount) =>
        competition.Status switch
        {
            CompetitionStatus.Draft or CompetitionStatus.Ready when organisation.Readiness.ReadyForMaterialization =>
                new CockpitNaturalProgressionDto(ActionMaterializeMatches),
            CompetitionStatus.Draft or CompetitionStatus.Ready when organisation.Readiness.ReadyForDraw =>
                new CockpitNaturalProgressionDto(ActionPublishDraw),
            CompetitionStatus.Draft or CompetitionStatus.Ready =>
                new CockpitNaturalProgressionDto(ProgressionContinueOrganisation),
            CompetitionStatus.Running or CompetitionStatus.Suspended when completion?.CanCompleteNormally == true =>
                new CockpitNaturalProgressionDto(ActionCompleteCompetition),
            CompetitionStatus.Running or CompetitionStatus.Suspended when attentionCount > 0 =>
                new CockpitNaturalProgressionDto(ProgressionOpenMatches),
            CompetitionStatus.Running or CompetitionStatus.Suspended =>
                new CockpitNaturalProgressionDto(ProgressionOpenMatches),
            CompetitionStatus.Completed or CompetitionStatus.Archived =>
                new CockpitNaturalProgressionDto(ProgressionOpenConsultation),
            _ => null
        };

    private static IReadOnlyList<CockpitNavigationHintDto> BuildNavigationHints(
        Competition competition,
        IReadOnlyList<CockpitSituationDto> situations,
        IReadOnlyList<Stage> stages,
        Dictionary<Guid, Guid> fixtureToMatch)
    {
        var hints = new List<CockpitNavigationHintDto>
        {
            new("Competition", competition.Id.Value.ToString(), null, null, competition.Id.Value),
            new("Organisation", competition.Id.Value.ToString(), null, null, competition.Id.Value)
        };
        hints.AddRange(stages.Select(stage =>
            new CockpitNavigationHintDto("Stage", stage.Id.Value.ToString(), null, stage.Id.Value, competition.Id.Value)));

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

            hints.Add(new CockpitNavigationHintDto(
                situation.TargetType,
                situation.TargetId,
                situation.MatchId,
                stageId,
                competition.Id.Value));
        }

        // Explicit Fixture → Match navigation when attachments are known (avoids SPA join).
        foreach (var (fixtureId, matchId) in fixtureToMatch)
        {
            hints.Add(new CockpitNavigationHintDto(
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
        var fixtureId = ResolveSourceFixtureIdForAttentionItem(item, stages);
        return fixtureId is not null && fixtureToMatch.TryGetValue(fixtureId.Value, out var matchId) ? matchId : null;
    }

    /// <summary>
    /// Resolves the source fixture for a Needs Attention item when TargetType is Fixture,
    /// or when a Slot-targeted progression item maps back through ProgressionRules.
    /// </summary>
    private static Guid? ResolveSourceFixtureIdForAttentionItem(
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
        if (parts.Length != 2
            || !Guid.TryParse(parts[0], out var destinationStageId)) return null;
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
                    return path.SourceFixtureId.Value;
                }
            }
        }

        return null;
    }

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

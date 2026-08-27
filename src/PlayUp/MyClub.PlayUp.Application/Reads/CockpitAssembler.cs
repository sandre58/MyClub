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
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Assembles the Cockpit Read projection from competition state (Phase 16.1).
/// </summary>
/// <remarks>
/// Composes existing Application diagnostics — does not re-implement Domain invariants.
/// Competition Prepare/Start are Host-executable and projected as available actions
/// opportunities when Domain preconditions appear satisfied (R19 — not execution guarantees).
/// They are intentional lifecycle transitions (L7) — not automatically elevated to naturalProgression.
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
        var operationalFocus = BuildOperationalFocus(
            competition,
            stages,
            matchesByStage,
            matchCounts,
            organisation.Format.Kind);
        var dimensions = BuildDimensions(competition, organisation, stages, matchCounts, matchesByStage);
        var actions = BuildActions(competition, stages, matchesByStage, organisation, attention, completion, fixtureToMatch);
        var fromSlotsOpportunities = stages
            .Where(stage => TryDescribeFromSlotsOpportunity(competition, stage, out _))
            .Select(stage => stage.Id)
            .ToArray();
        var swissNextRoundReady = TryEvaluateSwissGenerateNextRound(
            competition,
            organisation,
            stages,
            matchesByStage,
            out _,
            out _) is { Ready: true };
        var progression = ResolveNaturalProgression(
            competition,
            organisation,
            completion,
            attentionSummary.Count,
            fromSlotsOpportunities.Length > 0,
            swissNextRoundReady);
        var closure = new CockpitClosureHintDto(
            completion?.CanCompleteNormally ?? false,
            completion?.Reasons.Select(reason => reason.Code).ToArray() ?? []);
        var navigation = BuildNavigationHints(competition, situations, stages, fixtureToMatch);

        return new CockpitViewDto(
            competition.Id.Value,
            competition.Name.Value,
            competition.Status,
            competition.CompletionMode,
            BuildPeriod(stages),
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

    /// <summary>
    /// Derives operational calendar bounds from match placements (min/max start).
    /// Null when no placement exists — not declared competition season dates.
    /// </summary>
    private static CockpitCompetitionPeriodDto? BuildPeriod(IReadOnlyList<Stage> stages)
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

        if (earliest is null && latest is null)
        {
            return null;
        }

        return new CockpitCompetitionPeriodDto(earliest, latest);
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
        CockpitMatchCountsDto matchCounts,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>> matchesByStage)
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

        var structureFacts = new Dictionary<string, string>
        {
            ["formatKind"] = organisation.Format.Kind?.ToString() ?? "None",
            ["groupCount"] = organisation.Structure.GroupCount.ToString(CultureInfo.InvariantCulture),
            ["roundCount"] = organisation.Structure.RoundCount.ToString(CultureInfo.InvariantCulture),
            ["matchdayCount"] =
                organisation.Structure.MatchdayCount.ToString(CultureInfo.InvariantCulture),
            ["slotCount"] = organisation.Structure.SlotCount.ToString(CultureInfo.InvariantCulture)
        };
        if (organisation.Format.Kind != StructureFormatKind.Swiss)
        {
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
                new CockpitDimensionDto(structureProminence, structureFacts),
                BuildRegulationDimension(
                    competition,
                    organisation,
                    stages,
                    matchesByStage,
                    regulationProminence,
                    inConstruction),
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

        structureFacts["swissRoundCount"] =
            (organisation.Structure.SwissRoundCount ?? 0).ToString(CultureInfo.InvariantCulture);
        structureFacts["swissByeCount"] = stages
            .Where(stage => stage.IsSwiss)
            .Sum(stage => stage.SwissByeHistory.Count)
            .ToString(CultureInfo.InvariantCulture);

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
            new CockpitDimensionDto(structureProminence, structureFacts),
            BuildRegulationDimension(
                competition,
                organisation,
                stages,
                matchesByStage,
                regulationProminence,
                inConstruction),
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
    /// Competition Prepare/Start are projected in <see cref="BuildActions"/>, not as regulation readiness.
    /// </remarks>
    private static CockpitRegulationDimensionDto BuildRegulationDimension(
        Competition competition,
        OrganisationViewDto organisation,
        IReadOnlyList<Stage> stages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>> matchesByStage,
        string prominence,
        bool inConstruction)
    {
        var stageSummary = BuildStageRegulationSummary(organisation, stages);
        var mutable = competition.Status is CompetitionStatus.Draft or CompetitionStatus.Ready;
        var readiness = inConstruction
            ? BuildRegulationTransitionReadiness(organisation)
            : [];
        AppendFromSlotsTransitionReadiness(competition, stages, readiness);
        AppendSwissGenerateNextRoundReadiness(competition, organisation, stages, matchesByStage, readiness);

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
        var kind = organisation.Format.Kind;

        // Championship / Swiss never use the draw path — omit Draw readiness.
        if (kind is not StructureFormatKind.Championship and not StructureFormatKind.Swiss)
        {
            readiness.Add(
                new CockpitTransitionReadinessDto(
                    TransitionDraw,
                    organisation.Readiness.ReadyForDraw,
                    organisation.Readiness.ReadyForDraw ? [] : blockers));
        }

        // Swiss uses GenerateNextRound — omit MaterializeMatches (always false with empty blockers).
        if (kind is not StructureFormatKind.Swiss)
        {
            readiness.Add(
                new CockpitTransitionReadinessDto(
                    TransitionMaterializeMatches,
                    organisation.Readiness.ReadyForMaterialization,
                    organisation.Readiness.ReadyForMaterialization ? [] : blockers));
        }

        return readiness;
    }

    private static void AppendSwissGenerateNextRoundReadiness(
        Competition competition,
        OrganisationViewDto organisation,
        IReadOnlyList<Stage> stages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>> matchesByStage,
        List<CockpitTransitionReadinessDto> readiness)
    {
        if (organisation.Format.Kind is not StructureFormatKind.Swiss)
        {
            return;
        }

        if (competition.Status is CompetitionStatus.Completed or CompetitionStatus.Archived)
        {
            return;
        }

        var evaluation = TryEvaluateSwissGenerateNextRound(
            competition,
            organisation,
            stages,
            matchesByStage,
            out _,
            out var blockers);
        if (evaluation is null)
        {
            return;
        }

        readiness.Add(
            new CockpitTransitionReadinessDto(
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
        List<CockpitTransitionReadinessDto> readiness)
    {
        if (competition.Status is not (CompetitionStatus.Draft or CompetitionStatus.Ready or CompetitionStatus.Running))
        {
            return;
        }

        if (!stages.Any(stage => TryDescribeFromSlotsOpportunity(competition, stage, out _))) return;
        readiness.Add(
            new CockpitTransitionReadinessDto(
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

    private static CockpitOperationalFocusDto BuildOperationalFocus(
        Competition competition,
        IReadOnlyList<Stage> stages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>> matchesByStage,
        CockpitMatchCountsDto matchCounts,
        StructureFormatKind? formatKind)
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

        var swissByes = stages
            .Where(stage => stage.IsSwiss)
            .SelectMany(stage => stage.SwissByeHistory.Select(bye => new CockpitSwissByeDto(
                stage.Id.Value,
                bye.RoundIndex,
                bye.EntryId.Value,
                EntryDisplayNames.Resolve(names, bye.EntryId) ?? bye.EntryId.Value.ToString())))
            .OrderBy(bye => bye.RoundIndex)
            .ThenBy(bye => bye.EntryId)
            .ToArray();

        var (recentUnit, nextUnit) = BuildTemporalSportUnits(competition, stages, matchesByStage, names);

        return new CockpitOperationalFocusDto(
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
    /// Game-rule facts for Vue d'ensemble Règlement — ReferenceStage only.
    /// </summary>
    internal static CockpitReferenceStageGameRulesDto? BuildReferenceStageGameRules(
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
        var standing = reference.Regulation.StandingRules.Points;
        var tie = TieFormat.OrDefaultOneLeg(reference.Regulation.TieFormat);
        var formatKind = ResolveGameRulesFormatKind(reference, competitionFormatKind);

        return new CockpitReferenceStageGameRulesDto(
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
        StructureFormatKind? competitionFormatKind)
    {
        if (stage.IsSwiss)
        {
            return StructureFormatKind.Swiss;
        }

        if (competitionFormatKind is { } kind)
        {
            return kind;
        }

        if (stage.Rounds.Count > 0 && stage.Matchdays.Count == 0)
        {
            return StructureFormatKind.Cup;
        }

        if (stage.Groups.Count > 0)
        {
            return StructureFormatKind.Groups;
        }

        return StructureFormatKind.Championship;
    }

    /// <summary>
    /// Dernières / Prochaines on ReferenceStage only — full Matchday or Round units (no caps).
    /// </summary>
    internal static (CockpitSportUnitDto? Recent, CockpitSportUnitDto? Next) BuildTemporalSportUnits(
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
            if (slice.Matches.Exists(match =>
                    match.Status is MatchStatus.Live or MatchStatus.Finished))
            {
                recentSlice = slice;
                break;
            }
        }

        SportUnitSlice? nextSlice = null;
        if (recentSlice is null)
        {
            nextSlice = units.FirstOrDefault(slice => slice.Matches.Count > 0);
        }
        else
        {
            nextSlice = units.FirstOrDefault(slice =>
                slice.Order > recentSlice.Order && slice.Matches.Count > 0);
        }

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

        if (stage.Rounds.Count > 0)
        {
            var slices = new List<SportUnitSlice>(stage.Rounds.Count);
            for (var index = 0; index < stage.Rounds.Count; index++)
            {
                var round = stage.Rounds[index];
                slices.Add(new SportUnitSlice(
                    index + 1,
                    UnitKindRound,
                    round.Id.Value.ToString(),
                    MatchdayNumber: null,
                    round.Name,
                    CollectUnitMatches(round.Fixtures, matchesById)));
            }

            return slices;
        }

        return [];
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

    private static CockpitSportUnitDto ProjectSportUnit(
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

                return new CockpitMatchLineDto(
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

        return new CockpitSportUnitDto(
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

    private static CockpitStandingCompactDto? BuildStandingCompact(
        Competition competition,
        IReadOnlyList<Stage> stages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>> matchesByStage)
    {
        // Pilotage En cours / Terminée — not useful during construction.
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
        if (!section.Applicable || section.Tables.Count == 0)
        {
            return null;
        }

        return new CockpitStandingCompactDto(
            reference.Id.Value,
            reference.Name.Value,
            [
                .. section.Tables.Select(table => new CockpitStandingCompactTableDto(
                    table.Scope,
                    table.GroupId,
                    table.GroupName,
                    [
                        .. table.Rows.Select(row => new CockpitStandingCompactRowDto(
                            row.Position,
                            row.EntryId,
                            row.DisplayName,
                            row.Played,
                            row.Points))
                    ]))
            ]);
    }

    /// <summary>
    /// Reference stage for compact standing (V1):
    /// first Running or Suspended in StageIds order; else last Completed in StageIds order; else null.
    /// For Vue d'ensemble display, Suspended is treated as Running. Multiple candidates → first wins (no error).
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
        var items = (from item in attention.Items let actionCode = MapAttentionAction(item.Source) select CreateSituation(item.Source, NatureBlocking, item.TargetType, item.TargetId, ResolveMatchIdForAttentionItem(item, stages, fixtureToMatch), actionCode, MapAttentionImpact(item.Source), BuildSituationParams(item))).ToList();

        switch (competition.Status)
        {
            case CompetitionStatus.Draft or CompetitionStatus.Ready:
                items.AddRange(from blocker in organisation.Readiness.Blockers let actionCode = MapOrgBlockerAction(blocker) select CreateSituation(blocker, NatureBlocking, "Organisation", competition.Id.Value.ToString(), null, actionCode, ImpactBlocksConstruction, new Dictionary<string, string> { ["minimumTeams"] = organisation.Regulation.MinimumTeams.ToString(CultureInfo.InvariantCulture), ["activeCount"] = organisation.Participants.ActiveCount.ToString(CultureInfo.InvariantCulture) }));
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
            _ => null
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

        if (competition is { Status: CompetitionStatus.Draft, StageIds.Count: > 0 }
            && competition.Entries.Any(entry => entry.Status == EntryStatus.Active))
        {
            actions.Add(new CockpitActionDto(ActionPrepareCompetition, Guaranteed: false));
        }

        if (competition.Status == CompetitionStatus.Ready)
        {
            actions.Add(new CockpitActionDto(ActionStartCompetition, Guaranteed: false));
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

        if (TryEvaluateSwissGenerateNextRound(
                competition,
                organisation,
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
            actions.Add(new CockpitActionDto(
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

            actions.Add(new CockpitActionDto(
                ActionMaterializeFromOccupiedSlots,
                Guaranteed: false,
                stage.Id.Value,
                Params: new Dictionary<string, string>
                {
                    ["stageName"] = stage.Name.Value,
                    ["occupiedSlotCount"] = occupiedSlotCount.ToString(CultureInfo.InvariantCulture)
                }));
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
        int attentionCount,
        bool fromSlotsOpportunity,
        bool swissGenerateNextRoundReady) =>
        competition.Status switch
        {
            // From-slots (later Cup stage) before skeleton MaterializeMatches — avoid concurrent
            // "create matches" vs "configure confrontations" when multi-stage slots are ready.
            CompetitionStatus.Draft or CompetitionStatus.Ready when fromSlotsOpportunity =>
                new CockpitNaturalProgressionDto(ActionMaterializeFromOccupiedSlots),
            CompetitionStatus.Draft or CompetitionStatus.Ready when organisation.Readiness.ReadyForMaterialization =>
                new CockpitNaturalProgressionDto(ActionMaterializeMatches),
            CompetitionStatus.Draft or CompetitionStatus.Ready when organisation.Readiness.ReadyForDraw =>
                new CockpitNaturalProgressionDto(ActionPublishDraw),
            CompetitionStatus.Draft or CompetitionStatus.Ready =>
                new CockpitNaturalProgressionDto(ProgressionContinueOrganisation),
            CompetitionStatus.Running or CompetitionStatus.Suspended when fromSlotsOpportunity && attentionCount == 0 =>
                new CockpitNaturalProgressionDto(ActionMaterializeFromOccupiedSlots),
            CompetitionStatus.Running or CompetitionStatus.Suspended when swissGenerateNextRoundReady && attentionCount == 0 =>
                new CockpitNaturalProgressionDto(ActionGenerateNextRound),
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

    /// <summary>
    /// Evaluates whether Swiss <see cref="ActionGenerateNextRound"/> is an opportunity.
    /// Mirrors GenerateNextRound preconditions without inventing pairing rules.
    /// </summary>
    private static (bool Ready, Guid StageId)? TryEvaluateSwissGenerateNextRound(
        Competition competition,
        OrganisationViewDto organisation,
        IReadOnlyList<Stage> stages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>> matchesByStage,
        out Guid? stageId,
        out IReadOnlyList<string> blockers)
    {
        stageId = null;
        blockers = [];
        if (organisation.Format.Kind is not StructureFormatKind.Swiss
            || organisation.Format.PrimaryStageId is not { } primaryId)
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
            : new Dictionary<MatchId, Match>();

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

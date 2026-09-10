// -----------------------------------------------------------------------
// <copyright file="OverviewViewDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Aggregated Overview Read projection (Phase 16.1) — Application interpretation, not Domain.
/// </summary>
/// <remarks>
/// One projection among several Read Surfaces (R22). Does not replace workspace / structure / attention endpoints.
/// Available actions are opportunities based on known state (R19) — not execution guarantees.
/// Organizer-facing copy lives in the SPA i18n layer (codes + facts only on the wire).
/// </remarks>
/// <param name="CompetitionId">Competition identity.</param>
/// <param name="Name">Display name.</param>
/// <param name="Status">Official Domain lifecycle status (distinct from situations).</param>
/// <param name="CompletionMode">Completion mode when Completed/Archived.</param>
/// <param name="Period">Operational calendar span from match placements (null when none scheduled).</param>
/// <param name="CycleReading">Minimal cycle interpretation for pilotage (machine codes only).</param>
/// <param name="PreparationFocus">
/// Préparation sub-situation (Host-owned): Setup | GeneratedCalendar.
/// Not a cycle code — Draft/Ready stay Construction. SPA composes Prep variants from this field only.
/// </param>
/// <param name="CalendarSummary">
/// Overview calendar synthesis when <paramref name="PreparationFocus"/> is GeneratedCalendar; otherwise null.
/// </param>
/// <param name="CompetitionOutcome">
/// Derived final placements when Terminée can conclude them for the Résultat surface.
/// Sources: PlacementAwardRules + FixtureOutcome, else Championship/Swiss Overall Standing.
/// Includes Host <c>presentation</c> (<c>Winner</c> | <c>Podium</c>) — Read UX hint, not Domain.
/// Null when Abandoned / no presentable Result (no unique winner and not a podium case).
/// </param>
/// <param name="ConstructionDimensions">Équipes · Structure · Règlement · Matchs.</param>
/// <param name="OperationalFocus">Stages, draws, match counters, temporal sport units, compact standing.</param>
/// <param name="Situations">Derived pilotage situations (not persisted alerts).</param>
/// <param name="AttentionSummary">Attention subset derived from <paramref name="Situations"/>.</param>
/// <param name="AvailableActions">Semantic actions/transitions available from known state.</param>
/// <param name="NaturalProgression">
/// One structural tip to highlight, or null when none.
/// Draft/Ready: null is a valid calm Construction state (no ContinueStructure fallback).
/// Running/Suspended: null is a valid calm state (no OpenMatches fallback).
/// Distinct from <paramref name="AvailableActions"/> (full opportunity set).
/// PrepareCompetition / StartCompetition are never elevated here (lifecycle — L7).
/// </param>
/// <param name="ClosureHint">Completion synthesis (distinct from Attention).</param>
/// <param name="NavigationHints">Navigable targets including resolved match ids.</param>
public sealed record OverviewViewDto(
    Guid CompetitionId,
    string Name,
    CompetitionStatus Status,
    CompletionMode? CompletionMode,
    OverviewCompetitionPeriodDto? Period,
    OverviewCycleReadingDto CycleReading,
    string PreparationFocus,
    OverviewCalendarSummaryDto? CalendarSummary,
    CompetitionOutcomeDto? CompetitionOutcome,
    OverviewConstructionDimensionsDto ConstructionDimensions,
    OverviewOperationalFocusDto OperationalFocus,
    IReadOnlyList<OverviewSituationDto> Situations,
    OverviewAttentionSummaryDto AttentionSummary,
    IReadOnlyList<OverviewActionDto> AvailableActions,
    OverviewNaturalProgressionDto? NaturalProgression,
    OverviewClosureHintDto ClosureHint,
    IReadOnlyList<OverviewNavigationHintDto> NavigationHints);

/// <summary>
/// Read projection of competition final placements — not Domain.
/// <paramref name="Presentation"/> is Host UX composition (<c>Winner</c> | <c>Podium</c>), not a Domain concept.
/// </summary>
/// <param name="Places">
/// All determined final placements (full table when Championship Standing is the source).
/// May be partial; missing ranks are omitted.
/// </param>
/// <param name="Presentation">
/// <c>Winner</c> — hero vainqueur; <c>Podium</c> — Top-3 mise en scène.
/// </param>
public sealed record CompetitionOutcomeDto(
    IReadOnlyList<FinalPlacementDto> Places,
    string Presentation);

/// <summary>One final placement in a <see cref="CompetitionOutcomeDto"/>.</summary>
/// <param name="Rank">1-based final rank (Standing Position for Championship V1).</param>
/// <param name="EntryId">Entry identity.</param>
/// <param name="DisplayName">Entry display name for SPA.</param>
public sealed record FinalPlacementDto(int Rank, Guid EntryId, string DisplayName);

/// <summary>
/// Overview calendar synthesis for Préparation / GeneratedCalendar (not Match hub duplication).
/// </summary>
/// <param name="MatchdayCount">Number of matchdays on the reference Championship stage.</param>
/// <param name="MatchCount">Total matches in the competition projection (same as matchCounts.total).</param>
/// <param name="Matchdays">First matchdays preview (capped by Host).</param>
/// <param name="NextMatch">Earliest upcoming Scheduled match when identifiable; otherwise null.</param>
public sealed record OverviewCalendarSummaryDto(
    int MatchdayCount,
    int MatchCount,
    IReadOnlyList<OverviewCalendarMatchdayPreviewDto> Matchdays,
    OverviewCalendarNextMatchDto? NextMatch);

/// <summary>One matchday line in the calendar overview preview.</summary>
/// <param name="MatchdayNumber">Matchday number.</param>
/// <param name="MatchCount">Matches attached on that matchday.</param>
public sealed record OverviewCalendarMatchdayPreviewDto(int MatchdayNumber, int MatchCount);

/// <summary>Next rendez-vous hint for calendar overview (Scheduled only).</summary>
public sealed record OverviewCalendarNextMatchDto(
    Guid MatchId,
    Guid StageId,
    int? MatchdayNumber,
    DateTimeOffset? ScheduledAt,
    string HomeDisplayName,
    string AwayDisplayName);

/// <summary>
/// Operational competition period derived from placed match starts (Read fact, not Domain season dates).
/// </summary>
/// <param name="Start">Earliest match placement start, if any.</param>
/// <param name="End">Latest match placement start, if any.</param>
public sealed record OverviewCompetitionPeriodDto(DateTimeOffset? Start, DateTimeOffset? End);

/// <summary>Minimal cycle reading (machine codes — UX labels in SPA i18n).</summary>
/// <param name="Code">Construction | InProgress | Completed | Archived.</param>
public sealed record OverviewCycleReadingDto(string Code);

/// <summary>Four construction dimensions (R2).</summary>
public sealed record OverviewConstructionDimensionsDto(
    OverviewDimensionDto Teams,
    OverviewDimensionDto Structure,
    OverviewRegulationDimensionDto Regulation,
    OverviewDimensionDto Matches);

/// <summary>Prominence + machine-readable facts (no organizer prose).</summary>
/// <param name="Prominence">Present | Condensed | Dominant | Absent.</param>
/// <param name="Facts">Optional machine-readable facts for SPA templates.</param>
public sealed record OverviewDimensionDto(
    string Prominence,
    IReadOnlyDictionary<string, string> Facts);

/// <summary>
/// Regulation dimension: factual Competition + Stage summaries and transition-relative readiness.
/// </summary>
/// <remarks>
/// No global isValid / isSatisfactory. Readiness is always relative to a named transition.
/// Competition Regulation (Entry/Match/Standing) ≠ Stage Regulation (Draw/Qualification/Progression/TieFormat).
/// </remarks>
/// <param name="Prominence">Present | Condensed | Dominant | Absent.</param>
/// <param name="Competition">Competition regulation factual summary (always present — Domain requires it).</param>
/// <param name="Stage">Primary stage regulation factual flags when a primary stage exists; otherwise null.</param>
/// <param name="CompetitionRegulationMutable">True when Domain allows ReplaceRegulation (Draft/Ready).</param>
/// <param name="TransitionReadiness">Readiness relative to identified Host-relevant transitions (construction only).</param>
public sealed record OverviewRegulationDimensionDto(
    string Prominence,
    StructureRegulationSummaryDto Competition,
    OverviewStageRegulationSummaryDto? Stage,
    bool CompetitionRegulationMutable,
    IReadOnlyList<OverviewTransitionReadinessDto> TransitionReadiness);

/// <summary>Primary stage regulation facts (presence flags — null optional family ≠ invalid).</summary>
/// <param name="StageId">Primary stage identity.</param>
/// <param name="StageName">Display name.</param>
/// <param name="HasDrawRules">Whether DrawRules are set.</param>
/// <param name="NumberOfPots">PotRules.NumberOfPots when present.</param>
/// <param name="HasQualificationRules">Whether QualificationRules are set.</param>
/// <param name="QualificationPathCount">Path count when qualification rules exist.</param>
/// <param name="HasProgressionRules">Whether ProgressionRules are set.</param>
/// <param name="ProgressionPathCount">Path count when progression rules exist.</param>
/// <param name="HasTieFormat">Whether default TieFormat is set.</param>
public sealed record OverviewStageRegulationSummaryDto(
    Guid StageId,
    string StageName,
    bool HasDrawRules,
    int? NumberOfPots,
    bool HasQualificationRules,
    int QualificationPathCount,
    bool HasProgressionRules,
    int ProgressionPathCount,
    bool HasTieFormat);

/// <summary>Readiness relative to a concrete transition (R5) — not a global regulation validity claim.</summary>
/// <param name="Transition">Stable code (e.g. MaterializeMatches, Draw).</param>
/// <param name="Ready">True when Structure readiness says the transition path is identifiable.</param>
/// <param name="BlockerCodes">Same machine codes as Structure / Situations when not ready.</param>
public sealed record OverviewTransitionReadinessDto(
    string Transition,
    bool Ready,
    IReadOnlyList<string> BlockerCodes);

/// <summary>Operational focus across stages (no single “active stage” fiction).</summary>
/// <param name="Stages">Stage focus lines.</param>
/// <param name="Draws">Draw pipeline projection.</param>
/// <param name="MatchCounts">Match status counters.</param>
/// <param name="SwissByes">Recorded Swiss byes (pairing events — not fixtures/matches).</param>
/// <param name="RecentUnit">
/// Dernières rencontres — last engaged sport unit on ReferenceStage (null → SPA empty state).
/// </param>
/// <param name="NextUnit">
/// Prochaines rencontres — next sport unit after RecentUnit, or first unit before kickoff (null → SPA empty state).
/// </param>
/// <param name="StandingCompact">Compact standing for En cours / Terminée; null when not applicable (Cup / no structure).</param>
/// <param name="ReferenceStageGameRules">
/// Game-rule facts for Vue d'ensemble Règlement (ReferenceStage). Null when no ReferenceStage.
/// SPA picks 2–3 explanatory facts by formatKind — does not dump all fields.
/// </param>
public sealed record OverviewOperationalFocusDto(
    IReadOnlyList<OverviewStageFocusDto> Stages,
    IReadOnlyList<OverviewDrawFocusDto> Draws,
    OverviewMatchCountsDto MatchCounts,
    IReadOnlyList<OverviewSwissByeDto> SwissByes,
    OverviewSportUnitDto? RecentUnit,
    OverviewSportUnitDto? NextUnit,
    OverviewStandingCompactDto? StandingCompact,
    OverviewReferenceStageGameRulesDto? ReferenceStageGameRules);

/// <summary>
/// Machine facts for En cours Règlement — derived from ReferenceStage Domain regulation.
/// Organizer copy is SPA i18n; presence flags enable selective display (not a full regulation dump).
/// </summary>
/// <param name="StageId">Reference stage identity.</param>
/// <param name="StageName">Reference stage display name.</param>
/// <param name="FormatKind">Championship | Groups | Cup | Swiss (competition format, or inferred from stage).</param>
/// <param name="WinPoints">Standing win points.</param>
/// <param name="DrawPoints">Standing draw points.</param>
/// <param name="LossPoints">Standing loss points.</param>
/// <param name="NumberOfPeriods">Match periods.</param>
/// <param name="DurationPerPeriod">Minutes per period.</param>
/// <param name="HasExtraTime">Match ExtraTimePolicy present.</param>
/// <param name="HasPenaltyShootout">Match PenaltyShootoutPolicy present.</param>
/// <param name="NumberOfLegs">Effective tie legs (1 or 2); from stage TieFormat or default one-leg.</param>
/// <param name="AggregateScoring">Whether two-legged ties aggregate scores.</param>
/// <param name="HasTieExtraTime">TieFormat ExtraTimeRule present.</param>
/// <param name="HasTiePenaltyShootout">TieFormat PenaltyShootoutRule present.</param>
/// <param name="SwissPlannedRounds">SwissSettings.RoundCount when Swiss; otherwise null.</param>
public sealed record OverviewReferenceStageGameRulesDto(
    Guid StageId,
    string StageName,
    string FormatKind,
    int WinPoints,
    int DrawPoints,
    int LossPoints,
    int NumberOfPeriods,
    int DurationPerPeriod,
    bool HasExtraTime,
    bool HasPenaltyShootout,
    int NumberOfLegs,
    bool AggregateScoring,
    bool HasTieExtraTime,
    bool HasTiePenaltyShootout,
    int? SwissPlannedRounds);

/// <summary>
/// One sport unit (Matchday or Round) on the ReferenceStage for Vue d'ensemble temporal panels.
/// Full unit — no silent truncation. Organizer unit title copy is SPA i18n from facts.
/// </summary>
/// <param name="StageId">Reference stage identity.</param>
/// <param name="StageName">Reference stage display name.</param>
/// <param name="UnitKind">Matchday | Round.</param>
/// <param name="UnitKey">Stable key (matchday number or round id).</param>
/// <param name="MatchdayNumber">1-based matchday number when UnitKind is Matchday.</param>
/// <param name="RoundName">Domain round name when UnitKind is Round.</param>
/// <param name="MatchCount">Number of matches in this unit (equals Matches.Count).</param>
/// <param name="Matches">All matches in the unit (Live / Finished / Scheduled / …).</param>
public sealed record OverviewSportUnitDto(
    Guid StageId,
    string StageName,
    string UnitKind,
    string UnitKey,
    int? MatchdayNumber,
    string? RoundName,
    int MatchCount,
    IReadOnlyList<OverviewMatchLineDto> Matches);

/// <summary>Swiss bye projection — pairing event, never a fake match.</summary>
/// <param name="StageId">Owning Swiss stage.</param>
/// <param name="RoundIndex">1-based Swiss round / Matchday number.</param>
/// <param name="EntryId">Entry that received the bye.</param>
/// <param name="EntryDisplayName">Display name for SPA.</param>
public sealed record OverviewSwissByeDto(
    Guid StageId,
    int RoundIndex,
    Guid EntryId,
    string EntryDisplayName);

/// <summary>Stage line for operational focus.</summary>
public sealed record OverviewStageFocusDto(Guid StageId, string Name, StageStatus Status);

/// <summary>Draw pipeline projection including Application-derived Applied.</summary>
/// <remarks>Applied is not a Domain status — Publish ≠ Apply (Domain). Derived from occupancy/attachments.</remarks>
public sealed record OverviewDrawFocusDto(
    Guid StageId,
    Guid DrawId,
    DrawResolutionKind Kind,
    DrawStatus Status,
    DrawResolutionState ResolutionState,
    bool IsApplied);

/// <summary>Match status counters across the competition.</summary>
public sealed record OverviewMatchCountsDto(
    int Live,
    int Scheduled,
    int Finished,
    int Postponed,
    int Cancelled,
    int Total);

/// <summary>Match line inside a temporal sport unit (Dernières / Prochaines).</summary>
/// <param name="MatchId">Match identity.</param>
/// <param name="StageId">Owning stage.</param>
/// <param name="Status">Match status (Scheduled, Live, Finished, …).</param>
/// <param name="ScheduledAt">Placement start when known.</param>
/// <param name="HomeDisplayName">Home entry display name.</param>
/// <param name="AwayDisplayName">Away entry display name.</param>
/// <param name="Score">Play score when Finished; null otherwise (Domain has no in-progress score).</param>
public sealed record OverviewMatchLineDto(
    Guid MatchId,
    Guid StageId,
    MatchStatus Status,
    DateTimeOffset? ScheduledAt,
    string HomeDisplayName,
    string AwayDisplayName,
    MatchScoreDto? Score);

/// <summary>Compact standing for Vue d'ensemble — derived from ReferenceStage only (Running or Suspended preferred, else last Completed).</summary>
/// <param name="StageId">Reference stage identity.</param>
/// <param name="StageName">Reference stage display name.</param>
/// <param name="Tables">
/// Overall: one table. Group: one table per group (SPA may show one at a time).
/// Empty collection is not projected — StandingCompact is null instead.
/// </param>
public sealed record OverviewStandingCompactDto(
    Guid StageId,
    string StageName,
    IReadOnlyList<OverviewStandingCompactTableDto> Tables);

/// <summary>One compact standing table (Overall or a single Group).</summary>
/// <param name="Scope">Overall | Group (same codes as Consultation).</param>
/// <param name="GroupId">Group identity when Scope is Group.</param>
/// <param name="GroupName">Group display name when Scope is Group.</param>
/// <param name="Rows">All rows ordered by position (full table — no silent truncation).</param>
public sealed record OverviewStandingCompactTableDto(
    string Scope,
    Guid? GroupId,
    string? GroupName,
    IReadOnlyList<OverviewStandingCompactRowDto> Rows);

/// <summary>One compact standing row — pilotage subset of ConsultationStandingRowDto.</summary>
public sealed record OverviewStandingCompactRowDto(
    int Position,
    Guid EntryId,
    string DisplayName,
    int Played,
    int Points);

/// <summary>Derived situation unit (R8–R9) — codes + targets; copy in SPA i18n.</summary>
/// <remarks>
/// Stable identity = <see cref="Source"/> + <see cref="TargetType"/> + <see cref="TargetId"/> (not translated text).
/// Reason copy = SPA i18n keyed by <see cref="Source"/> (+ <see cref="Params"/>).
/// AttentionSummary V1 = situations with <see cref="Nature"/> = Blocking only.
/// </remarks>
/// <param name="Source">Stable source kind (identity + reason key).</param>
/// <param name="Nature">Blocking | Informational (V1 — Opportunity / richer natures OPEN).</param>
/// <param name="TargetType">Optional target kind.</param>
/// <param name="TargetId">Optional target identity.</param>
/// <param name="MatchId">Resolved match when Fixture target maps to an attachment.</param>
/// <param name="Actionable">True when <see cref="ActionCode"/> is projected (Host opportunity — not an execution guarantee).</param>
/// <param name="ActionCode">Optional related semantic action code.</param>
/// <param name="ImpactCode">Optional machine impact code for SPA i18n; omit when not reliably derivable.</param>
/// <param name="Params">Optional structured params for SPA templates (e.g. slotKey, stageName).</param>
public sealed record OverviewSituationDto(
    string Source,
    string Nature,
    string? TargetType,
    string? TargetId,
    Guid? MatchId,
    bool Actionable,
    string? ActionCode,
    string? ImpactCode,
    IReadOnlyDictionary<string, string> Params);

/// <summary>Attention view derived from situations (not a parallel list).</summary>
public sealed record OverviewAttentionSummaryDto(int Count, IReadOnlyList<OverviewSituationDto> Items);

/// <summary>Semantic action / transition opportunity (R6, R19) — label via SPA i18n.</summary>
/// <param name="Code">Stable machine code.</param>
/// <param name="Guaranteed">Always false — Domain/Application remain authoritative.</param>
/// <param name="StageId">Optional stage context.</param>
/// <param name="DrawId">Optional draw context.</param>
/// <param name="MatchId">Optional match context.</param>
/// <param name="FixtureId">Optional fixture context.</param>
/// <param name="Params">Optional structured params for SPA templates (e.g. stageName).</param>
public sealed record OverviewActionDto(
    string Code,
    bool Guaranteed,
    Guid? StageId = null,
    Guid? DrawId = null,
    Guid? MatchId = null,
    Guid? FixtureId = null,
    IReadOnlyDictionary<string, string>? Params = null);

/// <summary>Natural progression hint (R18) — guide without prescribing; label via SPA i18n.</summary>
public sealed record OverviewNaturalProgressionDto(string Code);

/// <summary>Closure synthesis — distinct from Attention (Completion blockers ≠ À traiter).</summary>
public sealed record OverviewClosureHintDto(
    bool CanCompleteNormally,
    IReadOnlyList<string> BlockerCodes);

/// <summary>Navigation hint for shell / drawer deep-links.</summary>
public sealed record OverviewNavigationHintDto(
    string TargetType,
    string TargetId,
    Guid? MatchId,
    Guid? StageId,
    Guid? CompetitionId);

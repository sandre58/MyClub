// -----------------------------------------------------------------------
// <copyright file="CockpitViewDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Aggregated Cockpit Read projection (Phase 16.1) — Application interpretation, not Domain.
/// </summary>
/// <remarks>
/// One projection among several Read Surfaces (R22). Does not replace workspace / organisation / attention endpoints.
/// Available actions are opportunities based on known state (R19) — not execution guarantees.
/// Organizer-facing copy lives in the SPA i18n layer (codes + facts only on the wire).
/// </remarks>
/// <param name="CompetitionId">Competition identity.</param>
/// <param name="Name">Display name.</param>
/// <param name="Status">Official Domain lifecycle status (distinct from situations).</param>
/// <param name="CompletionMode">Completion mode when Completed/Archived.</param>
/// <param name="CycleReading">Minimal cycle interpretation for pilotage (machine codes only).</param>
/// <param name="ConstructionDimensions">Équipes · Structure · Règlement · Matchs.</param>
/// <param name="OperationalFocus">Stages, draws, match counters, upcoming matches.</param>
/// <param name="Situations">Derived pilotage situations (not persisted alerts).</param>
/// <param name="AttentionSummary">Attention subset derived from <paramref name="Situations"/>.</param>
/// <param name="AvailableActions">Semantic actions/transitions available from known state.</param>
/// <param name="NaturalProgression">Natural next progression hint (replaces workspace nextAction stub).</param>
/// <param name="ClosureHint">Completion synthesis (distinct from Attention).</param>
/// <param name="NavigationHints">Navigable targets including resolved match ids.</param>
public sealed record CockpitViewDto(
    Guid CompetitionId,
    string Name,
    CompetitionStatus Status,
    CompletionMode? CompletionMode,
    CockpitCycleReadingDto CycleReading,
    CockpitConstructionDimensionsDto ConstructionDimensions,
    CockpitOperationalFocusDto OperationalFocus,
    IReadOnlyList<CockpitSituationDto> Situations,
    CockpitAttentionSummaryDto AttentionSummary,
    IReadOnlyList<CockpitActionDto> AvailableActions,
    CockpitNaturalProgressionDto? NaturalProgression,
    CockpitClosureHintDto ClosureHint,
    IReadOnlyList<CockpitNavigationHintDto> NavigationHints);

/// <summary>Minimal cycle reading (machine codes — UX labels in SPA i18n).</summary>
/// <param name="Code">Construction | InProgress | Completed | Archived.</param>
public sealed record CockpitCycleReadingDto(string Code);

/// <summary>Four construction dimensions (R2).</summary>
public sealed record CockpitConstructionDimensionsDto(
    CockpitDimensionDto Teams,
    CockpitDimensionDto Structure,
    CockpitRegulationDimensionDto Regulation,
    CockpitDimensionDto Matches);

/// <summary>Prominence + machine-readable facts (no organizer prose).</summary>
/// <param name="Prominence">Present | Condensed | Dominant | Absent.</param>
/// <param name="Facts">Optional machine-readable facts for SPA templates.</param>
public sealed record CockpitDimensionDto(
    string Prominence,
    IReadOnlyDictionary<string, string> Facts);

/// <summary>Regulation dimension with factual summary only (no satisfaction claim).</summary>
public sealed record CockpitRegulationDimensionDto(
    string Prominence,
    OrganisationRegulationSummaryDto Facts);

/// <summary>Operational focus across stages (no single “active stage” fiction).</summary>
public sealed record CockpitOperationalFocusDto(
    IReadOnlyList<CockpitStageFocusDto> Stages,
    IReadOnlyList<CockpitDrawFocusDto> Draws,
    CockpitMatchCountsDto MatchCounts,
    IReadOnlyList<CockpitUpcomingMatchDto> UpcomingMatches);

/// <summary>Stage line for operational focus.</summary>
public sealed record CockpitStageFocusDto(Guid StageId, string Name, StageStatus Status);

/// <summary>Draw pipeline projection including Application-derived Applied.</summary>
/// <remarks>Applied is not a Domain status — Publish ≠ Apply (Domain). Derived from occupancy/attachments.</remarks>
public sealed record CockpitDrawFocusDto(
    Guid StageId,
    Guid DrawId,
    DrawResolutionKind Kind,
    DrawStatus Status,
    DrawResolutionState ResolutionState,
    bool IsApplied);

/// <summary>Match status counters across the competition.</summary>
public sealed record CockpitMatchCountsDto(
    int Live,
    int Scheduled,
    int Finished,
    int Postponed,
    int Cancelled,
    int Total);

/// <summary>Upcoming scheduled match for pilotage.</summary>
public sealed record CockpitUpcomingMatchDto(
    Guid MatchId,
    Guid StageId,
    DateTimeOffset? ScheduledAt,
    string HomeDisplayName,
    string AwayDisplayName);

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
public sealed record CockpitSituationDto(
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
public sealed record CockpitAttentionSummaryDto(int Count, IReadOnlyList<CockpitSituationDto> Items);

/// <summary>Semantic action / transition opportunity (R6, R19) — label via SPA i18n.</summary>
/// <param name="Code">Stable machine code.</param>
/// <param name="Guaranteed">Always false — Domain/Application remain authoritative.</param>
/// <param name="StageId">Optional stage context.</param>
/// <param name="DrawId">Optional draw context.</param>
/// <param name="MatchId">Optional match context.</param>
/// <param name="FixtureId">Optional fixture context.</param>
/// <param name="Params">Optional structured params for SPA templates (e.g. stageName).</param>
public sealed record CockpitActionDto(
    string Code,
    bool Guaranteed,
    Guid? StageId = null,
    Guid? DrawId = null,
    Guid? MatchId = null,
    Guid? FixtureId = null,
    IReadOnlyDictionary<string, string>? Params = null);

/// <summary>Natural progression hint (R18) — guide without prescribing; label via SPA i18n.</summary>
public sealed record CockpitNaturalProgressionDto(string Code);

/// <summary>Closure synthesis — distinct from Attention (Completion blockers ≠ À traiter).</summary>
public sealed record CockpitClosureHintDto(
    bool CanCompleteNormally,
    IReadOnlyList<string> BlockerCodes);

/// <summary>Navigation hint for shell / drawer deep-links.</summary>
public sealed record CockpitNavigationHintDto(
    string TargetType,
    string TargetId,
    Guid? MatchId,
    Guid? StageId,
    Guid? CompetitionId);

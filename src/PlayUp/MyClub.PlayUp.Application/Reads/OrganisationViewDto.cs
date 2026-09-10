// -----------------------------------------------------------------------
// <copyright file="OrganisationViewDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Assembled Organisation hub read for Slice 2 (not a Domain mirror).
/// </summary>
/// <param name="CompetitionId">Competition identity.</param>
/// <param name="Name">Competition name.</param>
/// <param name="Status">Lifecycle status.</param>
/// <param name="Participants">Participant summary.</param>
/// <param name="Format">Inferred / configured format summary.</param>
/// <param name="Regulation">High-level regulation summary.</param>
/// <param name="Structure">Structure counts (not full Domain graph).</param>
/// <param name="Actions">Available organisation action codes.</param>
/// <param name="Readiness">Application readiness diagnostic for Slice 3.</param>
/// <param name="Stages">Per-stage topology + regulation tokens (Règlement hub Lot 1).</param>
/// <param name="ShortName">Optional abbreviated name.</param>
/// <param name="LogoMediaId">Optional Media Guid for the logo.</param>
/// <param name="ScheduledStart">Optional declared start.</param>
/// <param name="ScheduledEnd">Optional declared end.</param>
public sealed record OrganisationViewDto(
    Guid CompetitionId,
    string Name,
    CompetitionStatus Status,
    OrganisationParticipantsSummaryDto Participants,
    OrganisationFormatSummaryDto Format,
    OrganisationRegulationSummaryDto Regulation,
    OrganisationStructureSummaryDto Structure,
    IReadOnlyList<string> Actions,
    OrganisationReadinessDto Readiness,
    IReadOnlyList<OrganisationStageHubSummaryDto> Stages,
    string? ShortName = null,
    Guid? LogoMediaId = null,
    DateTimeOffset? ScheduledStart = null,
    DateTimeOffset? ScheduledEnd = null);

/// <summary>Participants section.</summary>
/// <param name="ActiveCount">Active entries.</param>
/// <param name="OccupyingCount">Entries still present (Active + Withdrawn — forfait keeps the place).</param>
/// <param name="Entries">Light entry rows.</param>
public sealed record OrganisationParticipantsSummaryDto(
    int ActiveCount,
    int OccupyingCount,
    IReadOnlyList<OrganisationEntryDto> Entries);

/// <summary>Entry row for Organisation.</summary>
/// <param name="EntryId">Entry identity.</param>
/// <param name="DisplayName">Display name.</param>
/// <param name="Status">Entry status.</param>
/// <param name="ShortName">Optional abbreviated name.</param>
/// <param name="LogoMediaId">Optional Media Guid for the logo.</param>
/// <param name="PrimaryColor">Optional primary kit color.</param>
/// <param name="SecondaryColor">Optional secondary kit color.</param>
/// <param name="DeclaredMembers">Declared roster members for this entry.</param>
public sealed record OrganisationEntryDto(
    Guid EntryId,
    string DisplayName,
    EntryStatus Status,
    string? ShortName = null,
    Guid? LogoMediaId = null,
    string? PrimaryColor = null,
    string? SecondaryColor = null,
    IReadOnlyList<DeclaredMemberDto>? DeclaredMembers = null);

/// <summary>Format summary (Application inference — not Domain Format aggregate).</summary>
/// <param name="Kind">Inferred format, or null when structure empty (authoritative for SPA i18n).</param>
/// <param name="PrimaryStageId">Primary stage id when present.</param>
/// <param name="PrimaryStageName">Primary stage name when present.</param>
/// <param name="PrimaryStageStatus">Primary stage status when present.</param>
public sealed record OrganisationFormatSummaryDto(
    StructureFormatKind? Kind,
    Guid? PrimaryStageId,
    string? PrimaryStageName,
    StageStatus? PrimaryStageStatus);

/// <summary>Regulation summary.</summary>
/// <param name="MinimumTeams">EntryRules.MinimumTeams.</param>
/// <param name="MaximumTeams">EntryRules.MaximumTeams.</param>
/// <param name="DurationPerPeriod">Match duration per period.</param>
/// <param name="NumberOfPeriods">Match periods.</param>
/// <param name="WinPoints">Standing win points.</param>
/// <param name="DrawPoints">Standing draw points.</param>
/// <param name="LossPoints">Standing loss points.</param>
/// <param name="AllowedTypes">Authorized disciplinary catalogue types (empty = none).</param>
/// <param name="HalfTimeDuration">Half-time break minutes.</param>
/// <param name="HasExtraTime">Match ExtraTimePolicy present.</param>
/// <param name="ExtraTimeDurationPerPeriod">ET minutes per period when HasExtraTime.</param>
/// <param name="ExtraTimeNumberOfPeriods">ET period count when HasExtraTime.</param>
/// <param name="HasPenaltyShootout">Match PenaltyShootoutPolicy present.</param>
/// <param name="PenaltyInitialKicksPerTeam">TAB initial kicks when HasPenaltyShootout.</param>
/// <param name="RankingCriteria">Ordered standing ranking criteria.</param>
/// <param name="ForfeitWinnerGoals">Administrative forfeit goals for the winning side.</param>
/// <param name="ForfeitLoserGoals">Administrative forfeit goals for the losing side.</param>
public sealed record OrganisationRegulationSummaryDto(
    int MinimumTeams,
    int MaximumTeams,
    int DurationPerPeriod,
    int NumberOfPeriods,
    int WinPoints,
    int DrawPoints,
    int LossPoints,
    IReadOnlyList<DisciplinaryType> AllowedTypes,
    int HalfTimeDuration = 0,
    bool HasExtraTime = false,
    int? ExtraTimeDurationPerPeriod = null,
    int? ExtraTimeNumberOfPeriods = null,
    bool HasPenaltyShootout = false,
    int? PenaltyInitialKicksPerTeam = null,
    IReadOnlyList<RankingCriterion>? RankingCriteria = null,
    int ForfeitWinnerGoals = 0,
    int ForfeitLoserGoals = 0);

/// <summary>Structure counts for the primary stage.</summary>
/// <param name="GroupCount">Groups.</param>
/// <param name="RoundCount">Rounds.</param>
/// <param name="MatchdayCount">Matchdays.</param>
/// <param name="SlotCount">Slots.</param>
/// <param name="HasDrawRules">Whether stage DrawRules are set.</param>
/// <param name="NumberOfPots">PotRules.NumberOfPots when present.</param>
/// <param name="MatchGenerationFormat">Championship / Groups generation mode.</param>
/// <param name="SwissRoundCount">Planned Swiss rounds K when Kind is Swiss.</param>
public sealed record OrganisationStructureSummaryDto(
    int GroupCount,
    int RoundCount,
    int MatchdayCount,
    int SlotCount,
    bool HasDrawRules,
    int? NumberOfPots,
    MatchGenerationFormat MatchGenerationFormat,
    int? SwissRoundCount = null);

/// <summary>Application readiness diagnostic (not persisted, not Domain).</summary>
/// <param name="ReadyForNextSlice">True when organisation is sufficient for Slice 3 entry.</param>
/// <param name="ReadyForDraw">True when a Draw path is identifiable.</param>
/// <param name="ReadyForMaterialization">True when Fixtures/Matches can be materialized (Cup: primary skeleton fixtures incomplete; not from-slots).</param>
/// <param name="ReadyForSchedule">True when attached Matches exist for scheduling.</param>
/// <param name="ReadyForMatchOperation">True when Slice 4 can start (Matches attached; schedule optional).</param>
/// <param name="ReadyForSchedulePath">Legacy Slice 2 hint: championship schedule path identifiable from structure.</param>
/// <param name="AttachedMatchCount">Matches attached to the primary stage.</param>
/// <param name="Blockers">Machine-readable blocker codes (authoritative for SPA i18n).</param>
public sealed record OrganisationReadinessDto(
    bool ReadyForNextSlice,
    bool ReadyForDraw,
    bool ReadyForMaterialization,
    bool ReadyForSchedule,
    bool ReadyForMatchOperation,
    bool ReadyForSchedulePath,
    int AttachedMatchCount,
    IReadOnlyList<string> Blockers);

/// <summary>
/// Per-stage hub row for Règlement lecture (topology facts + regulation tokens).
/// Optional families omitted when absent (présence seule).
/// </summary>
/// <param name="StageId">Stage identity.</param>
/// <param name="Name">Stage display name.</param>
/// <param name="Status">Stage lifecycle status.</param>
/// <param name="TeamCount">Topology: teams in this phase (Host-derived).</param>
/// <param name="MatchCount">Topology: attached matches in this phase.</param>
/// <param name="GroupCount">Topology: groups in this phase.</param>
/// <param name="RoundCount">Topology: cup rounds in this phase.</param>
/// <param name="NumberOfPeriods">MatchRules periods.</param>
/// <param name="DurationPerPeriod">MatchRules duration.</param>
/// <param name="HasExtraTime">Match ExtraTimePolicy present.</param>
/// <param name="ExtraTimeNumberOfPeriods">Extra-time periods when HasExtraTime.</param>
/// <param name="ExtraTimeDurationPerPeriod">Extra-time minutes per period when HasExtraTime.</param>
/// <param name="HasPenaltyShootout">Match PenaltyShootoutPolicy present.</param>
/// <param name="PenaltyInitialKicksPerTeam">Initial TAB kicks per team when HasPenaltyShootout.</param>
/// <param name="HasStandingRules">Whether StandingRules are present (classifying phase).</param>
/// <param name="WinPoints">Standing win points when HasStandingRules.</param>
/// <param name="DrawPoints">Standing draw points when HasStandingRules.</param>
/// <param name="LossPoints">Standing loss points when HasStandingRules.</param>
/// <param name="HasDrawRules">DrawRules present.</param>
/// <param name="DrawMode">DrawRules.Mode when HasDrawRules.</param>
/// <param name="NumberOfPots">PotRules.NumberOfPots when present.</param>
/// <param name="HasQualificationRules">QualificationRules present.</param>
/// <param name="QualificationPathCount">Qualification path count.</param>
/// <param name="HasProgressionRules">ProgressionRules present.</param>
/// <param name="ProgressionPathCount">Progression path count.</param>
/// <param name="HasTieFormat">TieFormat present (stage default or round).</param>
/// <param name="NumberOfLegs">TieFormat legs when present (default one-leg if none).</param>
/// <param name="AggregateScoring">TieFormat aggregate scoring when multi-leg.</param>
/// <param name="HasAwayGoalsRule">Away-goals rule on the effective TieFormat.</param>
/// <param name="HasTieExtraTime">Extra-time rule on the effective TieFormat (confrontation).</param>
/// <param name="HasTiePenaltyShootout">Penalty-shootout rule on the effective TieFormat.</param>
/// <param name="RankingCriteria">Stage StandingRules ranking criteria when HasStandingRules.</param>
/// <param name="HasPlacementAwardRules">PlacementAwardRules present.</param>
/// <param name="PlacementAwardCount">Placement award path count.</param>
/// <param name="PlacementAwards">Placement paths (rank + outcome), ordered by rank.</param>
/// <param name="FormatKind">Inferred structure format for this stage.</param>
/// <param name="SwissRoundCount">Planned Swiss rounds when FormatKind is Swiss.</param>
/// <param name="ForfeitWinnerGoals">Administrative forfeit goals for the winning side.</param>
/// <param name="ForfeitLoserGoals">Administrative forfeit goals for the losing side.</param>
/// <param name="NumberOfSeeds">SeedingRules.NumberOfSeeds when DrawRules seeding is set.</param>
/// <param name="DrawConstraints">DrawRules.Constraints (all stored constraints).</param>
/// <param name="DefaultsBinding">Provenance of heritable Match/Standing parts (DefaultsBinding).</param>
/// <param name="ConfrontationSegments">
/// Consecutive Round runs sharing the same effective TieFormat when HasTieFormat and the stage has rounds;
/// otherwise <see langword="null"/>.
/// </param>
public sealed record OrganisationStageHubSummaryDto(
    Guid StageId,
    string Name,
    StageStatus Status,
    int TeamCount,
    int MatchCount,
    int GroupCount,
    int RoundCount,
    int NumberOfPeriods,
    int DurationPerPeriod,
    bool HasExtraTime,
    int? ExtraTimeNumberOfPeriods,
    int? ExtraTimeDurationPerPeriod,
    bool HasPenaltyShootout,
    int? PenaltyInitialKicksPerTeam,
    bool HasStandingRules,
    int? WinPoints,
    int? DrawPoints,
    int? LossPoints,
    bool HasDrawRules,
    DrawMode? DrawMode,
    int? NumberOfPots,
    bool HasQualificationRules,
    int QualificationPathCount,
    bool HasProgressionRules,
    int ProgressionPathCount,
    bool HasTieFormat,
    int? NumberOfLegs,
    bool? AggregateScoring,
    bool HasAwayGoalsRule = false,
    bool HasTieExtraTime = false,
    bool HasTiePenaltyShootout = false,
    IReadOnlyList<RankingCriterion>? RankingCriteria = null,
    bool HasPlacementAwardRules = false,
    int PlacementAwardCount = 0,
    IReadOnlyList<OrganisationPlacementAwardDto>? PlacementAwards = null,
    StructureFormatKind? FormatKind = null,
    int? SwissRoundCount = null,
    int? ForfeitWinnerGoals = null,
    int? ForfeitLoserGoals = null,
    int? NumberOfSeeds = null,
    IReadOnlyList<OrganisationDrawConstraintDto>? DrawConstraints = null,
    OrganisationStageDefaultsBindingDto? DefaultsBinding = null,
    IReadOnlyList<OrganisationConfrontationSegmentDto>? ConfrontationSegments = null);

/// <summary>Round identity + display name inside a confrontation segment.</summary>
/// <param name="RoundId">Round identity.</param>
/// <param name="Name">Round display name.</param>
/// <param name="SortOrder">Stage round order (0-based), for stable multi-segment display.</param>
public sealed record OrganisationConfrontationRoundRefDto(Guid RoundId, string Name, int SortOrder);

/// <summary>
/// Consecutive rounds that share the same effective TieFormat (legs + resolution options).
/// </summary>
/// <param name="Rounds">Rounds in stage order for this segment.</param>
/// <param name="NumberOfLegs">Effective legs (1 or 2).</param>
/// <param name="AggregateScoring">Aggregate scoring on the effective TieFormat.</param>
/// <param name="HasAwayGoalsRule">Away-goals rule present.</param>
/// <param name="HasTieExtraTime">Confrontation extra-time rule present.</param>
/// <param name="HasTiePenaltyShootout">Confrontation penalty-shootout rule present.</param>
public sealed record OrganisationConfrontationSegmentDto(
    IReadOnlyList<OrganisationConfrontationRoundRefDto> Rounds,
    int NumberOfLegs,
    bool AggregateScoring,
    bool HasAwayGoalsRule,
    bool HasTieExtraTime,
    bool HasTiePenaltyShootout);

/// <summary>Whether a heritable part still follows Competition defaults.</summary>
/// <param name="IsBound"><see langword="true"/> when the part is bound to Competition.</param>
public sealed record OrganisationHeritablePartBindingDto(bool IsBound);

/// <summary>
/// Stage DefaultsBinding projection for hub lecture / impact preview (never value equality).
/// Standing parts are <see langword="null"/> when the phase does not classify.
/// </summary>
public sealed record OrganisationStageDefaultsBindingDto(
    OrganisationHeritablePartBindingDto MatchDuration,
    OrganisationHeritablePartBindingDto ExtraTime,
    OrganisationHeritablePartBindingDto PenaltyShootout,
    OrganisationHeritablePartBindingDto AdministrativeResult,
    OrganisationHeritablePartBindingDto? Points,
    OrganisationHeritablePartBindingDto? RankingCriteria);

/// <summary>One placement-award path for the Règlement hub.</summary>
/// <param name="Rank">1-based final competition rank.</param>
/// <param name="Outcome">Winner or Loser of the source confrontation.</param>
public sealed record OrganisationPlacementAwardDto(
    int Rank,
    ProgressionOutcome Outcome);

/// <summary>One draw constraint for the Règlement hub Tirage column.</summary>
/// <param name="Type">DrawConstraintType member name.</param>
/// <param name="Enforcement">Preferred or Required.</param>
/// <param name="MaxPerGroup">Only for MaxSameAssociationPerGroup.</param>
public sealed record OrganisationDrawConstraintDto(
    DrawConstraintType Type,
    ConstraintEnforcement Enforcement,
    int? MaxPerGroup = null);

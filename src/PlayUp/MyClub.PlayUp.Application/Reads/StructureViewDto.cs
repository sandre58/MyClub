// -----------------------------------------------------------------------
// <copyright file="StructureViewDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Assembled Structure hub read for Slice 2 (not a Domain mirror).
/// </summary>
/// <param name="CompetitionId">Competition identity.</param>
/// <param name="Name">Competition name.</param>
/// <param name="Status">Lifecycle status.</param>
/// <param name="Participants">Participant summary.</param>
/// <param name="Format">Inferred / configured format summary.</param>
/// <param name="Regulation">High-level regulation summary.</param>
/// <param name="Structure">Structure counts (not full Domain graph).</param>
/// <param name="Actions">Available structure action codes.</param>
/// <param name="Readiness">Application readiness diagnostic for Slice 3.</param>
/// <param name="Stages">Per-stage topology + regulation tokens (Règlement hub Lot 1).</param>
/// <param name="ShortName">Optional abbreviated name.</param>
/// <param name="LogoMediaId">Optional Media Guid for the logo.</param>
/// <param name="ScheduledStart">Optional declared start.</param>
/// <param name="ScheduledEnd">Optional declared end.</param>
public sealed record StructureViewDto(
    Guid CompetitionId,
    string Name,
    CompetitionStatus Status,
    StructureParticipantsSummaryDto Participants,
    StructureFormatSummaryDto Format,
    StructureRegulationSummaryDto Regulation,
    StructureTopologySummaryDto Structure,
    IReadOnlyList<string> Actions,
    StructureReadinessDto Readiness,
    IReadOnlyList<StructureStageHubSummaryDto> Stages,
    string? ShortName = null,
    Guid? LogoMediaId = null,
    DateTimeOffset? ScheduledStart = null,
    DateTimeOffset? ScheduledEnd = null);

/// <summary>Participants section.</summary>
/// <param name="ActiveCount">Active entries.</param>
/// <param name="OccupyingCount">Entries still present (Active + Withdrawn — forfait keeps the place).</param>
/// <param name="Entries">Light entry rows.</param>
public sealed record StructureParticipantsSummaryDto(
    int ActiveCount,
    int OccupyingCount,
    IReadOnlyList<StructureEntryDto> Entries);

/// <summary>Entry row for Structure.</summary>
/// <param name="EntryId">Entry identity.</param>
/// <param name="DisplayName">Display name.</param>
/// <param name="Status">Entry status.</param>
/// <param name="ShortName">Optional abbreviated name.</param>
/// <param name="LogoMediaId">Optional Media Guid for the logo.</param>
/// <param name="PrimaryColor">Optional primary kit color.</param>
/// <param name="SecondaryColor">Optional secondary kit color.</param>
/// <param name="DeclaredMembers">Declared roster members for this entry.</param>
public sealed record StructureEntryDto(
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
public sealed record StructureFormatSummaryDto(
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
public sealed record StructureRegulationSummaryDto(
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
public sealed record StructureTopologySummaryDto(
    int GroupCount,
    int RoundCount,
    int MatchdayCount,
    int SlotCount,
    bool HasDrawRules,
    int? NumberOfPots,
    MatchGenerationFormat MatchGenerationFormat,
    int? SwissRoundCount = null);

/// <summary>Application readiness diagnostic (not persisted, not Domain).</summary>
/// <param name="ReadyForNextSlice">True when structure is sufficient for Slice 3 entry.</param>
/// <param name="ReadyForDraw">True when a Draw path is identifiable.</param>
/// <param name="ReadyForMaterialization">True when Fixtures/Matches can be materialized (Cup: primary skeleton fixtures incomplete; not from-slots).</param>
/// <param name="ReadyForSchedule">True when attached Matches exist for scheduling.</param>
/// <param name="ReadyForMatchOperation">True when Slice 4 can start (Matches attached; schedule optional).</param>
/// <param name="ReadyForSchedulePath">Slice 2 hint: championship schedule path identifiable from structure.</param>
/// <param name="AttachedMatchCount">Matches attached to the primary stage.</param>
/// <param name="Blockers">Machine-readable blocker codes (authoritative for SPA i18n).</param>
public sealed record StructureReadinessDto(
    bool ReadyForNextSlice,
    bool ReadyForDraw,
    bool ReadyForMaterialization,
    bool ReadyForSchedule,
    bool ReadyForMatchOperation,
    bool ReadyForSchedulePath,
    int AttachedMatchCount,
    IReadOnlyList<string> Blockers);

/// <summary>
/// Per-stage hub row for Structure / Règlement (topology + regulation tokens + graph authoring).
/// Optional families omitted when absent (présence seule).
/// </summary>
/// <param name="StageId">Stage identity.</param>
/// <param name="Name">Stage display name.</param>
/// <param name="Status">Stage lifecycle status.</param>
/// <param name="TeamCount">Topology: teams in this phase (Host-derived).</param>
/// <param name="MatchCount">Topology: attached matches in this phase.</param>
/// <param name="GroupCount">Topology: groups in this phase.</param>
/// <param name="RoundCount">Topology: cup rounds in this phase.</param>
/// <param name="MatchdayCount">Topology: matchdays in this phase.</param>
/// <param name="SlotCount">Slots.</param>
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
/// <param name="Actions">Per-phase Structure mutation action codes (server-gated).</param>
/// <param name="QualificationPaths">Authoring projection of qualification paths when present.</param>
/// <param name="QualificationIntents">Authoring projection of qualification intents when present.</param>
/// <param name="ProgressionPaths">Authoring projection of progression paths when present.</param>
/// <param name="ProgressionIntents">Authoring projection of progression intents when present (V3).</param>
/// <param name="StructureIssues">Machine-readable graph validity codes for this phase (Draft-persistable).</param>
/// <param name="HalfTimeDuration">MatchRules half-time break minutes.</param>
/// <param name="DirectAssignmentCount">Configured DirectAssignment feed count (SlotKey → Entry).</param>
/// <param name="CompositionEntryCount">Root composition set size (k).</param>
/// <param name="CompositionEntryIds">Root composition entry identities (stable order as stored).</param>
/// <param name="CompositionCapacity">
/// Target Places N at T (≠ composition set k). Cup = entry places (1st-round cardinality;
/// ≠ total slotCount when multi-round); Championship/Swiss = Active;
/// Groups = groupCount × placesPerGroup. Null = indeterminable (E4), not zero.
/// </param>
/// <param name="CompositionPreviewNames">Short display-name preview for the composition set.</param>
/// <param name="CompositionPreviewOverflow">Count of composition entries beyond the preview.</param>
/// <param name="CompositionIneligibleCount">Composition entries that are no longer Active.</param>
/// <param name="IsRootComposition">True when the phase has no inbound Qualif/Prog feeds (Affectation).</param>
/// <param name="PlacesPerGroup">Groups form fact: places per group (SoT for Places N); independent of Draw.</param>
/// <param name="DefaultTieFormat">Stage regulation TieFormat (AddRound copy source); null when unset.</param>
public sealed record StructureStageHubSummaryDto(
    Guid StageId,
    string Name,
    StageStatus Status,
    int TeamCount,
    int MatchCount,
    int GroupCount,
    int RoundCount,
    int MatchdayCount,
    int SlotCount,
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
    IReadOnlyList<StructurePlacementAwardDto>? PlacementAwards = null,
    StructureFormatKind? FormatKind = null,
    int? SwissRoundCount = null,
    int? ForfeitWinnerGoals = null,
    int? ForfeitLoserGoals = null,
    int? NumberOfSeeds = null,
    IReadOnlyList<StructureDrawConstraintDto>? DrawConstraints = null,
    StructureStageDefaultsBindingDto? DefaultsBinding = null,
    IReadOnlyList<StructureConfrontationSegmentDto>? ConfrontationSegments = null,
    IReadOnlyList<string>? Actions = null,
    IReadOnlyList<StructureQualificationPathDto>? QualificationPaths = null,
    IReadOnlyList<StructureQualificationIntentDto>? QualificationIntents = null,
    IReadOnlyList<StructureProgressionPathDto>? ProgressionPaths = null,
    IReadOnlyList<StructureProgressionIntentDto>? ProgressionIntents = null,
    IReadOnlyList<string>? StructureIssues = null,
    int HalfTimeDuration = 0,
    int DirectAssignmentCount = 0,
    int CompositionEntryCount = 0,
    IReadOnlyList<Guid>? CompositionEntryIds = null,
    int? CompositionCapacity = null,
    IReadOnlyList<string>? CompositionPreviewNames = null,
    int CompositionPreviewOverflow = 0,
    int CompositionIneligibleCount = 0,
    bool IsRootComposition = true,
    int? PlacesPerGroup = null,
    StructureTieFormatSummaryDto? DefaultTieFormat = null);

/// <summary>Effective TieFormat flags for stage default or a confrontation segment.</summary>
public sealed record StructureTieFormatSummaryDto(
    int NumberOfLegs,
    bool AggregateScoring,
    bool HasAwayGoalsRule,
    bool HasTieExtraTime,
    bool HasTiePenaltyShootout);

/// <summary>One qualification authoring intent for Structure dialog.</summary>
public sealed record StructureQualificationIntentDto(
    Guid IntentId,
    int Order,
    QualificationIntentSourceKind SourceKind,
    int PositionFrom,
    int PositionTo,
    Guid DestinationStageId,
    Guid? GroupId = null,
    string? GroupName = null,
    int? AcrossGroupsPosition = null,
    int? MinimumPoints = null,
    int DestinationCount = 0,
    string? DestinationSlotKey = null);

/// <summary>One qualification path for Structure authoring / impact preview.</summary>
/// <param name="Order">Path order (≥ 1).</param>
/// <param name="SelectionMode">Selection mode.</param>
/// <param name="SelectionValue">Position, count, or range lower bound.</param>
/// <param name="DestinationStageId">Destination stage.</param>
/// <param name="RankingScope">Optional ranking scope.</param>
/// <param name="GroupId">Group when scope is Group.</param>
/// <param name="AcrossGroupsPosition">Across-groups position when applicable.</param>
/// <param name="SelectionEndValue">Range upper bound when mode is Range.</param>
/// <param name="MinimumPoints">Optional Points ≥ gate.</param>
/// <param name="GroupName">Resolved group display name when <paramref name="GroupId"/> is set.</param>
/// <param name="DestinationSlotKey">Destination slot key; null when targeting population.</param>
public sealed record StructureQualificationPathDto(
    int Order,
    SelectionMode SelectionMode,
    int SelectionValue,
    Guid DestinationStageId,
    RankingScope? RankingScope = null,
    Guid? GroupId = null,
    int? AcrossGroupsPosition = null,
    int? SelectionEndValue = null,
    int? MinimumPoints = null,
    string? GroupName = null,
    string? DestinationSlotKey = null);

/// <summary>One progression authoring intent for Structure dialog (V3).</summary>
public sealed record StructureProgressionIntentDto(
    Guid IntentId,
    int Order,
    Guid RoundId,
    string? RoundName,
    ProgressionOutcome Outcome,
    Guid DestinationStageId,
    string? DestinationSlotKey,
    int ExpandedPathCount);

/// <summary>One progression path for Structure authoring / impact preview.</summary>
/// <param name="SourceFixtureId">Source fixture on the rules-owning stage.</param>
/// <param name="Outcome">Winner or Loser.</param>
/// <param name="DestinationStageId">Destination stage.</param>
/// <param name="DestinationSlotKey">Destination slot key; null when targeting population (O2-a).</param>
/// <param name="SourceLabel">Resolved fixture label (round · #order · slots) when the fixture exists.</param>
public sealed record StructureProgressionPathDto(
    Guid SourceFixtureId,
    ProgressionOutcome Outcome,
    Guid DestinationStageId,
    string? DestinationSlotKey,
    string? SourceLabel = null);

/// <summary>Round identity + display name inside a confrontation segment.</summary>
/// <param name="RoundId">Round identity.</param>
/// <param name="Name">Round display name.</param>
/// <param name="SortOrder">Stage round order (0-based), for stable multi-segment display.</param>
public sealed record StructureConfrontationRoundRefDto(Guid RoundId, string Name, int SortOrder);

/// <summary>
/// Consecutive rounds that share the same effective TieFormat (legs + resolution options).
/// </summary>
/// <param name="Rounds">Rounds in stage order for this segment.</param>
/// <param name="NumberOfLegs">Effective legs (1 or 2).</param>
/// <param name="AggregateScoring">Aggregate scoring on the effective TieFormat.</param>
/// <param name="HasAwayGoalsRule">Away-goals rule present.</param>
/// <param name="HasTieExtraTime">Confrontation extra-time rule present.</param>
/// <param name="HasTiePenaltyShootout">Confrontation penalty-shootout rule present.</param>
public sealed record StructureConfrontationSegmentDto(
    IReadOnlyList<StructureConfrontationRoundRefDto> Rounds,
    int NumberOfLegs,
    bool AggregateScoring,
    bool HasAwayGoalsRule,
    bool HasTieExtraTime,
    bool HasTiePenaltyShootout);

/// <summary>Whether a heritable part still follows Competition defaults.</summary>
/// <param name="IsBound"><see langword="true"/> when the part is bound to Competition.</param>
public sealed record StructureHeritablePartBindingDto(bool IsBound);

/// <summary>
/// Stage DefaultsBinding projection for hub lecture / impact preview (never value equality).
/// Standing parts are <see langword="null"/> when the phase does not classify.
/// </summary>
public sealed record StructureStageDefaultsBindingDto(
    StructureHeritablePartBindingDto MatchDuration,
    StructureHeritablePartBindingDto ExtraTime,
    StructureHeritablePartBindingDto PenaltyShootout,
    StructureHeritablePartBindingDto AdministrativeResult,
    StructureHeritablePartBindingDto? Points,
    StructureHeritablePartBindingDto? RankingCriteria);

/// <summary>One placement-award path for the Règlement / Structure hubs.</summary>
/// <param name="Rank">1-based final competition rank.</param>
/// <param name="Outcome">Winner or Loser of the source confrontation.</param>
/// <param name="SourceFixtureId">Source fixture on the rules-owning stage.</param>
/// <param name="SourceLabel">Resolved fixture label (round · #order · slots) when found.</param>
public sealed record StructurePlacementAwardDto(
    int Rank,
    ProgressionOutcome Outcome,
    Guid? SourceFixtureId = null,
    string? SourceLabel = null);

/// <summary>One draw constraint for the Règlement hub Tirage column.</summary>
/// <param name="Type">DrawConstraintType member name.</param>
/// <param name="Enforcement">Preferred or Required.</param>
/// <param name="MaxPerGroup">Only for MaxSameAssociationPerGroup.</param>
public sealed record StructureDrawConstraintDto(
    DrawConstraintType Type,
    ConstraintEnforcement Enforcement,
    int? MaxPerGroup = null);

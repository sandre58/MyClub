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
/// Assembled Structure hub read (not a Domain mirror).
/// </summary>
/// <param name="CompetitionId">Competition identity.</param>
/// <param name="Name">Competition name.</param>
/// <param name="Status">Lifecycle status.</param>
/// <param name="Participants">Participant summary.</param>
/// <param name="Format">Inferred / configured format summary.</param>
/// <param name="Regulation">High-level regulation summary.</param>
/// <param name="Structure">Structure counts (not full Domain graph).</param>
/// <param name="Actions">Available structure action codes.</param>
/// <param name="Readiness">Application readiness diagnostic.</param>
/// <param name="Stages">Per-stage topology + regulation tokens (Regulation hub).</param>
/// <param name="HubNextStep">At most one hub-level structural next step; null when calm, ambiguous, or closed (not success).</param>
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
    StructureHubNextStepDto? HubNextStep = null,
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
/// <param name="NumberOfPots">Draw PotRules only (draw). Never a substitute for <c>PlacesPerGroup</c>.</param>
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

/// <summary>One readiness blocker with optional owning stage (canonical attribution).</summary>
/// <param name="Code">Machine-readable blocker code.</param>
/// <param name="StageId">Owner stage when known; null for competition-scoped blockers.</param>
public sealed record StructureReadinessBlockerDto(string Code, Guid? StageId);

/// <summary>Application readiness diagnostic (not persisted, not Domain).</summary>
/// <param name="ReadyForNextSlice">True when structure is sufficient for the next construction step.</param>
/// <param name="ReadyForDraw">True when a Draw path is identifiable.</param>
/// <param name="ReadyForMaterialization">True when Championship/Groups structure can run MaterializeMatches (not an uncovered-pair opportunity). Overview CTA uses uncovered determined pairs.</param>
/// <param name="ReadyForSchedule">True when attached Matches exist for scheduling.</param>
/// <param name="ReadyForMatchOperation">True when match operations can start (Matches attached; schedule optional).</param>
/// <param name="ReadyForSchedulePath">Structure hint: championship schedule path identifiable from structure.</param>
/// <param name="AttachedMatchCount">Matches attached to the primary stage.</param>
/// <param name="Blockers">Legacy unique codes for .Contains consumers (derived from <see cref="BlockerDetails"/>).</param>
/// <param name="NextConstructionStepHint">When <see cref="ReadyForNextSlice"/> is true, SPA title for the hub ready band (i18n key segment).</param>
/// <param name="BlockerDetails">Canonical list; duplicate codes allowed with different <see cref="StructureReadinessBlockerDto.StageId"/>.</param>
public sealed record StructureReadinessDto(
    bool ReadyForNextSlice,
    bool ReadyForDraw,
    bool ReadyForMaterialization,
    bool ReadyForSchedule,
    bool ReadyForMatchOperation,
    bool ReadyForSchedulePath,
    int AttachedMatchCount,
    IReadOnlyList<string> Blockers,
    string NextConstructionStepHint,
    IReadOnlyList<StructureReadinessBlockerDto> BlockerDetails);

/// <summary>
/// Per-stage hub row for Structure / Regulation (topology + regulation tokens + graph authoring).
/// Optional families omitted when absent (presence-only).
/// </summary>
/// <param name="StageId">Stage identity.</param>
/// <param name="Name">Stage display name.</param>
/// <param name="Status">Stage lifecycle status.</param>
/// <param name="TeamCount">Topology: teams in this phase (Host-derived).</param>
/// <param name="MatchCount">Topology: attached matches in this phase.</param>
/// <param name="GroupCount">Topology: groups in this phase.</param>
/// <param name="RoundCount">Topology: cup rounds in this phase.</param>
/// <param name="MatchdayCount">Topology: matchdays with at least one attached match (not skeleton seed).</param>
/// <param name="ConfiguredMatchdayCount">
/// Topology: organizer-facing skeleton matchdays (Championship / Groups exclude the lone technical empty seed).
/// Independent of attached matches.
/// </param>
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
/// <param name="NumberOfPots">Draw PotRules only (draw). Never a substitute for <c>PlacesPerGroup</c>.</param>
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
/// <param name="QualificationIntents">Authoring projection of qualification intents (legacy path rules synthesized on read).</param>
/// <param name="ProgressionIntents">Authoring projection of progression intents (legacy path rules synthesized on read).</param>
/// <param name="StructureIssues">Machine-readable graph validity codes for this phase (Draft-persistable).</param>
/// <param name="PresentedIssueCodes">
/// Host-authored presented diagnostics for this phase (structure projection + lifecycle + readiness attribution).
/// </param>
/// <param name="StructureIssueSemantics">Pilot organizer-problem semantics for selected issue codes.</param>
/// <param name="HalfTimeDuration">MatchRules half-time break minutes.</param>
/// <param name="DirectAssignmentCount">Configured DirectAssignment feed count (SlotKey → Entry).</param>
/// <param name="CompositionEntryCount">Runtime population membership size (Draw / Live pool).</param>
/// <param name="CompositionEntryIds">Runtime composition entry identities (stable order as stored).</param>
/// <param name="CompositionCapacity">
/// Target Places N at T (≠ composition set k). Cup = entry places (1st-round cardinality;
/// ≠ total slotCount when multi-round); Championship/Swiss = Active;
/// Groups = groupCount × placesPerGroup. Null = indeterminable (E4), not zero.
/// </param>
/// <param name="CompositionPreviewNames">Display names for runtime composition (Draw / Live).</param>
/// <param name="CompositionIneligibleCount">Runtime composition entries that are no longer Active (row chrome only).</param>
/// <param name="AffectationEntryCount">Affectation authoring set size (Population tile).</param>
/// <param name="AffectationEntryIds">Affectation authoring entry identities.</param>
/// <param name="AffectationPreviewNames">Display names for Affectation authoring.</param>
/// <param name="AffectationIneligibleCount">Affectation authoring entries that are no longer Active.</param>
/// <param name="IsRootComposition">True when the phase has no inbound Qualif/Prog feeds.</param>
/// <param name="PlacesPerGroup">Groups form fact: places per group (SoT for Places N). Independent of Draw <paramref name="NumberOfPots"/>.</param>
/// <param name="DefaultTieFormat">Stage regulation TieFormat (AddRound copy source); null when unset.</param>
/// <param name="DrawExecutionBadge">Topology execution badge when DrawRules engaged; null otherwise.</param>
/// <param name="Materialize">Workspace materialize capability (null when the format has no Materialize command).</param>
/// <param name="Dematerialize">Workspace dematerialize capability (null when nothing is materialized or the format is unsupported).</param>
/// <param name="Lifecycle">Phase lifecycle band (Prepare / Start / Complete transition + checklist / match progress).</param>
/// <param name="InboundComposition">Inbound Qual/Prog resolution for the Composition Qualifiés band; null when root composition.</param>
/// <param name="CompositionBandResolvedCount">Composition band — resolved seat count (inbound or roster size).</param>
/// <param name="SchematicOccupiedPlaceCount">
/// Structural schematic occupancy vs <see cref="CompositionCapacity"/> (strip / diagnostics); not hero.
/// Null when Places N is unknown or the format is out of scope.
/// </param>
public sealed record StructureStageHubSummaryDto(
    Guid StageId,
    string Name,
    StageStatus Status,
    int TeamCount,
    int MatchCount,
    int GroupCount,
    int RoundCount,
    int MatchdayCount,
    int ConfiguredMatchdayCount,
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
    IReadOnlyList<StructureQualificationIntentDto>? QualificationIntents = null,
    IReadOnlyList<StructureProgressionIntentDto>? ProgressionIntents = null,
    IReadOnlyList<string>? StructureIssues = null,
    IReadOnlyList<string>? PresentedIssueCodes = null,
    IReadOnlyList<OrganizerProblemReadDto>? StructureIssueSemantics = null,
    int HalfTimeDuration = 0,
    int DirectAssignmentCount = 0,
    int CompositionEntryCount = 0,
    IReadOnlyList<Guid>? CompositionEntryIds = null,
    int? CompositionCapacity = null,
    IReadOnlyList<string>? CompositionPreviewNames = null,
    int CompositionIneligibleCount = 0,
    int AffectationEntryCount = 0,
    IReadOnlyList<Guid>? AffectationEntryIds = null,
    IReadOnlyList<string>? AffectationPreviewNames = null,
    int AffectationIneligibleCount = 0,
    bool IsRootComposition = true,
    int? PlacesPerGroup = null,
    StructureTieFormatSummaryDto? DefaultTieFormat = null,
    StructureDrawExecutionBadge? DrawExecutionBadge = null,
    StructureMaterializeDto? Materialize = null,
    StructureDematerializeDto? Dematerialize = null,
    StructureStageLifecycleDto? Lifecycle = null,
    StructureInboundCompositionDto? InboundComposition = null,
    int CompositionBandResolvedCount = 0,
    int? SchematicOccupiedPlaceCount = null);

/// <summary>
/// Structure workspace projection of match materialization (not Overview opportunity).
/// Championship lists counts only; Groups/Cup list determined confrontations.
/// </summary>
/// <param name="CommandCode">MaterializeMatches or MaterializeFromOccupiedSlots.</param>
/// <param name="LabelVariant">create | complete | exhausted | idle.</param>
/// <param name="DeterminedCount">Confrontations whose opponents are known.</param>
/// <param name="EligibleCount">Confrontations the command can create now.</param>
/// <param name="MaterializedCount">Determined confrontations that already have a Match.</param>
/// <param name="CommandEnabled">True when the workspace command can run.</param>
/// <param name="Confrontations">Exhaustive rows (empty for Championship phase grain).</param>
/// <param name="DisabledReason">When command is disabled and not terminal — minimal causal code for UI copy.</param>
/// <param name="CanonicalEntryPairCount">Canonical Cup entry-round pair count; zero when not applicable.</param>
/// <param name="UndeterminedCanonicalEntryPairCount">Canonical pairs without two distinct entries; zero when not applicable.</param>
/// <param name="GroupsPlacementFilledCount">Groups: entries assigned across pools; zero when not applicable.</param>
/// <param name="GroupsPlacementCapacityCount">Groups: groupCount × placesPerGroup; zero when not applicable.</param>
/// <param name="ProspectiveConfrontationCount">Groups: round-robin pairs if every pool were filled to PlacesPerGroup; zero when not applicable.</param>
public sealed record StructureMaterializeDto(
    string CommandCode,
    string LabelVariant,
    int DeterminedCount,
    int EligibleCount,
    int MaterializedCount,
    bool CommandEnabled,
    IReadOnlyList<StructureMaterializeConfrontationDto> Confrontations,
    string? DisabledReason = null,
    int CanonicalEntryPairCount = 0,
    int UndeterminedCanonicalEntryPairCount = 0,
    int GroupsPlacementFilledCount = 0,
    int GroupsPlacementCapacityCount = 0,
    int ProspectiveConfrontationCount = 0);

/// <summary>
/// Structure workspace dematerialization capability (sibling of materialize; not Overview).
/// </summary>
/// <param name="Enabled">When true, POST dematerialize may succeed (command remains authoritative).</param>
/// <param name="DisabledReason">Stable code when exposed and disabled.</param>
/// <param name="Scope">FullPhase or LastSwissRound — UI copy only.</param>
public sealed record StructureDematerializeDto(
    bool Enabled,
    string? DisabledReason,
    string Scope);

/// <summary>One determined or structural confrontation for the Structure workspace.</summary>
/// <param name="Key">Cup = BracketPair.PairKey; Groups = directed Home|Away.</param>
/// <param name="SideA">Home / slot A display name (or slot key when unoccupied).</param>
/// <param name="SideB">Away / slot B display name (or slot key when unoccupied).</param>
/// <param name="Determined">Both opponents known and distinct.</param>
/// <param name="Materialized">A Match already covers this confrontation.</param>
/// <param name="Eligible">The workspace command can create this confrontation now.</param>
/// <param name="Selectable">True when the workspace may submit this key (Cup pairKeys).</param>
/// <param name="GroupName">Groups poule name when applicable.</param>
public sealed record StructureMaterializeConfrontationDto(
    string Key,
    string SideA,
    string SideB,
    bool Determined,
    bool Materialized,
    bool Eligible,
    bool Selectable = false,
    string? GroupName = null);

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
    IReadOnlyList<string>? DestinationSlotKeys = null,
    IReadOnlyList<Guid>? DestinationGroupIds = null,
    bool DestinationForm = false);

/// <summary>One progression authoring intent for Structure dialog.</summary>
public sealed record StructureProgressionIntentDto(
    Guid IntentId,
    int Order,
    Guid RoundId,
    string? RoundName,
    ProgressionOutcome Outcome,
    Guid DestinationStageId,
    IReadOnlyList<string>? DestinationSlotKeys,
    int ExpandedPathCount,
    IReadOnlyList<Guid>? DestinationGroupIds = null,
    bool DestinationForm = false);

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

/// <summary>One placement-award path for the Regulation / Structure hubs.</summary>
/// <param name="Rank">1-based final competition rank.</param>
/// <param name="Outcome">Winner or Loser of the source confrontation.</param>
/// <param name="SourcePairKey">Structural source key (Cup = BracketPair.PairKey).</param>
/// <param name="SourceLabel">Display label (PairKey; Match # overlay when materialized).</param>
/// <param name="BoundFixtureId">Optional execution fixture when the PairKey is materialized (read overlay only).</param>
public sealed record StructurePlacementAwardDto(
    int Rank,
    ProgressionOutcome Outcome,
    string SourcePairKey,
    string? SourceLabel = null,
    Guid? BoundFixtureId = null);

/// <summary>One draw constraint for the Regulation hub Draw column.</summary>
/// <param name="Type">DrawConstraintType member name.</param>
/// <param name="Enforcement">Preferred or Required.</param>
/// <param name="MaxPerGroup">Only for MaxSameAssociationPerGroup.</param>
public sealed record StructureDrawConstraintDto(
    DrawConstraintType Type,
    ConstraintEnforcement Enforcement,
    int? MaxPerGroup = null);

/**
 * Structure hub reads and Structure / stage-regulation authoring requests.
 */
import type {
  CompetitionStatus,
  ConstraintEnforcement,
  DisciplinaryType,
  DrawConstraintType,
  DrawMode,
  EntryStatus,
  MatchGenerationFormat,
  ProgressionOutcome,
  RankingCriterion,
  RankingScope,
  SelectionMode,
  StageStatus,
  StructureFormatKind,
} from './enums';
import type { DeclaredMember } from './competitions';

/** GET /competitions/{id}/structure — Host StructureView (SPA Structure hub). */
export interface StructureView {
  competitionId: string;
  name: string;
  status: CompetitionStatus;
  participants: StructureParticipantsSummary;
  format: StructureFormatSummary;
  regulation: StructureRegulationSummary;
  structure: StructureTopologySummary;
  actions: string[];
  readiness: StructureReadiness;
  /** Per-stage topology + regulation tokens (Regulation hub). */
  stages: StructureStageHubSummary[];
  shortName?: string | null;
  logoMediaId?: string | null;
  scheduledStart?: string | null;
  scheduledEnd?: string | null;
}

export interface StructureParticipantsSummary {
  activeCount: number;
  occupyingCount: number;
  entries: StructureEntry[];
}

export interface StructureEntry {
  entryId: string;
  displayName: string;
  status: EntryStatus;
  shortName?: string | null;
  logoMediaId?: string | null;
  primaryColor?: string | null;
  secondaryColor?: string | null;
  declaredMembers?: DeclaredMember[];
}

export interface StructureFormatSummary {
  kind: StructureFormatKind | null;
  primaryStageId: string | null;
  primaryStageName: string | null;
  primaryStageStatus: StageStatus | null;
}

export interface StructureRegulationSummary {
  minimumTeams: number;
  maximumTeams: number;
  durationPerPeriod: number;
  numberOfPeriods: number;
  winPoints: number;
  drawPoints: number;
  lossPoints: number;
  /** Authorized disciplinary catalogue types (empty = none). Omitted only in older fixtures. */
  allowedTypes?: DisciplinaryType[];
  /** Half-time break minutes (Host). Optional in older fixtures. */
  halfTimeDuration?: number;
  /** Competition MatchRules ExtraTimePolicy present. */
  hasExtraTime?: boolean;
  extraTimeDurationPerPeriod?: number | null;
  extraTimeNumberOfPeriods?: number | null;
  /** Competition MatchRules PenaltyShootoutPolicy present. */
  hasPenaltyShootout?: boolean;
  /** TAB initial kicks per team when hasPenaltyShootout. */
  penaltyInitialKicksPerTeam?: number | null;
  /** Ordered standing ranking criteria. */
  rankingCriteria?: RankingCriterion[];
  /** Administrative forfeit score — winner goals. */
  forfeitWinnerGoals?: number;
  /** Administrative forfeit score — loser goals. */
  forfeitLoserGoals?: number;
}

export interface StructureTopologySummary {
  groupCount: number;
  roundCount: number;
  matchdayCount: number;
  slotCount: number;
  hasDrawRules: boolean;
  numberOfPots: number | null;
  matchGenerationFormat: MatchGenerationFormat;
  /** Planned Swiss rounds K when kind is Swiss; null otherwise. */
  swissRoundCount?: number | null;
}

export interface StructureReadiness {
  readyForNextSlice: boolean;
  readyForDraw: boolean;
  readyForMaterialization: boolean;
  readyForSchedule: boolean;
  readyForMatchOperation: boolean;
  readyForSchedulePath: boolean;
  attachedMatchCount: number;
  blockers: string[];
}

/** GET structure `stages[]` — Regulation hub stage row. */
export interface StructureStageHubSummary {
  stageId: string;
  name: string;
  status: StageStatus;
  teamCount: number;
  matchCount: number;
  /** Topology: groups in this phase. Optional in older fixtures. */
  groupCount?: number;
  /** Topology: cup rounds in this phase. Optional in older fixtures. */
  roundCount?: number;
  /** Topology: matchdays in this phase. */
  matchdayCount?: number;
  /** Topology: bracket / cup slots in this phase. */
  slotCount?: number;
  numberOfPeriods: number;
  durationPerPeriod: number;
  hasExtraTime: boolean;
  extraTimeNumberOfPeriods?: number | null;
  extraTimeDurationPerPeriod?: number | null;
  hasPenaltyShootout: boolean;
  penaltyInitialKicksPerTeam?: number | null;
  /** MatchRules half-time break minutes. */
  halfTimeDuration?: number;
  /** A5: standing present only when the phase classifies. */
  hasStandingRules?: boolean;
  winPoints?: number | null;
  drawPoints?: number | null;
  lossPoints?: number | null;
  hasDrawRules: boolean;
  /** Topology execution badge when DrawRules engaged (SoT Structure × Draw chrome). */
  drawExecutionBadge?: 'ToLaunch' | 'InProgress' | 'ToApply' | 'Applied' | null;
  drawMode?: DrawMode | null;
  numberOfPots?: number | null;
  hasQualificationRules: boolean;
  qualificationPathCount: number;
  hasProgressionRules: boolean;
  progressionPathCount: number;
  hasTieFormat: boolean;
  numberOfLegs?: number | null;
  aggregateScoring?: boolean | null;
  /** Away-goals rule on the effective TieFormat. */
  hasAwayGoalsRule?: boolean;
  /** Extra-time rule on the confrontation TieFormat. */
  hasTieExtraTime?: boolean;
  /** Penalty-shootout rule on the confrontation TieFormat. */
  hasTiePenaltyShootout?: boolean;
  /** Stage StandingRules ranking criteria when hasStandingRules. */
  rankingCriteria?: RankingCriterion[];
  hasPlacementAwardRules?: boolean;
  placementAwardCount?: number;
  /** Placement paths (rank + outcome), ordered by rank. */
  placementAwards?: StructurePlacementAward[];
  /** Inferred structure format for schematic / badge context. */
  formatKind?: StructureFormatKind | null;
  /** Planned Swiss rounds when formatKind is Swiss. */
  swissRoundCount?: number | null;
  /** Administrative forfeit score — winner goals. */
  forfeitWinnerGoals?: number | null;
  /** Administrative forfeit score — loser goals. */
  forfeitLoserGoals?: number | null;
  /** SeedingRules.NumberOfSeeds when draw seeding is set. */
  numberOfSeeds?: number | null;
  /** DrawRules.Constraints (all stored constraints). */
  drawConstraints?: StructureDrawConstraint[];
  /** Provenance of heritable Match/Standing parts (DefaultsBinding). */
  defaultsBinding?: StructureStageDefaultsBinding;
  /**
   * Consecutive Round runs sharing the same effective TieFormat when hasTieFormat
   * and the stage has rounds; otherwise omitted/null.
   */
  confrontationSegments?: StructureConfrontationSegment[] | null;
  /** Per-phase Structure mutation action codes (server-gated). */
  actions?: string[];
  /** Authoring projection of qualification paths when present. */
  qualificationPaths?: StructureQualificationPath[] | null;
  /** Authoring projection of progression paths when present. */
  progressionPaths?: StructureProgressionPath[] | null;
  /** Authoring intents (Round × Outcome → Destination). Prefer over flat paths when present. */
  progressionIntents?: StructureProgressionIntent[] | null;
  /** Machine-readable graph validity codes (Draft-persistable). */
  structureIssues?: string[];
  /** Configured DirectAssignment count (slot → entry). */
  directAssignmentCount?: number;
  /** Runtime population membership size (Draw / Live pool). */
  compositionEntryCount?: number;
  /** Runtime composition entry identities. */
  compositionEntryIds?: string[] | null;
  /** Target Places N at T (≠ composition set k). Null = E4 indeterminable. */
  compositionCapacity?: number | null;
  /** Groups form fact: places per group (SoT for Places N); independent of Draw. */
  placesPerGroup?: number | null;
  /** Display names for runtime composition (Draw / Live). */
  compositionPreviewNames?: string[] | null;
  /** Always 0 — retained for API shape; rails no longer truncate. */
  compositionPreviewOverflow?: number;
  /** Runtime composition entries that are no longer Active. */
  compositionIneligibleCount?: number;
  /** Affectation authoring set size (Population tile). */
  affectationEntryCount?: number;
  /** Affectation authoring entry identities. */
  affectationEntryIds?: string[] | null;
  /** Display names for Affectation authoring. */
  affectationPreviewNames?: string[] | null;
  /** Affectation authoring entries that are no longer Active. */
  affectationIneligibleCount?: number;
  /** True when the phase has no inbound Qualif/Prog feeds. */
  isRootComposition?: boolean;
  /** Stage regulation TieFormat (AddRound copy source); null/omitted when unset. */
  defaultTieFormat?: StructureTieFormatSummary | null;
  /** Authoring intents (1 → N paths). Prefer over flat paths when present. */
  qualificationIntents?: StructureQualificationIntent[] | null;
}

/** One qualification authoring intent. */
export type QualificationIntentSourceKind =
  'SingleGroup' | 'EachGroup' | 'Overall' | 'AcrossGroups';

export interface StructureQualificationIntent {
  intentId: string;
  order: number;
  sourceKind: QualificationIntentSourceKind;
  positionFrom: number;
  positionTo: number;
  destinationStageId: string;
  /**
   * Cup Place slot keys (Expand index ↔ key). Empty/omitted when not slot-targeting.
   * Mutually exclusive with destinationGroupIds. Both empty = Population.
   * Wire camelCase matches Domain DestinationSlotKeys.
   */
  destinationSlotKeys?: string[] | null;
  /** @deprecated Dual-read only (legacy singular wire); writes use destinationSlotKeys. */
  destinationSlotKey?: string | null;
  /**
   * Groups A1 Place group ids (Expand index ↔ group). Duplicates allowed.
   * Mutually exclusive with destinationSlotKeys and destinationForm.
   */
  destinationGroupIds?: string[] | null;
  /**
   * Champ/Swiss Form Placement (Domain ForForm).
   * Mutually exclusive with destinationSlotKeys and destinationGroupIds.
   */
  destinationForm?: boolean | null;
  groupId?: string | null;
  groupName?: string | null;
  acrossGroupsPosition?: number | null;
  minimumPoints?: number | null;
  /** Expanded entry count toward destination population. */
  destinationCount?: number;
}

/** One qualification path for Structure authoring / impact preview. */
export interface StructureQualificationPath {
  order: number;
  selectionMode: SelectionMode;
  selectionValue: number;
  destinationStageId: string;
  /**
   * Destination slot when targeting Cup Place (Auto); null/omitted when
   * targeting phase Population or Groups Place.
   */
  destinationSlotKey?: string | null;
  /** Groups A1 destination group when targeting a poule; null otherwise. */
  destinationGroupId?: string | null;
  /** Champ/Swiss Form Placement when true. */
  destinationForm?: boolean | null;
  rankingScope?: RankingScope | null;
  groupId?: string | null;
  /** Resolved group display name when groupId is set. */
  groupName?: string | null;
  acrossGroupsPosition?: number | null;
  selectionEndValue?: number | null;
  minimumPoints?: number | null;
}

/** One progression authoring intent. */
export interface StructureProgressionIntent {
  intentId: string;
  order: number;
  roundId: string;
  roundName?: string | null;
  outcome: ProgressionOutcome;
  destinationStageId: string;
  /**
   * Cup Place slot keys (fixture index ↔ key). Empty/omitted when not slot-targeting.
   * Mutually exclusive with destinationGroupIds. Both empty = Population.
   */
  destinationSlotKeys?: string[] | null;
  /** @deprecated Dual-read only (legacy singular wire); writes use destinationSlotKeys. */
  destinationSlotKey?: string | null;
  /**
   * Groups A1 Place group ids (fixture index ↔ group). Duplicates allowed.
   * Mutually exclusive with destinationSlotKeys and destinationForm.
   */
  destinationGroupIds?: string[] | null;
  /**
   * Champ/Swiss Form Placement (Domain ForForm).
   * Mutually exclusive with destinationSlotKeys and destinationGroupIds.
   */
  destinationForm?: boolean | null;
  /** Expand preview: fixture count on the round. */
  expandedPathCount?: number;
}

/** One progression path for Structure authoring / impact preview. */
export interface StructureProgressionPath {
  /** Structural source key (Cup = BracketPair.PairKey). */
  sourcePairKey: string;
  outcome: ProgressionOutcome;
  destinationStageId: string;
  /**
   * Destination slot when targeting Cup Place; null/omitted when targeting
   * phase Population, Form, or Groups Place.
   */
  destinationSlotKey?: string | null;
  /** Groups A1 destination group when targeting a poule; null otherwise. */
  destinationGroupId?: string | null;
  /** Champ/Swiss Form Placement when true. */
  destinationForm?: boolean | null;
  /** Resolved confrontation label (PairKey or bound fixture) when available. */
  sourceLabel?: string | null;
}

/** DELETE /competitions/{id}/stages/{stageId} */
export interface RemoveCompetitionStageResponse {
  removedStageId: string;
  scrubbedQualificationPaths: number;
  scrubbedProgressionPaths: number;
  structure: StructureView;
}

/** POST /competitions/{id}/stages — identity + skeleton */
export type AddCompetitionStageRequest = {
  format: StructureFormatKind | string;
  name: string;
  groupCount?: number | null;
  participantsPerGroup?: number | null;
  bracketSize?: number | null;
  matchGenerationFormat?: MatchGenerationFormat | null;
  swissRoundCount?: number | null;
};

/** POST /competitions/{id}/stages */
export interface AddCompetitionStageResponse {
  stageId: string;
  name: string;
  structure: StructureView;
}

/** PUT /stages/{id}/structure — same-kind skeleton rebuild */
export type RebuildStageStructureRequest = {
  format: StructureFormatKind | string;
  stageName?: string | null;
  groupCount?: number | null;
  participantsPerGroup?: number | null;
  bracketSize?: number | null;
  matchGenerationFormat?: MatchGenerationFormat | null;
  swissRoundCount?: number | null;
};

export interface RebuildStageStructureResponse {
  impact: StructureRebuildImpact;
  structure: StructureView;
}

/** PUT /stages/{id}/qualification-rules (intents authoring SoT). */
export interface ReplaceQualificationRulesRequest {
  intents?: StructureQualificationIntent[] | null;
}

/** PUT /stages/{id}/progression-rules (intents authoring SoT). */
export interface ReplaceProgressionRulesRequest {
  intents?: StructureProgressionIntent[] | null;
}

/** PUT /stages/{id}/placement-award-rules */
export interface ReplacePlacementAwardRulesRequest {
  paths: StructurePlacementAward[] | null;
}

/** PUT /stages/{id}/match-rules */
export interface ReplaceStageMatchRulesRequest {
  durationPerPeriod: number;
  numberOfPeriods: number;
  halfTimeDuration: number;
  forfeitWinnerGoals?: number;
  forfeitLoserGoals?: number;
  hasExtraTime?: boolean;
  extraTimeDurationPerPeriod?: number | null;
  extraTimeNumberOfPeriods?: number | null;
  hasPenaltyShootout?: boolean;
  penaltyInitialKicksPerTeam?: number | null;
}

/** PUT /stages/{id}/standing-rules */
export interface ReplaceStageStandingRulesRequest {
  winPoints: number;
  drawPoints: number;
  lossPoints: number;
  rankingCriteria: RankingCriterion[];
}

/** POST /stages/{id}/bind-to-competition */
export interface BindStageRegulationRequest {
  scope: 'Match' | 'Standing';
}

/** PUT /stages/{id}/draw-rules */
export interface ReplaceStageDrawRulesRequest {
  clear?: boolean;
  mode?: DrawMode | null;
  numberOfPots?: number | null;
  numberOfSeeds?: number | null;
}

/** PUT /stages/{id}/tie-format */
export interface ReplaceStageDefaultTieFormatRequest {
  clear?: boolean;
  numberOfLegs?: number;
  hasAwayGoalsRule?: boolean;
  hasExtraTimeRule?: boolean;
  hasPenaltyShootoutRule?: boolean;
}

/** PUT /stages/{id}/rounds/{roundId}/tie-format */
export interface ReplaceRoundTieFormatRequest {
  clear?: boolean;
  numberOfLegs?: number;
  hasAwayGoalsRule?: boolean;
  hasExtraTimeRule?: boolean;
  hasPenaltyShootoutRule?: boolean;
}

/** Stage regulation or segment TieFormat summary. */
export interface StructureTieFormatSummary {
  numberOfLegs: number;
  aggregateScoring: boolean;
  hasAwayGoalsRule: boolean;
  hasTieExtraTime: boolean;
  hasTiePenaltyShootout: boolean;
}

/** Round identity + display name inside a confrontation segment. */
export interface StructureConfrontationRoundRef {
  roundId: string;
  name: string;
  /** Stage round order (0-based). */
  sortOrder: number;
}

/**
 * Consecutive rounds that share the same effective TieFormat
 * (legs + aggregate / away goals / tie ET / TAB).
 */
export interface StructureConfrontationSegment {
  rounds: StructureConfrontationRoundRef[];
  numberOfLegs: number;
  aggregateScoring: boolean;
  hasAwayGoalsRule: boolean;
  hasTieExtraTime: boolean;
  hasTiePenaltyShootout: boolean;
}

/** Whether a heritable part still follows Competition defaults. */
export interface StructureHeritablePartBinding {
  isBound: boolean;
}

/**
 * Stage DefaultsBinding projection — never value equality.
 * Standing parts are null when the phase does not classify.
 */
export interface StructureStageDefaultsBinding {
  matchDuration: StructureHeritablePartBinding;
  extraTime: StructureHeritablePartBinding;
  penaltyShootout: StructureHeritablePartBinding;
  administrativeResult: StructureHeritablePartBinding;
  points: StructureHeritablePartBinding | null;
  rankingCriteria: StructureHeritablePartBinding | null;
}

export interface StructurePlacementAward {
  rank: number;
  outcome: ProgressionOutcome;
  /** Structural source key (Cup = BracketPair.PairKey). */
  sourcePairKey: string;
  sourceLabel?: string | null;
  /** Optional execution fixture when the PairKey is materialized (read overlay). */
  boundFixtureId?: string | null;
}

export interface StructureDrawConstraint {
  type: DrawConstraintType;
  enforcement: ConstraintEnforcement;
  maxPerGroup?: number | null;
}

/** POST /competitions/{id}/structure */
export type ConfigureStructureRequest = {
  format: StructureFormatKind | string;
  stageName?: string | null;
  groupCount?: number | null;
  participantsPerGroup?: number | null;
  bracketSize?: number | null;
  /** Championship / Groups only; ignored for Cup / Swiss. Default SingleRoundRobin on Host. */
  matchGenerationFormat?: MatchGenerationFormat | null;
  /** Swiss planned rounds K (≥ 1). Matchdays created by GenerateNextRound. */
  swissRoundCount?: number | null;
};

/** Cleared topology counts when an existing skeleton was rebuilt. */
export interface StructureRebuildImpact {
  clearedMatchdays: number;
  clearedGroups: number;
  clearedRounds: number;
  clearedSlots: number;
  clearedDirectAssignments: number;
  clearedCompositionEntries: number;
  clearedDrawRules: boolean;
  clearedSwissSettings: boolean;
}

/** POST /competitions/{id}/structure response. */
export interface ConfigureStructureResponse {
  stageCreated: boolean;
  rebuildImpact: StructureRebuildImpact | null;
  structure: StructureView;
}

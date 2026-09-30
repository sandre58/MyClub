/**
 * GET /competitions/{id}/overview — aggregated Overview Read.
 */
import type {
  CompetitionStatus,
  CompletionMode,
  DrawResolutionKind,
  DrawResolutionState,
  DrawStatus,
  MatchStatus,
  StageStatus,
} from './enums';
import type { MatchScore } from './matches';
import type { StructureRegulationSummary } from './structure';

/** Cycle reading codes from OverviewAssembler (string on wire). */
export type OverviewCycleCode =
  'Construction' | 'InProgress' | 'Completed' | 'Archived';

/** Prominence codes from OverviewAssembler (string on wire). */
export type OverviewProminence =
  'Present' | 'Condensed' | 'Dominant' | 'Absent';

/** Situation nature on the wire (Blocking | Informational). */
export type OverviewSituationNature = 'Blocking' | 'Informational';

/**
 * GET /competitions/{id}/overview — aggregated Overview Read.
 * Codes + facts only; organizer copy lives in SPA i18n.
 */
export interface OverviewView {
  competitionId: string;
  name: string;
  status: CompetitionStatus;
  completionMode: CompletionMode | null;
  /** Optional competition period — populated when Read exposes boundaries. */
  period?: OverviewCompetitionPeriod | null;
  cycleReading: OverviewCycleReading;
  /**
   * Preparation sub-situation (Host-owned) — not a cycle code.
   * Setup | GeneratedCalendar. SPA must not infer from Ready + match counts.
   */
  preparationFocus: OverviewPreparationFocus | string;
  /** Calendar overview synthesis when preparationFocus is GeneratedCalendar; else null. */
  calendarSummary: OverviewCalendarSummary | null;
  /**
   * Derived final placements (`places[]`) when Completed and presentable.
   * Null when not Completed/Archived, Abandoned, or no Host presentation (Winner|Podium).
   * `presentation` is Host UX hint — independent of standingCompact.
   * SPA Result uses presentation; Standings = full consultation truth.
   */
  competitionOutcome: CompetitionOutcome | null;
  constructionDimensions: OverviewConstructionDimensions;
  operationalFocus: OverviewOperationalFocus;
  situations: OverviewSituation[];
  attentionSummary: OverviewAttentionSummary;
  availableActions: OverviewAction[];
  naturalProgression: OverviewNaturalProgression | null;
  closureHint: OverviewClosureHint;
  navigationHints: OverviewNavigationHint[];
}

/** Read projection — competition final placements (not Domain). */
export interface CompetitionOutcome {
  places: FinalPlacement[];
  /** Host UX: Winner = hero; Podium = Top-3 presentation staging. */
  presentation: 'Winner' | 'Podium';
}

export interface FinalPlacement {
  rank: number;
  entryId: string;
  displayName: string;
}

/** Host-owned preparation focus — wire codes. */
export type OverviewPreparationFocus = 'Setup' | 'GeneratedCalendar';

export interface OverviewCalendarSummary {
  matchdayCount: number;
  matchCount: number;
  matchdays: OverviewCalendarMatchdayPreview[];
  nextMatch: OverviewCalendarNextMatch | null;
}

export interface OverviewCalendarMatchdayPreview {
  matchdayNumber: number;
  matchCount: number;
}

export interface OverviewCalendarNextMatch {
  matchId: string;
  stageId: string;
  matchdayNumber?: number | null;
  scheduledAt?: string | null;
  homeDisplayName: string;
  awayDisplayName: string;
}

export interface OverviewCycleReading {
  code: OverviewCycleCode | string;
}

/** ISO date boundaries when exposed by Overview Read (optional). */
export interface OverviewCompetitionPeriod {
  start?: string | null;
  end?: string | null;
}

export interface OverviewConstructionDimensions {
  teams: OverviewDimension;
  structure: OverviewDimension;
  regulation: OverviewRegulationDimension;
  matches: OverviewDimension;
}

export interface OverviewDimension {
  prominence: OverviewProminence | string;
  facts: Record<string, string>;
}

export interface OverviewRegulationDimension {
  prominence: OverviewProminence | string;
  /** Competition regulation factual summary (Entry / Match / Standing). */
  competition: StructureRegulationSummary;
  /** Primary stage regulation flags when a primary stage exists. */
  stage: OverviewStageRegulationSummary | null;
  /** Domain: ReplaceRegulation allowed in Draft/Ready. */
  competitionRegulationMutable: boolean;
  /** Transition-relative readiness (construction only) — not a global isValid. */
  transitionReadiness: OverviewTransitionReadiness[];
}

export interface OverviewStageRegulationSummary {
  stageId: string;
  stageName: string;
  hasDrawRules: boolean;
  numberOfPots: number | null;
  hasQualificationRules: boolean;
  qualificationPathCount: number;
  hasProgressionRules: boolean;
  progressionPathCount: number;
  hasTieFormat: boolean;
}

/** Ready for a named transition — not regulation validity. */
export interface OverviewTransitionReadiness {
  transition: string;
  ready: boolean;
  blockerCodes: string[];
}

export interface OverviewOperationalFocus {
  stages: OverviewStageFocus[];
  draws: OverviewDrawFocus[];
  matchCounts: OverviewMatchCounts;
  /** Swiss bye pairing events — never fixtures/matches. */
  swissByes: OverviewSwissBye[];
  /** Recent — last engaged unit on ReferenceStage; null → empty state. */
  recentUnit: OverviewSportUnit | null;
  /** Upcoming — next unit (or first before kickoff); null → empty state. */
  nextUnit: OverviewSportUnit | null;
  /** Compact standing; null when not applicable (Cup / no structure). */
  standingCompact: OverviewStandingCompact | null;
  /**
   * Game-rule facts for In-progress Regulation (ReferenceStage).
   * Null when no ReferenceStage — SPA hides the card.
   */
  referenceStageGameRules: OverviewReferenceStageGameRules | null;
}

/** Machine facts for In-progress Regulation — SPA picks 2–3 by formatKind. */
export interface OverviewReferenceStageGameRules {
  stageId: string;
  stageName: string;
  formatKind: string;
  winPoints: number;
  drawPoints: number;
  lossPoints: number;
  numberOfPeriods: number;
  durationPerPeriod: number;
  hasExtraTime: boolean;
  hasPenaltyShootout: boolean;
  numberOfLegs: number;
  aggregateScoring: boolean;
  hasTieExtraTime: boolean;
  hasTiePenaltyShootout: boolean;
  swissPlannedRounds?: number | null;
}

/** Matchday or Round slice for Overview temporal panels. */
export interface OverviewSportUnit {
  stageId: string;
  stageName: string;
  unitKind: 'Matchday' | 'Round' | string;
  unitKey: string;
  matchdayNumber?: number | null;
  roundName?: string | null;
  matchCount: number;
  matches: OverviewMatchLine[];
}

export interface OverviewMatchLine {
  matchId: string;
  stageId: string;
  status: MatchStatus | string;
  scheduledAt?: string | null;
  homeDisplayName: string;
  awayDisplayName: string;
  score?: MatchScore | null;
}

export interface OverviewStandingCompact {
  stageId: string;
  stageName: string;
  /** Overall: one table. Group: one table per group (SPA may show one at a time). */
  tables: OverviewStandingCompactTable[];
}

export interface OverviewStandingCompactTable {
  scope: string;
  groupId?: string | null;
  groupName?: string | null;
  rows: OverviewStandingCompactRow[];
}

export interface OverviewStandingCompactRow {
  position: number;
  entryId: string;
  displayName: string;
  played: number;
  points: number;
}

export interface OverviewSwissBye {
  stageId: string;
  roundIndex: number;
  entryId: string;
  entryDisplayName: string;
}

export interface OverviewStageFocus {
  stageId: string;
  name: string;
  status: StageStatus;
}

export interface OverviewDrawFocus {
  stageId: string;
  drawId: string;
  kind: DrawResolutionKind;
  status: DrawStatus;
  resolutionState: DrawResolutionState;
  /** Application-derived (Publish ≠ Apply). Do not recompute in React. */
  isApplied: boolean;
}

export interface OverviewMatchCounts {
  live: number;
  scheduled: number;
  finished: number;
  postponed: number;
  cancelled: number;
  total: number;
}

export interface OverviewSituation {
  source: string;
  nature: OverviewSituationNature | string;
  targetType: string | null;
  targetId: string | null;
  matchId: string | null;
  /** Host-projected; do not infer from source in React. */
  actionable: boolean;
  actionCode: string | null;
  /** Optional impact code for i18n; omit/null when not provided. */
  impactCode: string | null;
  params: Record<string, string>;
}

export interface OverviewAttentionSummary {
  count: number;
  items: OverviewSituation[];
}

export interface OverviewAction {
  code: string;
  guaranteed: boolean;
  stageId?: string | null;
  drawId?: string | null;
  matchId?: string | null;
  fixtureId?: string | null;
  params?: Record<string, string> | null;
}

export interface OverviewNaturalProgression {
  code: string;
}

export interface OverviewClosureHint {
  canCompleteNormally: boolean;
  blockerCodes: string[];
}

export interface OverviewNavigationHint {
  targetType: string;
  targetId: string;
  matchId: string | null;
  stageId: string | null;
  competitionId: string | null;
}

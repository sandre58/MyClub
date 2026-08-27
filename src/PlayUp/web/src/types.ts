/**
 * Manual mirrors of Application Reads DTOs.
 * ASP.NET Core JSON uses camelCase property names.
 * Phase 12.8: enums are JSON strings (enum member names), not numbers.
 * See docs/guides/http-api-contract.md.
 *
 * TypeScript types document the expected shape at compile time.
 * They do NOT validate JSON at runtime — a mismatched API still type-checks.
 */

import i18n from './i18n'

export type CompetitionStatus =
  | 'Draft'
  | 'Ready'
  | 'Running'
  | 'Suspended'
  | 'Completed'
  | 'Archived'

/** Host CompletionMode — string enum member names. */
export type CompletionMode = 'Normal' | 'Administrative' | 'Abandoned'

export type EntryStatus =
  | 'Active'
  | 'Qualified'
  | 'Eliminated'
  | 'Withdrawn'
  | 'Excluded'

export type StageStatus =
  | 'Draft'
  | 'Ready'
  | 'Running'
  | 'Suspended'
  | 'Completed'

export type MatchStatus =
  | 'Scheduled'
  | 'Live'
  | 'Finished'
  | 'Postponed'
  | 'Cancelled'

export type ResultType = 'Played' | 'Forfeit' | 'WalkOver' | 'Administrative'

export type DrawStatus = 'Draft' | 'Published' | 'Cancelled'

export type DrawResolutionKind = 'Slot' | 'Group' | 'Pairing'

export type DrawResolutionState = 'NotResolved' | 'Resolved' | 'NoSolution'

/** Application StructureFormatKind — organisation format intent (string on wire). */
export type StructureFormatKind = 'Championship' | 'Groups' | 'Cup' | 'Swiss'

/** Domain MatchGenerationFormat — Championship / Groups RR mode (string on wire). */
export type MatchGenerationFormat = 'SingleRoundRobin' | 'DoubleRoundRobin'

export interface CompetitionEntrySummary {
  entryId: string
  displayName: string
  status: EntryStatus
  shortName?: string | null
  logoMediaId?: string | null
  primaryColor?: string | null
  secondaryColor?: string | null
}

export interface CompetitionStageSummary {
  stageId: string
  name: string
  status: StageStatus
}

/** GET /competitions — one row in the organizer list. */
export interface CompetitionListItem {
  id: string
  name: string
  status: CompetitionStatus
  shortName?: string | null
  logoMediaId?: string | null
}

/** POST /competitions — create Draft competition (name only). */
export interface CreateCompetitionRequest {
  name: string
}

/** Domain CompetitionName.MaxLength — client hint; Host remains authority. */
export const COMPETITION_NAME_MAX_LENGTH = 100

/**
 * GET /competitions/{id}/workspace — Accueil / competition landing.
 * nextActionCode and attention/completion fields are Read facts from the Host.
 */
export interface WorkspaceSummary {
  id: string
  name: string
  status: CompetitionStatus
  nextActionCode: string | null
  attentionCount: number
  completionMode: CompletionMode | null
  canCompleteNormally: boolean
  completionBlockers: string[] | null
  shortName?: string | null
  logoMediaId?: string | null
}

/** Cycle reading codes from CockpitAssembler (string on wire). */
export type CockpitCycleCode =
  | 'Construction'
  | 'InProgress'
  | 'Completed'
  | 'Archived'

/** Prominence codes from CockpitAssembler (string on wire). */
export type CockpitProminence =
  | 'Present'
  | 'Condensed'
  | 'Dominant'
  | 'Absent'

/** Situation nature — minimal V1 (string on wire). */
export type CockpitSituationNature = 'Blocking' | 'Informational'

/**
 * GET /competitions/{id}/cockpit — Phase 16.1 aggregated Cockpit Read.
 * Codes + facts only; organizer copy lives in SPA i18n.
 */
export interface CockpitView {
  competitionId: string
  name: string
  status: CompetitionStatus
  completionMode: CompletionMode | null
  /** Optional competition period — populated when Read exposes boundaries. */
  period?: CockpitCompetitionPeriod | null
  cycleReading: CockpitCycleReading
  constructionDimensions: CockpitConstructionDimensions
  operationalFocus: CockpitOperationalFocus
  situations: CockpitSituation[]
  attentionSummary: CockpitAttentionSummary
  availableActions: CockpitAction[]
  naturalProgression: CockpitNaturalProgression | null
  closureHint: CockpitClosureHint
  navigationHints: CockpitNavigationHint[]
}

export interface CockpitCycleReading {
  code: CockpitCycleCode | string
}

/** ISO date boundaries when exposed by Cockpit Read (optional). */
export interface CockpitCompetitionPeriod {
  start?: string | null
  end?: string | null
}

export interface CockpitConstructionDimensions {
  teams: CockpitDimension
  structure: CockpitDimension
  regulation: CockpitRegulationDimension
  matches: CockpitDimension
}

export interface CockpitDimension {
  prominence: CockpitProminence | string
  facts: Record<string, string>
}

export interface CockpitRegulationDimension {
  prominence: CockpitProminence | string
  /** Competition regulation factual summary (Entry / Match / Standing). */
  competition: OrganisationRegulationSummary
  /** Primary stage regulation flags when a primary stage exists. */
  stage: CockpitStageRegulationSummary | null
  /** Domain: ReplaceRegulation allowed in Draft/Ready. */
  competitionRegulationMutable: boolean
  /** Transition-relative readiness (construction only) — not a global isValid. */
  transitionReadiness: CockpitTransitionReadiness[]
}

export interface CockpitStageRegulationSummary {
  stageId: string
  stageName: string
  hasDrawRules: boolean
  numberOfPots: number | null
  hasQualificationRules: boolean
  qualificationPathCount: number
  hasProgressionRules: boolean
  progressionPathCount: number
  hasTieFormat: boolean
}

/** Ready for a named transition — not regulation validity. */
export interface CockpitTransitionReadiness {
  transition: string
  ready: boolean
  blockerCodes: string[]
}

export interface CockpitOperationalFocus {
  stages: CockpitStageFocus[]
  draws: CockpitDrawFocus[]
  matchCounts: CockpitMatchCounts
  /** Swiss bye pairing events — never fixtures/matches. */
  swissByes: CockpitSwissBye[]
  /** Dernières — last engaged unit on ReferenceStage; null → empty state. */
  recentUnit: CockpitSportUnit | null
  /** Prochaines — next unit (or first before kickoff); null → empty state. */
  nextUnit: CockpitSportUnit | null
  /** Compact standing; null when not applicable (Cup / no structure). */
  standingCompact: CockpitStandingCompact | null
  /**
   * Game-rule facts for En cours Règlement (ReferenceStage).
   * Null when no ReferenceStage — SPA hides the card.
   */
  referenceStageGameRules: CockpitReferenceStageGameRules | null
}

/** Machine facts for En cours Règlement — SPA picks 2–3 by formatKind. */
export interface CockpitReferenceStageGameRules {
  stageId: string
  stageName: string
  formatKind: string
  winPoints: number
  drawPoints: number
  lossPoints: number
  numberOfPeriods: number
  durationPerPeriod: number
  hasExtraTime: boolean
  hasPenaltyShootout: boolean
  numberOfLegs: number
  aggregateScoring: boolean
  hasTieExtraTime: boolean
  hasTiePenaltyShootout: boolean
  swissPlannedRounds?: number | null
}

/** Matchday or Round slice for Vue d'ensemble temporal panels. */
export interface CockpitSportUnit {
  stageId: string
  stageName: string
  unitKind: 'Matchday' | 'Round' | string
  unitKey: string
  matchdayNumber?: number | null
  roundName?: string | null
  matchCount: number
  matches: CockpitMatchLine[]
}

export interface CockpitMatchLine {
  matchId: string
  stageId: string
  status: MatchStatus | string
  scheduledAt?: string | null
  homeDisplayName: string
  awayDisplayName: string
  score?: MatchScore | null
}

export interface CockpitStandingCompact {
  stageId: string
  stageName: string
  /** Overall: one table. Group: one table per group (SPA may show one at a time). */
  tables: CockpitStandingCompactTable[]
}

export interface CockpitStandingCompactTable {
  scope: string
  groupId?: string | null
  groupName?: string | null
  rows: CockpitStandingCompactRow[]
}

export interface CockpitStandingCompactRow {
  position: number
  entryId: string
  displayName: string
  played: number
  points: number
}

export interface CockpitSwissBye {
  stageId: string
  roundIndex: number
  entryId: string
  entryDisplayName: string
}

export interface CockpitStageFocus {
  stageId: string
  name: string
  status: StageStatus
}

export interface CockpitDrawFocus {
  stageId: string
  drawId: string
  kind: DrawResolutionKind
  status: DrawStatus
  resolutionState: DrawResolutionState
  /** Application-derived (Publish ≠ Apply). Do not recompute in React. */
  isApplied: boolean
}

export interface CockpitMatchCounts {
  live: number
  scheduled: number
  finished: number
  postponed: number
  cancelled: number
  total: number
}

export interface CockpitSituation {
  source: string
  nature: CockpitSituationNature | string
  targetType: string | null
  targetId: string | null
  matchId: string | null
  /** Host-projected; do not infer from source in React. */
  actionable: boolean
  actionCode: string | null
  /** Optional impact code for i18n; omit/null when not provided. */
  impactCode: string | null
  params: Record<string, string>
}

export interface CockpitAttentionSummary {
  count: number
  items: CockpitSituation[]
}

export interface CockpitAction {
  code: string
  guaranteed: boolean
  stageId?: string | null
  drawId?: string | null
  matchId?: string | null
  fixtureId?: string | null
  params?: Record<string, string> | null
}

export interface CockpitNaturalProgression {
  code: string
}

export interface CockpitClosureHint {
  canCompleteNormally: boolean
  blockerCodes: string[]
}

export interface CockpitNavigationHint {
  targetType: string
  targetId: string
  matchId: string | null
  stageId: string | null
  competitionId: string | null
}

/** GET /competitions/{id}/attention — derived Needs Attention hub. */
export interface NeedsAttention {
  competitionId: string
  items: NeedsAttentionItem[]
  /** Host also exposes Count; prefer items.length when omitted. */
  count?: number
}

export interface NeedsAttentionItem {
  source: string
  severity: string
  targetType: string | null
  targetId: string | null
}

export interface CompetitionOverview {
  id: string
  name: string
  status: CompetitionStatus
  entries: CompetitionEntrySummary[]
  stages: CompetitionStageSummary[]
  completionMode?: CompletionMode | null
  shortName?: string | null
  logoMediaId?: string | null
  scheduledStart?: string | null
  scheduledEnd?: string | null
}

/** GET /competitions/{id}/organisation — Slice 2 Organisation hub. */
export interface OrganisationView {
  competitionId: string
  name: string
  status: CompetitionStatus
  participants: OrganisationParticipantsSummary
  format: OrganisationFormatSummary
  regulation: OrganisationRegulationSummary
  structure: OrganisationStructureSummary
  actions: string[]
  readiness: OrganisationReadiness
  shortName?: string | null
  logoMediaId?: string | null
  scheduledStart?: string | null
  scheduledEnd?: string | null
}

export interface OrganisationParticipantsSummary {
  activeCount: number
  occupyingCount: number
  entries: OrganisationEntry[]
}

export interface OrganisationEntry {
  entryId: string
  displayName: string
  status: EntryStatus
  shortName?: string | null
  logoMediaId?: string | null
  primaryColor?: string | null
  secondaryColor?: string | null
}

export interface OrganisationFormatSummary {
  kind: StructureFormatKind | null
  primaryStageId: string | null
  primaryStageName: string | null
  primaryStageStatus: StageStatus | null
}

export interface OrganisationRegulationSummary {
  minimumTeams: number
  maximumTeams: number
  durationPerPeriod: number
  numberOfPeriods: number
  winPoints: number
  drawPoints: number
  lossPoints: number
}

export interface OrganisationStructureSummary {
  groupCount: number
  roundCount: number
  matchdayCount: number
  slotCount: number
  hasDrawRules: boolean
  numberOfPots: number | null
  matchGenerationFormat: MatchGenerationFormat
  /** Planned Swiss rounds K when kind is Swiss; null otherwise. */
  swissRoundCount?: number | null
}

export interface OrganisationReadiness {
  readyForNextSlice: boolean
  readyForDraw: boolean
  readyForMaterialization: boolean
  readyForSchedule: boolean
  readyForMatchOperation: boolean
  readyForSchedulePath: boolean
  attachedMatchCount: number
  blockers: string[]
}

/** POST /competitions/{id}/entries */
export interface AddEntryRequest {
  displayName: string
  teamId?: string | null
  shortName?: string | null
  logoMediaId?: string | null
  primaryColor?: string | null
  secondaryColor?: string | null
}

/** POST /competitions/{id}/presentation */
export interface UpdateCompetitionPresentationRequest {
  shortName: string | null
  logoMediaId: string | null
}

/** POST /competitions/{id}/schedule */
export interface SetCompetitionScheduleRequest {
  scheduledStart: string | null
  scheduledEnd: string | null
}

/** POST .../entries/{entryId}/presentation */
export interface UpdateEntryPresentationRequest {
  shortName: string | null
  logoMediaId: string | null
  primaryColor: string | null
  secondaryColor: string | null
}

/** POST .../entries/{entryId}/rename */
export interface RenameEntryRequest {
  displayName: string
}

/** PUT /competitions/{id}/regulation */
export interface ReplaceRegulationRequest {
  minimumTeams: number
  maximumTeams: number
  durationPerPeriod: number
  numberOfPeriods: number
  halfTimeDuration: number
  winPoints: number
  drawPoints: number
  lossPoints: number
  forfeitWinnerGoals?: number
  forfeitLoserGoals?: number
}

/** POST /competitions/{id}/organisation/structure */
export type ConfigureStructureRequest = {
  format: StructureFormatKind | string
  stageName?: string | null
  matchdayCount?: number | null
  groupCount?: number | null
  participantsPerGroup?: number | null
  bracketSize?: number | null
  /** Championship / Groups only; ignored for Cup / Swiss. Default SingleRoundRobin on Host. */
  matchGenerationFormat?: MatchGenerationFormat | null
  /** Swiss planned rounds K (≥ 1). Matchdays created by GenerateNextRound. */
  swissRoundCount?: number | null
}

/** GET /competitions/{id}/consultation — Slice 7 multi-consumer Read (camelCase wire). */
export interface ConsultationView {
  competitionId: string
  name: string
  status: CompetitionStatus
  completionMode: CompletionMode | null
  formatKind: StructureFormatKind | null
  formatLabel: string
  results: ConsultationResult[]
  standings: ConsultationStandingsSection
  structure: ConsultationStructure
}

export interface ConsultationResult {
  matchId: string
  stageId: string
  fixtureId: string | null
  roundId: string | null
  matchdayNumber: number | null
  contextLabel: string | null
  status: MatchStatus
  home: EntrySide
  away: EntrySide
  score: MatchScore | null
  resultType: ResultType | null
  scheduledAt: string | null
}

export interface ConsultationStandingsSection {
  applicable: boolean
  notApplicableReason: string | null
  tables: ConsultationStandingTable[]
}

export interface ConsultationStandingTable {
  scope: string
  stageId: string
  stageName: string
  groupId: string | null
  groupName: string | null
  rows: ConsultationStandingRow[]
}

export interface ConsultationStandingRow {
  position: number
  entryId: string
  displayName: string
  played: number
  wins: number
  draws: number
  losses: number
  goalsFor: number
  goalsAgainst: number
  goalDifference: number
  points: number
}

/** Structure section — typed for contract fidelity; Classements does not render it. */
export interface ConsultationStructure {
  formatKind: StructureFormatKind | null
  stages: ConsultationStageStructure[]
}

export interface ConsultationStageStructure {
  stageId: string
  name: string
  status: StageStatus
  formatKind: StructureFormatKind | null
  matchGenerationFormat: MatchGenerationFormat
  groups: unknown[]
  matchdays: unknown[]
  rounds: unknown[]
  slots: unknown[]
}

export interface EntrySide {
  entryId: string
  displayName: string | null
  shortName?: string | null
  logoMediaId?: string | null
  primaryColor?: string | null
  secondaryColor?: string | null
}

export interface MatchScore {
  homeGoals: number
  awayGoals: number
}

export interface MatchResult {
  type: ResultType
  homeGoals: number
  awayGoals: number
  extraTimePlayed: boolean
  shootout: MatchScore | null
}

export interface MatchSummary {
  matchId: string
  stageId: string
  status: MatchStatus
  home: EntrySide
  away: EntrySide
  score: MatchScore | null
  fixtureId: string | null
  roundId: string | null
  /** Kickoff from Stage placement when scheduled. */
  scheduledAt?: string | null
  matchdayNumber?: number | null
  /** Knockout round display name from the Read. */
  roundName?: string | null
  /** Present when Domain has a result; never inferred in React. */
  resultType?: ResultType | null
}

export interface MatchDetail {
  matchId: string
  competitionId: string
  stageId: string
  status: MatchStatus
  home: EntrySide
  away: EntrySide
  result: MatchResult | null
  fixtureId: string | null
  legIndex: number | null
}

/**
 * Body for POST /matches/{id}/finish — mirrors Host FinishMatchRequest (camelCase JSON).
 * Shootout fields: send both or neither (Host rejects a partial pair).
 * `type` is a ResultType string enum member name (e.g. "Played").
 */
export interface FinishMatchRequest {
  type: ResultType
  homeGoals: number
  awayGoals: number
  extraTimePlayed: boolean
  penaltyShootoutHomeGoals?: number | null
  penaltyShootoutAwayGoals?: number | null
}

/** Body for POST .../draws/{id}/apply — Pairing needs one fixture id per pairing (same order). */
export interface ApplyDrawRequest {
  fixtureIds: string[]
}

export interface StageFixtureAttachment {
  matchId: string
  legIndex: number
}

export interface StageFixture {
  id: string
  slotAKey: string | null
  slotBKey: string | null
  attachments: StageFixtureAttachment[]
}

export interface StageRound {
  id: string
  name: string
  fixtures: StageFixture[]
}

export interface StageSlot {
  slotKey: string
  entryId: string | null
  displayName: string | null
  /** True when a complete Fixture already covers this slot (exclude from from-slots pairing). */
  coveredByCompleteFixture: boolean
}

export interface StageDrawPairing {
  entryAId: string
  entryADisplayName: string | null
  entryBId: string
  entryBDisplayName: string | null
}

export interface StageDrawSlotPlacement {
  slotKey: string
  entryId: string
  displayName: string | null
}

export interface StageDraw {
  id: string
  kind: DrawResolutionKind
  status: DrawStatus
  resolutionState: DrawResolutionState
  pairings: StageDrawPairing[]
  slotPlacements: StageDrawSlotPlacement[]
}

export interface StageOverview {
  id: string
  competitionId: string
  name: string
  status: StageStatus
  rounds: StageRound[]
  slots: StageSlot[]
  draws: StageDraw[]
}

export const resultTypeOptions: readonly ResultType[] = [
  'Played',
  'Forfeit',
  'WalkOver',
  'Administrative',
] as const

export function sideLabel(side: EntrySide): string {
  return side.displayName?.trim() || i18n.t('unknownEntry')
}

export function formatScore(score: MatchScore): string {
  return `${score.homeGoals}–${score.awayGoals}`
}

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

export type EntryStatus = 'Active' | 'Withdrawn'

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
  scheduledStart?: string | null
  scheduledEnd?: string | null
}

/** POST /competitions — create Draft competition (name only). */
export interface CreateCompetitionRequest {
  name: string
}

/** Domain CompetitionName.MaxLength — client hint; Host remains authority. */
export const COMPETITION_NAME_MAX_LENGTH = 100

/** Domain DeclaredMember.DisplayNameMaxLength — client hint; Host remains authority. */
export const MEMBER_DISPLAY_NAME_MAX_LENGTH = 100

/** Host DeclaredMemberRole — string enum member names. */
export type DeclaredMemberRole = 'Player' | 'Staff'

/** GET organisation `entries[].declaredMembers[]`. */
export interface DeclaredMember {
  memberId: string
  displayName: string
  role: DeclaredMemberRole
  /** True when still listed on a match composition sheet — remove is blocked. */
  referencedOnMatchSheet?: boolean
}

/** POST .../declared-members — SPA always sends Player. */
export interface AddDeclaredMemberRequest {
  displayName: string
  role: DeclaredMemberRole
}

/** POST .../declared-members/{memberId}/rename */
export interface RenameDeclaredMemberRequest {
  displayName: string
}

/** POST .../entry-lots/delete | withdraw */
export interface EntryIdsRequest {
  entryIds: string[]
}

/** POST .../declared-member-lots/remove */
export interface MemberIdsRequest {
  memberIds: string[]
}

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

/** Cycle reading codes from OverviewAssembler (string on wire). */
export type OverviewCycleCode =
  | 'Construction'
  | 'InProgress'
  | 'Completed'
  | 'Archived'

/** Prominence codes from OverviewAssembler (string on wire). */
export type OverviewProminence =
  | 'Present'
  | 'Condensed'
  | 'Dominant'
  | 'Absent'

/** Situation nature — minimal V1 (string on wire). */
export type OverviewSituationNature = 'Blocking' | 'Informational'

/**
 * GET /competitions/{id}/overview — Phase 16.1 aggregated Overview Read.
 * Codes + facts only; organizer copy lives in SPA i18n.
 */
export interface OverviewView {
  competitionId: string
  name: string
  status: CompetitionStatus
  completionMode: CompletionMode | null
  /** Optional competition period — populated when Read exposes boundaries. */
  period?: OverviewCompetitionPeriod | null
  cycleReading: OverviewCycleReading
  /**
   * Préparation sub-situation (Host-owned) — not a cycle code.
   * Setup | GeneratedCalendar. SPA must not infer from Ready + match counts.
   */
  preparationFocus: OverviewPreparationFocus | string
  /** Calendar overview synthesis when preparationFocus is GeneratedCalendar; else null. */
  calendarSummary: OverviewCalendarSummary | null
  /**
   * Derived final placements (`places[]`) when Terminée and presentable.
   * Null when not Terminée/Archived, Abandoned, or no Host presentation (Winner|Podium).
   * `presentation` is Host UX hint — independent of standingCompact.
   * SPA Résultat uses presentation; Classements = full consultation truth.
   */
  competitionOutcome: CompetitionOutcome | null
  constructionDimensions: OverviewConstructionDimensions
  operationalFocus: OverviewOperationalFocus
  situations: OverviewSituation[]
  attentionSummary: OverviewAttentionSummary
  availableActions: OverviewAction[]
  naturalProgression: OverviewNaturalProgression | null
  closureHint: OverviewClosureHint
  navigationHints: OverviewNavigationHint[]
}

/** Read projection — competition final placements (not Domain). */
export interface CompetitionOutcome {
  places: FinalPlacement[]
  /** Host UX: Winner = hero; Podium = Top-3 mise en scène. */
  presentation: 'Winner' | 'Podium'
}

export interface FinalPlacement {
  rank: number
  entryId: string
  displayName: string
}

/** Host-owned Préparation focus — wire codes. */
export type OverviewPreparationFocus = 'Setup' | 'GeneratedCalendar'

export interface OverviewCalendarSummary {
  matchdayCount: number
  matchCount: number
  matchdays: OverviewCalendarMatchdayPreview[]
  nextMatch: OverviewCalendarNextMatch | null
}

export interface OverviewCalendarMatchdayPreview {
  matchdayNumber: number
  matchCount: number
}

export interface OverviewCalendarNextMatch {
  matchId: string
  stageId: string
  matchdayNumber?: number | null
  scheduledAt?: string | null
  homeDisplayName: string
  awayDisplayName: string
}

export interface OverviewCycleReading {
  code: OverviewCycleCode | string
}

/** ISO date boundaries when exposed by Overview Read (optional). */
export interface OverviewCompetitionPeriod {
  start?: string | null
  end?: string | null
}

export interface OverviewConstructionDimensions {
  teams: OverviewDimension
  structure: OverviewDimension
  regulation: OverviewRegulationDimension
  matches: OverviewDimension
}

export interface OverviewDimension {
  prominence: OverviewProminence | string
  facts: Record<string, string>
}

export interface OverviewRegulationDimension {
  prominence: OverviewProminence | string
  /** Competition regulation factual summary (Entry / Match / Standing). */
  competition: OrganisationRegulationSummary
  /** Primary stage regulation flags when a primary stage exists. */
  stage: OverviewStageRegulationSummary | null
  /** Domain: ReplaceRegulation allowed in Draft/Ready. */
  competitionRegulationMutable: boolean
  /** Transition-relative readiness (construction only) — not a global isValid. */
  transitionReadiness: OverviewTransitionReadiness[]
}

export interface OverviewStageRegulationSummary {
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
export interface OverviewTransitionReadiness {
  transition: string
  ready: boolean
  blockerCodes: string[]
}

export interface OverviewOperationalFocus {
  stages: OverviewStageFocus[]
  draws: OverviewDrawFocus[]
  matchCounts: OverviewMatchCounts
  /** Swiss bye pairing events — never fixtures/matches. */
  swissByes: OverviewSwissBye[]
  /** Dernières — last engaged unit on ReferenceStage; null → empty state. */
  recentUnit: OverviewSportUnit | null
  /** Prochaines — next unit (or first before kickoff); null → empty state. */
  nextUnit: OverviewSportUnit | null
  /** Compact standing; null when not applicable (Cup / no structure). */
  standingCompact: OverviewStandingCompact | null
  /**
   * Game-rule facts for En cours Règlement (ReferenceStage).
   * Null when no ReferenceStage — SPA hides the card.
   */
  referenceStageGameRules: OverviewReferenceStageGameRules | null
}

/** Machine facts for En cours Règlement — SPA picks 2–3 by formatKind. */
export interface OverviewReferenceStageGameRules {
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
export interface OverviewSportUnit {
  stageId: string
  stageName: string
  unitKind: 'Matchday' | 'Round' | string
  unitKey: string
  matchdayNumber?: number | null
  roundName?: string | null
  matchCount: number
  matches: OverviewMatchLine[]
}

export interface OverviewMatchLine {
  matchId: string
  stageId: string
  status: MatchStatus | string
  scheduledAt?: string | null
  homeDisplayName: string
  awayDisplayName: string
  score?: MatchScore | null
}

export interface OverviewStandingCompact {
  stageId: string
  stageName: string
  /** Overall: one table. Group: one table per group (SPA may show one at a time). */
  tables: OverviewStandingCompactTable[]
}

export interface OverviewStandingCompactTable {
  scope: string
  groupId?: string | null
  groupName?: string | null
  rows: OverviewStandingCompactRow[]
}

export interface OverviewStandingCompactRow {
  position: number
  entryId: string
  displayName: string
  played: number
  points: number
}

export interface OverviewSwissBye {
  stageId: string
  roundIndex: number
  entryId: string
  entryDisplayName: string
}

export interface OverviewStageFocus {
  stageId: string
  name: string
  status: StageStatus
}

export interface OverviewDrawFocus {
  stageId: string
  drawId: string
  kind: DrawResolutionKind
  status: DrawStatus
  resolutionState: DrawResolutionState
  /** Application-derived (Publish ≠ Apply). Do not recompute in React. */
  isApplied: boolean
}

export interface OverviewMatchCounts {
  live: number
  scheduled: number
  finished: number
  postponed: number
  cancelled: number
  total: number
}

export interface OverviewSituation {
  source: string
  nature: OverviewSituationNature | string
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

export interface OverviewAttentionSummary {
  count: number
  items: OverviewSituation[]
}

export interface OverviewAction {
  code: string
  guaranteed: boolean
  stageId?: string | null
  drawId?: string | null
  matchId?: string | null
  fixtureId?: string | null
  params?: Record<string, string> | null
}

export interface OverviewNaturalProgression {
  code: string
}

export interface OverviewClosureHint {
  canCompleteNormally: boolean
  blockerCodes: string[]
}

export interface OverviewNavigationHint {
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
  /** Optional structured params for SPA templates (e.g. activeCount, minimumTeams). */
  params?: Record<string, string> | null
}

export interface CompetitionDetail {
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

/** GET /competitions/{id}/matches-hub — Match Hub single load. */
export interface MatchHubView {
  detail: CompetitionDetail
  stages: MatchHubStageMatches[]
}

export interface MatchHubStageMatches {
  stageId: string
  name: string
  status: StageStatus
  matches: MatchSummary[]
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
  /** Per-stage topology + regulation tokens (Règlement hub). */
  stages: OrganisationStageHubSummary[]
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
  declaredMembers?: DeclaredMember[]
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
  /** Authorized disciplinary catalogue types (empty = none). Omitted only in older fixtures. */
  allowedTypes?: DisciplinaryType[]
  /** Half-time break minutes (Host). Optional in older fixtures. */
  halfTimeDuration?: number
  /** Competition MatchRules ExtraTimePolicy present. */
  hasExtraTime?: boolean
  extraTimeDurationPerPeriod?: number | null
  extraTimeNumberOfPeriods?: number | null
  /** Competition MatchRules PenaltyShootoutPolicy present. */
  hasPenaltyShootout?: boolean
  /** TAB initial kicks per team when hasPenaltyShootout. */
  penaltyInitialKicksPerTeam?: number | null
  /** Ordered standing ranking criteria. */
  rankingCriteria?: RankingCriterion[]
  /** Administrative forfeit score — winner goals. */
  forfeitWinnerGoals?: number
  /** Administrative forfeit score — loser goals. */
  forfeitLoserGoals?: number
}

/** Host RankingCriterion — string enum member names. */
export type RankingCriterion =
  | 'Points'
  | 'GoalDifference'
  | 'GoalsFor'
  | 'GoalsAgainst'
  | 'Wins'
  | 'HeadToHead'

/** Host DisciplinaryType — string enum member names. */
export type DisciplinaryType = 'Yellow' | 'Red' | 'White'

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

/** Host DrawMode — string enum member names. */
export type DrawMode = 'Random'

/** GET organisation `stages[]` — Règlement hub phase row. */
export interface OrganisationStageHubSummary {
  stageId: string
  name: string
  status: StageStatus
  teamCount: number
  matchCount: number
  /** Topology: groups in this phase. Optional in older fixtures. */
  groupCount?: number
  /** Topology: cup rounds in this phase. Optional in older fixtures. */
  roundCount?: number
  numberOfPeriods: number
  durationPerPeriod: number
  hasExtraTime: boolean
  extraTimeNumberOfPeriods?: number | null
  extraTimeDurationPerPeriod?: number | null
  hasPenaltyShootout: boolean
  penaltyInitialKicksPerTeam?: number | null
  /** A5: standing present only when the phase classifies. */
  hasStandingRules?: boolean
  winPoints?: number | null
  drawPoints?: number | null
  lossPoints?: number | null
  hasDrawRules: boolean
  drawMode?: DrawMode | null
  numberOfPots?: number | null
  hasQualificationRules: boolean
  qualificationPathCount: number
  hasProgressionRules: boolean
  progressionPathCount: number
  hasTieFormat: boolean
  numberOfLegs?: number | null
  aggregateScoring?: boolean | null
  /** Away-goals rule on the effective TieFormat. */
  hasAwayGoalsRule?: boolean
  /** Extra-time rule on the confrontation TieFormat. */
  hasTieExtraTime?: boolean
  /** Penalty-shootout rule on the confrontation TieFormat. */
  hasTiePenaltyShootout?: boolean
  /** Stage StandingRules ranking criteria when hasStandingRules. */
  rankingCriteria?: RankingCriterion[]
  hasPlacementAwardRules?: boolean
  placementAwardCount?: number
  /** Placement paths (rank + outcome), ordered by rank. */
  placementAwards?: OrganisationPlacementAward[]
  /** Inferred structure format for schematic / badge context. */
  formatKind?: StructureFormatKind | null
  /** Planned Swiss rounds when formatKind is Swiss. */
  swissRoundCount?: number | null
  /** Administrative forfeit score — winner goals. */
  forfeitWinnerGoals?: number | null
  /** Administrative forfeit score — loser goals. */
  forfeitLoserGoals?: number | null
  /** SeedingRules.NumberOfSeeds when draw seeding is set. */
  numberOfSeeds?: number | null
  /** DrawRules.Constraints (all stored constraints). */
  drawConstraints?: OrganisationDrawConstraint[]
}

/** Host ProgressionOutcome — placement / progression selector. */
export type ProgressionOutcome = 'Winner' | 'Loser'

export interface OrganisationPlacementAward {
  rank: number
  outcome: ProgressionOutcome
}

/** Host DrawConstraintType — string enum member names. */
export type DrawConstraintType =
  | 'SameTeamAvoidance'
  | 'SameGroupAvoidance'
  | 'SameAssociationAvoidance'
  | 'MaxSameAssociationPerGroup'

/** Host ConstraintEnforcement — string enum member names. */
export type ConstraintEnforcement = 'Preferred' | 'Required'

export interface OrganisationDrawConstraint {
  type: DrawConstraintType
  enforcement: ConstraintEnforcement
  maxPerGroup?: number | null
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
  shortName: string
  logoMediaId: string | null
  primaryColor: string | null
  secondaryColor: string | null
}

/** POST .../entries/{entryId}/rename */
export interface RenameEntryRequest {
  displayName: string
}

/** PUT /competitions/{id}/regulation — full competition regulation + bound-stage propagation. */
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
  /**
   * Omit to preserve server DisciplinaryRules; send [] for None;
   * send explicit catalogue to replace.
   */
  allowedTypes?: DisciplinaryType[] | null
  /** Ordered ranking criteria; omit/empty → Host bootstrap baseline. */
  rankingCriteria?: RankingCriterion[] | null
  hasExtraTime?: boolean
  extraTimeDurationPerPeriod?: number | null
  extraTimeNumberOfPeriods?: number | null
  hasPenaltyShootout?: boolean
  penaltyInitialKicksPerTeam?: number | null
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

/** Host Side — string enum member names. */
export type MatchSide = 'Home' | 'Away'

/** Host CompositionStatus — string enum member names. */
export type CompositionStatus = 'Starter' | 'Bench'

/** GET match detail declaredParticipations[] */
export interface DeclaredParticipation {
  memberId: string
  displayName: string | null
  side: MatchSide
  compositionStatus: CompositionStatus
  jerseyNumber: number | null
}

/** POST /matches/{id}/declared-participations */
export interface AddDeclaredParticipationRequest {
  memberId: string
  side: MatchSide
  compositionStatus: CompositionStatus
  jerseyNumber?: number | null
}

/** GET match detail recordedGoals[] */
export interface RecordedGoal {
  goalId: string
  scorerMemberId: string
  scorerDisplayName: string | null
  creditedSide: MatchSide
  assisterMemberId: string | null
  assisterDisplayName: string | null
  isOwnGoal: boolean
}

/** POST/PUT /matches/{id}/recorded-goals */
export interface RecordGoalRequest {
  scorerMemberId: string
  creditedSide: MatchSide
  assisterMemberId?: string | null
}

/** GET match detail recordedSubstitutions[] */
export interface RecordedSubstitution {
  substitutionId: string
  side: MatchSide
  outMemberId: string
  outDisplayName: string | null
  inMemberId: string
  inDisplayName: string | null
}

/** POST/PUT /matches/{id}/recorded-substitutions */
export interface RecordSubstitutionRequest {
  outMemberId: string
  inMemberId: string
  side: MatchSide
}

/** GET match detail recordedDisciplinaryEvents[] */
export interface RecordedDisciplinaryEvent {
  disciplinaryEventId: string
  memberId: string
  memberDisplayName: string | null
  type: DisciplinaryType
}

/** POST/PUT /matches/{id}/recorded-disciplinary-events */
export interface RecordDisciplinaryEventRequest {
  memberId: string
  type: DisciplinaryType
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
  scheduledAt?: string | null
  hasObservedLive?: boolean
  runningScore?: MatchScore | null
  declaredParticipations?: DeclaredParticipation[]
  recordedGoals?: RecordedGoal[]
  recordedSubstitutions?: RecordedSubstitution[]
  recordedDisciplinaryEvents?: RecordedDisciplinaryEvent[]
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

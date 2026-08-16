/**
 * Manual mirrors of Application Reads DTOs.
 * ASP.NET Core JSON uses camelCase property names.
 * Phase 12.8: enums are JSON strings (enum member names), not numbers.
 * See docs/guides/http-api-contract.md.
 *
 * TypeScript types document the expected shape at compile time.
 * They do NOT validate JSON at runtime — a mismatched API still type-checks.
 */

export type CompetitionStatus =
  | 'Draft'
  | 'Ready'
  | 'Running'
  | 'Suspended'
  | 'Completed'
  | 'Archived'

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

export interface CompetitionEntrySummary {
  entryId: string
  displayName: string
  status: EntryStatus
}

export interface CompetitionStageSummary {
  stageId: string
  name: string
  status: StageStatus
}

export interface CompetitionOverview {
  id: string
  name: string
  status: CompetitionStatus
  entries: CompetitionEntrySummary[]
  stages: CompetitionStageSummary[]
}

export interface EntrySide {
  entryId: string
  displayName: string | null
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

export const competitionStatusLabel: Record<CompetitionStatus, string> = {
  Draft: 'Draft',
  Ready: 'Ready',
  Running: 'Running',
  Suspended: 'Suspended',
  Completed: 'Completed',
  Archived: 'Archived',
}

export const entryStatusLabel: Record<EntryStatus, string> = {
  Active: 'Active',
  Qualified: 'Qualified',
  Eliminated: 'Eliminated',
  Withdrawn: 'Withdrawn',
  Excluded: 'Excluded',
}

export const stageStatusLabel: Record<StageStatus, string> = {
  Draft: 'Draft',
  Ready: 'Ready',
  Running: 'Running',
  Suspended: 'Suspended',
  Completed: 'Completed',
}

export const matchStatusLabel: Record<MatchStatus, string> = {
  Scheduled: 'Scheduled',
  Live: 'Live',
  Finished: 'Finished',
  Postponed: 'Postponed',
  Cancelled: 'Cancelled',
}

export const resultTypeLabel: Record<ResultType, string> = {
  Played: 'Played',
  Forfeit: 'Forfeit',
  WalkOver: 'Walk-over',
  Administrative: 'Administrative',
}

export const drawStatusLabel: Record<DrawStatus, string> = {
  Draft: 'Draft',
  Published: 'Published',
  Cancelled: 'Cancelled',
}

export const drawResolutionKindLabel: Record<DrawResolutionKind, string> = {
  Slot: 'Slot',
  Group: 'Group',
  Pairing: 'Pairing',
}

export const drawResolutionStateLabel: Record<DrawResolutionState, string> = {
  NotResolved: 'Not resolved',
  Resolved: 'Resolved',
  NoSolution: 'No solution',
}

export const resultTypeOptions: readonly ResultType[] = [
  'Played',
  'Forfeit',
  'WalkOver',
  'Administrative',
] as const

export function sideLabel(side: EntrySide): string {
  return side.displayName?.trim() || 'Unknown entry'
}

export function formatScore(score: MatchScore): string {
  return `${score.homeGoals}–${score.awayGoals}`
}

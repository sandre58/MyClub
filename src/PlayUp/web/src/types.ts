/**
 * Manual mirrors of Application Reads DTOs (11.1).
 * ASP.NET Core JSON uses camelCase; enums are numeric unless configured otherwise.
 *
 * TypeScript types document the expected shape at compile time.
 * They do NOT validate JSON at runtime — a mismatched API still type-checks.
 */

export type CompetitionStatus = 0 | 1 | 2 | 3 | 4 | 5
export type EntryStatus = 0 | 1 | 2 | 3 | 4
export type StageStatus = 0 | 1 | 2 | 3 | 4
export type MatchStatus = 0 | 1 | 2 | 3 | 4
export type ResultType = 0 | 1 | 2 | 3
export type DrawStatus = 0 | 1 | 2
export type DrawResolutionKind = 0 | 1 | 2
export type DrawResolutionState = 0 | 1 | 2

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
  0: 'Draft',
  1: 'Ready',
  2: 'Running',
  3: 'Suspended',
  4: 'Completed',
  5: 'Archived',
}

export const entryStatusLabel: Record<EntryStatus, string> = {
  0: 'Active',
  1: 'Qualified',
  2: 'Eliminated',
  3: 'Withdrawn',
  4: 'Excluded',
}

export const stageStatusLabel: Record<StageStatus, string> = {
  0: 'Draft',
  1: 'Ready',
  2: 'Running',
  3: 'Suspended',
  4: 'Completed',
}

export const matchStatusLabel: Record<MatchStatus, string> = {
  0: 'Scheduled',
  1: 'Live',
  2: 'Finished',
  3: 'Postponed',
  4: 'Cancelled',
}

export const resultTypeLabel: Record<ResultType, string> = {
  0: 'Played',
  1: 'Forfeit',
  2: 'Walk-over',
  3: 'Administrative',
}

export const drawStatusLabel: Record<DrawStatus, string> = {
  0: 'Draft',
  1: 'Published',
  2: 'Cancelled',
}

export const drawResolutionKindLabel: Record<DrawResolutionKind, string> = {
  0: 'Slot',
  1: 'Group',
  2: 'Pairing',
}

export const drawResolutionStateLabel: Record<DrawResolutionState, string> = {
  0: 'Not resolved',
  1: 'Resolved',
  2: 'No solution',
}

export function sideLabel(side: EntrySide): string {
  return side.displayName?.trim() || 'Unknown entry'
}

export function formatScore(score: MatchScore): string {
  return `${score.homeGoals}–${score.awayGoals}`
}

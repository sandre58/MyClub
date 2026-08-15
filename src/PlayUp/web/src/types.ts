/**
 * Manual mirror of Application Reads CompetitionOverviewDto (+ nested summaries).
 * ASP.NET Core JSON uses camelCase; enums are numeric unless configured otherwise.
 */

export type CompetitionStatus = 0 | 1 | 2 | 3 | 4 | 5
export type EntryStatus = 0 | 1 | 2 | 3 | 4
export type StageStatus = 0 | 1 | 2 | 3 | 4

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

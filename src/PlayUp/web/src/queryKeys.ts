/**
 * TanStack Query keys — single source for reads and invalidations.
 * Keep arrays stable; do not invent parallel string shapes.
 */
export const queryKeys = {
  competitions: {
    all: ['competitions'] as const,
    detail: (competitionId: string) =>
      ['competitions', competitionId] as const,
    workspace: (competitionId: string) =>
      ['competitions', competitionId, 'workspace'] as const,
    cockpit: (competitionId: string) =>
      ['competitions', competitionId, 'cockpit'] as const,
    organisation: (competitionId: string) =>
      ['competitions', competitionId, 'organisation'] as const,
    attention: (competitionId: string) =>
      ['competitions', competitionId, 'attention'] as const,
    consultation: (competitionId: string) =>
      ['competitions', competitionId, 'consultation'] as const,
  },
  stages: {
    detail: (stageId: string) => ['stages', stageId] as const,
  },
  matches: {
    detail: (matchId: string) => ['matches', matchId] as const,
    byStage: (stageId: string) => ['matches', 'by-stage', stageId] as const,
  },
} as const

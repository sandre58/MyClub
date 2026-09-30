/**
 * TanStack Query keys — single source for reads and invalidations.
 * Keep arrays stable; do not invent parallel string shapes.
 */
export const queryKeys = {
  competitions: {
    all: ['competitions'] as const,
    detail: (competitionId: string) => ['competitions', competitionId] as const,
    /** Invalidate-only today — no SPA useQuery/fetch for GET workspace (Host still has the route). */
    workspace: (competitionId: string) =>
      ['competitions', competitionId, 'workspace'] as const,
    overview: (competitionId: string) =>
      ['competitions', competitionId, 'overview'] as const,
    structure: (competitionId: string) =>
      ['competitions', competitionId, 'structure'] as const,
    attention: (competitionId: string) =>
      ['competitions', competitionId, 'attention'] as const,
    consultation: (competitionId: string) =>
      ['competitions', competitionId, 'consultation'] as const,
    matchHub: (competitionId: string) =>
      ['competitions', competitionId, 'matches-hub'] as const,
  },
  stages: {
    detail: (stageId: string) => ['stages', stageId] as const,
    schematic: (stageId: string) => ['stages', stageId, 'schematic'] as const,
  },
  matches: {
    detail: (matchId: string) => ['matches', matchId] as const,
    byStage: (stageId: string) => ['matches', 'by-stage', stageId] as const,
  },
} as const;

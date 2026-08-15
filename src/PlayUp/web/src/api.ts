import type {
  CompetitionOverview,
  MatchDetail,
  MatchSummary,
  StageOverview,
} from './types'

export class ApiError extends Error {
  readonly status: number
  readonly detail?: string

  constructor(status: number, message: string, detail?: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.detail = detail
  }
}

/**
 * Shared JSON GET helper.
 * Problem: four read endpoints would otherwise copy the same !ok / ProblemDetails parsing.
 * Not a generic “API layer” — just one fetch path with typed return.
 */
async function getJson<T>(url: string): Promise<T> {
  const response = await fetch(url)

  if (!response.ok) {
    let detail: string | undefined
    try {
      const problem = (await response.json()) as {
        title?: string
        detail?: string
      }
      detail = problem.detail ?? problem.title
    } catch {
      // Non-JSON body (rare)
    }

    throw new ApiError(
      response.status,
      detail ?? `HTTP ${response.status}`,
      detail,
    )
  }

  return (await response.json()) as T
}

/** Relative URL → Vite proxy → Host GET /competitions/{id} */
export function fetchCompetitionOverview(
  competitionId: string,
): Promise<CompetitionOverview> {
  return getJson(`/competitions/${competitionId}`)
}

/** Relative URL → Vite proxy → Host GET /stages/{id} */
export function fetchStageOverview(stageId: string): Promise<StageOverview> {
  return getJson(`/stages/${stageId}`)
}

/** Relative URL → Vite proxy → Host GET /stages/{id}/matches */
export function fetchMatchesByStage(
  stageId: string,
): Promise<MatchSummary[]> {
  return getJson(`/stages/${stageId}/matches`)
}

/** Relative URL → Vite proxy → Host GET /matches/{id} */
export function fetchMatchDetail(matchId: string): Promise<MatchDetail> {
  return getJson(`/matches/${matchId}`)
}

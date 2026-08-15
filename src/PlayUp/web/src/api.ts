import type { CompetitionOverview } from './types'

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
 * Relative URL → Vite proxy → Host GET /competitions/{id}
 */
export async function fetchCompetitionOverview(
  competitionId: string,
): Promise<CompetitionOverview> {
  const response = await fetch(`/competitions/${competitionId}`)

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

  return (await response.json()) as CompetitionOverview
}

import type {
  AddEntryRequest,
  ApplyDrawRequest,
  CompetitionListItem,
  CompetitionOverview,
  ConfigureStructureRequest,
  FinishMatchRequest,
  MatchDetail,
  MatchSummary,
  NeedsAttention,
  OrganisationView,
  RenameEntryRequest,
  ReplaceRegulationRequest,
  StageOverview,
  WorkspaceSummary,
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
 * Shared failure path for GET and POST.
 * Prefer ProblemDetails.detail, then title, then a generic HTTP status message.
 */
async function throwIfNotOk(response: Response): Promise<void> {
  if (response.ok) {
    return
  }

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

/**
 * Shared JSON GET helper.
 * Problem: four read endpoints would otherwise copy the same !ok / ProblemDetails parsing.
 * Not a generic “API layer” — just one fetch path with typed return.
 */
async function getJson<T>(url: string): Promise<T> {
  const response = await fetch(url)
  await throwIfNotOk(response)
  return (await response.json()) as T
}

/**
 * POST/PUT helpers for Host commands that return a JSON body (Organisation mutations).
 */
async function sendJson<T>(
  method: 'POST' | 'PUT',
  url: string,
  body?: unknown,
): Promise<T> {
  const response = await fetch(url, {
    method,
    headers:
      body === undefined
        ? undefined
        : { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  })
  await throwIfNotOk(response)
  return (await response.json()) as T
}

/**
 * POST for Host commands that return 204 No Content.
 * Do not call response.json() — an empty body is not JSON.
 */
async function postNoContent(
  url: string,
  body?: unknown,
): Promise<void> {
  const response = await fetch(url, {
    method: 'POST',
    headers:
      body === undefined
        ? undefined
        : { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  })
  await throwIfNotOk(response)
}

/** Relative URL → Vite proxy → Host GET /competitions */
export function fetchCompetitions(): Promise<CompetitionListItem[]> {
  return getJson('/competitions')
}

/** Relative URL → Vite proxy → Host GET /competitions/{id}/workspace */
export function fetchCompetitionWorkspace(
  competitionId: string,
): Promise<WorkspaceSummary> {
  return getJson(`/competitions/${competitionId}/workspace`)
}

/** Relative URL → Vite proxy → Host GET /competitions/{id} */
export function fetchCompetitionOverview(
  competitionId: string,
): Promise<CompetitionOverview> {
  return getJson(`/competitions/${competitionId}`)
}

/** Relative URL → Vite proxy → Host GET /competitions/{id}/organisation */
export function fetchOrganisationView(
  competitionId: string,
): Promise<OrganisationView> {
  return getJson(`/competitions/${competitionId}/organisation`)
}

/** Relative URL → Vite proxy → Host GET /competitions/{id}/attention */
export function fetchNeedsAttention(
  competitionId: string,
): Promise<NeedsAttention> {
  return getJson(`/competitions/${competitionId}/attention`)
}

/** POST /competitions/{id}/entries → OrganisationView */
export function addCompetitionEntry(
  competitionId: string,
  request: AddEntryRequest,
): Promise<OrganisationView> {
  return sendJson('POST', `/competitions/${competitionId}/entries`, request)
}

/** POST .../entries/{entryId}/rename → OrganisationView */
export function renameCompetitionEntry(
  competitionId: string,
  entryId: string,
  request: RenameEntryRequest,
): Promise<OrganisationView> {
  return sendJson(
    'POST',
    `/competitions/${competitionId}/entries/${entryId}/rename`,
    request,
  )
}

/** POST .../entries/{entryId}/withdraw → OrganisationView */
export function withdrawCompetitionEntry(
  competitionId: string,
  entryId: string,
): Promise<OrganisationView> {
  return sendJson(
    'POST',
    `/competitions/${competitionId}/entries/${entryId}/withdraw`,
  )
}

/** POST .../entries/{entryId}/exclude → OrganisationView */
export function excludeCompetitionEntry(
  competitionId: string,
  entryId: string,
): Promise<OrganisationView> {
  return sendJson(
    'POST',
    `/competitions/${competitionId}/entries/${entryId}/exclude`,
  )
}

/** PUT /competitions/{id}/regulation → OrganisationView */
export function replaceCompetitionRegulation(
  competitionId: string,
  request: ReplaceRegulationRequest,
): Promise<OrganisationView> {
  return sendJson('PUT', `/competitions/${competitionId}/regulation`, request)
}

/** POST /competitions/{id}/organisation/structure → OrganisationView */
export function configureOrganisationStructure(
  competitionId: string,
  request: ConfigureStructureRequest,
): Promise<OrganisationView> {
  return sendJson(
    'POST',
    `/competitions/${competitionId}/organisation/structure`,
    request,
  )
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

/** POST /matches/{id}/start → 204 */
export function startMatch(matchId: string): Promise<void> {
  return postNoContent(`/matches/${matchId}/start`)
}

/** POST /matches/{id}/finish → 204 */
export function finishMatch(
  matchId: string,
  request: FinishMatchRequest,
): Promise<void> {
  return postNoContent(`/matches/${matchId}/finish`, request)
}

/** POST /stages/{stageId}/fixtures/{fixtureId}/apply-progression → 204 */
export function applyProgressionOutcome(
  stageId: string,
  fixtureId: string,
): Promise<void> {
  return postNoContent(
    `/stages/${stageId}/fixtures/${fixtureId}/apply-progression`,
  )
}

/** POST /stages/{stageId}/prepare → 204 (bodyless; Draft → Ready) */
export function prepareStage(stageId: string): Promise<void> {
  return postNoContent(`/stages/${stageId}/prepare`)
}

/** POST /stages/{stageId}/start → 204 (bodyless; Ready → Running) */
export function startStage(stageId: string): Promise<void> {
  return postNoContent(`/stages/${stageId}/start`)
}

/** POST /stages/{stageId}/draws/{drawId}/publish → 204 */
export function publishDraw(stageId: string, drawId: string): Promise<void> {
  return postNoContent(`/stages/${stageId}/draws/${drawId}/publish`)
}

/** POST /stages/{stageId}/draws/{drawId}/apply → 204 */
export function applyDraw(
  stageId: string,
  drawId: string,
  request: ApplyDrawRequest,
): Promise<void> {
  return postNoContent(`/stages/${stageId}/draws/${drawId}/apply`, request)
}

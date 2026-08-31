import type {
  AddDeclaredMemberRequest,
  AddEntryRequest,
  ApplyDrawRequest,
  CockpitView,
  CompetitionListItem,
  CompetitionOverview,
  ConfigureStructureRequest,
  ConsultationView,
  CreateCompetitionRequest,
  FinishMatchRequest,
  MatchDetail,
  MatchScore,
  MatchSummary,
  NeedsAttention,
  OrganisationView,
  RenameDeclaredMemberRequest,
  RenameEntryRequest,
  ReplaceRegulationRequest,
  SetCompetitionScheduleRequest,
  StageOverview,
  UpdateCompetitionPresentationRequest,
  UpdateEntryPresentationRequest,
  WorkspaceSummary,
} from './types'

export class ApiError extends Error {
  readonly status: number
  readonly detail?: string
  /** ProblemDetails extensions.code when present. */
  readonly code?: string

  constructor(status: number, message: string, detail?: string, code?: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.detail = detail
    this.code = code
  }
}

/**
 * Shared failure path for GET and POST.
 * Prefer ProblemDetails.extensions.code for SPA i18n; keep detail as diagnostic fallback.
 */
async function throwIfNotOk(response: Response): Promise<void> {
  if (response.ok) {
    return
  }

  let detail: string | undefined
  let code: string | undefined
  try {
    const problem = (await response.json()) as {
      title?: string
      detail?: string
      code?: string
    }
    detail = problem.detail ?? problem.title
    code = typeof problem.code === 'string' ? problem.code : undefined
  } catch {
    // Non-JSON body (rare)
  }

  throw new ApiError(
    response.status,
    detail ?? `HTTP ${response.status}`,
    detail,
    code,
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
  method: 'POST' | 'PUT' | 'DELETE',
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
 * POST/PUT for Host commands that return 204 No Content.
 * Do not call response.json() — an empty body is not JSON.
 */
async function sendNoContent(
  method: 'POST' | 'PUT',
  url: string,
  body?: unknown,
): Promise<void> {
  const response = await fetch(url, {
    method,
    headers:
      body === undefined
        ? undefined
        : { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  })
  await throwIfNotOk(response)
}

async function postNoContent(
  url: string,
  body?: unknown,
): Promise<void> {
  return sendNoContent('POST', url, body)
}

/** Relative URL → Vite proxy → Host GET /competitions */
export function fetchCompetitions(): Promise<CompetitionListItem[]> {
  return getJson('/competitions')
}

/**
 * POST /competitions → WorkspaceSummary (201).
 * Host Location points at legacy /workspace; SPA navigates to Organisation.
 */
export function createCompetition(
  request: CreateCompetitionRequest,
): Promise<WorkspaceSummary> {
  return sendJson('POST', '/competitions', request)
}

/** Relative URL → Vite proxy → Host GET /competitions/{id}/workspace */
export function fetchCompetitionWorkspace(
  competitionId: string,
): Promise<WorkspaceSummary> {
  return getJson(`/competitions/${competitionId}/workspace`)
}

/** Relative URL → Vite proxy → Host GET /competitions/{id}/cockpit */
export function fetchCompetitionCockpit(
  competitionId: string,
): Promise<CockpitView> {
  return getJson(`/competitions/${competitionId}/cockpit`)
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

/** Relative URL → Vite proxy → Host GET /competitions/{id}/consultation */
export function fetchConsultation(
  competitionId: string,
): Promise<ConsultationView> {
  return getJson(`/competitions/${competitionId}/consultation`)
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

/** POST /media — multipart file upload → Media metadata. */
export async function uploadMedia(file: File): Promise<{ id: string }> {
  const form = new FormData()
  form.append('file', file)
  const response = await fetch('/media', { method: 'POST', body: form })
  await throwIfNotOk(response)
  return (await response.json()) as { id: string }
}

/** POST /competitions/{id}/presentation → OrganisationView */
export function updateCompetitionPresentation(
  competitionId: string,
  request: UpdateCompetitionPresentationRequest,
): Promise<OrganisationView> {
  return sendJson('POST', `/competitions/${competitionId}/presentation`, request)
}

/** POST /competitions/{id}/schedule → OrganisationView */
export function setCompetitionSchedule(
  competitionId: string,
  request: SetCompetitionScheduleRequest,
): Promise<OrganisationView> {
  return sendJson('POST', `/competitions/${competitionId}/schedule`, request)
}

/** POST .../entries/{entryId}/presentation → OrganisationView */
export function updateEntryPresentation(
  competitionId: string,
  entryId: string,
  request: UpdateEntryPresentationRequest,
): Promise<OrganisationView> {
  return sendJson(
    'POST',
    `/competitions/${competitionId}/entries/${entryId}/presentation`,
    request,
  )
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

/** POST .../entries/{entryId}/declared-members → OrganisationView */
export function addDeclaredMember(
  competitionId: string,
  entryId: string,
  request: AddDeclaredMemberRequest,
): Promise<OrganisationView> {
  return sendJson(
    'POST',
    `/competitions/${competitionId}/entries/${entryId}/declared-members`,
    request,
  )
}

/** DELETE .../declared-members/{memberId} → OrganisationView */
export function removeDeclaredMember(
  competitionId: string,
  entryId: string,
  memberId: string,
): Promise<OrganisationView> {
  return sendJson(
    'DELETE',
    `/competitions/${competitionId}/entries/${entryId}/declared-members/${memberId}`,
  )
}

/** POST .../declared-members/{memberId}/rename → OrganisationView */
export function renameDeclaredMember(
  competitionId: string,
  entryId: string,
  memberId: string,
  request: RenameDeclaredMemberRequest,
): Promise<OrganisationView> {
  return sendJson(
    'POST',
    `/competitions/${competitionId}/entries/${entryId}/declared-members/${memberId}/rename`,
    request,
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

/** PUT /matches/{id}/running-score → 204 (Live only; absolute). */
export function setRunningScore(
  matchId: string,
  score: MatchScore,
): Promise<void> {
  return sendNoContent('PUT', `/matches/${matchId}/running-score`, {
    homeGoals: score.homeGoals,
    awayGoals: score.awayGoals,
  })
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

/** POST /stages/{stageId}/matches/materialize → MaterializeMatchesResponse */
export function materializeMatches(
  stageId: string,
): Promise<{ createdCount: number; attachedMatchIds: string[]; alreadyComplete: boolean }> {
  return sendJson('POST', `/stages/${stageId}/matches/materialize`)
}

/** POST /stages/{stageId}/swiss/generate-next-round → GenerateNextRoundResponse */
export function generateNextSwissRound(stageId: string): Promise<{
  roundIndex: number
  createdCount: number
  attachedMatchIds: string[]
  byeEntryId: string | null
  alreadyComplete: boolean
}> {
  return sendJson('POST', `/stages/${stageId}/swiss/generate-next-round`)
}

/** POST /stages/{stageId}/matches/materialize-from-slots → MaterializeMatchesResponse */
export function materializeCupFromOccupiedSlots(
  stageId: string,
  pairs: { slotAKey: string; slotBKey: string }[],
): Promise<{ createdCount: number; attachedMatchIds: string[]; alreadyComplete: boolean }> {
  return sendJson('POST', `/stages/${stageId}/matches/materialize-from-slots`, {
    pairs,
  })
}

/** POST /stages/{stageId}/qualification/apply → QualificationApplyResponse */
export function applyQualification(
  stageId: string,
): Promise<{ appliedCount: number; assignments: unknown[] }> {
  return sendJson('POST', `/stages/${stageId}/qualification/apply`)
}

/** POST /competitions/{id}/prepare → 204 (bodyless; Draft → Ready) */
export function prepareCompetition(competitionId: string): Promise<void> {
  return postNoContent(`/competitions/${competitionId}/prepare`)
}

/** POST /competitions/{id}/start → 204 (bodyless; Ready → Running) */
export function startCompetition(competitionId: string): Promise<void> {
  return postNoContent(`/competitions/${competitionId}/start`)
}

/** POST /competitions/{id}/complete → 204 */
export function completeCompetition(
  competitionId: string,
  mode: 'Normal' | 'Administrative' | 'Abandoned' = 'Normal',
): Promise<void> {
  return postNoContent(`/competitions/${competitionId}/complete`, { mode })
}

/** POST /competitions/{id}/archive → 204 */
export function archiveCompetition(competitionId: string): Promise<void> {
  return postNoContent(`/competitions/${competitionId}/archive`)
}

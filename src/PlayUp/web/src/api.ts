import type {
  AddDeclaredMemberRequest,
  AddDeclaredParticipationRequest,
  AddEntryRequest,
  AddCompetitionStageResponse,
  EntryIdsRequest,
  MemberIdsRequest,
  ApplyDrawRequest,
  OverviewView,
  CompetitionListItem,
  CompetitionDetail,
  CompositionStatus,
  ConfigureStructureRequest,
  ConfigureStructureResponse,
  ConsultationView,
  CreateCompetitionRequest,
  FinishMatchRequest,
  MatchDetail,
  MatchHubView,
  MatchScore,
  MatchSummary,
  MatchGenerationFormat,
  NeedsAttention,
  StructureView,
  RecordDisciplinaryEventRequest,
  RecordGoalRequest,
  RecordSubstitutionRequest,
  RemoveCompetitionStageResponse,
  RenameDeclaredMemberRequest,
  RenameEntryRequest,
  ReplaceProgressionRulesRequest,
  ReplaceQualificationRulesRequest,
  ReplaceRegulationRequest,
  ReplaceStageDefaultTieFormatRequest,
  ReplaceStageDrawRulesRequest,
  ReplaceStageMatchRulesRequest,
  ReplaceStageStandingRulesRequest,
  BindStageRegulationRequest,
  SetCompetitionScheduleRequest,
  StageOverview,
  UpdateCompetitionPresentationRequest,
  UpdateEntryPresentationRequest,
  WorkspaceSummary,
} from './types';

export class ApiError extends Error {
  readonly status: number;
  readonly detail?: string;
  /** ProblemDetails extensions.code when present. */
  readonly code?: string;

  constructor(status: number, message: string, detail?: string, code?: string) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.detail = detail;
    this.code = code;
  }
}

/**
 * Shared failure path for GET and POST.
 * Prefer ProblemDetails.extensions.code for SPA i18n; keep detail as diagnostic fallback.
 */
async function throwIfNotOk(response: Response): Promise<void> {
  if (response.ok) {
    return;
  }

  let detail: string | undefined;
  let code: string | undefined;
  try {
    const problem = (await response.json()) as {
      title?: string;
      detail?: string;
      code?: string;
    };
    detail = problem.detail ?? problem.title;
    code = typeof problem.code === 'string' ? problem.code : undefined;
  } catch {
    // Non-JSON body (rare)
  }

  throw new ApiError(
    response.status,
    detail ?? `HTTP ${response.status}`,
    detail,
    code,
  );
}

/**
 * Shared JSON GET helper.
 * Problem: four read endpoints would otherwise copy the same !ok / ProblemDetails parsing.
 * Not a generic “API layer” — just one fetch path with typed return.
 */
async function getJson<T>(url: string): Promise<T> {
  const response = await fetch(url);
  await throwIfNotOk(response);
  return (await response.json()) as T;
}

/**
 * POST/PUT helpers for Host commands that return a JSON body (Structure mutations).
 */
async function sendJson<T>(
  method: 'POST' | 'PUT' | 'DELETE',
  url: string,
  body?: unknown,
): Promise<T> {
  const response = await fetch(url, {
    method,
    headers:
      body === undefined ? undefined : { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  await throwIfNotOk(response);
  return (await response.json()) as T;
}

/**
 * POST/PUT for Host commands that return 204 No Content.
 * Do not call response.json() — an empty body is not JSON.
 */
async function sendNoContent(
  method: 'POST' | 'PUT' | 'DELETE',
  url: string,
  body?: unknown,
): Promise<void> {
  const response = await fetch(url, {
    method,
    headers:
      body === undefined ? undefined : { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  await throwIfNotOk(response);
}

async function postNoContent(url: string, body?: unknown): Promise<void> {
  return sendNoContent('POST', url, body);
}

/** Relative URL → Vite proxy → Host GET /competitions */
export function fetchCompetitions(): Promise<CompetitionListItem[]> {
  return getJson('/competitions');
}

/**
 * POST /competitions → WorkspaceSummary (201).
 * Host Location points at /workspace; SPA navigates to Structure.
 */
export function createCompetition(
  request: CreateCompetitionRequest,
): Promise<WorkspaceSummary> {
  return sendJson('POST', '/competitions', request);
}

/** Relative URL → Vite proxy → Host GET /competitions/{id}/overview */
export function fetchCompetitionOverview(
  competitionId: string,
): Promise<OverviewView> {
  return getJson(`/competitions/${competitionId}/overview`);
}

/** Relative URL → Vite proxy → Host GET /competitions/{id} */
export function fetchCompetitionDetail(
  competitionId: string,
): Promise<CompetitionDetail> {
  return getJson(`/competitions/${competitionId}`);
}

/** Relative URL → Vite proxy → Host GET /competitions/{id}/structure */
export function fetchStructureView(
  competitionId: string,
): Promise<StructureView> {
  return getJson(`/competitions/${competitionId}/structure`);
}

/** Relative URL → Vite proxy → Host GET /competitions/{id}/consultation */
export function fetchConsultation(
  competitionId: string,
): Promise<ConsultationView> {
  return getJson(`/competitions/${competitionId}/consultation`);
}

/** Relative URL → Vite proxy → Host GET /competitions/{id}/attention */
export function fetchNeedsAttention(
  competitionId: string,
): Promise<NeedsAttention> {
  return getJson(`/competitions/${competitionId}/attention`);
}

/** POST /competitions/{id}/entries → StructureView */
export function addCompetitionEntry(
  competitionId: string,
  request: AddEntryRequest,
): Promise<StructureView> {
  return sendJson('POST', `/competitions/${competitionId}/entries`, request);
}

/** POST /media — multipart file upload → Media metadata. */
export async function uploadMedia(file: File): Promise<{ id: string }> {
  const form = new FormData();
  form.append('file', file);
  const response = await fetch('/media', { method: 'POST', body: form });
  await throwIfNotOk(response);
  return (await response.json()) as { id: string };
}

/** POST /competitions/{id}/presentation → StructureView */
export function updateCompetitionPresentation(
  competitionId: string,
  request: UpdateCompetitionPresentationRequest,
): Promise<StructureView> {
  return sendJson(
    'POST',
    `/competitions/${competitionId}/presentation`,
    request,
  );
}

/** POST /competitions/{id}/schedule → StructureView */
export function setCompetitionSchedule(
  competitionId: string,
  request: SetCompetitionScheduleRequest,
): Promise<StructureView> {
  return sendJson('POST', `/competitions/${competitionId}/schedule`, request);
}

/** POST .../entries/{entryId}/presentation → StructureView */
export function updateEntryPresentation(
  competitionId: string,
  entryId: string,
  request: UpdateEntryPresentationRequest,
): Promise<StructureView> {
  return sendJson(
    'POST',
    `/competitions/${competitionId}/entries/${entryId}/presentation`,
    request,
  );
}

/** POST .../entries/{entryId}/rename → StructureView */
export function renameCompetitionEntry(
  competitionId: string,
  entryId: string,
  request: RenameEntryRequest,
): Promise<StructureView> {
  return sendJson(
    'POST',
    `/competitions/${competitionId}/entries/${entryId}/rename`,
    request,
  );
}

/** POST .../entries/{entryId}/withdraw → StructureView */
export function withdrawCompetitionEntry(
  competitionId: string,
  entryId: string,
): Promise<StructureView> {
  return sendJson(
    'POST',
    `/competitions/${competitionId}/entries/${entryId}/withdraw`,
  );
}

/** POST .../entries/{entryId}/declared-members → StructureView */
export function addDeclaredMember(
  competitionId: string,
  entryId: string,
  request: AddDeclaredMemberRequest,
): Promise<StructureView> {
  return sendJson(
    'POST',
    `/competitions/${competitionId}/entries/${entryId}/declared-members`,
    request,
  );
}

/** DELETE .../declared-members/{memberId} → StructureView */
export function removeDeclaredMember(
  competitionId: string,
  entryId: string,
  memberId: string,
): Promise<StructureView> {
  return sendJson(
    'DELETE',
    `/competitions/${competitionId}/entries/${entryId}/declared-members/${memberId}`,
  );
}

/** POST .../declared-member-lots/remove → StructureView */
export function removeDeclaredMembers(
  competitionId: string,
  entryId: string,
  request: MemberIdsRequest,
): Promise<StructureView> {
  return sendJson(
    'POST',
    `/competitions/${competitionId}/entries/${entryId}/declared-member-lots/remove`,
    request,
  );
}

/** POST .../declared-members/{memberId}/rename → StructureView */
export function renameDeclaredMember(
  competitionId: string,
  entryId: string,
  memberId: string,
  request: RenameDeclaredMemberRequest,
): Promise<StructureView> {
  return sendJson(
    'POST',
    `/competitions/${competitionId}/entries/${entryId}/declared-members/${memberId}/rename`,
    request,
  );
}

/** POST .../entries/{entryId}/delete → StructureView */
export function deleteCompetitionEntry(
  competitionId: string,
  entryId: string,
): Promise<StructureView> {
  return sendJson(
    'POST',
    `/competitions/${competitionId}/entries/${entryId}/delete`,
  );
}

/** POST .../entry-lots/delete → StructureView */
export function deleteCompetitionEntries(
  competitionId: string,
  request: EntryIdsRequest,
): Promise<StructureView> {
  return sendJson(
    'POST',
    `/competitions/${competitionId}/entry-lots/delete`,
    request,
  );
}

/** POST .../entry-lots/withdraw → StructureView */
export function withdrawCompetitionEntries(
  competitionId: string,
  request: EntryIdsRequest,
): Promise<StructureView> {
  return sendJson(
    'POST',
    `/competitions/${competitionId}/entry-lots/withdraw`,
    request,
  );
}

/** PUT /competitions/{id}/regulation → StructureView */
export function replaceCompetitionRegulation(
  competitionId: string,
  request: ReplaceRegulationRequest,
): Promise<StructureView> {
  return sendJson('PUT', `/competitions/${competitionId}/regulation`, request);
}

/** POST /competitions/{id}/structure → ConfigureStructureResponse */
export function configureStructure(
  competitionId: string,
  request: ConfigureStructureRequest,
): Promise<ConfigureStructureResponse> {
  return sendJson(
    'POST',
    `/competitions/${competitionId}/structure`,
    request,
  );
}

/** POST /stages/{id}/rename → 204 */
export function renameStage(stageId: string, name: string): Promise<void> {
  return sendNoContent('POST', `/stages/${stageId}/rename`, { name });
}

/** POST /stages/{id}/matchdays → AddStageMatchdayResponse */
export function addStageMatchday(
  stageId: string,
  number?: number | null,
): Promise<{ matchdayId: string; number: number }> {
  return sendJson('POST', `/stages/${stageId}/matchdays`, { number: number ?? null });
}

/** POST /stages/{id}/groups → AddStageGroupResponse */
export function addStageGroup(
  stageId: string,
  name?: string | null,
): Promise<{ groupId: string; name: string }> {
  return sendJson('POST', `/stages/${stageId}/groups`, { name: name ?? null });
}

/** PUT /stages/{id}/match-generation-format → 204 */
export function replaceStageMatchGenerationFormat(
  stageId: string,
  format: MatchGenerationFormat,
): Promise<void> {
  return sendNoContent('PUT', `/stages/${stageId}/match-generation-format`, {
    format,
  });
}

/** PUT /stages/{id}/swiss-settings → 204 */
export function replaceStageSwissSettings(
  stageId: string,
  roundCount: number,
): Promise<void> {
  return sendNoContent('PUT', `/stages/${stageId}/swiss-settings`, {
    roundCount,
  });
}

/** POST /stages/{id}/rounds → AddStageRoundResponse */
export function addStageRound(
  stageId: string,
  name: string,
  numberOfLegs?: number | null,
  aggregateScoring?: boolean | null,
): Promise<{ roundId: string; name: string }> {
  return sendJson('POST', `/stages/${stageId}/rounds`, {
    name,
    numberOfLegs: numberOfLegs ?? null,
    aggregateScoring: aggregateScoring ?? null,
  });
}

/** POST /stages/{id}/slots → AddStageSlotResponse */
export function addStageSlot(
  stageId: string,
  slotKey: string,
): Promise<{ slotKey: string }> {
  return sendJson('POST', `/stages/${stageId}/slots`, { slotKey });
}

/** POST /competitions/{id}/stages → AddCompetitionStageResponse */
export function addCompetitionStage(
  competitionId: string,
  name: string,
): Promise<AddCompetitionStageResponse> {
  return sendJson('POST', `/competitions/${competitionId}/stages`, { name });
}

/** DELETE /competitions/{id}/stages/{stageId} → RemoveCompetitionStageResponse */
export function removeCompetitionStage(
  competitionId: string,
  stageId: string,
): Promise<RemoveCompetitionStageResponse> {
  return sendJson(
    'DELETE',
    `/competitions/${competitionId}/stages/${stageId}`,
  );
}

/** PUT /stages/{id}/qualification-rules → 204 */
export function replaceStageQualificationRules(
  stageId: string,
  request: ReplaceQualificationRulesRequest,
): Promise<void> {
  return sendNoContent('PUT', `/stages/${stageId}/qualification-rules`, request);
}

/** PUT /stages/{id}/progression-rules → 204 */
export function replaceStageProgressionRules(
  stageId: string,
  request: ReplaceProgressionRulesRequest,
): Promise<void> {
  return sendNoContent('PUT', `/stages/${stageId}/progression-rules`, request);
}

/** PUT /stages/{id}/match-rules → 204 */
export function replaceStageMatchRules(
  stageId: string,
  request: ReplaceStageMatchRulesRequest,
): Promise<void> {
  return sendNoContent('PUT', `/stages/${stageId}/match-rules`, request);
}

/** PUT /stages/{id}/standing-rules → 204 */
export function replaceStageStandingRules(
  stageId: string,
  request: ReplaceStageStandingRulesRequest,
): Promise<void> {
  return sendNoContent('PUT', `/stages/${stageId}/standing-rules`, request);
}

/** POST /stages/{id}/bind-to-competition → 204 */
export function bindStageRegulation(
  stageId: string,
  request: BindStageRegulationRequest,
): Promise<void> {
  return postNoContent(`/stages/${stageId}/bind-to-competition`, request);
}

/** PUT /stages/{id}/draw-rules → 204 */
export function replaceStageDrawRules(
  stageId: string,
  request: ReplaceStageDrawRulesRequest,
): Promise<void> {
  return sendNoContent('PUT', `/stages/${stageId}/draw-rules`, request);
}

/** PUT /stages/{id}/tie-format → 204 */
export function replaceStageDefaultTieFormat(
  stageId: string,
  request: ReplaceStageDefaultTieFormatRequest,
): Promise<void> {
  return sendNoContent('PUT', `/stages/${stageId}/tie-format`, request);
}

/** Relative URL → Vite proxy → Host GET /stages/{id} */
export function fetchStageOverview(stageId: string): Promise<StageOverview> {
  return getJson(`/stages/${stageId}`);
}

/** Relative URL → Vite proxy → Host GET /competitions/{id}/matches-hub */
export function fetchMatchHub(competitionId: string): Promise<MatchHubView> {
  return getJson(`/competitions/${competitionId}/matches-hub`);
}

/** Relative URL → Vite proxy → Host GET /stages/{id}/matches */
export function fetchMatchesByStage(stageId: string): Promise<MatchSummary[]> {
  return getJson(`/stages/${stageId}/matches`);
}

/** Relative URL → Vite proxy → Host GET /matches/{id} */
export function fetchMatchDetail(matchId: string): Promise<MatchDetail> {
  return getJson(`/matches/${matchId}`);
}

/** POST /matches/{id}/start → 204 */
export function startMatch(matchId: string): Promise<void> {
  return postNoContent(`/matches/${matchId}/start`);
}

/** POST /matches/{id}/finish → 204 */
export function finishMatch(
  matchId: string,
  request: FinishMatchRequest,
): Promise<void> {
  return postNoContent(`/matches/${matchId}/finish`, request);
}

/** PUT /matches/{id}/running-score → 204 (Live only; absolute). */
export function setRunningScore(
  matchId: string,
  score: MatchScore,
): Promise<void> {
  return sendNoContent('PUT', `/matches/${matchId}/running-score`, {
    homeGoals: score.homeGoals,
    awayGoals: score.awayGoals,
  });
}

/** POST /matches/{id}/declared-participations → 204 */
export function addDeclaredParticipation(
  matchId: string,
  request: AddDeclaredParticipationRequest,
): Promise<void> {
  return postNoContent(`/matches/${matchId}/declared-participations`, request);
}

/** DELETE /matches/{id}/declared-participations/{memberId} → 204 */
export function removeDeclaredParticipation(
  matchId: string,
  memberId: string,
): Promise<void> {
  return sendNoContent(
    'DELETE',
    `/matches/${matchId}/declared-participations/${memberId}`,
  );
}

/** PUT .../declared-participations/{memberId}/composition-status → 204 */
export function changeDeclaredParticipationCompositionStatus(
  matchId: string,
  memberId: string,
  compositionStatus: CompositionStatus,
): Promise<void> {
  return sendNoContent(
    'PUT',
    `/matches/${matchId}/declared-participations/${memberId}/composition-status`,
    { compositionStatus },
  );
}

/** PUT .../declared-participations/{memberId}/jersey-number → 204 */
export function setDeclaredParticipationJerseyNumber(
  matchId: string,
  memberId: string,
  jerseyNumber: number | null,
): Promise<void> {
  return sendNoContent(
    'PUT',
    `/matches/${matchId}/declared-participations/${memberId}/jersey-number`,
    { jerseyNumber },
  );
}

/** POST /matches/{id}/recorded-goals → 204 (faits-only; Live RS is a separate call). */
export function recordGoal(
  matchId: string,
  request: RecordGoalRequest,
): Promise<void> {
  return postNoContent(`/matches/${matchId}/recorded-goals`, request);
}

/** PUT /matches/{id}/recorded-goals/{goalId} → 204 */
export function correctRecordedGoal(
  matchId: string,
  goalId: string,
  request: RecordGoalRequest,
): Promise<void> {
  return sendNoContent(
    'PUT',
    `/matches/${matchId}/recorded-goals/${goalId}`,
    request,
  );
}

/** DELETE /matches/{id}/recorded-goals/{goalId} → 204 */
export function removeRecordedGoal(
  matchId: string,
  goalId: string,
): Promise<void> {
  return sendNoContent(
    'DELETE',
    `/matches/${matchId}/recorded-goals/${goalId}`,
  );
}

/** POST /matches/{id}/recorded-substitutions → 204 */
export function recordSubstitution(
  matchId: string,
  request: RecordSubstitutionRequest,
): Promise<void> {
  return postNoContent(`/matches/${matchId}/recorded-substitutions`, request);
}

/** PUT /matches/{id}/recorded-substitutions/{substitutionId} → 204 */
export function correctRecordedSubstitution(
  matchId: string,
  substitutionId: string,
  request: RecordSubstitutionRequest,
): Promise<void> {
  return sendNoContent(
    'PUT',
    `/matches/${matchId}/recorded-substitutions/${substitutionId}`,
    request,
  );
}

/** DELETE /matches/{id}/recorded-substitutions/{substitutionId} → 204 */
export function removeRecordedSubstitution(
  matchId: string,
  substitutionId: string,
): Promise<void> {
  return sendNoContent(
    'DELETE',
    `/matches/${matchId}/recorded-substitutions/${substitutionId}`,
  );
}

/** POST /matches/{id}/recorded-disciplinary-events → 204 */
export function recordDisciplinaryEvent(
  matchId: string,
  request: RecordDisciplinaryEventRequest,
): Promise<void> {
  return postNoContent(
    `/matches/${matchId}/recorded-disciplinary-events`,
    request,
  );
}

/** PUT /matches/{id}/recorded-disciplinary-events/{disciplinaryEventId} → 204 */
export function correctRecordedDisciplinaryEvent(
  matchId: string,
  disciplinaryEventId: string,
  request: RecordDisciplinaryEventRequest,
): Promise<void> {
  return sendNoContent(
    'PUT',
    `/matches/${matchId}/recorded-disciplinary-events/${disciplinaryEventId}`,
    request,
  );
}

/** DELETE /matches/{id}/recorded-disciplinary-events/{disciplinaryEventId} → 204 */
export function removeRecordedDisciplinaryEvent(
  matchId: string,
  disciplinaryEventId: string,
): Promise<void> {
  return sendNoContent(
    'DELETE',
    `/matches/${matchId}/recorded-disciplinary-events/${disciplinaryEventId}`,
  );
}

/** POST /stages/{stageId}/fixtures/{fixtureId}/apply-progression → 204 */
export function applyProgressionOutcome(
  stageId: string,
  fixtureId: string,
): Promise<void> {
  return postNoContent(
    `/stages/${stageId}/fixtures/${fixtureId}/apply-progression`,
  );
}

/** POST /stages/{stageId}/prepare → 204 (bodyless; Draft → Ready) */
export function prepareStage(stageId: string): Promise<void> {
  return postNoContent(`/stages/${stageId}/prepare`);
}

/** POST /stages/{stageId}/start → 204 (bodyless; Ready → Running) */
export function startStage(stageId: string): Promise<void> {
  return postNoContent(`/stages/${stageId}/start`);
}

/** POST /stages/{stageId}/draws/{drawId}/publish → 204 */
export function publishDraw(stageId: string, drawId: string): Promise<void> {
  return postNoContent(`/stages/${stageId}/draws/${drawId}/publish`);
}

/** POST /stages/{stageId}/draws/{drawId}/apply → 204 */
export function applyDraw(
  stageId: string,
  drawId: string,
  request: ApplyDrawRequest,
): Promise<void> {
  return postNoContent(`/stages/${stageId}/draws/${drawId}/apply`, request);
}

/** POST /stages/{stageId}/matches/materialize → MaterializeMatchesResponse */
export function materializeMatches(stageId: string): Promise<{
  createdCount: number;
  attachedMatchIds: string[];
  alreadyComplete: boolean;
}> {
  return sendJson('POST', `/stages/${stageId}/matches/materialize`);
}

/** POST /stages/{stageId}/swiss/generate-next-round → GenerateNextRoundResponse */
export function generateNextSwissRound(stageId: string): Promise<{
  roundIndex: number;
  createdCount: number;
  attachedMatchIds: string[];
  byeEntryId: string | null;
  alreadyComplete: boolean;
}> {
  return sendJson('POST', `/stages/${stageId}/swiss/generate-next-round`);
}

/** POST /stages/{stageId}/matches/materialize-from-slots → MaterializeMatchesResponse */
export function materializeCupFromOccupiedSlots(
  stageId: string,
  pairs: { slotAKey: string; slotBKey: string }[],
): Promise<{
  createdCount: number;
  attachedMatchIds: string[];
  alreadyComplete: boolean;
}> {
  return sendJson('POST', `/stages/${stageId}/matches/materialize-from-slots`, {
    pairs,
  });
}

/** POST /stages/{stageId}/qualification/apply → QualificationApplyResponse */
export function applyQualification(
  stageId: string,
): Promise<{ appliedCount: number; assignments: unknown[] }> {
  return sendJson('POST', `/stages/${stageId}/qualification/apply`);
}

/** POST /competitions/{id}/prepare → 204 (bodyless; Draft → Ready) */
export function prepareCompetition(competitionId: string): Promise<void> {
  return postNoContent(`/competitions/${competitionId}/prepare`);
}

/** POST /competitions/{id}/start → 204 (bodyless; Ready → Running) */
export function startCompetition(competitionId: string): Promise<void> {
  return postNoContent(`/competitions/${competitionId}/start`);
}

/** POST /competitions/{id}/complete → 204 */
export function completeCompetition(
  competitionId: string,
  mode: 'Normal' | 'Administrative' | 'Abandoned' = 'Normal',
): Promise<void> {
  return postNoContent(`/competitions/${competitionId}/complete`, { mode });
}

/** POST /competitions/{id}/archive → 204 */
export function archiveCompetition(competitionId: string): Promise<void> {
  return postNoContent(`/competitions/${competitionId}/archive`);
}

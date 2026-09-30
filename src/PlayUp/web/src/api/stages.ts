/**
 * Stage reads and Structure / regulation / lifecycle mutations on a stage.
 */
import type {
  BindStageRegulationRequest,
  RebuildStageStructureRequest,
  RebuildStageStructureResponse,
  ReplacePlacementAwardRulesRequest,
  ReplaceProgressionRulesRequest,
  ReplaceQualificationRulesRequest,
  ReplaceRoundTieFormatRequest,
  ReplaceStageDefaultTieFormatRequest,
  ReplaceStageDrawRulesRequest,
  ReplaceStageMatchRulesRequest,
  ReplaceStageStandingRulesRequest,
  StageOverview,
  StageSchematic,
} from '../types';
import { getJson, postNoContent, sendJson, sendNoContent } from './http';

/** POST /stages/{id}/rename → 204 */
export function renameStage(stageId: string, name: string): Promise<void> {
  return sendNoContent('POST', `/stages/${stageId}/rename`, { name });
}

/** PUT /stages/{id}/structure → RebuildStageStructureResponse */
export function rebuildStageStructure(
  stageId: string,
  request: RebuildStageStructureRequest,
): Promise<RebuildStageStructureResponse> {
  return sendJson('PUT', `/stages/${stageId}/structure`, request);
}

/** PUT /stages/{id}/qualification-rules → 204 */
export function replaceStageQualificationRules(
  stageId: string,
  request: ReplaceQualificationRulesRequest,
): Promise<void> {
  return sendNoContent(
    'PUT',
    `/stages/${stageId}/qualification-rules`,
    request,
  );
}

/** PUT /stages/{id}/progression-rules → 204 */
export function replaceStageProgressionRules(
  stageId: string,
  request: ReplaceProgressionRulesRequest,
): Promise<void> {
  return sendNoContent('PUT', `/stages/${stageId}/progression-rules`, request);
}

/** PUT /stages/{id}/placement-award-rules → 204 */
export function replaceStagePlacementAwardRules(
  stageId: string,
  request: ReplacePlacementAwardRulesRequest,
): Promise<void> {
  return sendNoContent(
    'PUT',
    `/stages/${stageId}/placement-award-rules`,
    request,
  );
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

/** PUT /stages/{id}/affectation → 204 — replace Affectation authoring set */
export function replaceStageAffectationAuthoring(
  stageId: string,
  entryIds: string[],
): Promise<void> {
  return sendNoContent('PUT', `/stages/${stageId}/affectation`, { entryIds });
}

/** PUT /stages/{id}/slots/{slotKey}/assignment → 204 — manual Cup placement */
export function assignEntryToSlot(
  stageId: string,
  slotKey: string,
  entryId: string,
): Promise<void> {
  return sendNoContent(
    'PUT',
    `/stages/${stageId}/slots/${encodeURIComponent(slotKey)}/assignment`,
    { entryId },
  );
}

/** DELETE /stages/{id}/slots/{slotKey}/assignment → 204 */
export function clearSlotAssignment(
  stageId: string,
  slotKey: string,
): Promise<void> {
  return sendNoContent(
    'DELETE',
    `/stages/${stageId}/slots/${encodeURIComponent(slotKey)}/assignment`,
  );
}

/** PUT /stages/{id}/tie-format → 204 */
export function replaceStageDefaultTieFormat(
  stageId: string,
  request: ReplaceStageDefaultTieFormatRequest,
): Promise<void> {
  return sendNoContent('PUT', `/stages/${stageId}/tie-format`, request);
}

/** PUT /stages/{id}/rounds/{roundId}/tie-format → 204 */
export function replaceRoundTieFormat(
  stageId: string,
  roundId: string,
  request: ReplaceRoundTieFormatRequest,
): Promise<void> {
  return sendNoContent(
    'PUT',
    `/stages/${stageId}/rounds/${roundId}/tie-format`,
    request,
  );
}

/** Relative URL → Vite proxy → Host GET /stages/{id} */
export function fetchStageOverview(stageId: string): Promise<StageOverview> {
  return getJson(`/stages/${stageId}`);
}

/** Relative URL → Vite proxy → Host GET /stages/{id}/schematic */
export function fetchStageSchematic(stageId: string): Promise<StageSchematic> {
  return getJson(`/stages/${stageId}/schematic`);
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
  pairKeys?: string[],
): Promise<{
  createdCount: number;
  attachedMatchIds: string[];
  alreadyComplete: boolean;
}> {
  return sendJson('POST', `/stages/${stageId}/matches/materialize-from-slots`, {
    pairKeys: pairKeys ?? [],
  });
}

/** POST /stages/{stageId}/qualification/apply → QualificationApplyResponse */
export function applyQualification(
  stageId: string,
): Promise<{ appliedCount: number; assignments: unknown[] }> {
  return sendJson('POST', `/stages/${stageId}/qualification/apply`);
}

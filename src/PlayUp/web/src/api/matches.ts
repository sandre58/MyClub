/**
 * Match hub, stage match lists, match detail and live events.
 */
import type {
  AddDeclaredParticipationRequest,
  CompositionStatus,
  FinishMatchRequest,
  MatchDetail,
  MatchHubView,
  MatchScore,
  MatchSummary,
  RecordDisciplinaryEventRequest,
  RecordGoalRequest,
  RecordSubstitutionRequest,
} from '../types';
import { getJson, postNoContent, sendNoContent } from './http';

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

/** POST /matches/{id}/recorded-goals → 204 (facts only; Live RS is a separate call). */
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

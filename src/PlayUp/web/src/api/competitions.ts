/**
 * Competition reads, lifecycle, presentation, schedule, regulation, structure configure.
 */
import type {
  AddCompetitionStageRequest,
  AddCompetitionStageResponse,
  ConfigureStructureRequest,
  ConfigureStructureResponse,
  ConsultationView,
  CreateCompetitionRequest,
  CompetitionListItem,
  CompetitionDetail,
  NeedsAttention,
  OverviewView,
  RemoveCompetitionStageResponse,
  ReplaceRegulationRequest,
  StructureView,
  WorkspaceSummary,
} from '../types';
import { getJson, postNoContent, sendJson } from './http';

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
  return sendJson('POST', `/competitions/${competitionId}/structure`, request);
}

/** POST /competitions/{id}/stages → AddCompetitionStageResponse */
export function addCompetitionStage(
  competitionId: string,
  request: AddCompetitionStageRequest,
): Promise<AddCompetitionStageResponse> {
  return sendJson('POST', `/competitions/${competitionId}/stages`, request);
}

/** DELETE /competitions/{id}/stages/{stageId} → RemoveCompetitionStageResponse */
export function removeCompetitionStage(
  competitionId: string,
  stageId: string,
): Promise<RemoveCompetitionStageResponse> {
  return sendJson('DELETE', `/competitions/${competitionId}/stages/${stageId}`);
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

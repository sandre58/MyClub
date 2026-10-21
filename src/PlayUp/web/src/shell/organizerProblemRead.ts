import type { OrganizerProblemRead, StructureStageHubSummary } from '../types';

/** i18n / presentation fact code (O1: canon when Application sent `problem`). */
export function organizerProblemPresentationCode(
  problem: OrganizerProblemRead | null | undefined,
  transportWireCode: string,
): string {
  const canon = problem?.canonCode?.trim();
  return canon && canon.length > 0 ? canon : transportWireCode;
}

export function findStructureIssueProblem(
  stage: Pick<StructureStageHubSummary, 'structureIssueSemantics'>,
  issueCode: string,
): OrganizerProblemRead | undefined {
  const list = stage.structureIssueSemantics;
  if (!list?.length) {
    return undefined;
  }
  return list.find(
    (entry) =>
      entry.canonCode === issueCode || entry.transportCode === issueCode,
  );
}

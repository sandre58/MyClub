import type { StructureView } from '../../types';

export type StructureReadinessBlocker = {
  code: string;
  stageId?: string | null;
};

/** Global presence only — never use to infer owning stage. */
export function readinessHasBlockerCode(
  data: StructureView,
  code: string,
): boolean {
  if (data.readiness.blockers.includes(code)) {
    return true;
  }
  return (data.readiness.blockerDetails ?? []).some(
    (entry) => entry.code === code,
  );
}

export function blockerDetailsForStage(
  data: StructureView,
  stageId: string,
  code?: string,
): StructureReadinessBlocker[] {
  return (data.readiness.blockerDetails ?? []).filter((entry) => {
    if (entry.stageId !== stageId) {
      return false;
    }
    return code === undefined || entry.code === code;
  });
}

export function readinessBlockerCodesForStage(
  data: StructureView,
  stageId: string,
): string[] {
  const codes = blockerDetailsForStage(data, stageId).map(
    (entry) => entry.code,
  );
  return [...new Set(codes)];
}

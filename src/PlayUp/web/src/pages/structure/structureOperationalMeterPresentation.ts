import type { StageStatus } from '../../types';
import type { StructurePresentationTone } from './structureDiagnosticPresentationPolicy';
import type { StripOperationalMessageKind } from './structureOperationalStripUi';

export function presentationToneToMeterKind(
  tone: StructurePresentationTone | null | undefined,
): StripOperationalMessageKind {
  if (tone === 'danger') {
    return 'danger';
  }
  if (tone === 'attention') {
    return 'attention';
  }
  return 'info';
}

/** Incomplete bracket fill in Draft is expected — not error chrome. */
export function isDraftLikeStageStatus(
  stageStatus?: StageStatus | null,
): boolean {
  return stageStatus === 'Draft';
}

/** Exploitation paused — gauge copy is state readout, not construction checklist. */
export function isSuspendedStageStatus(
  stageStatus?: StageStatus | null,
): boolean {
  return stageStatus === 'Suspended';
}

/** Phase finished — meters are final-state readout, not construction checklist. */
export function isCompletedStageStatus(
  stageStatus?: StageStatus | null,
): boolean {
  return stageStatus === 'Completed';
}

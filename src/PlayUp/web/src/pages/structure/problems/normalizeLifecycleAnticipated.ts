import type { OrganizerProblemRead } from '../../../types';
import { organizerProblemPresentationCode } from '../../../shell/organizerProblemRead';
import { PROBLEMS_LIFECYCLE_PREFIX } from './constants';
import type { NormalizedLifecycleAnticipated } from './types';

export function lifecycleAnticipatedMessageKey(wireCode: string): string {
  return `${PROBLEMS_LIFECYCLE_PREFIX}${wireCode}`;
}

export function normalizeLifecycleAnticipated(
  wireCode: string,
  problem?: OrganizerProblemRead | null,
): NormalizedLifecycleAnticipated {
  const presentationCode = organizerProblemPresentationCode(problem, wireCode);
  return {
    source: 'lifecycle.anticipated',
    wireCode,
    messageKey: lifecycleAnticipatedMessageKey(presentationCode),
  };
}

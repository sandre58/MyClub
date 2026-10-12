import { PROBLEMS_LIFECYCLE_PREFIX } from './constants';
import type { NormalizedLifecycleAnticipated } from './types';

export function lifecycleAnticipatedMessageKey(wireCode: string): string {
  return `${PROBLEMS_LIFECYCLE_PREFIX}${wireCode}`;
}

export function normalizeLifecycleAnticipated(
  wireCode: string,
): NormalizedLifecycleAnticipated {
  return {
    source: 'lifecycle.anticipated',
    wireCode,
    messageKey: lifecycleAnticipatedMessageKey(wireCode),
  };
}

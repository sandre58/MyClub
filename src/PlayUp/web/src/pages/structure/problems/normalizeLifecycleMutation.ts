import { ApiError } from '../../../api';
import {
  MUTATION_UNKNOWN_KEY,
  PROBLEMS_LIFECYCLE_PREFIX,
  STAGE_COMPLETION_BLOCKED,
} from './constants';
import { resolveLifecycleMutationMessageKey } from './lifecycleMutationMessageKey';
import type { NormalizedLifecycleMutation } from './types';

export function normalizeLifecycleMutationError(
  error: unknown,
): NormalizedLifecycleMutation {
  if (error instanceof ApiError) {
    const wireCode = error.code ?? null;
    if (wireCode === STAGE_COMPLETION_BLOCKED) {
      return {
        source: 'lifecycle.mutation.api',
        wireCode,
        messageKey: `${PROBLEMS_LIFECYCLE_PREFIX}OpenMatches`,
      };
    }
    if (wireCode) {
      return {
        source: 'lifecycle.mutation.api',
        wireCode,
        messageKey: resolveLifecycleMutationMessageKey(wireCode),
      };
    }
    return {
      source: 'lifecycle.mutation.api',
      wireCode: null,
      messageKey: MUTATION_UNKNOWN_KEY,
    };
  }
  return {
    source: 'lifecycle.mutation.api',
    wireCode: null,
    messageKey: MUTATION_UNKNOWN_KEY,
  };
}

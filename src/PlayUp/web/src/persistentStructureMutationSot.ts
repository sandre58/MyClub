import { ApiError } from './api';

/** Application codes that encode persistent Structure graph / feed state (Q6). */
const PERSISTENT_STRUCTURE_MUTATION_CODES = new Set([
  'Application.SlotFeedsInvalid',
  'Application.DanglingFeedTarget',
]);

/**
 * When a mutation fails because of persistent Structure state, point at Structure SoT.
 * Footer remains the event — Structure owns the lasting signal.
 */
export function persistentStructureMutationSotHref(
  error: unknown,
  competitionId: string | null | undefined,
): string | null {
  if (!competitionId || !(error instanceof ApiError) || !error.code) {
    return null;
  }
  if (!PERSISTENT_STRUCTURE_MUTATION_CODES.has(error.code)) {
    return null;
  }
  return `/competitions/${competitionId}/structure`;
}

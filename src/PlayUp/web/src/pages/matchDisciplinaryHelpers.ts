import type { MatchDetail } from '../types';

/** Domain CanMutateDisciplinaryEventsFreely — Create/Remove/Correct UI V1 (#4-like). */
export function canMutateRecordedDisciplinaryEvents(
  match: MatchDetail,
): boolean {
  if (
    match.status === 'Scheduled' ||
    match.status === 'Postponed' ||
    match.status === 'Live'
  ) {
    return true;
  }

  return match.status === 'Finished' && !match.hasObservedLive;
}

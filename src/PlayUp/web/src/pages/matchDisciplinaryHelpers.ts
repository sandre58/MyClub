import type { MatchDetail } from '../types';

/** Domain CanMutateDisciplinaryEventsFreely — Create/Remove/Correct UI gate. */
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

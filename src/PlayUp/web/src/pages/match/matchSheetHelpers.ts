import type { MatchDetail } from '../../types';

export function canMutateMatchSheet(match: MatchDetail): boolean {
  if (match.status === 'Scheduled' || match.status === 'Postponed') {
    return true;
  }

  return match.status === 'Finished' && !match.hasObservedLive;
}

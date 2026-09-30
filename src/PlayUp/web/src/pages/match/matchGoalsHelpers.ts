import type { MatchDetail, MatchScore, MatchSide } from '../../types';

export function canMutateRecordedGoals(match: MatchDetail): boolean {
  if (
    match.status === 'Scheduled' ||
    match.status === 'Postponed' ||
    match.status === 'Live'
  ) {
    return true;
  }

  return match.status === 'Finished' && !match.hasObservedLive;
}

export function adjustRunningScore(
  current: MatchScore | null | undefined,
  creditedSide: MatchSide,
  delta: number,
): MatchScore {
  const base = current ?? { homeGoals: 0, awayGoals: 0 };
  if (creditedSide === 'Home') {
    return {
      homeGoals: Math.max(0, base.homeGoals + delta),
      awayGoals: base.awayGoals,
    };
  }

  return {
    homeGoals: base.homeGoals,
    awayGoals: Math.max(0, base.awayGoals + delta),
  };
}

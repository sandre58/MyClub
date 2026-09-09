import type { TFunction } from 'i18next';
import { resultTypeLabel } from '../i18n/enumLabels';
import type { MatchSummary } from '../types';

/**
 * Sporting context from the Match Read — never rebuilds matchday/round rules.
 */
export function matchSportingContext(
  match: MatchSummary,
  t: TFunction<'matches'>,
  matchdayKey: 'list.matchday' | 'stageList.matchday' = 'list.matchday',
): string | null {
  if (match.roundName?.trim()) {
    return match.roundName.trim();
  }
  if (match.matchdayNumber != null) {
    return t(matchdayKey, { number: match.matchdayNumber });
  }
  return null;
}

/** Kickoff display from Read scheduledAt (ISO). */
export function formatMatchKickoff(
  scheduledAt: string | null | undefined,
): string | null {
  if (!scheduledAt) {
    return null;
  }
  const date = new Date(scheduledAt);
  if (Number.isNaN(date.getTime())) {
    return null;
  }
  return new Intl.DateTimeFormat('fr-FR', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(date);
}

/** Date and time parts for Match Hero meta band. */
export function formatKickoffParts(scheduledAt: string | null | undefined): {
  date: string | null;
  time: string | null;
} {
  if (!scheduledAt) {
    return { date: null, time: null };
  }
  const date = new Date(scheduledAt);
  if (Number.isNaN(date.getTime())) {
    return { date: null, time: null };
  }
  return {
    date: new Intl.DateTimeFormat('fr-FR', { dateStyle: 'medium' }).format(
      date,
    ),
    time: new Intl.DateTimeFormat('fr-FR', { timeStyle: 'short' }).format(date),
  };
}

export function matchScheduledLabel(match: MatchSummary): string | null {
  return formatMatchKickoff(match.scheduledAt);
}

/** Result type label from Read — null when Domain has no result. */
export function matchResultTypeLabel(match: MatchSummary): string | null {
  return match.resultType ? resultTypeLabel(match.resultType) : null;
}

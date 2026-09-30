import { describe, expect, it } from 'vitest';
import i18n from '../../i18n';
import type { MatchSummary } from '../../types';
import {
  matchResultTypeLabel,
  matchScheduledLabel,
  matchSportingContext,
} from './matchListMeta';

const base: MatchSummary = {
  matchId: 'm1',
  stageId: 's1',
  status: 'Scheduled',
  home: { entryId: 'h', displayName: 'A' },
  away: { entryId: 'a', displayName: 'B' },
  score: null,
  fixtureId: null,
  roundId: null,
};

describe('matchListMeta', () => {
  const t = i18n.getFixedT('fr', 'matches');

  it('formats matchday from Read number via i18n', () => {
    expect(matchSportingContext({ ...base, matchdayNumber: 4 }, t)).toBe(
      'Journée 4',
    );
  });

  it('uses roundName from Read without inventing labels', () => {
    expect(
      matchSportingContext(
        { ...base, roundName: 'Quarts', matchdayNumber: 1 },
        t,
      ),
    ).toBe('Quarts');
  });

  it('does not invent resultType when the Read omits it', () => {
    expect(matchResultTypeLabel(base)).toBeNull();
    expect(matchResultTypeLabel({ ...base, resultType: 'Played' })).toBe(
      'Joué',
    );
  });

  it('formats scheduledAt from the Read ISO value', () => {
    expect(
      matchScheduledLabel({
        ...base,
        scheduledAt: '2026-09-01T15:00:00.000Z',
      }),
    ).toMatch(/2026/);
  });
});

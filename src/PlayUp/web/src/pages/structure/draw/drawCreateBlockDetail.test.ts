import { describe, expect, it } from 'vitest';

import {
  drawCreateBlockDetailNameIndex,
  formatDrawCreateBlockDetailLines,
} from './drawCreateBlockDetail';

const GROUP = '11111111-1111-4111-8111-111111111111';
const ALPHA = '22222222-2222-4222-8222-222222222222';
const BETA = '33333333-3333-4333-8333-333333333333';

function t(key: string, options?: Record<string, unknown>): string {
  if (key === 'place.group') {
    return `Groupe ${options?.name}`;
  }
  if (key === 'fiche.drawWorkflow.createBlocked.named.potConflict') {
    return `${options?.group}, pot ${options?.pot} : ${options?.teamA} et ${options?.teamB}`;
  }
  return key;
}

describe('drawCreateBlockDetailNameIndex', () => {
  it('names teams from the roster by id when the roster order differs', () => {
    const lines = formatDrawCreateBlockDetailLines(
      [`group:${GROUP}:pot:1:entries:${ALPHA},${BETA}`],
      drawCreateBlockDetailNameIndex({
        groups: [{ id: GROUP, name: 'A' }],
        roster: [
          { entryId: BETA, displayName: 'Beta' },
          { entryId: ALPHA, displayName: 'Alpha' },
        ],
      }),
      t,
    );

    expect(lines).toEqual(['Groupe A, pot 1 : Alpha et Beta']);
  });
});

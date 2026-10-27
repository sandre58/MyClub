import { describe, expect, it } from 'vitest';
import { insertSplitsLinkedPhases } from './structureInsertLinkedPhases';

const league = 'league';
const middle = 'middle';
const barrage = 'barrage';

describe('insertSplitsLinkedPhases', () => {
  const stages = [
    {
      stageId: league,
      qualificationIntents: [{ destinationStageId: barrage }],
    },
    { stageId: middle },
    { stageId: barrage },
  ];

  it('warns when the cut crosses a non-adjacent qualification', () => {
    expect(
      insertSplitsLinkedPhases(stages, {
        anchorStageId: league,
        position: 'After',
      }),
    ).toBe(true);
    expect(
      insertSplitsLinkedPhases(stages, {
        anchorStageId: middle,
        position: 'After',
      }),
    ).toBe(true);
  });

  it('stays quiet before the first stage and after the last', () => {
    expect(
      insertSplitsLinkedPhases(stages, {
        anchorStageId: league,
        position: 'Before',
      }),
    ).toBe(false);
    expect(
      insertSplitsLinkedPhases(stages, {
        anchorStageId: barrage,
        position: 'After',
      }),
    ).toBe(false);
  });

  it('warns when a backward progression is split', () => {
    expect(
      insertSplitsLinkedPhases(
        [
          { stageId: league },
          {
            stageId: barrage,
            progressionIntents: [{ destinationStageId: league }],
          },
        ],
        { anchorStageId: league, position: 'After' },
      ),
    ).toBe(true);
  });

  it('ignores a dangling destination and an unknown anchor', () => {
    expect(
      insertSplitsLinkedPhases(
        [
          {
            stageId: league,
            qualificationIntents: [{ destinationStageId: 'missing' }],
          },
          { stageId: barrage },
        ],
        { anchorStageId: league, position: 'After' },
      ),
    ).toBe(false);
    expect(
      insertSplitsLinkedPhases(stages, {
        anchorStageId: 'missing',
        position: 'Before',
      }),
    ).toBe(false);
  });
});

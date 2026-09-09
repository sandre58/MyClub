import { describe, expect, it } from 'vitest';
import { needsAttentionItemToSituation } from './needsAttentionToSituation';
import { situationDescription } from '../i18n/situationCopy';

describe('needsAttentionItemToSituation', () => {
  it('maps InsufficientParticipants to AddEntry / BlocksConstruction for Shell rows', () => {
    const situation = needsAttentionItemToSituation({
      source: 'InsufficientParticipants',
      severity: 'Blocking',
      targetType: 'Competition',
      targetId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
      params: {
        activeCount: '1',
        minimumTeams: '3',
        missingCount: '2',
      },
    });

    expect(situation).toMatchObject({
      source: 'InsufficientParticipants',
      nature: 'Blocking',
      actionable: true,
      actionCode: 'AddEntry',
      impactCode: 'BlocksConstruction',
      targetType: 'Competition',
      params: {
        activeCount: '1',
        minimumTeams: '3',
        missingCount: '2',
      },
    });
  });
});

describe('situationDescription', () => {
  it('describes how many teams are missing to start', () => {
    expect(
      situationDescription('InsufficientParticipants', {
        activeCount: 1,
        minimumTeams: 3,
        missingCount: 2,
      }),
    ).toBe('Il manque 2 équipes pour démarrer (1 / 3).');
  });

  it('returns null for sources without a description template', () => {
    expect(situationDescription('ProgressionPending')).toBeNull();
  });
});

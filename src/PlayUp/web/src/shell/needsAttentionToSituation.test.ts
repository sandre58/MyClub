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
  it('keeps InsufficientParticipants title-only (Teams SoT holds the long copy)', () => {
    expect(
      situationDescription('InsufficientParticipants', {
        activeCount: 1,
        minimumTeams: 3,
        missingCount: 2,
      }),
    ).toBeNull();
  });

  it('returns null for sources without an echo description', () => {
    expect(situationDescription('ProgressionPending')).toBeNull();
  });
});

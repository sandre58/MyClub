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

  it('uses suggestedActionCode from optional problem payload', () => {
    const situation = needsAttentionItemToSituation({
      source: 'QualificationPending',
      severity: 'Blocking',
      targetType: 'Slot',
      targetId: 'stage:slot-a',
      problem: {
        canonCode: 'QualificationPending',
        semanticLevel: 'Attention',
        blockedActions: [],
        suggestedActionCode: 'ApplyQualification',
      },
    });

    expect(situation.actionCode).toBe('ApplyQualification');
  });

  it('keeps DrawNoSolution non-actionable even with problem payload', () => {
    const situation = needsAttentionItemToSituation({
      source: 'DrawNoSolution',
      severity: 'Blocking',
      targetType: 'Draw',
      targetId: 'draw-1',
      problem: {
        canonCode: 'DrawNoSolution',
        semanticLevel: 'Attention',
        blockedActions: [],
      },
    });
    expect(situation.actionable).toBe(false);
    expect(situation.actionCode).toBeNull();
    expect(situation.problem?.canonCode).toBe('DrawNoSolution');
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

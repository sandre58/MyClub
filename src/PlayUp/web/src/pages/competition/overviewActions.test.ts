import { describe, expect, it } from 'vitest';
import {
  overviewActionKey,
  resolveOverviewActionIntent,
} from './overviewActions';
import { overviewIds, overviewView } from '../../test/overviewFixtures';
import { situationHref } from '../../shell/situationRoutes';

const { competitionId, stageId, drawId, matchId } = overviewIds;

describe('resolveOverviewActionIntent', () => {
  it('executes PrepareStage when stageId is provided by the Read', () => {
    const view = overviewView();
    const intent = resolveOverviewActionIntent(
      { code: 'PrepareStage', guaranteed: false, stageId },
      view,
    );
    expect(intent.kind).toBe('execute');
  });

  it('executes PrepareCompetition as a bodyless Host command', () => {
    const intent = resolveOverviewActionIntent(
      { code: 'PrepareCompetition', guaranteed: false },
      overviewView(),
    );
    expect(intent.kind).toBe('execute');
  });

  it('executes StartCompetition as a bodyless Host command', () => {
    const intent = resolveOverviewActionIntent(
      { code: 'StartCompetition', guaranteed: false },
      overviewView(),
    );
    expect(intent.kind).toBe('execute');
  });

  it('keeps unknown action codes unsupported', () => {
    const intent = resolveOverviewActionIntent(
      { code: 'InventedAction', guaranteed: false },
      overviewView(),
    );
    expect(intent).toEqual({ kind: 'unsupported' });
  });

  it('navigates structure actions instead of inventing POST bodies', () => {
    const intent = resolveOverviewActionIntent(
      { code: 'AddEntry', guaranteed: false },
      overviewView(),
    );
    expect(intent).toEqual({
      kind: 'navigate',
      to: `/competitions/${competitionId}/teams`,
    });
  });

  it('navigates OpenConsultation to Classements', () => {
    const intent = resolveOverviewActionIntent(
      { code: 'OpenConsultation', guaranteed: false },
      overviewView(),
    );
    expect(intent).toEqual({
      kind: 'navigate',
      to: `/competitions/${competitionId}/classements`,
    });
  });

  it('executes ApplyDraw for Slot/Group (no body)', () => {
    const intent = resolveOverviewActionIntent(
      { code: 'ApplyDraw', guaranteed: false, stageId, drawId },
      overviewView(),
    );
    expect(intent.kind).toBe('execute');
  });

  it('executes MaterializeFromOccupiedSlots for all eligible Cup pairs', () => {
    const intent = resolveOverviewActionIntent(
      { code: 'MaterializeFromOccupiedSlots', guaranteed: false, stageId },
      overviewView(),
    );
    expect(intent.kind).toBe('execute');
  });

  it('executes GenerateNextRound when stageId is provided', () => {
    const intent = resolveOverviewActionIntent(
      { code: 'GenerateNextRound', guaranteed: false, stageId },
      overviewView(),
    );
    expect(intent.kind).toBe('execute');
  });

  it('builds a stable action key from Host ids', () => {
    expect(
      overviewActionKey({
        code: 'PublishDraw',
        guaranteed: false,
        stageId,
        drawId,
      }),
    ).toBe(`PublishDraw:${stageId}:${drawId}::`);
  });
});

describe('situationHref', () => {
  it('prefers Host matchId over Fixture join', () => {
    expect(
      situationHref(
        {
          source: 'ProgressionPending',
          nature: 'Blocking',
          targetType: 'Fixture',
          targetId: 'fixture-id',
          matchId,
          actionable: true,
          actionCode: 'ApplyProgression',
          impactCode: 'BlocksProgression',
          params: {},
        },
        competitionId,
      ),
    ).toBe(`/matches/${matchId}`);
  });

  it('maps InsufficientParticipants to Équipes', () => {
    expect(
      situationHref(
        {
          source: 'InsufficientParticipants',
          nature: 'Blocking',
          targetType: 'Structure',
          targetId: competitionId,
          matchId: null,
          actionable: true,
          actionCode: 'AddEntry',
          impactCode: 'BlocksConstruction',
          params: {},
        },
        competitionId,
      ),
    ).toBe(`/competitions/${competitionId}/teams`);
  });

  it('maps CupBracketInvalid with stageId to construction deep link', () => {
    const stageId = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
    expect(
      situationHref(
        {
          source: 'CupBracketInvalid',
          nature: 'Blocking',
          targetType: 'Competition',
          targetId: competitionId,
          matchId: null,
          actionable: true,
          actionCode: 'FixStructure',
          impactCode: 'BlocksConstruction',
          params: { stageId },
        },
        competitionId,
      ),
    ).toBe(
      `/competitions/${competitionId}/structure?stage=${encodeURIComponent(stageId)}&section=construction`,
    );
  });

  it('keeps MissingPotRules on structure hub without construction section', () => {
    const stageId = 'cccccccc-cccc-cccc-cccc-cccccccccccc';
    expect(
      situationHref(
        {
          source: 'MissingPotRules',
          nature: 'Blocking',
          targetType: 'Competition',
          targetId: competitionId,
          matchId: null,
          actionable: true,
          actionCode: 'FixStructure',
          impactCode: 'BlocksConstruction',
          params: { stageId },
        },
        competitionId,
      ),
    ).toBe(
      `/competitions/${competitionId}/structure?stage=${encodeURIComponent(stageId)}`,
    );
  });
});

import { describe, expect, it } from 'vitest';
import { overviewSituation, overviewView } from '../../test/overviewFixtures';
import {
  actionPresentationSlot,
  actionsForDraw,
  actionsForStage,
  isProminenceVisible,
  primaryTeamActions,
  secondaryActions,
  sortConstructionSlots,
} from './overviewComposition';
import { overviewActionKey } from './overviewActions';

describe('overviewComposition', () => {
  it('hides Absent prominence', () => {
    expect(isProminenceVisible('Absent')).toBe(false);
    expect(isProminenceVisible('Dominant')).toBe(true);
  });

  it('orders construction slots by prominence without Absent', () => {
    const slots = sortConstructionSlots(
      overviewView({
        constructionDimensions: {
          ...overviewView().constructionDimensions,
          teams: { prominence: 'Present', facts: { activeCount: '1' } },
          structure: { prominence: 'Dominant', facts: { formatKind: 'None' } },
          regulation: {
            ...overviewView().constructionDimensions.regulation,
            prominence: 'Condensed',
          },
          matches: { prominence: 'Absent', facts: { total: '0' } },
        },
      }),
    );
    expect(slots).toEqual(['structure', 'teams', 'regulation']);
  });

  it('maps action codes to presentation slots without inventing métier rules', () => {
    expect(actionPresentationSlot('AddEntry')).toBe('teams');
    expect(actionPresentationSlot('ConfigureStructure')).toBe('structure');
    expect(actionPresentationSlot('PublishDraw')).toBe('operational');
    expect(actionPresentationSlot('PublishAndApplyDraw')).toBe('operational');
    expect(actionPresentationSlot('CompleteCompetition')).toBe('closure');
    // Lifecycle Prepare/Start stay secondary — closure slot is hidden during Construction.
    expect(actionPresentationSlot('PrepareCompetition')).toBe('secondary');
    expect(actionPresentationSlot('StartCompetition')).toBe('secondary');
  });

  it('excludes already-rendered and team-admin actions from the secondary strip', () => {
    const actions = [
      { code: 'AddEntry', guaranteed: false },
      { code: 'RenameEntry', guaranteed: false },
      { code: 'MaterializeMatches', guaranteed: false },
    ];
    const rendered = new Set([overviewActionKey(actions[0])]);
    expect(secondaryActions(actions, rendered).map((a) => a.code)).toEqual([
      'MaterializeMatches',
    ]);
  });

  it('keeps only AddEntry as primary team overview action', () => {
    expect(
      primaryTeamActions([
        { code: 'AddEntry', guaranteed: false },
        { code: 'RenameEntry', guaranteed: false },
        { code: 'WithdrawEntry', guaranteed: false },
      ]).map((a) => a.code),
    ).toEqual(['AddEntry']);
  });

  it('attaches PrepareStage and PublishAndApplyDraw to stage/draw ids from the Read', () => {
    const stageId = 'stage-1';
    const drawId = 'draw-1';
    const actions = [
      { code: 'PrepareStage', guaranteed: false, stageId },
      { code: 'PublishAndApplyDraw', guaranteed: false, stageId, drawId },
      { code: 'PrepareStage', guaranteed: false, stageId: 'other' },
    ];
    expect(actionsForStage(actions, stageId).map((a) => a.code)).toEqual([
      'PrepareStage',
    ]);
    expect(actionsForDraw(actions, stageId, drawId).map((a) => a.code)).toEqual(
      ['PublishAndApplyDraw'],
    );
  });

  it('does not treat attentionSummary as a separate situation source', () => {
    const situation = overviewSituation();
    const view = overviewView({
      situations: [situation],
      attentionSummary: { count: 1, items: [situation] },
    });
    expect(view.attentionSummary.items[0].source).toBe(
      view.situations[0].source,
    );
  });
});

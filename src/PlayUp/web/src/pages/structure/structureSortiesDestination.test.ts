import { describe, expect, it } from 'vitest';
import {
  applyDestinationToDraft,
  applyTargetKindToDraft,
  isFormOnlyDestination,
  normalizeFormOnlyDestinationDraft,
  showsPopulationPlaceChoice,
  type SortiesDestinationFields,
} from './structureSortiesDestination';
import {
  emptyProgIntent,
  toApiIntent as toProgApiIntent,
} from './structureProgressionDraft';
import {
  emptyQualIntent,
  toApiIntent as toQualApiIntent,
} from './structureQualificationDraft';

function fields(
  partial: Partial<SortiesDestinationFields> = {},
): SortiesDestinationFields {
  return {
    targetKind: 'population',
    destinationStageId: '',
    destinationSlotKeys: [],
    destinationGroupIds: [],
    destinationForm: false,
    ...partial,
  };
}

describe('isFormOnlyDestination / showsPopulationPlaceChoice', () => {
  it('locks Championship and Swiss as form-only', () => {
    expect(isFormOnlyDestination('Championship')).toBe(true);
    expect(isFormOnlyDestination('Swiss')).toBe(true);
    expect(showsPopulationPlaceChoice('Championship')).toBe(false);
    expect(showsPopulationPlaceChoice('Swiss')).toBe(false);
  });

  it('keeps Cup and Groups as Population|Place choice', () => {
    expect(isFormOnlyDestination('Cup')).toBe(false);
    expect(isFormOnlyDestination('Groups')).toBe(false);
    expect(showsPopulationPlaceChoice('Cup')).toBe(true);
    expect(showsPopulationPlaceChoice('Groups')).toBe(true);
  });
});

describe('applyDestinationToDraft', () => {
  it('Championship → place + destinationForm, maps cleared', () => {
    const draft = fields({
      targetKind: 'population',
      destinationStageId: 'cup',
      destinationSlotKeys: ['s1'],
      destinationGroupIds: ['g1'],
    });
    const next = applyDestinationToDraft(draft, 'champ', 'Championship');
    expect(next).toMatchObject({
      destinationStageId: 'champ',
      targetKind: 'place',
      destinationForm: true,
      destinationSlotKeys: [],
      destinationGroupIds: [],
    });
  });

  it('Championship → Cup clears form and defaults to population', () => {
    const draft = fields({
      destinationStageId: 'champ',
      targetKind: 'place',
      destinationForm: true,
    });
    const next = applyDestinationToDraft(draft, 'cup', 'Cup');
    expect(next).toMatchObject({
      destinationStageId: 'cup',
      targetKind: 'population',
      destinationForm: false,
      destinationSlotKeys: [],
      destinationGroupIds: [],
    });
  });

  it('Cup Place → Championship locks form and clears map', () => {
    const draft = fields({
      destinationStageId: 'cup',
      targetKind: 'place',
      destinationForm: false,
      destinationSlotKeys: ['SF1-A', 'SF1-B'],
    });
    const next = applyDestinationToDraft(draft, 'champ', 'Championship');
    expect(next).toMatchObject({
      destinationStageId: 'champ',
      targetKind: 'place',
      destinationForm: true,
      destinationSlotKeys: [],
      destinationGroupIds: [],
    });
  });

  it('Cup → Cup keeps place kind when not leaving form', () => {
    const draft = fields({
      destinationStageId: 'cup-a',
      targetKind: 'place',
      destinationForm: false,
      destinationSlotKeys: ['A'],
    });
    const next = applyDestinationToDraft(draft, 'cup-b', 'Cup');
    expect(next.targetKind).toBe('place');
    expect(next.destinationForm).toBe(false);
    expect(next.destinationSlotKeys).toEqual([]);
  });
});

describe('applyTargetKindToDraft', () => {
  it('no-ops when destinationForm is locked', () => {
    const draft = fields({
      targetKind: 'place',
      destinationForm: true,
    });
    expect(applyTargetKindToDraft(draft, 'population')).toBe(draft);
  });

  it('clears maps when switching to population on Cup', () => {
    const draft = fields({
      targetKind: 'place',
      destinationSlotKeys: ['A'],
    });
    expect(applyTargetKindToDraft(draft, 'population')).toMatchObject({
      targetKind: 'population',
      destinationForm: false,
      destinationSlotKeys: [],
      destinationGroupIds: [],
    });
  });
});

describe('normalizeFormOnlyDestinationDraft (legacy open)', () => {
  it('legacy ForPopulation → Championship becomes Form draft', () => {
    const draft = fields({
      destinationStageId: 'champ',
      targetKind: 'population',
      destinationForm: false,
    });
    const next = normalizeFormOnlyDestinationDraft(draft, 'Championship');
    expect(next).toMatchObject({
      targetKind: 'place',
      destinationForm: true,
      destinationSlotKeys: [],
      destinationGroupIds: [],
    });
  });

  it('does not change Cup drafts', () => {
    const draft = fields({
      destinationStageId: 'cup',
      targetKind: 'population',
    });
    expect(normalizeFormOnlyDestinationDraft(draft, 'Cup')).toBe(draft);
  });

  it('is idempotent when already Form-locked', () => {
    const draft = fields({
      destinationStageId: 'champ',
      targetKind: 'place',
      destinationForm: true,
    });
    expect(normalizeFormOnlyDestinationDraft(draft, 'Championship')).toBe(
      draft,
    );
  });
});

describe('save payload ForForm after Champ destination', () => {
  it('Qual: Champ draft saves destinationForm', () => {
    const draft = applyDestinationToDraft(
      emptyQualIntent('champ'),
      'champ',
      'Championship',
    );
    const api = toQualApiIntent(draft, 1, []);
    expect(api.destinationForm).toBe(true);
    expect(api.destinationSlotKeys).toBeNull();
    expect(api.destinationGroupIds).toBeNull();
  });

  it('Prog: Champ draft saves destinationForm', () => {
    const draft = applyDestinationToDraft(
      emptyProgIntent('champ'),
      'champ',
      'Championship',
    );
    draft.roundId = 'r1';
    const api = toProgApiIntent(draft, 1);
    expect(api.destinationForm).toBe(true);
    expect(api.destinationSlotKeys).toBeNull();
    expect(api.destinationGroupIds).toBeNull();
  });

  it('legacy Qual ForPopulation → Champ normalize → save ForForm', () => {
    const legacy = emptyQualIntent('champ', 'population');
    const normalized = normalizeFormOnlyDestinationDraft(
      legacy,
      'Championship',
    );
    expect(toQualApiIntent(normalized, 1, []).destinationForm).toBe(true);
  });

  it('legacy Prog ForPopulation → Champ normalize → save ForForm', () => {
    const legacy = emptyProgIntent('champ', 'population');
    legacy.roundId = 'r1';
    const normalized = normalizeFormOnlyDestinationDraft(
      legacy,
      'Championship',
    );
    expect(toProgApiIntent(normalized, 1).destinationForm).toBe(true);
  });
});

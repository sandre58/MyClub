import { describe, expect, it } from 'vitest';
import { structurePageHeadHubStepLabelKey } from './structurePageHeadHubStepCopy';

describe('structurePageHeadHubStepLabelKey', () => {
  it('maps materialize command codes to one label key', () => {
    expect(
      structurePageHeadHubStepLabelKey('MaterializeMatches', {
        includeStageName: true,
      }),
    ).toBe('readiness.hubStep.MaterializeMatches');
    expect(
      structurePageHeadHubStepLabelKey('MaterializeFromOccupiedSlots', {
        includeStageName: true,
      }),
    ).toBe('readiness.hubStep.MaterializeMatches');
  });

  it('uses solo suffix when stage name is omitted', () => {
    expect(
      structurePageHeadHubStepLabelKey('PrepareStage', {
        includeStageName: false,
      }),
    ).toBe('readiness.hubStep.PrepareStage_solo');
  });
});

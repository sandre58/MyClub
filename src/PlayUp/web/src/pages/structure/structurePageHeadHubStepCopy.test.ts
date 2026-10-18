import { describe, expect, it } from 'vitest';
import { structurePageHeadHubStepLabelKey } from './structurePageHeadHubStepCopy';

describe('structurePageHeadHubStepLabelKey', () => {
  it('maps materialize command codes to one label key', () => {
    expect(structurePageHeadHubStepLabelKey('MaterializeMatches')).toBe(
      'readiness.hubStep.MaterializeMatches',
    );
    expect(structurePageHeadHubStepLabelKey('MaterializeFromOccupiedSlots')).toBe(
      'readiness.hubStep.MaterializeMatches',
    );
  });

  it('maps PrepareStage', () => {
    expect(structurePageHeadHubStepLabelKey('PrepareStage')).toBe(
      'readiness.hubStep.PrepareStage',
    );
  });
});

import { describe, expect, it } from 'vitest';

import { organizerProblemPresentationCode } from './organizerProblemRead';

describe('organizerProblemPresentationCode', () => {
  it('prefers canon over lifecycle transport aggregate', () => {
    expect(
      organizerProblemPresentationCode(
        {
          canonCode: 'CycleDetected',
          semanticLevel: 'Gate',
          blockedActions: ['PrepareStage'],
          transportCode: 'StructureGraphInvalid',
        },
        'StructureGraphInvalid',
      ),
    ).toBe('CycleDetected');
  });

  it('falls back to transport when problem is absent', () => {
    expect(organizerProblemPresentationCode(null, 'OpenMatches')).toBe(
      'OpenMatches',
    );
  });
});

import { describe, expect, it } from 'vitest';

import {
  shouldShowPrimaryBlockMessage,
  shouldShowRevertBlockMessage,
} from './structurePhaseStatusBandModel';

describe('structurePhaseStatusBandModel', () => {
  it('shows block messages for draft prepare gates', () => {
    expect(shouldShowPrimaryBlockMessage('Draft', 'StageNotReady')).toBe(true);
    expect(shouldShowPrimaryBlockMessage('Draft', 'StructureGraphInvalid')).toBe(
      true,
    );
  });

  it('hides open-matches banner on running phase', () => {
    expect(
      shouldShowPrimaryBlockMessage('Running', 'OpenMatches'),
    ).toBe(false);
  });

  it('shows revert block message when disabled reason is set', () => {
    expect(shouldShowRevertBlockMessage('ProductionPresent')).toBe(true);
    expect(shouldShowRevertBlockMessage(null)).toBe(false);
  });
});

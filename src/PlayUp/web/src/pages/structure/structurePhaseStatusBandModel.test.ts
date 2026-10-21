import { describe, expect, it } from 'vitest';

import {
  matchProgressAllClosed,
  matchProgressClosed,
  matchProgressRemaining,
} from './structurePhaseStatusBandModel';

describe('matchProgressClosed helpers', () => {
  it('counts finished and cancelled as closed', () => {
    expect(matchProgressClosed(2, 1)).toBe(3);
    expect(matchProgressRemaining(5, 2, 1)).toBe(2);
  });

  it('allClosed when open is zero regardless of finished vs cancelled split', () => {
    expect(matchProgressAllClosed(3, 1, 2, 0)).toBe(true);
    expect(matchProgressAllClosed(3, 3, 0, 0)).toBe(true);
    expect(matchProgressAllClosed(3, 2, 0, 1)).toBe(false);
    expect(matchProgressAllClosed(0, 0, 0, 0)).toBe(false);
  });
});

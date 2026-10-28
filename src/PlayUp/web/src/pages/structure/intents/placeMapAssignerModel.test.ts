import { describe, expect, it } from 'vitest';
import {
  alignPlaceAssignments,
  assignPlaceToSource,
  canFillPlaceMap,
  clearSourceAssignment,
  fillPlaceMap,
} from './placeMapAssignerModel';

describe('placeMapAssignerModel', () => {
  it('writes a place on the source index and pads missing rows', () => {
    expect(alignPlaceAssignments(['SF-A'], 3)).toEqual(['SF-A', '', '']);
    expect(assignPlaceToSource(['SF-A', ''], 2, 1, 'SF-B', true)).toEqual([
      'SF-A',
      'SF-B',
    ]);
  });

  it('refuses a Cup slot already used by another source', () => {
    expect(assignPlaceToSource(['SF-A', ''], 2, 1, 'SF-A', true)).toBeNull();
    expect(assignPlaceToSource(['SF-A', ''], 2, 0, 'SF-A', true)).toEqual([
      'SF-A',
      '',
    ]);
  });

  it('allows Groups to reuse a pool', () => {
    expect(assignPlaceToSource(['grp-a', ''], 2, 1, 'grp-a', false)).toEqual([
      'grp-a',
      'grp-a',
    ]);
  });

  it('clears one source index', () => {
    expect(clearSourceAssignment(['SF-A', 'SF-B'], 2, 0)).toEqual(['', 'SF-B']);
  });

  it('fills only empty indexes', () => {
    expect(
      fillPlaceMap(['SF-A', '', ''], ['SF-A', 'SF-B', 'SF-C'], true),
    ).toEqual(['SF-A', 'SF-B', 'SF-C']);
    expect(fillPlaceMap(['grp-a', ''], ['grp-a'], false)).toEqual([
      'grp-a',
      'grp-a',
    ]);
    expect(canFillPlaceMap(['SF-A', 'SF-B'], ['SF-A', 'SF-B'], true)).toBe(
      false,
    );
    expect(canFillPlaceMap(['SF-A', ''], ['SF-A'], true)).toBe(false);
    expect(canFillPlaceMap(['grp-a', ''], ['grp-a'], false)).toBe(true);
  });
});

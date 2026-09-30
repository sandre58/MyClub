import type { MatchGenerationFormat, StructureFormatKind } from '../types';

export type SkeletonFormState = {
  format: StructureFormatKind;
  groupCount: number;
  participantsPerGroup: number;
  bracketSize: number;
  swissRoundCount: number;
  matchGenerationFormat: MatchGenerationFormat;
};

export function defaultSkeletonForm(
  format: StructureFormatKind = 'Championship',
): SkeletonFormState {
  return {
    format,
    groupCount: 2,
    participantsPerGroup: 2,
    bracketSize: 4,
    swissRoundCount: 3,
    matchGenerationFormat: 'SingleRoundRobin',
  };
}

export function skeletonPayload(state: SkeletonFormState) {
  return {
    groupCount: state.format === 'Groups' ? state.groupCount : null,
    participantsPerGroup:
      state.format === 'Groups' ? state.participantsPerGroup : null,
    bracketSize: state.format === 'Cup' ? state.bracketSize : null,
    swissRoundCount: state.format === 'Swiss' ? state.swissRoundCount : null,
    matchGenerationFormat:
      state.format === 'Championship' || state.format === 'Groups'
        ? state.matchGenerationFormat
        : null,
  };
}

export function isPowerOfTwo(value: number): boolean {
  return value > 0 && (value & (value - 1)) === 0;
}

export function skeletonStepValid(state: SkeletonFormState): boolean {
  switch (state.format) {
    case 'Championship':
      return true;
    case 'Groups':
      return state.groupCount >= 2 && state.participantsPerGroup >= 2;
    case 'Cup':
      return (
        state.bracketSize >= 2 &&
        state.bracketSize <= 64 &&
        isPowerOfTwo(state.bracketSize)
      );
    case 'Swiss':
      return state.swissRoundCount >= 1;
    default:
      return false;
  }
}

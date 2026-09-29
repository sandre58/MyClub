import type {
  DeclaredParticipation,
  MatchDetail,
  MatchSide,
  RecordedSubstitution,
} from '../types';

/** Domain CanMutateSubstitutionsFreely — Create/Remove/Correct UI gate. */
export function canMutateRecordedSubstitutions(match: MatchDetail): boolean {
  if (match.status === 'Live') {
    return true;
  }

  return match.status === 'Finished' && !match.hasObservedLive;
}

/**
 * Derived on-field presence for one side: Starter baseline + ordered replay.
 * `upToExclusive` = apply only substitutions[0..upToExclusive) (for Correct at index).
 */
export function deriveOnFieldMembers(
  sheet: DeclaredParticipation[],
  substitutions: RecordedSubstitution[],
  side: MatchSide,
  upToExclusive?: number,
): Set<string> {
  const onField = new Set(
    sheet
      .filter((row) => row.side === side && row.compositionStatus === 'Starter')
      .map((row) => row.memberId),
  );

  const limit =
    upToExclusive === undefined ? substitutions.length : upToExclusive;

  for (let index = 0; index < limit; index += 1) {
    const sub = substitutions[index];
    if (sub == null || sub.side !== side) {
      continue;
    }
    onField.delete(sub.outMemberId);
    onField.add(sub.inMemberId);
  }

  return onField;
}

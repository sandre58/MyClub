import type { StructurePageHeadNoteKind } from './structurePageHeadReadiness';
import type { StructureReadinessSummary } from './structureReadinessSummary';

/** PageHead diagnostic titles only — gravity and note kind unchanged. */
export function shouldUsePausedStructurePageHeadCopy(args: {
  noteKind: StructurePageHeadNoteKind;
  summary: Pick<
    StructureReadinessSummary,
    | 'presentedStructureProblemsSuspendedOnly'
    | 'hasAny'
    | 'showPageHeadNegative'
    | 'shapeInvalid'
  >;
  presentableBlockingAttentionCount: number;
  presentableNonBlockingAttentionCount: number;
}): boolean {
  const attentionDrawerEmpty =
    args.presentableBlockingAttentionCount === 0 &&
    args.presentableNonBlockingAttentionCount === 0;

  if (
    !args.summary.presentedStructureProblemsSuspendedOnly ||
    !attentionDrawerEmpty
  ) {
    return false;
  }

  if (
    args.noteKind === 'warning' &&
    args.summary.hasAny &&
    !args.summary.showPageHeadNegative
  ) {
    return true;
  }

  if (
    args.noteKind === 'negative' &&
    args.summary.showPageHeadNegative &&
    !args.summary.shapeInvalid
  ) {
    return true;
  }

  return false;
}

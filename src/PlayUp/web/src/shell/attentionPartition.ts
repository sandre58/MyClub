import type {
  CompetitionStageSummary,
  NeedsAttentionItem,
  OverviewSituation,
} from '../types';
import { needsAttentionItemsToSituations } from './needsAttentionToSituation';
import { applyStructureAttentionPresentation } from './structureAttentionPresentation';

export type AttentionPresentationContext = {
  stages?: readonly CompetitionStageSummary[];
};

/**
 * Sources that must never appear in AttentionDrawer (birth + aggregates).
 * Host should not emit them — SPA denylist is a guardrail.
 */
export const ATTENTION_DRAWER_CONSTRUCTION_DENYLIST = new Set([
  'MissingStage',
  'MissingStructure',
  'StructureGraphInvalid',
]);

export function isAttentionDrawerForbiddenSource(source: string): boolean {
  return ATTENTION_DRAWER_CONSTRUCTION_DENYLIST.has(source);
}

export function partitionAttentionItems(items: OverviewSituation[]): {
  blocking: OverviewSituation[];
  attention: OverviewSituation[];
} {
  const blocking: OverviewSituation[] = [];
  const attention: OverviewSituation[] = [];

  for (const item of items) {
    if (isAttentionDrawerForbiddenSource(item.source)) {
      continue;
    }
    if (item.nature === 'Blocking') {
      blocking.push(item);
    } else {
      attention.push(item);
    }
  }

  return { blocking, attention };
}

function attentionSituationKey(item: OverviewSituation): string {
  return `${item.source}\0${item.targetType ?? ''}\0${item.targetId ?? ''}`;
}

/** Aligns with OverviewAssembler.DeduplicateSituations (Source + TargetType + TargetId). */
export function dedupeAttentionSituations(
  items: OverviewSituation[],
): OverviewSituation[] {
  const seen = new Set<string>();
  const out: OverviewSituation[] = [];
  for (const item of items) {
    const key = attentionSituationKey(item);
    if (seen.has(key)) {
      continue;
    }
    seen.add(key);
    out.push(item);
  }
  return out;
}

/** Drawer-visible rows: map wire → dedupe → drop construction denylist. */
export function presentableAttentionSituations(
  wireItems: NeedsAttentionItem[],
  context?: AttentionPresentationContext,
): OverviewSituation[] {
  const mapped = needsAttentionItemsToSituations(wireItems);
  const deduped = dedupeAttentionSituations(mapped);
  const filtered = deduped.filter(
    (item) => !isAttentionDrawerForbiddenSource(item.source),
  );
  if (context?.stages?.length) {
    return applyStructureAttentionPresentation(filtered, context.stages);
  }
  return filtered;
}

export function presentableAttentionCount(
  wireItems: NeedsAttentionItem[],
  context?: AttentionPresentationContext,
): number {
  return presentableAttentionSituations(wireItems, context).length;
}

/** Blocking vs warning groups for already-presentable situations. */
export function partitionPresentableAttention(
  wireItems: NeedsAttentionItem[],
  context?: AttentionPresentationContext,
): {
  blocking: OverviewSituation[];
  attention: OverviewSituation[];
} {
  return partitionAttentionItems(
    presentableAttentionSituations(wireItems, context),
  );
}

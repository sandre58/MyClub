/**
 * Construction blockers that must never appear in AttentionDrawer (Q1).
 * Host `/attention` does not emit them today — SPA denylist is a guardrail.
 */
export const ATTENTION_DRAWER_CONSTRUCTION_DENYLIST = new Set([
  'MissingStage',
  'MissingStructure',
  'MissingPotRules',
  'CupBracketInvalid',
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

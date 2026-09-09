import type { OverviewSituation } from '../types';

export function partitionAttentionItems(items: OverviewSituation[]): {
  blocking: OverviewSituation[];
  attention: OverviewSituation[];
} {
  const blocking: OverviewSituation[] = [];
  const attention: OverviewSituation[] = [];

  for (const item of items) {
    if (item.nature === 'Blocking') {
      blocking.push(item);
    } else {
      attention.push(item);
    }
  }

  return { blocking, attention };
}

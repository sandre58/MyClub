// -----------------------------------------------------------------------
// Sorties rail — authoring Intentions (not Expand paths).
// Decision: Population = chemins ; Sorties = intentions.
// -----------------------------------------------------------------------

import type {
  StructureProgressionIntent,
  StructureQualificationIntent,
  StructureStageHubSummary,
  StructureView,
} from '../types';
import {
  intentFromApi as qualIntentFromApi,
  summarizeIntentWhoParts,
} from './structureQualificationDraft';

export type SortiesFeedFamily = 'place' | 'result';

export type SortiesFeedRow = {
  key: string;
  peerId: string;
  peerName: string;
  peerOrder: number;
  badge: string;
  badgeTone: 'win' | 'loss' | 'neutral' | 'accent';
  context: string;
  extra?: string;
  badgeAsText?: boolean;
  volume: number;
  family: SortiesFeedFamily;
  sortPrimary: number;
  sortSecondary: string;
};

type Translate = (key: string, opts?: Record<string, unknown>) => string;

function stagePeerOrder(data: StructureView, stageId: string): number {
  const index = data.stages.findIndex((s) => s.stageId === stageId);
  return index >= 0 ? index : Number.MAX_SAFE_INTEGER;
}

function nameOf(data: StructureView, id: string): string {
  return data.stages.find((s) => s.stageId === id)?.name ?? id;
}

/**
 * Outbound Sorties: intentions when present per family, else Expand paths
 * (legacy / incomplete migration).
 */
export function outboundSortiesFeeds(
  data: StructureView,
  stage: StructureStageHubSummary,
  locale: string,
  t: Translate,
  pathFallback: SortiesFeedRow[],
): SortiesFeedRow[] {
  const quals = stage.qualificationIntents ?? [];
  const progs = stage.progressionIntents ?? [];
  const hasQualIntents = quals.length > 0;
  const hasProgIntents = progs.length > 0;

  if (!hasQualIntents && !hasProgIntents) {
    return pathFallback;
  }

  const feeds: SortiesFeedRow[] = [];
  if (hasQualIntents) {
    for (const intent of quals) {
      feeds.push(qualificationIntentRow(data, intent, locale, t));
    }
  } else {
    feeds.push(...pathFallback.filter((f) => f.family === 'place'));
  }

  if (hasProgIntents) {
    for (const intent of progs) {
      feeds.push(progressionIntentRow(data, stage, intent, t));
    }
  } else {
    feeds.push(...pathFallback.filter((f) => f.family === 'result'));
  }

  return feeds;
}

export function qualificationIntentRow(
  data: StructureView,
  intent: StructureQualificationIntent,
  locale: string,
  t: Translate,
): SortiesFeedRow {
  const draft = qualIntentFromApi(intent);
  const { selection, scope } = summarizeIntentWhoParts(draft, locale, t);
  const destId = intent.destinationStageId;
  const volume = Math.max(1, intent.destinationCount ?? 1);
  const extra =
    draft.conditionKind === 'points' && Number.isFinite(Number(draft.minimumPoints))
      ? t('fiche.rule.minimumPoints', { n: Number(draft.minimumPoints) })
      : undefined;
  return {
    key: `qi-${intent.intentId}`,
    peerId: destId,
    peerName: nameOf(data, destId),
    peerOrder: stagePeerOrder(data, destId),
    badge: selection || t('qualification.newPath'),
    badgeTone: 'accent',
    context: scope,
    extra,
    volume,
    family: 'place',
    sortPrimary: intent.order,
    sortSecondary: `${selection}|${scope}`,
  };
}

export function progressionIntentRow(
  data: StructureView,
  sourceStage: StructureStageHubSummary,
  intent: StructureProgressionIntent,
  t: Translate,
): SortiesFeedRow {
  const isWinner = intent.outcome === 'Winner';
  const destId = intent.destinationStageId;
  const volume = Math.max(1, intent.expandedPathCount ?? 1);
  const round =
    intent.roundName?.trim() ||
    (intent.roundId
      ? t('progression.roundFallback', {
          id: intent.roundId.slice(0, 8),
        })
      : '');
  // On the source phase Sorties rail, repeating the current phase name
  // (common when Cup round name === stage name) adds no information.
  const context =
    round &&
    round.localeCompare(sourceStage.name.trim(), undefined, {
      sensitivity: 'accent',
    }) !== 0
      ? round
      : '';
  return {
    key: `pi-${intent.intentId}`,
    peerId: destId,
    peerName: nameOf(data, destId),
    peerOrder: stagePeerOrder(data, destId),
    badge: isWinner
      ? t('fiche.rule.winner', { count: volume })
      : t('fiche.rule.loser', { count: volume }),
    badgeTone: isWinner ? 'win' : 'loss',
    context,
    volume,
    family: 'result',
    sortPrimary: isWinner ? 0 : 1,
    sortSecondary: `${String(intent.order).padStart(6, '0')}-${round}`,
  };
}

/** True when the stage exposes at least one authoring intent for Sorties. */
export function stageHasOutboundIntents(
  stage: StructureStageHubSummary,
): boolean {
  return (
    (stage.qualificationIntents?.length ?? 0) > 0 ||
    (stage.progressionIntents?.length ?? 0) > 0
  );
}

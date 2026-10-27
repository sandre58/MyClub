// -----------------------------------------------------------------------
// Structure phase fiche — inbound/outbound Expand path feeds (Sorties rails).
// Pure helpers; UI lives in StructurePhaseRails.tsx.
// -----------------------------------------------------------------------

import type {
  SelectionMode,
  StructurePlacementAward,
  StructureProgressionIntent,
  StructureProgressionPath,
  StructureQualificationIntent,
  StructureQualificationPath,
  StructureStageHubSummary,
  StructureView,
} from '../../../types';
import {
  structureProgressionSortKey,
  structureProgressionSourceContext,
} from './structureConfrontationDisplay';
import { qualificationPathVolume } from './structurePopulationVolume';
import {
  intentFromApi,
  summarizeIntentWhoParts,
} from '../intents/structureQualificationDraft';
import {
  progressionEdgeKeyParts,
  progressionEdgeDestination,
  progressionEdges,
  progressionEdgeVolume,
  qualificationEdgeDestination,
  qualificationEdges,
  qualificationEdgeOrder,
  qualificationEdgeVolume,
  type ProgressionEdge,
  type QualificationEdge,
} from '../intents/structureStageQualProgProjection';

export type FeedFamily = 'place' | 'result';

export type FeedRow = {
  key: string;
  peerId: string;
  peerName: string;
  peerOrder: number;
  badge: string;
  badgeTone: 'win' | 'loss' | 'neutral' | 'accent';
  context: string;
  extra?: string;
  /** When true, badge is plain text (Sorties Qual intention summaries). */
  badgeAsText?: boolean;
  volume: number;
  family: FeedFamily;
  /** Place lower bound, or Winner=0 / Loser=1. */
  sortPrimary: number;
  /** Group name (alpha) or match number (numeric as string padded). */
  sortSecondary: string;
  qualPathOrder?: number;
  progressionPairKey?: string;
  progressionOutcome?: string;
};

export type FeedGroup = {
  peerId: string;
  peerName: string;
  peerOrder: number;
  volume: number;
  rules: {
    key: string;
    badge: string;
    badgeTone: 'win' | 'loss' | 'neutral' | 'accent';
    context: string;
    extra?: string;
    badgeAsText?: boolean;
    volume: number;
    family: FeedFamily;
    sortPrimary: number;
    sortSecondary: string;
    qualPathOrder?: number;
    progressionPairKey?: string;
    progressionOutcome?: string;
  }[];
};

export function selectionVolume(path: StructureQualificationPath): number {
  return qualificationPathVolume(path);
}

export function formatPlace(
  value: number,
  t: (key: string, opts?: Record<string, unknown>) => string,
): string {
  return value === 1
    ? t('fiche.rule.placeFirst')
    : t('fiche.rule.placeNth', { value });
}

function selectionModeBadge(
  mode: SelectionMode,
  value: number,
  t: (key: string, opts?: Record<string, unknown>) => string,
): string {
  switch (mode) {
    case 'Top':
      return t('fiche.rule.selectionTop', { value });
    case 'Bottom':
      return t('fiche.rule.selectionBottom', { value });
    case 'Best':
      return t('fiche.rule.selectionBest', { value });
    case 'Worst':
      return t('fiche.rule.selectionWorst', { value });
    default:
      return t('fiche.rule.topLike', { mode, value });
  }
}

function acrossGroupsScopeLabel(
  path: StructureQualificationPath,
  t: (key: string, opts?: Record<string, unknown>) => string,
): string {
  const placeRank = path.acrossGroupsPosition ?? 1;
  return t('qualification.summary.scopeAcrossPlace', {
    place: formatPlace(placeRank, t),
  });
}

export function qualificationRuleParts(
  path: StructureQualificationPath,
  t: (key: string, opts?: Record<string, unknown>) => string,
): Pick<
  FeedRow,
  | 'badge'
  | 'badgeTone'
  | 'context'
  | 'extra'
  | 'family'
  | 'sortPrimary'
  | 'sortSecondary'
> {
  const place = formatPlace(path.selectionValue, t);
  const group = path.groupName?.trim() ?? '';
  const mode = path.selectionMode as SelectionMode;
  const sortPrimary = path.selectionValue;
  const sortSecondary = group;
  const isAcross =
    path.rankingScope === 'AcrossGroups' || path.acrossGroupsPosition != null;
  // R1: rails never show Place/Population chips — only non-form extras (points gate).
  const extra =
    path.minimumPoints != null
      ? t('fiche.rule.minimumPoints', { n: path.minimumPoints })
      : undefined;

  if (mode === 'Range') {
    const from = formatPlace(path.selectionValue, t);
    const to = formatPlace(path.selectionEndValue ?? path.selectionValue, t);
    const badge = `${from}–${to}`;
    if (group) {
      return {
        badge,
        badgeTone: 'accent',
        context: t('fiche.rule.contextGroup', { group }),
        extra,
        family: 'place',
        sortPrimary,
        sortSecondary,
      };
    }
    if (isAcross) {
      return {
        badge,
        badgeTone: 'accent',
        context: acrossGroupsScopeLabel(path, t),
        extra,
        family: 'place',
        sortPrimary,
        sortSecondary: String(path.acrossGroupsPosition ?? ''),
      };
    }
    return {
      badge,
      badgeTone: 'accent',
      context: t('fiche.rule.contextOverall'),
      extra,
      family: 'place',
      sortPrimary,
      sortSecondary,
    };
  }

  if (mode === 'Position') {
    if (group) {
      return {
        badge: place,
        badgeTone: 'accent',
        context: t('fiche.rule.contextGroup', { group }),
        extra,
        family: 'place',
        sortPrimary,
        sortSecondary,
      };
    }
    if (isAcross) {
      return {
        badge: place,
        badgeTone: 'accent',
        context: acrossGroupsScopeLabel(path, t),
        extra,
        family: 'place',
        sortPrimary,
        sortSecondary: String(path.acrossGroupsPosition ?? ''),
      };
    }
    return {
      badge: place,
      badgeTone: 'accent',
      context: t('fiche.rule.contextOverall'),
      extra,
      family: 'place',
      sortPrimary,
      sortSecondary,
    };
  }

  const badge = selectionModeBadge(mode, path.selectionValue, t);
  if (group) {
    return {
      badge,
      badgeTone: 'accent',
      context: t('fiche.rule.contextGroup', { group }),
      extra,
      family: 'place',
      sortPrimary,
      sortSecondary,
    };
  }
  if (isAcross) {
    return {
      badge,
      badgeTone: 'accent',
      context: acrossGroupsScopeLabel(path, t),
      extra,
      family: 'place',
      sortPrimary,
      sortSecondary: String(path.acrossGroupsPosition ?? ''),
    };
  }
  return {
    badge,
    badgeTone: 'accent',
    context: t('fiche.rule.contextOverall'),
    extra,
    family: 'place',
    sortPrimary,
    sortSecondary,
  };
}

function progressionSourceContext(
  sourcePairKey: string | null | undefined,
  matchNumberInContainer: number | null | undefined,
  t: (key: string, opts?: Record<string, unknown>) => string,
): string {
  return structureProgressionSourceContext(
    sourcePairKey,
    matchNumberInContainer,
    (n) => t('fiche.rule.matchNumber', { n }),
    () => t('fiche.rule.matchUnknown'),
  );
}

function progressionSortKey(
  sourcePairKey: string | null | undefined,
  matchNumberInContainer: number | null | undefined,
): string {
  return structureProgressionSortKey(sourcePairKey, matchNumberInContainer);
}

function qualificationIntentRuleParts(
  intent: StructureQualificationIntent,
  locale: string,
  t: (key: string, opts?: Record<string, unknown>) => string,
): Pick<
  FeedRow,
  | 'badge'
  | 'badgeTone'
  | 'context'
  | 'extra'
  | 'family'
  | 'sortPrimary'
  | 'sortSecondary'
> {
  const draft = intentFromApi(intent);
  const { selection, scope } = summarizeIntentWhoParts(draft, locale, t);
  const extra =
    draft.conditionKind === 'points' &&
    Number.isFinite(Number(draft.minimumPoints))
      ? t('fiche.rule.minimumPoints', { n: Number(draft.minimumPoints) })
      : undefined;
  return {
    badge: selection || t('qualification.newPath'),
    badgeTone: 'accent',
    context: scope,
    extra,
    family: 'place',
    sortPrimary: intent.order,
    sortSecondary: scope,
  };
}

function progressionIntentRuleParts(
  intent: StructureProgressionIntent,
  t: (key: string, opts?: Record<string, unknown>) => string,
): Pick<
  FeedRow,
  | 'badge'
  | 'badgeTone'
  | 'context'
  | 'extra'
  | 'family'
  | 'sortPrimary'
  | 'sortSecondary'
> {
  const isWinner = intent.outcome === 'Winner';
  const volume = Math.max(1, intent.expandedPathCount ?? 1);
  const round =
    intent.roundName?.trim() ||
    (intent.roundId
      ? t('progression.roundFallback', { id: intent.roundId.slice(0, 8) })
      : '');
  return {
    badge: isWinner
      ? t('fiche.rule.winner', { count: volume })
      : t('fiche.rule.loser', { count: volume }),
    badgeTone: isWinner ? 'win' : 'loss',
    context: round,
    family: 'result',
    sortPrimary: isWinner ? 0 : 1,
    sortSecondary: `${String(intent.order).padStart(6, '0')}-${round}`,
  };
}

function qualificationEdgeRuleParts(
  edge: QualificationEdge,
  locale: string,
  t: (key: string, opts?: Record<string, unknown>) => string,
): Pick<
  FeedRow,
  | 'badge'
  | 'badgeTone'
  | 'context'
  | 'extra'
  | 'family'
  | 'sortPrimary'
  | 'sortSecondary'
  | 'badgeAsText'
> {
  const parts = qualificationIntentRuleParts(edge.intent, locale, t);
  return { ...parts, badgeAsText: true };
}

function progressionEdgeRuleParts(
  edge: ProgressionEdge,
  t: (key: string, opts?: Record<string, unknown>) => string,
): Pick<
  FeedRow,
  | 'badge'
  | 'badgeTone'
  | 'context'
  | 'extra'
  | 'family'
  | 'sortPrimary'
  | 'sortSecondary'
> {
  return progressionIntentRuleParts(edge.intent, t);
}

export function progressionRuleParts(
  path: StructureProgressionPath,
  t: (key: string, opts?: Record<string, unknown>) => string,
): Pick<
  FeedRow,
  | 'badge'
  | 'badgeTone'
  | 'context'
  | 'extra'
  | 'family'
  | 'sortPrimary'
  | 'sortSecondary'
> {
  const isWinner = path.outcome === 'Winner';
  // R1: destination Place | Population lives on the schematic, not the rail.
  return {
    badge: isWinner
      ? t('fiche.rule.winner', { count: 1 })
      : t('fiche.rule.loser', { count: 1 }),
    badgeTone: isWinner ? 'win' : 'loss',
    context: progressionSourceContext(
      path.sourcePairKey,
      path.matchNumberInContainer,
      t,
    ),
    family: 'result',
    sortPrimary: isWinner ? 0 : 1,
    sortSecondary: progressionSortKey(
      path.sourcePairKey,
      path.matchNumberInContainer,
    ),
  };
}

export function placementRuleParts(
  award: StructurePlacementAward,
  t: (key: string, opts?: Record<string, unknown>) => string,
): {
  rank: number;
  medal: 'gold' | 'silver' | 'bronze' | null;
  badge: string;
  badgeTone: 'win' | 'loss';
  context: string;
} {
  const isWinner = award.outcome === 'Winner';
  return {
    rank: award.rank,
    medal:
      award.rank === 1
        ? 'gold'
        : award.rank === 2
          ? 'silver'
          : award.rank === 3
            ? 'bronze'
            : null,
    badge: isWinner
      ? t('fiche.rule.winner', { count: 1 })
      : t('fiche.rule.loser', { count: 1 }),
    badgeTone: isWinner ? 'win' : 'loss',
    context: progressionSourceContext(
      award.sourcePairKey ?? undefined,
      award.matchNumberInContainer,
      t,
    ),
  };
}

function stagePeerOrder(data: StructureView, stageId: string): number {
  const index = data.stages.findIndex((s) => s.stageId === stageId);
  return index >= 0 ? index : Number.MAX_SAFE_INTEGER;
}

export function compareFeedRules(
  a: Pick<FeedRow, 'family' | 'sortPrimary' | 'sortSecondary'>,
  b: Pick<FeedRow, 'family' | 'sortPrimary' | 'sortSecondary'>,
): number {
  const familyOrder = (f: FeedFamily) => (f === 'place' ? 0 : 1);
  const byFamily = familyOrder(a.family) - familyOrder(b.family);
  if (byFamily !== 0) return byFamily;
  if (a.sortPrimary !== b.sortPrimary) return a.sortPrimary - b.sortPrimary;
  return a.sortSecondary.localeCompare(b.sortSecondary, undefined, {
    numeric: true,
    sensitivity: 'base',
  });
}

export function inboundFeeds(
  data: StructureView,
  stageId: string,
  locale: string,
  t: (key: string, opts?: Record<string, unknown>) => string,
) {
  const feeds: FeedRow[] = [];

  for (const source of data.stages) {
    for (const edge of qualificationEdges(source)) {
      if (qualificationEdgeDestination(edge) !== stageId) continue;
      const parts = qualificationEdgeRuleParts(edge, locale, t);
      const order = qualificationEdgeOrder(edge);
      feeds.push({
        key: `q-i-${source.stageId}-${edge.intent.intentId}`,
        peerId: source.stageId,
        peerName: source.name,
        peerOrder: stagePeerOrder(data, source.stageId),
        ...parts,
        volume: qualificationEdgeVolume(edge),
        qualPathOrder: order,
      });
    }
    for (const edge of progressionEdges(source)) {
      if (progressionEdgeDestination(edge) !== stageId) continue;
      const parts = progressionEdgeRuleParts(edge, t);
      const keyParts = progressionEdgeKeyParts(edge);
      feeds.push({
        key: `p-i-${source.stageId}-${edge.intent.intentId}`,
        peerId: source.stageId,
        peerName: source.name,
        peerOrder: stagePeerOrder(data, source.stageId),
        ...parts,
        volume: progressionEdgeVolume(edge),
        progressionPairKey: keyParts.sourcePairKey,
        progressionOutcome: keyParts.outcome,
      });
    }
  }
  return feeds;
}

export function outboundFeeds(
  data: StructureView,
  stage: StructureStageHubSummary,
  locale: string,
  t: (key: string, opts?: Record<string, unknown>) => string,
) {
  const feeds: FeedRow[] = [];
  const nameOf = (id: string) =>
    data.stages.find((s) => s.stageId === id)?.name ?? id;

  for (const edge of qualificationEdges(stage)) {
    const destId = qualificationEdgeDestination(edge);
    const parts = qualificationEdgeRuleParts(edge, locale, t);
    const order = qualificationEdgeOrder(edge);
    feeds.push({
      key: `q-out-i-${edge.intent.intentId}`,
      peerId: destId,
      peerName: nameOf(destId),
      peerOrder: stagePeerOrder(data, destId),
      ...parts,
      volume: qualificationEdgeVolume(edge),
      qualPathOrder: order,
    });
  }
  for (const edge of progressionEdges(stage)) {
    const destId = progressionEdgeDestination(edge);
    const parts = progressionEdgeRuleParts(edge, t);
    const keyParts = progressionEdgeKeyParts(edge);
    feeds.push({
      key: `p-out-i-${edge.intent.intentId}`,
      peerId: destId,
      peerName: nameOf(destId),
      peerOrder: stagePeerOrder(data, destId),
      ...parts,
      volume: progressionEdgeVolume(edge),
      progressionPairKey: keyParts.sourcePairKey,
      progressionOutcome: keyParts.outcome,
    });
  }
  return feeds;
}

export function groupFeeds(feeds: FeedRow[]): FeedGroup[] {
  const map = new Map<string, FeedGroup>();
  for (const feed of feeds) {
    const existing = map.get(feed.peerId);
    if (existing) {
      existing.volume += feed.volume;
      existing.rules.push({
        key: feed.key,
        badge: feed.badge,
        badgeTone: feed.badgeTone,
        context: feed.context,
        extra: feed.extra,
        badgeAsText: feed.badgeAsText,
        volume: feed.volume,
        family: feed.family,
        sortPrimary: feed.sortPrimary,
        sortSecondary: feed.sortSecondary,
        qualPathOrder: feed.qualPathOrder,
        progressionPairKey: feed.progressionPairKey,
        progressionOutcome: feed.progressionOutcome,
      });
    } else {
      map.set(feed.peerId, {
        peerId: feed.peerId,
        peerName: feed.peerName,
        peerOrder: feed.peerOrder,
        volume: feed.volume,
        rules: [
          {
            key: feed.key,
            badge: feed.badge,
            badgeTone: feed.badgeTone,
            context: feed.context,
            extra: feed.extra,
            badgeAsText: feed.badgeAsText,
            volume: feed.volume,
            family: feed.family,
            sortPrimary: feed.sortPrimary,
            sortSecondary: feed.sortSecondary,
            qualPathOrder: feed.qualPathOrder,
            progressionPairKey: feed.progressionPairKey,
            progressionOutcome: feed.progressionOutcome,
          },
        ],
      });
    }
  }

  const groups = [...map.values()];
  for (const group of groups) {
    group.rules.sort(compareFeedRules);
  }
  groups.sort((a, b) => {
    if (a.peerOrder !== b.peerOrder) return a.peerOrder - b.peerOrder;
    return a.peerName.localeCompare(b.peerName, undefined, {
      sensitivity: 'base',
    });
  });
  return groups;
}

export type ExitKind = 'qualification' | 'progression';

export function exitKindsPresent(group: FeedGroup): Set<ExitKind> {
  const kinds = new Set<ExitKind>();
  for (const rule of group.rules) {
    kinds.add(rule.family === 'place' ? 'qualification' : 'progression');
  }
  return kinds;
}

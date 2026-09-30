// -----------------------------------------------------------------------
// Structure phase fiche — inbound/outbound Expand path feeds (Sorties rails).
// Pure helpers; UI lives in StructurePhaseRails.tsx.
// -----------------------------------------------------------------------

import type {
  SelectionMode,
  StructurePlacementAward,
  StructureProgressionPath,
  StructureQualificationPath,
  StructureStageHubSummary,
  StructureView,
} from '../types';
import { qualificationPathVolume } from './structurePopulationVolume';

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

/** Structural PairKey primary; Match # is an optional post-materialize overlay. */
function progressionSourceContext(
  sourcePairKey: string | null | undefined,
  sourceLabel: string | null | undefined,
  t: (key: string, opts?: Record<string, unknown>) => string,
): string {
  const pair = sourcePairKey?.trim();
  const fromLabel = sourceLabel?.match(/#(\d+)/);
  if (pair && fromLabel?.[1]) {
    return `${pair} · ${t('fiche.rule.matchNumber', { n: fromLabel[1] })}`;
  }
  if (pair) {
    return pair;
  }
  return matchNumberContext(sourceLabel, sourcePairKey, t);
}

function progressionSortKey(
  sourcePairKey: string | null | undefined,
  sourceLabel: string | null | undefined,
): string {
  const pair = sourcePairKey?.trim();
  if (pair) {
    const fromLabel = sourceLabel?.match(/#(\d+)/);
    return fromLabel?.[1] ? `${pair}-${fromLabel[1].padStart(8, '0')}` : pair;
  }
  return matchSortKey(sourceLabel, sourcePairKey);
}

/** M2: always Match #n — phase name already shown by flux grouping. */
function matchNumberContext(
  sourceLabel: string | null | undefined,
  fixtureId: string | null | undefined,
  t: (key: string, opts?: Record<string, unknown>) => string,
): string {
  const fromLabel = sourceLabel?.match(/#(\d+)/);
  if (fromLabel?.[1]) {
    return t('fiche.rule.matchNumber', { n: fromLabel[1] });
  }
  if (fixtureId) {
    return t('fiche.rule.matchFallback', { id: fixtureId.slice(0, 8) });
  }
  return t('fiche.rule.matchUnknown');
}

function matchSortKey(
  sourceLabel: string | null | undefined,
  fixtureId: string | null | undefined,
): string {
  const fromLabel = sourceLabel?.match(/#(\d+)/);
  if (fromLabel?.[1]) {
    return fromLabel[1].padStart(8, '0');
  }
  return fixtureId ?? '';
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
    context: progressionSourceContext(path.sourcePairKey, path.sourceLabel, t),
    family: 'result',
    sortPrimary: isWinner ? 0 : 1,
    sortSecondary: progressionSortKey(path.sourcePairKey, path.sourceLabel),
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
      award.sourceLabel,
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
  t: (key: string, opts?: Record<string, unknown>) => string,
) {
  const feeds: FeedRow[] = [];

  for (const source of data.stages) {
    for (const path of source.qualificationPaths ?? []) {
      if (path.destinationStageId !== stageId) continue;
      const parts = qualificationRuleParts(path, t);
      feeds.push({
        key: `q-${source.stageId}-${path.order}`,
        peerId: source.stageId,
        peerName: source.name,
        peerOrder: stagePeerOrder(data, source.stageId),
        ...parts,
        volume: selectionVolume(path),
      });
    }
    for (const path of source.progressionPaths ?? []) {
      if (path.destinationStageId !== stageId) continue;
      const parts = progressionRuleParts(path, t);
      feeds.push({
        key: `p-${source.stageId}-${path.sourcePairKey}-${path.outcome}`,
        peerId: source.stageId,
        peerName: source.name,
        peerOrder: stagePeerOrder(data, source.stageId),
        ...parts,
        volume: 1,
      });
    }
  }
  return feeds;
}

export function outboundFeeds(
  data: StructureView,
  stage: StructureStageHubSummary,
  t: (key: string, opts?: Record<string, unknown>) => string,
) {
  const feeds: FeedRow[] = [];
  const nameOf = (id: string) =>
    data.stages.find((s) => s.stageId === id)?.name ?? id;

  for (const path of stage.qualificationPaths ?? []) {
    const parts = qualificationRuleParts(path, t);
    feeds.push({
      key: `q-out-${path.order}-${path.destinationStageId}`,
      peerId: path.destinationStageId,
      peerName: nameOf(path.destinationStageId),
      peerOrder: stagePeerOrder(data, path.destinationStageId),
      ...parts,
      volume: selectionVolume(path),
    });
  }
  for (const path of stage.progressionPaths ?? []) {
    const parts = progressionRuleParts(path, t);
    feeds.push({
      key: `p-out-${path.sourcePairKey}-${path.outcome}-${path.destinationStageId}`,
      peerId: path.destinationStageId,
      peerName: nameOf(path.destinationStageId),
      peerOrder: stagePeerOrder(data, path.destinationStageId),
      ...parts,
      volume: 1,
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

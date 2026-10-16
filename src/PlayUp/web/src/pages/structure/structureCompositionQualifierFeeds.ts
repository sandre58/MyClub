// -----------------------------------------------------------------------
// Composition Qualifiés — path-aligned feed rows from hub inboundComposition only.
// -----------------------------------------------------------------------

import type {
  StructureInboundComposition,
  StructureInboundFeedRule,
  StructureProgressionPath,
  StructureQualificationPath,
  StructureView,
} from '../../types';
import {
  compareFeedRules,
  type FeedGroup,
  progressionRuleParts,
  qualificationRuleParts,
} from './structurePhaseFeeds';
import { inboundFeedSeatsFromHub } from './structureCompositionInboundHub';

export type CompositionQualifierRule = FeedGroup['rules'][number] & {
  hubSeats?: ReturnType<typeof inboundFeedSeatsFromHub>;
};

export type CompositionQualifierFeedGroup = Omit<FeedGroup, 'rules'> & {
  rules: CompositionQualifierRule[];
};

export function compositionQualifierFeedGroups(input: {
  data: StructureView;
  inbound: StructureInboundComposition | null | undefined;
  t: (key: string, opts?: Record<string, unknown>) => string;
  /** Intent-based groups when hub path projection is missing (legacy). */
  intentFallbackGroups: FeedGroup[];
}): CompositionQualifierFeedGroup[] {
  const { data, inbound, t, intentFallbackGroups } = input;
  if (!inbound?.rules.length) {
    return [];
  }

  const intentRuleByHubKey = buildIntentRuleLookup(intentFallbackGroups);
  const map = new Map<string, CompositionQualifierFeedGroup>();

  for (const rule of inbound.rules) {
    const peer = data.stages.find((s) => s.stageId === rule.sourceStageId);
    const peerName = peer?.name ?? rule.sourceStageId;
    const peerOrder = stagePeerOrder(data, rule.sourceStageId);
    const labelParts = labelPartsForInboundRule(
      rule,
      resolveIntentRule(intentRuleByHubKey, rule),
      t,
    );
    const hubSeats = inboundFeedSeatsFromHub(rule.seats);
    const volume = Math.max(1, hubSeats.length);

    const feedRule: CompositionQualifierRule = {
      key: rule.feedKey,
      volume,
      hubSeats,
      ...labelParts,
    };

    const existing = map.get(rule.sourceStageId);
    if (existing) {
      existing.volume += volume;
      existing.rules.push(feedRule);
    } else {
      map.set(rule.sourceStageId, {
        peerId: rule.sourceStageId,
        peerName,
        peerOrder,
        volume,
        rules: [feedRule],
      });
    }
  }

  const groups = [...map.values()];
  for (const group of groups) {
    group.rules.sort(compareFeedRules);
  }
  groups.sort((a, b) => {
    if (a.peerOrder !== b.peerOrder) return a.peerOrder - b.peerOrder;
    return a.peerName.localeCompare(b.peerName, undefined, { sensitivity: 'base' });
  });
  return groups;
}

function buildIntentRuleLookup(
  groups: FeedGroup[],
): Map<string, FeedGroup['rules'][number]> {
  const map = new Map<string, FeedGroup['rules'][number]>();
  for (const group of groups) {
    for (const rule of group.rules) {
      map.set(inboundHubKeyFromIntentRule(group.peerId, rule), rule);
    }
  }
  return map;
}

function resolveIntentRule(
  intentRuleByHubKey: Map<string, FeedGroup['rules'][number]>,
  rule: StructureInboundFeedRule,
): FeedGroup['rules'][number] | undefined {
  const direct = intentRuleByHubKey.get(rule.feedKey);
  if (direct) {
    return direct;
  }
  const order = rule.qualificationPath?.order;
  if (order != null) {
    return intentRuleByHubKey.get(`q-${rule.sourceStageId}-${order}`);
  }
  return undefined;
}

function inboundHubKeyFromIntentRule(
  sourceStageId: string,
  rule: FeedGroup['rules'][number],
): string {
  if (rule.qualPathOrder != null) {
    return `q-${sourceStageId}-${rule.qualPathOrder}`;
  }
  const pairKey = rule.progressionPairKey?.trim();
  const outcome = rule.progressionOutcome?.trim();
  if (pairKey && outcome) {
    return `p-${sourceStageId}-${pairKey}-${outcome}`;
  }
  return rule.key;
}

function labelPartsForInboundRule(
  rule: StructureInboundFeedRule,
  intentRule: FeedGroup['rules'][number] | undefined,
  t: (key: string, opts?: Record<string, unknown>) => string,
): Pick<
  CompositionQualifierRule,
  | 'badge'
  | 'badgeTone'
  | 'context'
  | 'extra'
  | 'badgeAsText'
  | 'family'
  | 'sortPrimary'
  | 'sortSecondary'
  | 'qualPathOrder'
  | 'progressionPairKey'
  | 'progressionOutcome'
> {
  const qualPath = inboundQualificationPath(rule.qualificationPath);
  if (qualPath) {
    const parts = qualificationRuleParts(qualPath, t);
    return {
      ...parts,
      badgeAsText: true,
      qualPathOrder: qualPath.order,
    };
  }

  const progPath = inboundProgressionPath(rule.progressionPath);
  if (progPath) {
    const parts = progressionRuleParts(progPath, t);
    return {
      ...parts,
      progressionPairKey: progPath.sourcePairKey,
      progressionOutcome: progPath.outcome,
    };
  }

  if (intentRule) {
    return {
      badge: intentRule.badge,
      badgeTone: intentRule.badgeTone,
      context: intentRule.context,
      extra: intentRule.extra,
      badgeAsText: intentRule.badgeAsText,
      family: intentRule.family,
      sortPrimary: intentRule.sortPrimary,
      sortSecondary: intentRule.sortSecondary,
      qualPathOrder: intentRule.qualPathOrder,
      progressionPairKey: intentRule.progressionPairKey,
      progressionOutcome: intentRule.progressionOutcome,
    };
  }

  return {
    badge: t('composition.qualifiers.pathPending'),
    badgeTone: 'neutral',
    context: '',
    family: 'place',
    sortPrimary: 0,
    sortSecondary: rule.feedKey,
    badgeAsText: true,
  };
}

function inboundQualificationPath(
  path: StructureInboundFeedRule['qualificationPath'],
): StructureQualificationPath | null {
  if (!path) {
    return null;
  }
  return {
    order: path.order,
    selectionMode: path.selectionMode,
    selectionValue: path.selectionValue,
    selectionEndValue: path.selectionEndValue ?? null,
    destinationStageId: path.destinationStageId,
    destinationSlotKey: path.destinationSlotKey ?? null,
    destinationGroupId: path.destinationGroupId ?? null,
    destinationForm: path.destinationForm ?? null,
    rankingScope: path.rankingScope ?? null,
    groupId: path.groupId ?? null,
    groupName: path.groupName ?? null,
    acrossGroupsPosition: path.acrossGroupsPosition ?? null,
    minimumPoints: path.minimumPoints ?? null,
  };
}

function inboundProgressionPath(
  path: StructureInboundFeedRule['progressionPath'],
): StructureProgressionPath | null {
  if (!path) {
    return null;
  }
  return {
    sourcePairKey: path.sourcePairKey,
    outcome: path.outcome,
    destinationStageId: path.destinationStageId,
    destinationSlotKey: path.destinationSlotKey ?? null,
    destinationGroupId: path.destinationGroupId ?? null,
    destinationForm: path.destinationForm ?? null,
    sourceLabel: path.sourceLabel ?? null,
  };
}

function stagePeerOrder(data: StructureView, stageId: string): number {
  const index = data.stages.findIndex((s) => s.stageId === stageId);
  return index >= 0 ? index : Number.MAX_SAFE_INTEGER;
}


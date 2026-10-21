// Composition Équipes — merge inbound qual/prog seats with runtime pool (exploitation / historical).
import type { StructureEntry, StructureInboundComposition } from '../../types';
import { inboundSeatsForFeedRule } from './structureCompositionInboundHub';
import type {
  CompositionQualifierFeedGroup,
  CompositionQualifierRule,
} from './structureCompositionQualifierFeeds';
import type { StructureStageHubSummary } from '../../types';
import {
  listEngagedCompositionEntries,
  resolveEngagedPoolResolution,
  type CompositionTileMode,
} from './compositionTilePresentation';
import type { ChipTone } from '../../design-system/components/Chip';
import {
  compactCompositionProvenanceBadge,
  fullInterPhaseProvenanceLabel,
  progressionOutcomeFromRule,
} from './compositionTeamProvenanceLabel';

export type CompositionTeamProvenance = {
  full: string;
  badge: string;
  chipTone?: ChipTone;
  progressionOutcome?: 'Winner' | 'Loser';
};

export type CompositionTeamRow =
  | {
      key: string;
      kind: 'resolved';
      entry: StructureEntry;
      provenance: CompositionTeamProvenance | null;
    }
  | {
      key: string;
      kind: 'unresolved';
      provenance: CompositionTeamProvenance;
    };

export function provenanceLabelsForQualifierRule(
  group: CompositionQualifierFeedGroup,
  rule: CompositionQualifierRule,
): CompositionTeamProvenance {
  const full = fullInterPhaseProvenanceLabel(group.peerName, rule);
  const badge = compactCompositionProvenanceBadge(group.peerName, rule);
  const progressionOutcome = progressionOutcomeFromRule(rule);
  const chipTone =
    progressionOutcome === 'Loser'
      ? 'loss'
      : progressionOutcome === 'Winner'
        ? 'win'
        : 'soft';
  return {
    full,
    badge,
    chipTone,
    progressionOutcome: progressionOutcome ?? undefined,
  };
}

export function inboundEntryProvenanceByEntryId(
  groups: readonly CompositionQualifierFeedGroup[],
): ReadonlyMap<string, CompositionTeamProvenance> {
  const map = new Map<string, CompositionTeamProvenance>();
  for (const group of groups) {
    for (const rule of group.rules) {
      const labels = provenanceLabelsForQualifierRule(group, rule);
      for (const seat of rule.hubSeats ?? []) {
        const entryId = seat.entryId?.trim();
        if (entryId) {
          map.set(entryId, labels);
        }
      }
    }
  }
  return map;
}

export function listCompositionTeamRows(
  mode: CompositionTileMode,
  stage: StructureStageHubSummary,
  entries: readonly StructureEntry[],
  groups: readonly CompositionQualifierFeedGroup[],
  inbound: StructureInboundComposition | null | undefined,
): CompositionTeamRow[] | null {
  if (mode === 'prep' || groups.length === 0) {
    return null;
  }

  const rows: CompositionTeamRow[] = [];
  const seenEntryIds = new Set<string>();

  for (const group of groups) {
    for (const rule of group.rules) {
      const provenance = provenanceLabelsForQualifierRule(group, rule);
      const seats =
        rule.hubSeats ??
        inboundSeatsForFeedRule(
          inbound,
          {
            uiFeedKey: rule.key,
            sourceStageId: group.peerId,
            qualPathOrder: rule.qualPathOrder,
            progressionPairKey: rule.progressionPairKey,
            progressionOutcome: rule.progressionOutcome,
          },
          rule.volume,
        );
      seats.forEach((seat, seatIndex) => {
        const key = `${rule.key}-${seatIndex}`;
        if (seat.resolved && seat.entryId) {
          const entry = entries.find((e) => e.entryId === seat.entryId);
          if (entry) {
            seenEntryIds.add(entry.entryId);
            rows.push({
              key,
              kind: 'resolved',
              entry,
              provenance,
            });
          } else {
            rows.push({ key, kind: 'unresolved', provenance });
          }
        } else {
          rows.push({ key, kind: 'unresolved', provenance });
        }
      });
    }
  }

  if (resolveEngagedPoolResolution(stage).kind !== 'undetermined') {
    for (const entry of listEngagedCompositionEntries(stage, entries)) {
      if (seenEntryIds.has(entry.entryId)) {
        continue;
      }
      seenEntryIds.add(entry.entryId);
      const fromMap = inboundEntryProvenanceByEntryId(groups).get(
        entry.entryId,
      );
      rows.push({
        key: entry.entryId,
        kind: 'resolved',
        entry,
        provenance: fromMap ?? null,
      });
    }
  }

  return rows;
}

export function compositionTeamRowsResolutionStats(
  rows: CompositionTeamRow[] | null | undefined,
): { resolved: number; total: number } | null {
  if (!rows?.length) {
    return null;
  }
  let resolved = 0;
  let total = 0;
  for (const row of rows) {
    total += 1;
    if (row.kind === 'resolved') {
      resolved += 1;
    }
  }
  return { resolved, total };
}

/** Exploitation / historical composition band — always « Résolues » when total > 0. */
export function resolveCompositionResolvedGaugeStats(
  mode: CompositionTileMode,
  teamRows: CompositionTeamRow[] | null | undefined,
  engagedPoolFallback: { resolved: number; capacity: number } | null,
): { resolved: number; total: number } | null {
  if (mode === 'prep') {
    return null;
  }
  const fromInbound = compositionTeamRowsResolutionStats(teamRows);
  if (fromInbound != null && fromInbound.total > 0) {
    return fromInbound;
  }
  if (engagedPoolFallback != null && engagedPoolFallback.capacity > 0) {
    return {
      resolved: Math.min(
        engagedPoolFallback.resolved,
        engagedPoolFallback.capacity,
      ),
      total: engagedPoolFallback.capacity,
    };
  }
  return null;
}

export function hasCompositionTeamsInboundMerge(
  mode: CompositionTileMode,
  groups: readonly CompositionQualifierFeedGroup[],
): boolean {
  return mode !== 'prep' && groups.length > 0;
}

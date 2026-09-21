// -----------------------------------------------------------------------
// Qualification Intent drafts — Expand client preview (Qual population|place).
// -----------------------------------------------------------------------

import type {
  QualificationIntentSourceKind,
  RankingScope,
  StructureQualificationIntent,
  StructureQualificationPath,
} from '../types';
import { isPopulationDestination } from './structureProgression';
import {
  coerceDestinationSlotKeys,
  countEmptyPlaceSlots,
  fillEmptyPlaceSlotKeys,
  placeMappingGap,
  resizeDestinationSlotKeys,
} from './structurePlaceMapping';

export {
  coerceDestinationSlotKeys,
  fillEmptyPlaceSlotKeys,
  resizeDestinationSlotKeys,
} from './structurePlaceMapping';

export type QualTargetKind = 'population' | 'place';

export type QualIntentDraft = {
  id: string;
  sourceKind: QualificationIntentSourceKind;
  groupId: string;
  groupName: string;
  positionFrom: string;
  positionTo: string;
  acrossGroupsPosition: string;
  conditionKind: 'none' | 'points';
  minimumPoints: string;
  targetKind: QualTargetKind;
  destinationStageId: string;
  /**
   * Place slot keys aligned with expandOccurrences order (index ↔ key).
   * Empty array for Population. Length should match Expand count for Place.
   */
  destinationSlotKeys: string[];
};

export type SourceOccurrence = {
  scope: RankingScope;
  position: number;
  groupId?: string | null;
  groupName?: string | null;
  acrossGroupsPosition?: number | null;
};

export type QualIncompleteReason =
  | 'Destination'
  | 'Group'
  | 'Selection'
  | 'Points'
  | 'PlaceUnavailable'
  | 'MultiSlot'
  | 'DuplicateSlot'
  | 'DuplicatePlace';

export function newIntentId(): string {
  return crypto.randomUUID();
}

/** Count empty Place slots across intents (after Expand-aligned resize). */
export function countUnmappedPlaceSlots(
  intents: QualIntentDraft[],
  groups: { id: string; name: string }[],
): number {
  let n = 0;
  for (const intent of intents) {
    if (intent.targetKind !== 'place') continue;
    const occ = expandOccurrences(intent, groups);
    n += countEmptyPlaceSlots(intent.destinationSlotKeys, occ.length);
  }
  return n;
}

/** Keep Place keys aligned with current Expand occurrence count. */
export function syncPlaceSlotKeys(
  draft: QualIntentDraft,
  groups: { id: string; name: string }[],
): QualIntentDraft {
  if (draft.targetKind !== 'place') {
    if (draft.destinationSlotKeys.length === 0) return draft;
    return { ...draft, destinationSlotKeys: [] };
  }
  const n = expandOccurrences(draft, groups).length;
  const next = resizeDestinationSlotKeys(draft.destinationSlotKeys, n);
  if (
    next.length === draft.destinationSlotKeys.length &&
    next.every((k, i) => k === draft.destinationSlotKeys[i])
  ) {
    return draft;
  }
  return { ...draft, destinationSlotKeys: next };
}

export function emptyQualIntent(
  destinationStageId = '',
  targetKind: QualTargetKind = 'population',
): QualIntentDraft {
  return {
    id: newIntentId(),
    sourceKind: 'EachGroup',
    groupId: '',
    groupName: '',
    positionFrom: '1',
    positionTo: '1',
    acrossGroupsPosition: '1',
    conditionKind: 'none',
    minimumPoints: '',
    targetKind,
    destinationStageId,
    destinationSlotKeys: [],
  };
}

export function intentFromApi(
  intent: StructureQualificationIntent,
): QualIntentDraft {
  const keys = coerceDestinationSlotKeys(
    intent.destinationSlotKeys,
    intent.destinationSlotKey,
  );
  const population = keys.length === 0;
  return {
    id: intent.intentId,
    sourceKind: intent.sourceKind,
    groupId: intent.groupId ?? '',
    groupName: intent.groupName ?? '',
    positionFrom: String(intent.positionFrom),
    positionTo: String(intent.positionTo),
    acrossGroupsPosition:
      intent.acrossGroupsPosition != null
        ? String(intent.acrossGroupsPosition)
        : '1',
    conditionKind:
      intent.minimumPoints != null && intent.minimumPoints >= 0
        ? 'points'
        : 'none',
    minimumPoints:
      intent.minimumPoints != null ? String(intent.minimumPoints) : '',
    targetKind: population ? 'population' : 'place',
    destinationStageId: intent.destinationStageId,
    destinationSlotKeys: population ? [] : keys,
  };
}

/** Legacy fallback: one singleton intent per path. */
export function pathToSingletonIntent(
  path: StructureQualificationPath,
): QualIntentDraft {
  const scope = path.rankingScope;
  let sourceKind: QualificationIntentSourceKind = 'Overall';
  if (scope === 'AcrossGroups' || path.acrossGroupsPosition != null) {
    sourceKind = 'AcrossGroups';
  } else if (scope === 'Group' || path.groupId) {
    sourceKind = 'SingleGroup';
  }

  const k = path.selectionValue;
  const population = isPopulationDestination(path.destinationSlotKey);
  return {
    id: newIntentId(),
    sourceKind,
    groupId: path.groupId ?? '',
    groupName: path.groupName ?? '',
    positionFrom: String(k),
    positionTo: String(k),
    acrossGroupsPosition:
      path.acrossGroupsPosition != null
        ? String(path.acrossGroupsPosition)
        : '1',
    conditionKind:
      path.minimumPoints != null && path.minimumPoints >= 0 ? 'points' : 'none',
    minimumPoints:
      path.minimumPoints != null ? String(path.minimumPoints) : '',
    targetKind: population ? 'population' : 'place',
    destinationStageId: path.destinationStageId,
    destinationSlotKeys: population
      ? []
      : coerceDestinationSlotKeys(null, path.destinationSlotKey),
  };
}

export function parsePositiveInt(raw: string): number | null {
  const n = Number(raw);
  if (!Number.isInteger(n) || n < 1) return null;
  return n;
}

export function expandOccurrences(
  draft: QualIntentDraft,
  groups: { id: string; name: string }[],
): SourceOccurrence[] {
  const from = parsePositiveInt(draft.positionFrom);
  const to = parsePositiveInt(draft.positionTo);
  if (from == null || to == null || to < from) return [];

  const list: SourceOccurrence[] = [];
  if (draft.sourceKind === 'EachGroup') {
    for (const g of groups) {
      for (let k = from; k <= to; k++) {
        list.push({
          scope: 'Group',
          position: k,
          groupId: g.id,
          groupName: g.name,
        });
      }
    }
  } else if (draft.sourceKind === 'SingleGroup') {
    if (!draft.groupId.trim()) return [];
    for (let k = from; k <= to; k++) {
      list.push({
        scope: 'Group',
        position: k,
        groupId: draft.groupId,
        groupName: draft.groupName || draft.groupId,
      });
    }
  } else if (draft.sourceKind === 'Overall') {
    for (let k = from; k <= to; k++) {
      list.push({ scope: 'Overall', position: k });
    }
  } else {
    const p = parsePositiveInt(draft.acrossGroupsPosition);
    if (p == null) return [];
    for (let k = from; k <= to; k++) {
      list.push({
        scope: 'AcrossGroups',
        position: k,
        acrossGroupsPosition: p,
      });
    }
  }
  return list;
}

export function occurrenceLabel(
  o: SourceOccurrence,
  ordinal: (n: number) => string,
  t: (key: string, opts?: Record<string, unknown>) => string,
): string {
  const rank = ordinal(o.position);
  if (o.scope === 'Group') {
    return t('qualification.summary.group', {
      rank,
      group: o.groupName || t('qualification.unnamedGroup'),
    });
  }
  if (o.scope === 'AcrossGroups') {
    const place = ordinal(o.acrossGroupsPosition ?? 1);
    return o.position <= 1
      ? t('qualification.summary.acrossBest', { place })
      : t('qualification.summary.acrossNth', { rank, place });
  }
  return t('qualification.summary.overall', { rank });
}

export function summarizeIntentWho(
  draft: QualIntentDraft,
  locale: string,
  t: (key: string, opts?: Record<string, unknown>) => string,
): string {
  let who = summarizeIntentWhoLegacy(draft, locale, t);

  if (draft.conditionKind === 'points') {
    const pts = Number(draft.minimumPoints);
    if (Number.isFinite(pts)) {
      who = t('qualification.summary.withPoints', { who, points: pts });
    }
  }
  return who;
}

/**
 * Rail-friendly split: selection chip + scope context
 * (e.g. "1er et 2e" + "Chaque groupe").
 */
export function summarizeIntentWhoParts(
  draft: QualIntentDraft,
  locale: string,
  t: (key: string, opts?: Record<string, unknown>) => string,
): { selection: string; scope: string } {
  const from = parsePositiveInt(draft.positionFrom) ?? 0;
  const to = parsePositiveInt(draft.positionTo) ?? 0;
  const ord = (n: number) => ordinalRank(Math.max(n, 1), locale);
  const selection = formatSelectionBadge(from, to, ord, t);

  if (draft.sourceKind === 'EachGroup') {
    return { selection, scope: t('qualification.summary.scopeEachGroup') };
  }
  if (draft.sourceKind === 'SingleGroup') {
    const group =
      draft.groupName.trim() || t('qualification.unnamedGroup');
    return {
      selection,
      scope: t('qualification.summary.scopeGroup', { group }),
    };
  }
  if (draft.sourceKind === 'Overall') {
    return { selection, scope: t('qualification.summary.scopeOverall') };
  }

  const place = ord(parsePositiveInt(draft.acrossGroupsPosition) ?? 1);
  return {
    selection,
    scope: t('qualification.summary.scopeAcrossPlace', { place }),
  };
}

function formatSelectionBadge(
  from: number,
  to: number,
  ord: (n: number) => string,
  t: (key: string, opts?: Record<string, unknown>) => string,
): string {
  if (from === to) {
    return ord(from);
  }
  if (to === from + 1) {
    return t('qualification.summary.selectionPair', {
      from: ord(from),
      to: ord(to),
    });
  }
  return t('qualification.summary.selectionRange', {
    from: ord(from),
    to: ord(to),
  });
}

function summarizeIntentWhoLegacy(
  draft: QualIntentDraft,
  locale: string,
  t: (key: string, opts?: Record<string, unknown>) => string,
): string {
  const from = parsePositiveInt(draft.positionFrom) ?? 0;
  const to = parsePositiveInt(draft.positionTo) ?? 0;
  const ord = (n: number) => ordinalRank(Math.max(n, 1), locale);

  if (draft.sourceKind === 'EachGroup') {
    return from === to
      ? t('qualification.summary.eachGroupOne', { rank: ord(from) })
      : to === from + 1
        ? t('qualification.summary.eachGroupRange', {
            from: ord(from),
            to: ord(to),
          })
        : t('qualification.summary.eachGroupSpan', {
            from: ord(from),
            to: ord(to),
          });
  }
  if (draft.sourceKind === 'SingleGroup') {
    const group =
      draft.groupName.trim() || t('qualification.unnamedGroup');
    return from === to
      ? t('qualification.summary.group', { rank: ord(from), group })
      : t('qualification.summary.groupRange', {
          from: ord(from),
          to: ord(to),
          group,
        });
  }
  if (draft.sourceKind === 'Overall') {
    return from === to
      ? t('qualification.summary.overall', { rank: ord(from) })
      : t('qualification.summary.overallRange', {
          from: ord(from),
          to: ord(to),
        });
  }
  const place = ord(parsePositiveInt(draft.acrossGroupsPosition) ?? 1);
  if (from === to && from <= 1) {
    return t('qualification.summary.acrossBest', { place });
  }
  if (from === to) {
    return t('qualification.summary.acrossNth', {
      rank: ord(from),
      place,
    });
  }
  return t('qualification.summary.acrossRange', {
    from: ord(from),
    to: ord(to),
    place,
  });
}

export function ordinalRank(n: number, locale: string): string {
  if (locale.startsWith('fr')) {
    return n === 1 ? '1er' : `${n}e`;
  }
  const mod100 = n % 100;
  if (mod100 >= 11 && mod100 <= 13) return `${n}th`;
  switch (n % 10) {
    case 1:
      return `${n}st`;
    case 2:
      return `${n}nd`;
    case 3:
      return `${n}rd`;
    default:
      return `${n}th`;
  }
}

export function isIntentComplete(
  draft: QualIntentDraft,
  groups: { id: string; name: string }[],
  placesLabeled = true,
  all: QualIntentDraft[] = [draft],
): boolean {
  return incompleteIntentReason(draft, groups, placesLabeled, all) == null;
}

/**
 * Dominant incompleteness cause for collapsed-row hint (one message).
 * Returns an i18n key suffix under `qualification.incompleteHint*`.
 */
export function incompleteIntentReason(
  draft: QualIntentDraft,
  groups: { id: string; name: string }[],
  placesLabeled = true,
  all: QualIntentDraft[] = [draft],
): QualIncompleteReason | null {
  if (draft.targetKind === 'place') {
    if (!placesLabeled) return 'PlaceUnavailable';
    if (!draft.destinationStageId.trim()) {
      return 'Destination';
    }
  } else if (!draft.destinationStageId.trim()) {
    return 'Destination';
  }

  if (draft.sourceKind === 'SingleGroup' && !draft.groupId.trim()) {
    return 'Group';
  }
  if (draft.conditionKind === 'points') {
    const pts = Number(draft.minimumPoints);
    if (!Number.isFinite(pts) || pts < 0) return 'Points';
  }

  const occurrences = expandOccurrences(draft, groups);
  if (occurrences.length === 0) return 'Selection';

  if (draft.targetKind === 'place') {
    const gap = placeMappingGap(
      draft.destinationSlotKeys,
      occurrences.length,
    );
    if (gap) return gap;

    const mine = new Set(placeOccupancyKeys(draft));
    if (mine.size > 0) {
      for (const other of all) {
        if (other.id === draft.id) continue;
        for (const pk of placeOccupancyKeys(other)) {
          if (mine.has(pk)) {
            return 'DuplicatePlace';
          }
        }
      }
    }
  }

  return null;
}

/** Cross-intent Place occupancy: stageId|slotKey for each filled key. */
function placeOccupancyKeys(draft: QualIntentDraft): string[] {
  if (draft.targetKind !== 'place') return [];
  const dest = draft.destinationStageId.trim();
  if (!dest) return [];
  return draft.destinationSlotKeys
    .map((k) => k.trim())
    .filter((k) => k.length > 0)
    .map((k) => `${dest}|${k}`);
}

/** Stable key for one expanded source occurrence (points condition ignored). */
function occurrenceKey(o: SourceOccurrence): string {
  if (o.scope === 'Group') {
    return `g:${o.groupId ?? ''}:${o.position}`;
  }
  if (o.scope === 'AcrossGroups') {
    return `a:${o.acrossGroupsPosition ?? 0}:${o.position}`;
  }
  return `o:${o.position}`;
}

/**
 * Soft warning: another intent expands at least one shared source occurrence
 * (any destination). Points gates are ignored. Duplicate detection stays
 * source-occurrence based (destination / Place ignored).
 */
export function hasDuplicateSourceOccurrence(
  draft: QualIntentDraft,
  all: QualIntentDraft[],
  groups: { id: string; name: string }[],
): boolean {
  const mine = new Set(
    expandOccurrences(draft, groups).map(occurrenceKey),
  );
  if (mine.size === 0) return false;

  for (const other of all) {
    if (other.id === draft.id) continue;
    for (const occ of expandOccurrences(other, groups)) {
      if (mine.has(occurrenceKey(occ))) return true;
    }
  }
  return false;
}

/** True when any pair of intents shares a source occurrence. */
export function hasAnyDuplicateSourceOccurrence(
  all: QualIntentDraft[],
  groups: { id: string; name: string }[],
): boolean {
  return all.some((intent) =>
    hasDuplicateSourceOccurrence(intent, all, groups),
  );
}

export function toApiIntent(
  draft: QualIntentDraft,
  order: number,
  groups: { id: string; name: string }[] = [],
) {
  const placeKeys =
    draft.targetKind === 'place'
      ? resizeDestinationSlotKeys(
          draft.destinationSlotKeys,
          expandOccurrences(draft, groups).length,
        ).map((k) => k.trim())
      : [];

  return {
    intentId: draft.id,
    order,
    sourceKind: draft.sourceKind,
    positionFrom: parsePositiveInt(draft.positionFrom) ?? 1,
    positionTo: parsePositiveInt(draft.positionTo) ?? 1,
    destinationStageId: draft.destinationStageId,
    destinationSlotKeys:
      draft.targetKind === 'place' && placeKeys.length > 0 ? placeKeys : null,
    groupId:
      draft.sourceKind === 'SingleGroup' ? draft.groupId || null : null,
    acrossGroupsPosition:
      draft.sourceKind === 'AcrossGroups'
        ? (parsePositiveInt(draft.acrossGroupsPosition) ?? 1)
        : null,
    minimumPoints:
      draft.conditionKind === 'points' ? Number(draft.minimumPoints) : null,
  };
}

/** Stable fingerprint of intents (save payload shape) for dirty detection. */
export function serializeIntents(
  intents: QualIntentDraft[],
  groups: { id: string; name: string }[] = [],
): string {
  return JSON.stringify(
    intents.map((intent, index) => toApiIntent(intent, index + 1, groups)),
  );
}

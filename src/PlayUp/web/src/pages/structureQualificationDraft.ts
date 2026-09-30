// -----------------------------------------------------------------------
// Qualification Intent drafts — Expand client preview (Qual population|place).
// UX Place = Placement destination picker (Slot | Group).
// Intent maps: destinationSlotKeys XOR destinationGroupIds (duplicates OK for groups).
// -----------------------------------------------------------------------

import type {
  QualificationIntentSourceKind,
  RankingScope,
  StructureQualificationIntent,
  StructureQualificationPath,
} from '../types';
import { isPopulationDestination } from './structureProgression';
import {
  coerceDestinationGroupIds,
  coerceDestinationSlotKeys,
  countEmptyPlaceSlots,
  placeMappingGap,
  resizeDestinationSlotKeys,
} from './structurePlaceMapping';

export {
  coerceDestinationGroupIds,
  coerceDestinationSlotKeys,
  fillEmptyPlaceKeysAllowingReuse,
  fillEmptyPlaceSlotKeys,
  resizeDestinationSlotKeys,
} from './structurePlaceMapping';

export {
  applyDestinationToDraft,
  applyTargetKindToDraft,
  isFormOnlyDestination,
  normalizeFormOnlyDestinationDraft,
  showsPopulationPlaceChoice,
} from './structureSortiesDestination';

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
   * Cup Place slot keys aligned with expandOccurrences order (index ↔ key).
   * Empty when Population or Groups Place. Length should match Expand for Cup Place.
   */
  destinationSlotKeys: string[];
  /**
   * Groups A1 Place group ids aligned with Expand (index ↔ groupId).
   * Empty when Population, Cup Place, or Form Place. Duplicates allowed.
   */
  destinationGroupIds: string[];
  /** Champ/Swiss Form Placement (Domain DestinationForm / ForForm). */
  destinationForm: boolean;
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

/** Active Place map for this draft (slot XOR group XOR form). */
export function placeMapKeys(draft: QualIntentDraft): {
  grain: 'slot' | 'group' | 'form';
  keys: string[];
} | null {
  if (draft.targetKind !== 'place') return null;
  if (draft.destinationForm) {
    return { grain: 'form', keys: [] };
  }
  if (draft.destinationGroupIds.length > 0) {
    return { grain: 'group', keys: draft.destinationGroupIds };
  }
  return { grain: 'slot', keys: draft.destinationSlotKeys };
}

/** Count empty Place slots across intents (after Expand-aligned resize). */
export function countUnmappedPlaceSlots(
  intents: QualIntentDraft[],
  groups: { id: string; name: string }[],
): number {
  let n = 0;
  for (const intent of intents) {
    if (intent.targetKind !== 'place' || intent.destinationForm) continue;
    const occ = expandOccurrences(intent, groups);
    const map = placeMapKeys(intent);
    n += countEmptyPlaceSlots(map?.keys ?? [], occ.length);
  }
  return n;
}

/** Keep Place keys aligned with current Expand occurrence count. */
export function syncPlaceSlotKeys(
  draft: QualIntentDraft,
  groups: { id: string; name: string }[],
): QualIntentDraft {
  if (draft.targetKind !== 'place') {
    if (
      draft.destinationSlotKeys.length === 0 &&
      draft.destinationGroupIds.length === 0 &&
      !draft.destinationForm
    ) {
      return draft;
    }
    return {
      ...draft,
      destinationSlotKeys: [],
      destinationGroupIds: [],
      destinationForm: false,
    };
  }
  if (draft.destinationForm) {
    if (
      draft.destinationSlotKeys.length === 0 &&
      draft.destinationGroupIds.length === 0
    ) {
      return draft;
    }
    return { ...draft, destinationSlotKeys: [], destinationGroupIds: [] };
  }
  const n = expandOccurrences(draft, groups).length;
  if (draft.destinationGroupIds.length > 0) {
    const next = resizeDestinationSlotKeys(draft.destinationGroupIds, n);
    if (
      next.length === draft.destinationGroupIds.length &&
      next.every((k, i) => k === draft.destinationGroupIds[i]) &&
      draft.destinationSlotKeys.length === 0
    ) {
      return draft;
    }
    return {
      ...draft,
      destinationGroupIds: next,
      destinationSlotKeys: [],
      destinationForm: false,
    };
  }
  const next = resizeDestinationSlotKeys(draft.destinationSlotKeys, n);
  if (
    next.length === draft.destinationSlotKeys.length &&
    next.every((k, i) => k === draft.destinationSlotKeys[i]) &&
    draft.destinationGroupIds.length === 0
  ) {
    return draft;
  }
  return {
    ...draft,
    destinationSlotKeys: next,
    destinationGroupIds: [],
    destinationForm: false,
  };
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
    destinationGroupIds: [],
    destinationForm: false,
  };
}

export function intentFromApi(
  intent: StructureQualificationIntent,
): QualIntentDraft {
  const groupIds = coerceDestinationGroupIds(intent.destinationGroupIds);
  const keys = coerceDestinationSlotKeys(
    intent.destinationSlotKeys,
    intent.destinationSlotKey,
  );
  const form = !!intent.destinationForm;
  const population = !form && groupIds.length === 0 && keys.length === 0;
  const isGroupPlace = !population && !form && groupIds.length > 0;
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
    destinationSlotKeys: population || form || isGroupPlace ? [] : keys,
    destinationGroupIds: population || form || !isGroupPlace ? [] : groupIds,
    destinationForm: form,
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
  const groupId = path.destinationGroupId?.trim() ?? '';
  const form = !!path.destinationForm;
  const population =
    !form && isPopulationDestination(path.destinationSlotKey) && !groupId;
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
    minimumPoints: path.minimumPoints != null ? String(path.minimumPoints) : '',
    targetKind: population ? 'population' : 'place',
    destinationStageId: path.destinationStageId,
    destinationSlotKeys:
      population || form || groupId
        ? []
        : coerceDestinationSlotKeys(null, path.destinationSlotKey),
    destinationGroupIds: population || form || !groupId ? [] : [groupId],
    destinationForm: form,
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
    const group = draft.groupName.trim() || t('qualification.unnamedGroup');
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
    const group = draft.groupName.trim() || t('qualification.unnamedGroup');
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

export function ordinalRankSuffix(n: number, locale: string): string {
  if (locale.startsWith('fr')) {
    return n === 1 ? 'er' : 'e';
  }
  const mod100 = n % 100;
  if (mod100 >= 11 && mod100 <= 13) return 'th';
  switch (n % 10) {
    case 1:
      return 'st';
    case 2:
      return 'nd';
    case 3:
      return 'rd';
    default:
      return 'th';
  }
}

export function ordinalRank(n: number, locale: string): string {
  return `${n}${ordinalRankSuffix(n, locale)}`;
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
    if (draft.destinationForm) {
      return null;
    }
    const map = placeMapKeys(draft);
    const grain = map?.grain ?? 'slot';
    const keys = map?.keys ?? [];
    const gap = placeMappingGap(keys, occurrences.length, {
      allowDuplicates: grain === 'group',
    });
    if (gap) return gap;

    // DuplicatePlace only for Cup slots — Groups allow shared pools.
    if (grain === 'slot') {
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
  }

  return null;
}

/** Cross-intent Place occupancy: stageId|slotKey for Cup Place only. */
function placeOccupancyKeys(draft: QualIntentDraft): string[] {
  if (draft.targetKind !== 'place') return [];
  if (draft.destinationGroupIds.length > 0) return [];
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
  const mine = new Set(expandOccurrences(draft, groups).map(occurrenceKey));
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
  const expandN = expandOccurrences(draft, groups).length;
  const isFormPlace = draft.targetKind === 'place' && draft.destinationForm;
  const isGroupPlace =
    draft.targetKind === 'place' &&
    !isFormPlace &&
    draft.destinationGroupIds.length > 0;
  const placeKeys =
    draft.targetKind === 'place' && !isGroupPlace && !isFormPlace
      ? resizeDestinationSlotKeys(draft.destinationSlotKeys, expandN).map((k) =>
          k.trim(),
        )
      : [];
  const groupIds = isGroupPlace
    ? resizeDestinationSlotKeys(draft.destinationGroupIds, expandN).map((k) =>
        k.trim(),
      )
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
    destinationGroupIds:
      draft.targetKind === 'place' && groupIds.length > 0 ? groupIds : null,
    destinationForm: isFormPlace ? true : false,
    groupId: draft.sourceKind === 'SingleGroup' ? draft.groupId || null : null,
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

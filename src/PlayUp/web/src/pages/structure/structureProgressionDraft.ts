// -----------------------------------------------------------------------
// Progression Intent drafts — Round × Outcome → Destination.
// UX Place = Placement destination picker (Slot | Group).
// Place D1: Expand[i] ↔ destinationSlotKeys[i] XOR destinationGroupIds[i].
// -----------------------------------------------------------------------

import type {
  ProgressionOutcome,
  StructureProgressionIntent,
  StructureProgressionPath,
} from '../../types';
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

export type ProgTargetKind = 'population' | 'place';

export type ProgIntentDraft = {
  id: string;
  order: number;
  roundId: string;
  roundName: string;
  outcome: ProgressionOutcome;
  targetKind: ProgTargetKind;
  destinationStageId: string;
  /**
   * Cup Place — Expand index ↔ SlotKey (fixture order on the round).
   * Empty for Population or Groups Place.
   */
  destinationSlotKeys: string[];
  /**
   * Groups A1 Place — Expand index ↔ groupId. Duplicates allowed.
   * Empty for Population, Cup Place, or Form Place.
   */
  destinationGroupIds: string[];
  /** Champ/Swiss Form Placement (Domain DestinationForm / ForForm). */
  destinationForm: boolean;
  /** Expand preview: fixture count for the selected round. */
  expandedPathCount: number;
};

export type ProgIncompleteReason =
  | 'Round'
  | 'Destination'
  | 'PlaceUnavailable'
  | 'MultiSlot'
  | 'DuplicateSlot'
  | 'DuplicateRoundOutcome'
  | 'DuplicatePlace'
  | 'ChampionshipTerminalRound';

/** Place ChoiceTile / map gate — Cup slots, Groups pools, or Champ/Swiss Form. */
export { areProgressionPlacesLabeled } from './structurePlaceLabel';

export function newProgIntentId(): string {
  return crypto.randomUUID();
}

/** Expand count for Place D1 (fixtures on the selected round). */
export function expandCount(draft: ProgIntentDraft): number {
  return Math.max(draft.expandedPathCount, 0);
}

/** Active Place map for this draft (slot XOR group XOR form). */
export function placeMapKeys(draft: ProgIntentDraft): {
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

/** Keep Place keys aligned with current Expand fixture count. */
export function syncPlaceSlotKeys(draft: ProgIntentDraft): ProgIntentDraft {
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
  const n = expandCount(draft);
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

/** Count empty Place slots across intents (after Expand-aligned resize). */
export function countUnmappedPlaceSlots(intents: ProgIntentDraft[]): number {
  let n = 0;
  for (const intent of intents) {
    if (intent.targetKind !== 'place' || intent.destinationForm) continue;
    const map = placeMapKeys(intent);
    n += countEmptyPlaceSlots(map?.keys ?? [], expandCount(intent));
  }
  return n;
}

export function emptyProgIntent(
  destinationStageId = '',
  targetKind: ProgTargetKind = 'population',
  order = 1,
): ProgIntentDraft {
  return {
    id: newProgIntentId(),
    order,
    roundId: '',
    roundName: '',
    outcome: 'Winner',
    targetKind,
    destinationStageId,
    destinationSlotKeys: [],
    destinationGroupIds: [],
    destinationForm: false,
    expandedPathCount: 0,
  };
}

export function intentFromApi(
  intent: StructureProgressionIntent,
  _sourceStageId?: string,
): ProgIntentDraft {
  const groupIds = coerceDestinationGroupIds(intent.destinationGroupIds);
  const keys = coerceDestinationSlotKeys(
    intent.destinationSlotKeys,
    intent.destinationSlotKey,
  );
  const form = !!intent.destinationForm;
  const population = !form && groupIds.length === 0 && keys.length === 0;
  const isGroupPlace = !population && !form && groupIds.length > 0;

  const draft: ProgIntentDraft = {
    id: intent.intentId || newProgIntentId(),
    order: intent.order,
    roundId: intent.roundId,
    roundName: intent.roundName?.trim() ?? '',
    outcome: intent.outcome,
    targetKind: population ? 'population' : 'place',
    destinationStageId: intent.destinationStageId,
    destinationSlotKeys: population || form || isGroupPlace ? [] : keys,
    destinationGroupIds: population || form || !isGroupPlace ? [] : groupIds,
    destinationForm: form,
    expandedPathCount: intent.expandedPathCount ?? 0,
  };
  return syncPlaceSlotKeys(draft);
}

/** Legacy fallback: one singleton intent per path (path-list authoring). */
export function pathToSingletonIntent(
  path: StructureProgressionPath,
  _sourceStageId: string,
  roundId: string,
  roundName: string,
  order: number,
): ProgIntentDraft {
  const groupId = path.destinationGroupId?.trim() ?? '';
  const form = !!path.destinationForm;
  const population =
    !form && isPopulationDestination(path.destinationSlotKey) && !groupId;

  return syncPlaceSlotKeys({
    id: newProgIntentId(),
    order,
    roundId,
    roundName,
    outcome: path.outcome,
    targetKind: population ? 'population' : 'place',
    destinationStageId: path.destinationStageId,
    destinationSlotKeys:
      population || form || groupId
        ? []
        : coerceDestinationSlotKeys(null, path.destinationSlotKey),
    destinationGroupIds: population || form || !groupId ? [] : [groupId],
    destinationForm: form,
    expandedPathCount: 1,
  });
}

export function serializeIntents(intents: ProgIntentDraft[]): string {
  return JSON.stringify(
    intents.map((p) => ({
      order: p.order,
      roundId: p.roundId,
      outcome: p.outcome,
      targetKind: p.targetKind,
      destinationStageId: p.destinationStageId,
      destinationSlotKeys:
        p.targetKind === 'place' && !p.destinationForm
          ? p.destinationSlotKeys
          : [],
      destinationGroupIds:
        p.targetKind === 'place' && !p.destinationForm
          ? p.destinationGroupIds
          : [],
      destinationForm: p.targetKind === 'place' && p.destinationForm,
    })),
  );
}

export function toApiIntent(
  draft: ProgIntentDraft,
  order: number,
): StructureProgressionIntent {
  if (draft.targetKind === 'population') {
    return {
      intentId: draft.id,
      order,
      roundId: draft.roundId.trim(),
      roundName: draft.roundName || null,
      outcome: draft.outcome,
      destinationStageId: draft.destinationStageId,
      destinationSlotKeys: null,
      destinationGroupIds: null,
      destinationForm: false,
      expandedPathCount: draft.expandedPathCount,
    };
  }
  if (draft.destinationForm) {
    return {
      intentId: draft.id,
      order,
      roundId: draft.roundId.trim(),
      roundName: draft.roundName || null,
      outcome: draft.outcome,
      destinationStageId: draft.destinationStageId,
      destinationSlotKeys: null,
      destinationGroupIds: null,
      destinationForm: true,
      expandedPathCount: draft.expandedPathCount,
    };
  }
  const n = expandCount(draft);
  const isGroupPlace = draft.destinationGroupIds.length > 0;
  if (isGroupPlace) {
    const ids = resizeDestinationSlotKeys(draft.destinationGroupIds, n).map(
      (k) => k.trim(),
    );
    return {
      intentId: draft.id,
      order,
      roundId: draft.roundId.trim(),
      roundName: draft.roundName || null,
      outcome: draft.outcome,
      destinationStageId: draft.destinationStageId,
      destinationSlotKeys: null,
      destinationGroupIds: ids.length > 0 ? ids : null,
      destinationForm: false,
      expandedPathCount: draft.expandedPathCount,
    };
  }
  const keys = resizeDestinationSlotKeys(draft.destinationSlotKeys, n).map(
    (k) => k.trim(),
  );
  return {
    intentId: draft.id,
    order,
    roundId: draft.roundId.trim(),
    roundName: draft.roundName || null,
    outcome: draft.outcome,
    destinationStageId: draft.destinationStageId,
    destinationSlotKeys: keys.length > 0 ? keys : null,
    destinationGroupIds: null,
    destinationForm: false,
    expandedPathCount: draft.expandedPathCount,
  };
}

function roundOutcomeKey(draft: ProgIntentDraft): string {
  return `${draft.roundId.trim()}|${draft.outcome}`;
}

/** Cross-intent Place occupancy: stageId|slotKey for Cup Place only. */
function placeKeys(draft: ProgIntentDraft): string[] {
  if (draft.targetKind !== 'place' || draft.destinationForm) return [];
  if (draft.destinationGroupIds.length > 0) return [];
  const dest = draft.destinationStageId.trim();
  if (!dest) return [];
  return draft.destinationSlotKeys
    .map((k) => k.trim())
    .filter((k) => k.length > 0)
    .map((k) => `${dest}|${k}`);
}

export function incompleteIntentReason(
  draft: ProgIntentDraft,
  all: ProgIntentDraft[],
  placesLabeled: boolean,
  championshipTerminalRoundId: string | null = null,
): ProgIncompleteReason | null {
  if (!draft.roundId.trim()) {
    return 'Round';
  }
  if (
    draft.outcome === 'Winner' &&
    (championshipTerminalRoundId == null ||
      draft.roundId.trim() !== championshipTerminalRoundId)
  ) {
    return 'ChampionshipTerminalRound';
  }
  if (draft.targetKind === 'place') {
    if (!placesLabeled) {
      return 'PlaceUnavailable';
    }
    if (!draft.destinationStageId.trim()) {
      return 'Destination';
    }
  } else if (!draft.destinationStageId.trim()) {
    return 'Destination';
  }

  if (draft.targetKind === 'place' && !draft.destinationForm) {
    const n = expandCount(draft);
    if (n <= 0) {
      return 'Destination';
    }
    const map = placeMapKeys(draft);
    const grain = map?.grain ?? 'slot';
    const gap = placeMappingGap(map?.keys ?? [], n, {
      allowDuplicates: grain === 'group',
    });
    if (gap) return gap;
  }

  const rk = roundOutcomeKey(draft);
  if (
    all.some((other) => other.id !== draft.id && roundOutcomeKey(other) === rk)
  ) {
    return 'DuplicateRoundOutcome';
  }

  const mine = new Set(placeKeys(draft));
  if (mine.size > 0) {
    for (const other of all) {
      if (other.id === draft.id) continue;
      for (const pk of placeKeys(other)) {
        if (mine.has(pk)) {
          return 'DuplicatePlace';
        }
      }
    }
  }

  return null;
}

export function isIntentComplete(
  draft: ProgIntentDraft,
  all: ProgIntentDraft[],
  placesLabeled: boolean,
  championshipTerminalRoundId: string | null = null,
): boolean {
  return (
    incompleteIntentReason(
      draft,
      all,
      placesLabeled,
      championshipTerminalRoundId,
    ) == null
  );
}

export function summarizeIntentWho(
  draft: ProgIntentDraft,
  t: (key: string, opts?: Record<string, unknown>) => string,
): string {
  const outcome =
    draft.outcome === 'Winner'
      ? t('progression.outcomeWinner')
      : t('progression.outcomeLoser');
  const round =
    draft.roundName.trim() ||
    (draft.roundId.trim()
      ? t('progression.roundFallback', {
          id: draft.roundId.trim().slice(0, 8),
        })
      : '');
  if (!round) {
    return outcome;
  }
  return t('progression.summary.who', { outcome, match: round });
}

/** Expand preview count for capacity soft-warnings (fixtures × intents to dest). */
export function expandContribution(draft: ProgIntentDraft): number {
  // Place Auto dual-writes Population+Place on Apply — both kinds occupy capacity.
  return Math.max(draft.expandedPathCount, draft.roundId ? 1 : 0);
}

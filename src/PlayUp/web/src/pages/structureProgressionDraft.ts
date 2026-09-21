// -----------------------------------------------------------------------
// Progression Intent drafts — Round × Outcome → Destination (Prog V3).
// Place D1: Expand[i] ↔ destinationSlotKeys[i] (N fixtures on the round).
// -----------------------------------------------------------------------

import type {
  ProgressionOutcome,
  StructureProgressionIntent,
  StructureProgressionPath,
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
   * Place D1 — Expand index ↔ SlotKey (fixture order on the round).
   * Empty array for Population. For Place, length must equal expandCount (N).
   * Remplir is only an authoring aid for this array; it does not define semantics.
   */
  destinationSlotKeys: string[];
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

/** Place ChoiceTile / map gate — Cup schematic must expose labeled Places (U4). */
export { areProgressionPlacesLabeled } from './structurePlaceLabel';

export function newProgIntentId(): string {
  return crypto.randomUUID();
}

/** Expand count for Place D1 (fixtures on the selected round). */
export function expandCount(draft: ProgIntentDraft): number {
  return Math.max(draft.expandedPathCount, 0);
}

/** Keep Place keys aligned with current Expand fixture count. */
export function syncPlaceSlotKeys(draft: ProgIntentDraft): ProgIntentDraft {
  if (draft.targetKind !== 'place') {
    if (draft.destinationSlotKeys.length === 0) return draft;
    return { ...draft, destinationSlotKeys: [] };
  }
  const n = expandCount(draft);
  const next = resizeDestinationSlotKeys(draft.destinationSlotKeys, n);
  if (
    next.length === draft.destinationSlotKeys.length &&
    next.every((k, i) => k === draft.destinationSlotKeys[i])
  ) {
    return draft;
  }
  return { ...draft, destinationSlotKeys: next };
}

/** Count empty Place slots across intents (after Expand-aligned resize). */
export function countUnmappedPlaceSlots(intents: ProgIntentDraft[]): number {
  let n = 0;
  for (const intent of intents) {
    if (intent.targetKind !== 'place') continue;
    n += countEmptyPlaceSlots(intent.destinationSlotKeys, expandCount(intent));
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
    expandedPathCount: 0,
  };
}

export function intentFromApi(
  intent: StructureProgressionIntent,
  _sourceStageId?: string,
): ProgIntentDraft {
  const keys = coerceDestinationSlotKeys(
    intent.destinationSlotKeys,
    intent.destinationSlotKey,
  );
  const population = keys.length === 0;

  const draft: ProgIntentDraft = {
    id: intent.intentId || newProgIntentId(),
    order: intent.order,
    roundId: intent.roundId,
    roundName: intent.roundName?.trim() ?? '',
    outcome: intent.outcome,
    targetKind: population ? 'population' : 'place',
    destinationStageId: intent.destinationStageId,
    destinationSlotKeys: population ? [] : keys,
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
  const population = isPopulationDestination(path.destinationSlotKey);

  return syncPlaceSlotKeys({
    id: newProgIntentId(),
    order,
    roundId,
    roundName,
    outcome: path.outcome,
    targetKind: population ? 'population' : 'place',
    destinationStageId: path.destinationStageId,
    destinationSlotKeys: population
      ? []
      : coerceDestinationSlotKeys(null, path.destinationSlotKey),
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
        p.targetKind === 'place' ? p.destinationSlotKeys : [],
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
      destinationSlotKey: null,
      expandedPathCount: draft.expandedPathCount,
    };
  }
  const n = expandCount(draft);
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
    destinationSlotKey: keys[0] || null,
    expandedPathCount: draft.expandedPathCount,
  };
}

function roundOutcomeKey(draft: ProgIntentDraft): string {
  return `${draft.roundId.trim()}|${draft.outcome}`;
}

/** Cross-intent Place occupancy: stageId|slotKey for each filled key. */
function placeKeys(draft: ProgIntentDraft): string[] {
  if (draft.targetKind !== 'place') return [];
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

  if (draft.targetKind === 'place') {
    const n = expandCount(draft);
    if (n <= 0) {
      return 'Destination';
    }
    const gap = placeMappingGap(draft.destinationSlotKeys, n);
    if (gap) return gap;
  }

  const rk = roundOutcomeKey(draft);
  if (
    all.some(
      (other) => other.id !== draft.id && roundOutcomeKey(other) === rk,
    )
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

// -----------------------------------------------------------------------
// Progression Intent drafts — Round × Outcome → Destination (Prog V3).
// -----------------------------------------------------------------------

import type {
  ProgressionOutcome,
  StructureProgressionIntent,
  StructureProgressionPath,
} from '../types';
import { isPopulationDestination } from './structureProgression';

export type ProgTargetKind = 'population' | 'place';

export type ProgIntentDraft = {
  id: string;
  order: number;
  roundId: string;
  roundName: string;
  outcome: ProgressionOutcome;
  targetKind: ProgTargetKind;
  destinationStageId: string;
  /** SlotKey when targeting Place (Auto); empty for Population. */
  destinationSlotKey: string;
  /** Expand preview: fixture count for the selected round. */
  expandedPathCount: number;
};

export type ProgIncompleteReason =
  | 'Round'
  | 'Destination'
  | 'PlaceUnavailable'
  | 'DuplicateRoundOutcome'
  | 'DuplicatePlace';

/** Place ChoiceTile gate — Cup schematic must expose targetable labeled Places (U4). */
export { areProgressionPlacesLabeled } from './structurePlaceLabel';

export function newProgIntentId(): string {
  return crypto.randomUUID();
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
    destinationSlotKey: '',
    expandedPathCount: 0,
  };
}

/** Prefer destinationSlotKeys[0]; coerce legacy singular. Draft stays 0|1 Place. */
function coerceProgSlotKey(intent: StructureProgressionIntent): string {
  const keys = intent.destinationSlotKeys;
  if (keys != null && keys.length > 0) {
    return keys[0]?.trim() ?? '';
  }
  return intent.destinationSlotKey?.trim() ?? '';
}

export function intentFromApi(
  intent: StructureProgressionIntent,
  _sourceStageId?: string,
): ProgIntentDraft {
  const slot = coerceProgSlotKey(intent);
  const population = isPopulationDestination(slot);

  return {
    id: intent.intentId || newProgIntentId(),
    order: intent.order,
    roundId: intent.roundId,
    roundName: intent.roundName?.trim() ?? '',
    outcome: intent.outcome,
    targetKind: population ? 'population' : 'place',
    destinationStageId: intent.destinationStageId,
    destinationSlotKey: population ? '' : slot,
    expandedPathCount: intent.expandedPathCount ?? 0,
  };
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

  return {
    id: newProgIntentId(),
    order,
    roundId,
    roundName,
    outcome: path.outcome,
    targetKind: population ? 'population' : 'place',
    destinationStageId: path.destinationStageId,
    destinationSlotKey: population
      ? ''
      : (path.destinationSlotKey?.trim() ?? ''),
    expandedPathCount: 1,
  };
}

export function serializeIntents(intents: ProgIntentDraft[]): string {
  return JSON.stringify(
    intents.map((p) => ({
      order: p.order,
      roundId: p.roundId,
      outcome: p.outcome,
      targetKind: p.targetKind,
      destinationStageId: p.destinationStageId,
      destinationSlotKey:
        p.targetKind === 'place' ? p.destinationSlotKey : '',
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
  const slot = draft.destinationSlotKey.trim();
  return {
    intentId: draft.id,
    order,
    roundId: draft.roundId.trim(),
    roundName: draft.roundName || null,
    outcome: draft.outcome,
    destinationStageId: draft.destinationStageId,
    /** Minimal Prog Place: singular draft ↔ 0|1 array (full N→N UI later). */
    destinationSlotKeys: slot ? [slot] : null,
    destinationSlotKey: slot || null,
    expandedPathCount: draft.expandedPathCount,
  };
}

function roundOutcomeKey(draft: ProgIntentDraft): string {
  return `${draft.roundId.trim()}|${draft.outcome}`;
}

function placeKey(draft: ProgIntentDraft): string | null {
  if (draft.targetKind !== 'place') return null;
  const slot = draft.destinationSlotKey.trim();
  if (!slot) return null;
  return `${draft.destinationStageId}|${slot}`;
}

export function incompleteIntentReason(
  draft: ProgIntentDraft,
  all: ProgIntentDraft[],
  placesLabeled: boolean,
): ProgIncompleteReason | null {
  if (!draft.roundId.trim()) {
    return 'Round';
  }
  if (draft.targetKind === 'place') {
    if (!placesLabeled) {
      return 'PlaceUnavailable';
    }
    if (
      !draft.destinationStageId.trim() ||
      !draft.destinationSlotKey.trim()
    ) {
      return 'Destination';
    }
  } else if (!draft.destinationStageId.trim()) {
    return 'Destination';
  }

  const rk = roundOutcomeKey(draft);
  if (
    all.some(
      (other) => other.id !== draft.id && roundOutcomeKey(other) === rk,
    )
  ) {
    return 'DuplicateRoundOutcome';
  }

  const pk = placeKey(draft);
  if (
    pk &&
    all.some((other) => other.id !== draft.id && placeKey(other) === pk)
  ) {
    return 'DuplicatePlace';
  }

  return null;
}

export function isIntentComplete(
  draft: ProgIntentDraft,
  all: ProgIntentDraft[],
  placesLabeled: boolean,
): boolean {
  return incompleteIntentReason(draft, all, placesLabeled) == null;
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

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
  /** Preserved for Place when U4 unlocks; never shown as raw SlotKey while gated. */
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

/** Place ChoiceTile stays gated until schematic exposes labeled Places (Décision / U4). */
export function areProgressionPlacesLabeled(): boolean {
  return false;
}

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

export function intentFromApi(
  intent: StructureProgressionIntent,
  sourceStageId: string,
): ProgIntentDraft {
  const population = isPopulationDestination(intent.destinationSlotKey);
  // V3 purge: Place must be forme-owner. Cross-stage slot → coerce to Population.
  const crossPlace =
    !population && intent.destinationStageId !== sourceStageId;

  return {
    id: intent.intentId || newProgIntentId(),
    order: intent.order,
    roundId: intent.roundId,
    roundName: intent.roundName?.trim() ?? '',
    outcome: intent.outcome,
    targetKind: population || crossPlace ? 'population' : 'place',
    destinationStageId:
      population || crossPlace
        ? intent.destinationStageId
        : sourceStageId,
    destinationSlotKey:
      population || crossPlace
        ? ''
        : (intent.destinationSlotKey?.trim() ?? ''),
    expandedPathCount: intent.expandedPathCount ?? 0,
  };
}

/** Legacy fallback: one singleton intent per path (path-list authoring). */
export function pathToSingletonIntent(
  path: StructureProgressionPath,
  sourceStageId: string,
  roundId: string,
  roundName: string,
  order: number,
): ProgIntentDraft {
  const population = isPopulationDestination(path.destinationSlotKey);
  const crossPlace =
    !population && path.destinationStageId !== sourceStageId;

  return {
    id: newProgIntentId(),
    order,
    roundId,
    roundName,
    outcome: path.outcome,
    targetKind: population || crossPlace ? 'population' : 'place',
    destinationStageId:
      population || crossPlace
        ? path.destinationStageId
        : sourceStageId,
    destinationSlotKey:
      population || crossPlace
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
      destinationSlotKey: null,
      expandedPathCount: draft.expandedPathCount,
    };
  }
  return {
    intentId: draft.id,
    order,
    roundId: draft.roundId.trim(),
    roundName: draft.roundName || null,
    outcome: draft.outcome,
    destinationStageId: draft.destinationStageId,
    destinationSlotKey: draft.destinationSlotKey.trim(),
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

/** Expand preview count for capacity soft-warnings (fixtures × intents to peer). */
export function expandContribution(draft: ProgIntentDraft): number {
  if (draft.targetKind !== 'population') return 0;
  return Math.max(draft.expandedPathCount, draft.roundId ? 1 : 0);
}

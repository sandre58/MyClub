// -----------------------------------------------------------------------
// Progression path drafts — atomic ProgressionPath authoring (Prog V2).
// -----------------------------------------------------------------------

import type {
  ProgressionOutcome,
  StructureProgressionPath,
} from '../types';
import { isPopulationDestination } from './structureProgression';

export type ProgTargetKind = 'population' | 'place';

export type ProgPathDraft = {
  id: string;
  sourceFixtureId: string;
  sourceLabel: string;
  outcome: ProgressionOutcome;
  targetKind: ProgTargetKind;
  destinationStageId: string;
  /** Preserved for reload/save of existing Place paths; never shown as raw SlotKey. */
  destinationSlotKey: string;
  /**
   * Loaded Place targeting another phase — Domain legacy, not V2 authoring.
   * Blocks save until the user changes destination.
   */
  legacyInterPhasePlace: boolean;
};

export type ProgIncompleteReason =
  | 'Fixture'
  | 'Destination'
  | 'PlaceUnavailable'
  | 'LegacyPlace'
  | 'DuplicateSource'
  | 'DuplicatePlace';

/** Place ChoiceTile stays gated until schematic exposes labeled Places (Décision / U4). */
export function areProgressionPlacesLabeled(): boolean {
  return false;
}

export function newProgPathId(): string {
  return crypto.randomUUID();
}

export function emptyProgPath(
  destinationStageId = '',
  targetKind: ProgTargetKind = 'population',
): ProgPathDraft {
  return {
    id: newProgPathId(),
    sourceFixtureId: '',
    sourceLabel: '',
    outcome: 'Winner',
    targetKind,
    destinationStageId,
    destinationSlotKey: '',
    legacyInterPhasePlace: false,
  };
}

export function pathFromApi(
  path: StructureProgressionPath,
  sourceStageId: string,
): ProgPathDraft {
  const population = isPopulationDestination(path.destinationSlotKey);
  const destinationStageId = path.destinationStageId;
  const legacyInterPhasePlace =
    !population && destinationStageId !== sourceStageId;

  return {
    id: newProgPathId(),
    sourceFixtureId: path.sourceFixtureId,
    sourceLabel: path.sourceLabel?.trim() ?? '',
    outcome: path.outcome,
    targetKind: population ? 'population' : 'place',
    destinationStageId: population
      ? destinationStageId
      : legacyInterPhasePlace
        ? destinationStageId
        : sourceStageId,
    destinationSlotKey: population
      ? ''
      : (path.destinationSlotKey?.trim() ?? ''),
    legacyInterPhasePlace,
  };
}

export function serializePaths(paths: ProgPathDraft[]): string {
  return JSON.stringify(
    paths.map((p) => ({
      sourceFixtureId: p.sourceFixtureId,
      outcome: p.outcome,
      targetKind: p.targetKind,
      destinationStageId: p.destinationStageId,
      destinationSlotKey:
        p.targetKind === 'place' ? p.destinationSlotKey : '',
      legacyInterPhasePlace: p.legacyInterPhasePlace,
    })),
  );
}

export function toApiPath(draft: ProgPathDraft): StructureProgressionPath {
  if (draft.targetKind === 'population') {
    return {
      sourceFixtureId: draft.sourceFixtureId.trim(),
      outcome: draft.outcome,
      destinationStageId: draft.destinationStageId,
      destinationSlotKey: null,
    };
  }
  return {
    sourceFixtureId: draft.sourceFixtureId.trim(),
    outcome: draft.outcome,
    destinationStageId: draft.destinationStageId,
    destinationSlotKey: draft.destinationSlotKey.trim(),
  };
}

function sourceKey(draft: ProgPathDraft): string {
  return `${draft.sourceFixtureId.trim()}|${draft.outcome}`;
}

function placeKey(draft: ProgPathDraft): string | null {
  if (draft.targetKind !== 'place') return null;
  const slot = draft.destinationSlotKey.trim();
  if (!slot) return null;
  return `${draft.destinationStageId}|${slot}`;
}

export function incompletePathReason(
  draft: ProgPathDraft,
  all: ProgPathDraft[],
  placesLabeled: boolean,
): ProgIncompleteReason | null {
  if (draft.legacyInterPhasePlace) {
    return 'LegacyPlace';
  }
  if (!draft.sourceFixtureId.trim()) {
    return 'Fixture';
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

  const sk = sourceKey(draft);
  if (
    all.some(
      (other) => other.id !== draft.id && sourceKey(other) === sk,
    )
  ) {
    return 'DuplicateSource';
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

export function isPathComplete(
  draft: ProgPathDraft,
  all: ProgPathDraft[],
  placesLabeled: boolean,
): boolean {
  return incompletePathReason(draft, all, placesLabeled) == null;
}

export function summarizePathWho(
  draft: ProgPathDraft,
  t: (key: string, opts?: Record<string, unknown>) => string,
): string {
  const outcome =
    draft.outcome === 'Winner'
      ? t('progression.outcomeWinner')
      : t('progression.outcomeLoser');
  const match =
    draft.sourceLabel.trim() ||
    (draft.sourceFixtureId.trim()
      ? t('fiche.rule.matchFallback', {
          id: draft.sourceFixtureId.trim().slice(0, 8),
        })
      : '');
  if (!match) {
    return outcome;
  }
  return t('progression.summary.who', { outcome, match });
}

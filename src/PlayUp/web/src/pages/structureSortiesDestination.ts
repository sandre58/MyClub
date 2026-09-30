// -----------------------------------------------------------------------
// Sorties Where — destination-first draft sync (Qual / Prog shared).
// Championship/Swiss → ForForm lock; Cup/Groups → Population | Place.
// -----------------------------------------------------------------------

import type { StructureFormatKind } from '../types';
import { placeGrainForFormat } from './structurePlaceLabel';

export type SortiesTargetKind = 'population' | 'place';

/** Draft fields synced by destination (Qual + Prog). */
export type SortiesDestinationFields = {
  targetKind: SortiesTargetKind;
  destinationStageId: string;
  destinationSlotKeys: string[];
  destinationGroupIds: string[];
  destinationForm: boolean;
};

/** Champ/Suisse: Structure exposes only Place → Forme (no Population|Place tiles). */
export function isFormOnlyDestination(
  formatKind: StructureFormatKind | string | null | undefined,
): boolean {
  return formatKind === 'Championship' || formatKind === 'Swiss';
}

/**
 * SoT for destination change — single sync point (no React effect chains).
 * Form-only formats lock place + destinationForm; Cup/Groups clear form and maps.
 */
export function applyDestinationToDraft<T extends SortiesDestinationFields>(
  draft: T,
  destinationStageId: string,
  formatKind: StructureFormatKind | string | null | undefined,
): T {
  if (isFormOnlyDestination(formatKind)) {
    return {
      ...draft,
      destinationStageId,
      targetKind: 'place',
      destinationForm: true,
      destinationSlotKeys: [],
      destinationGroupIds: [],
    };
  }

  // Leaving Forme (or picking Cup/Groups): Population default when coming from form.
  const targetKind: SortiesTargetKind = draft.destinationForm
    ? 'population'
    : draft.targetKind;

  return {
    ...draft,
    destinationStageId,
    targetKind,
    destinationForm: false,
    destinationSlotKeys: [],
    destinationGroupIds: [],
  };
}

/**
 * User toggles Population | Place — only meaningful for Cup/Groups.
 * No-op when draft is already Form-locked (Champ/Swiss).
 */
export function applyTargetKindToDraft<T extends SortiesDestinationFields>(
  draft: T,
  kind: SortiesTargetKind,
): T {
  if (draft.destinationForm) {
    return draft;
  }
  if (kind === 'population') {
    return {
      ...draft,
      targetKind: 'population',
      destinationForm: false,
      destinationSlotKeys: [],
      destinationGroupIds: [],
    };
  }
  return {
    ...draft,
    targetKind: 'place',
    destinationForm: false,
    destinationSlotKeys: [],
    destinationGroupIds: [],
  };
}

/**
 * Open-editor normalization: legacy ForPopulation → Champ/Swiss becomes Form draft.
 * No persist until save.
 */
export function normalizeFormOnlyDestinationDraft<
  T extends SortiesDestinationFields,
>(draft: T, formatKind: StructureFormatKind | string | null | undefined): T {
  if (!isFormOnlyDestination(formatKind)) {
    return draft;
  }
  if (
    draft.targetKind === 'place' &&
    draft.destinationForm &&
    draft.destinationSlotKeys.length === 0 &&
    draft.destinationGroupIds.length === 0
  ) {
    return draft;
  }
  return applyDestinationToDraft(draft, draft.destinationStageId, formatKind);
}

/** Whether UI should show Population | Place tiles for this destination format. */
export function showsPopulationPlaceChoice(
  formatKind: StructureFormatKind | string | null | undefined,
): boolean {
  const grain = placeGrainForFormat(formatKind);
  return grain === 'slot' || grain === 'group';
}

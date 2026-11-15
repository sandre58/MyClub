import type { TFunction } from 'i18next';

import type { StageSchematic, StructureInboundFeedRule } from '../../../types';
import {
  primaryPlaceLabel,
  type EntryPlaceProjection,
} from '../draw/structureEntryPlacementProjection';
import { placeChromeForDestinationSlotKey } from '../draw/structurePlaceLabel';
import type { InboundFeedSlotView } from './structureCompositionInboundHub';

export function inboundRuleDestinationSlotKey(
  rule: StructureInboundFeedRule | undefined,
): string | null {
  if (!rule) {
    return null;
  }
  return (
    rule.qualificationPath?.destinationSlotKey?.trim() ??
    rule.progressionPath?.destinationSlotKey?.trim() ??
    null
  );
}

export function qualifierSeatPlaceLabel(input: {
  seat: InboundFeedSlotView;
  inboundRule: StructureInboundFeedRule | undefined;
  schematic: StageSchematic | null | undefined;
  entryPlaceProjections: ReadonlyMap<string, EntryPlaceProjection>;
}): string | null {
  const { seat, inboundRule, schematic, entryPlaceProjections } = input;
  if (seat.entryId) {
    return primaryPlaceLabel(entryPlaceProjections.get(seat.entryId));
  }
  if (!seat.placed) {
    return null;
  }
  const slotKey = inboundRuleDestinationSlotKey(inboundRule);
  if (!slotKey) {
    return null;
  }
  return placeChromeForDestinationSlotKey(schematic, slotKey);
}

export function qualifierSeatPlacedTooltip(
  placed: boolean,
  placeLabel: string | null,
  t: TFunction,
): string | null {
  if (!placed) {
    return null;
  }
  if (placeLabel) {
    return t('entries.entryPlacedTooltip', { place: placeLabel });
  }
  return t('entries.entryPlacedStatus');
}

import type { TFunction } from 'i18next';

import type { StageSchematic, StructureInboundFeedRule } from '../../../types';
import {
  primaryPlaceLabel,
  type EntryPlaceProjection,
  type PlacedTooltipModel,
} from '../draw/structureEntryPlacementProjection';
import { placeChromeForDestinationSlotKey } from '../draw/structurePlaceLabel';
import type { InboundFeedSlotView } from './structureCompositionInboundHub';

export function inboundRuleDestinationSlotKey(
  rule: StructureInboundFeedRule | undefined,
): string | null {
  if (!rule) {
    return null;
  }
  const qualificationSlotKey =
    rule.qualificationPath?.destinationSlotKey?.trim() ?? '';
  if (qualificationSlotKey) {
    return qualificationSlotKey;
  }
  const progressionSlotKey =
    rule.progressionPath?.destinationSlotKey?.trim() ?? '';
  return progressionSlotKey || null;
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

export function qualifierSeatPlacedTooltipModel(
  placed: boolean,
  placeLabel: string | null,
  destinationPhaseName: string | null | undefined,
  t: TFunction,
): PlacedTooltipModel | null {
  if (!placed || !placeLabel?.trim()) {
    return null;
  }
  const phase = destinationPhaseName?.trim() ?? '';
  const where = phase ? `${phase} — ${placeLabel.trim()}` : placeLabel.trim();
  const intro = t('entries.teamAlreadyPlacedIntro');
  return {
    intro,
    whereLines: [where],
    ariaLabel: `${intro} ${where}`,
  };
}

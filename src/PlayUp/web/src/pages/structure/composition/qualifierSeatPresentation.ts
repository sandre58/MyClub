import type { StructureEntry, StructureFormatKind } from '../../../types';
import type { InboundFeedSlotView } from './structureCompositionInboundHub';
import { showPerEntryPlacement } from '../draw/structureEntryPlacementProjection';

export function qualifierSeatShowUnplacedOpacity(
  formatKind: StructureFormatKind | null | undefined,
  seat: InboundFeedSlotView,
  entry: StructureEntry | undefined,
): boolean {
  return (
    showPerEntryPlacement(formatKind) &&
    seat.resolved &&
    seat.entryId != null &&
    !seat.placed &&
    entry?.status === 'Active'
  );
}

import type {
  StructureInboundComposition,
  StructureInboundFeedSeat,
} from '../../types';

export type InboundFeedSlotView = {
  resolved: boolean;
  teamName: string | null;
  entryId: string | null;
  placed: boolean;
};

export function inboundSeatsForFeedKey(
  inbound: StructureInboundComposition | null | undefined,
  feedKey: string,
  fallbackVolume: number,
): InboundFeedSlotView[] {
  const rule = inbound?.rules.find((row) => row.feedKey === feedKey);
  if (!rule) {
    return Array.from({ length: fallbackVolume }, () => emptySeat());
  }
  const seats = rule.seats.map((seat) => mapSeat(seat));
  while (seats.length < fallbackVolume) {
    seats.push(emptySeat());
  }
  return seats.slice(0, fallbackVolume);
}

function mapSeat(seat: StructureInboundFeedSeat): InboundFeedSlotView {
  const entryId = seat.entryId?.trim() || null;
  const teamName = seat.displayName?.trim() || null;
  const resolved = Boolean(entryId);
  return {
    resolved,
    teamName: resolved ? teamName : null,
    entryId,
    placed: seat.placed,
  };
}

function emptySeat(): InboundFeedSlotView {
  return {
    resolved: false,
    teamName: null,
    entryId: null,
    placed: false,
  };
}

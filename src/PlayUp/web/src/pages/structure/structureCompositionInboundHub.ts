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

/** Hub assembler keys (regulation paths), distinct from intent-based UI feed row keys. */
export type InboundFeedRuleLookup = {
  uiFeedKey: string;
  sourceStageId: string;
  qualPathOrder?: number;
  progressionPairKey?: string;
  progressionOutcome?: string;
};

export function inboundHubFeedKey(lookup: InboundFeedRuleLookup): string {
  if (lookup.qualPathOrder != null) {
    return `q-${lookup.sourceStageId}-${lookup.qualPathOrder}`;
  }
  const pairKey = lookup.progressionPairKey?.trim();
  const outcome = lookup.progressionOutcome?.trim();
  if (pairKey && outcome) {
    return `p-${lookup.sourceStageId}-${pairKey}-${outcome}`;
  }
  return lookup.uiFeedKey;
}

export function inboundSeatsForFeedRule(
  inbound: StructureInboundComposition | null | undefined,
  lookup: InboundFeedRuleLookup,
  fallbackVolume: number,
): InboundFeedSlotView[] {
  const hubKey = inboundHubFeedKey(lookup);
  const rule =
    inbound?.rules.find((row) => row.feedKey === hubKey) ??
    inbound?.rules.find((row) => row.feedKey === lookup.uiFeedKey);
  if (!rule) {
    return Array.from({ length: fallbackVolume }, () => emptySeat());
  }
  return inboundFeedSeatsFromHub(rule.seats, fallbackVolume);
}

export function qualPathOrderFromFeedKey(
  feedKey: string,
  sourceStageId: string,
): number | null {
  const stageId = sourceStageId.trim();
  if (!stageId || !feedKey.startsWith('q-')) {
    return null;
  }
  const marker = `-${stageId}-`;
  const lowerKey = feedKey.toLowerCase();
  const lowerMarker = marker.toLowerCase();
  const at = lowerKey.indexOf(lowerMarker);
  if (at === 1) {
    const order = Number(feedKey.slice(1 + marker.length));
    if (Number.isInteger(order) && order >= 1) {
      return order;
    }
  }
  return qualPathOrderFromFeedKeyLoose(feedKey);
}

/** Hub keys end with path order: `q-{sourceStageId}-{order}` (GUID contains dashes). */
export function qualPathOrderFromFeedKeyLoose(feedKey: string): number | null {
  if (!feedKey.startsWith('q-')) {
    return null;
  }
  const lastDash = feedKey.lastIndexOf('-');
  if (lastDash <= 2) {
    return null;
  }
  const order = Number(feedKey.slice(lastDash + 1));
  return Number.isInteger(order) && order >= 1 ? order : null;
}

export function progressionPartsFromFeedKey(
  feedKey: string,
  sourceStageId: string,
): { pairKey: string; outcome: string } | null {
  const stageId = sourceStageId.trim();
  if (!stageId || !feedKey.startsWith('p-')) {
    return null;
  }
  const marker = `-${stageId}-`;
  const lowerKey = feedKey.toLowerCase();
  const lowerMarker = marker.toLowerCase();
  const at = lowerKey.indexOf(lowerMarker);
  if (at !== 1) {
    return null;
  }
  const rest = feedKey.slice(1 + marker.length);
  const outcomeMatch = rest.match(/-(Winner|Loser)$/);
  if (!outcomeMatch) {
    return null;
  }
  const outcome = outcomeMatch[1];
  const pairKey = rest.slice(0, -outcomeMatch[0].length);
  return pairKey ? { pairKey, outcome } : null;
}

export function inboundFeedSeatsFromHub(
  seats: readonly StructureInboundFeedSeat[],
  fallbackVolume?: number,
): InboundFeedSlotView[] {
  const mapped = seats.map((seat) => mapSeat(seat));
  const target = fallbackVolume ?? mapped.length;
  while (mapped.length < target) {
    mapped.push(emptySeat());
  }
  return mapped.slice(0, target);
}

export function inboundSeatsForFeedKey(
  inbound: StructureInboundComposition | null | undefined,
  feedKey: string,
  fallbackVolume: number,
): InboundFeedSlotView[] {
  return inboundSeatsForFeedRule(
    inbound,
    { uiFeedKey: feedKey, sourceStageId: '' },
    fallbackVolume,
  );
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

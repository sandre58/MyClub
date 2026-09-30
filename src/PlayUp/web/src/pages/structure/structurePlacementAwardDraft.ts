// -----------------------------------------------------------------------
// Placement awards — confrontation-centric authoring drafts.
// UI: 1 card = structural source (PairKey / interim fixture) + winnerRank? + loserRank?
// → Domain: 0..2 paths. Cup: BracketPairs before fixtures.
// -----------------------------------------------------------------------

import type {
  StageBracketPair,
  StageRound,
  StructurePlacementAward,
} from '../../types';

/** Visual grouping only — not an authoring grain. */
export type PlacementRoundSection = {
  /** Null = cards without a resolvable round (empty or unknown source). */
  roundId: string | null;
  roundName: string | null;
  cards: PlacementAwardCardDraft[];
};

export type PlacementAwardCardDraft = {
  id: string;
  /** Cup = BracketPair.PairKey. */
  sourcePairKey: string;
  /** Empty string = no Winner award. */
  winnerRank: string;
  /** Empty string = no Loser award. */
  loserRank: string;
};

export type PlacementIncompleteReason =
  | 'Fixture'
  | 'Placement'
  | 'Rank'
  | 'DuplicateFixture'
  | 'DuplicateRank'
  | 'UnknownFixture';

export type PlacementSourceOption = {
  id: string;
  label: string;
};

export function newPlacementCardId(): string {
  return crypto.randomUUID();
}

export function emptyPlacementCard(): PlacementAwardCardDraft {
  return {
    id: newPlacementCardId(),
    sourcePairKey: '',
    winnerRank: '',
    loserRank: '',
  };
}

function parsePositiveInt(raw: string): number | null {
  const trimmed = raw.trim();
  if (!trimmed) return null;
  const n = Number(trimmed);
  if (!Number.isInteger(n) || n < 1) return null;
  return n;
}

/** Parse a rank field; empty is allowed (no award for that outcome). */
export function parseOptionalRank(raw: string): number | null | 'invalid' {
  const trimmed = raw.trim();
  if (!trimmed) return null;
  const n = parsePositiveInt(trimmed);
  return n == null ? 'invalid' : n;
}

/**
 * Authoring catalogue: BracketPairs (PairKey identity).
 * Empty when the stage has no structural pairs yet.
 */
export function listPlacementSourceOptions(
  bracketPairs: readonly StageBracketPair[],
  _rounds: StageRound[],
  _formatMatch: (n: number) => string,
): PlacementSourceOption[] {
  if (bracketPairs.length === 0) {
    return [];
  }

  return [...bracketPairs]
    .sort((a, b) => a.pairKey.localeCompare(b.pairKey))
    .map((p) => ({
      id: p.pairKey,
      label: `${p.pairKey} · ${p.slotAKey} vs ${p.slotBKey}`,
    }));
}

/**
 * Group persisted paths by source PairKey into confrontation cards.
 */
export function cardsFromApiPaths(
  paths: StructurePlacementAward[],
): PlacementAwardCardDraft[] {
  const bySource = new Map<string, PlacementAwardCardDraft>();
  const order: string[] = [];

  for (const path of paths) {
    const pairKey = path.sourcePairKey?.trim() || '';
    if (!pairKey) continue;

    let card = bySource.get(pairKey);
    if (!card) {
      card = {
        id: newPlacementCardId(),
        sourcePairKey: pairKey,
        winnerRank: '',
        loserRank: '',
      };
      bySource.set(pairKey, card);
      order.push(pairKey);
    }

    if (path.outcome === 'Winner') {
      card.winnerRank = String(path.rank);
    } else if (path.outcome === 'Loser') {
      card.loserRank = String(path.rank);
    }
  }

  return order.map((id) => bySource.get(id)!);
}

/** Expand cards to Domain paths (stable: Winner then Loser per card). */
export function cardsToApiPaths(
  cards: PlacementAwardCardDraft[],
): StructurePlacementAward[] {
  const paths: StructurePlacementAward[] = [];
  for (const card of cards) {
    const sourcePairKey = card.sourcePairKey.trim();
    if (!sourcePairKey) continue;

    const winner = parseOptionalRank(card.winnerRank);
    if (typeof winner === 'number') {
      paths.push({
        sourcePairKey,
        outcome: 'Winner',
        rank: winner,
      });
    }

    const loser = parseOptionalRank(card.loserRank);
    if (typeof loser === 'number') {
      paths.push({
        sourcePairKey,
        outcome: 'Loser',
        rank: loser,
      });
    }
  }
  return paths;
}

/** Deterministic serialize for dirty detection. */
export function serializeCards(cards: PlacementAwardCardDraft[]): string {
  return JSON.stringify(
    cards.map((c) => ({
      f: c.sourcePairKey.trim(),
      w: c.winnerRank.trim(),
      l: c.loserRank.trim(),
    })),
  );
}

export function awardedRankCount(cards: PlacementAwardCardDraft[]): number {
  return cardsToApiPaths(cards).length;
}

function cardRanks(card: PlacementAwardCardDraft): number[] {
  const ranks: number[] = [];
  const winner = parseOptionalRank(card.winnerRank);
  const loser = parseOptionalRank(card.loserRank);
  if (typeof winner === 'number') ranks.push(winner);
  if (typeof loser === 'number') ranks.push(loser);
  return ranks;
}

function nextUnusedRank(used: ReadonlySet<number>, start: number): number {
  let n = Math.max(1, start);
  while (used.has(n)) n += 1;
  return n;
}

/**
 * Prefill a new card so Add does not open incomplete:
 * first unused source (catalogue order) + two lowest free ranks (Winner then Loser).
 * Source stays empty when none remain available.
 */
export function createNextPlacementCard(
  existing: PlacementAwardCardDraft[],
  sourceKeysInOrder: readonly string[],
): PlacementAwardCardDraft {
  const usedSources = new Set<string>();
  const usedRanks = new Set<number>();
  for (const card of existing) {
    const key = card.sourcePairKey.trim();
    if (key) usedSources.add(key);
    for (const rank of cardRanks(card)) usedRanks.add(rank);
  }

  const sourcePairKey =
    sourceKeysInOrder.find((id) => id && !usedSources.has(id)) ?? '';

  const winner = nextUnusedRank(usedRanks, 1);
  usedRanks.add(winner);
  const loser = nextUnusedRank(usedRanks, winner + 1);

  return {
    id: newPlacementCardId(),
    sourcePairKey,
    winnerRank: String(winner),
    loserRank: String(loser),
  };
}

export function incompleteCardReason(
  card: PlacementAwardCardDraft,
  all: PlacementAwardCardDraft[],
  knownSourceKeys: ReadonlySet<string>,
): PlacementIncompleteReason | null {
  const sourceKey = card.sourcePairKey.trim();
  if (!sourceKey) return 'Fixture';

  if (knownSourceKeys.size > 0 && !knownSourceKeys.has(sourceKey)) {
    return 'UnknownFixture';
  }

  const winner = parseOptionalRank(card.winnerRank);
  const loser = parseOptionalRank(card.loserRank);
  if (winner === 'invalid' || loser === 'invalid') return 'Rank';
  if (winner == null && loser == null) return 'Placement';

  const duplicateSource = all.some(
    (other) => other.id !== card.id && other.sourcePairKey.trim() === sourceKey,
  );
  if (duplicateSource) return 'DuplicateFixture';

  const mine = cardRanks(card);
  if (mine.length !== new Set(mine).size) return 'DuplicateRank';

  const others = new Set<number>();
  for (const other of all) {
    if (other.id === card.id) continue;
    for (const r of cardRanks(other)) others.add(r);
  }
  if (mine.some((r) => others.has(r))) return 'DuplicateRank';

  return null;
}

export function isCardComplete(
  card: PlacementAwardCardDraft,
  all: PlacementAwardCardDraft[],
  knownSourceKeys: ReadonlySet<string>,
): boolean {
  return incompleteCardReason(card, all, knownSourceKeys) == null;
}

export function areCardsComplete(
  cards: PlacementAwardCardDraft[],
  knownSourceKeys: ReadonlySet<string>,
): boolean {
  return cards.every((c) => isCardComplete(c, cards, knownSourceKeys));
}

/** Soft warning: awarded ranks have a gap (e.g. 1,2,4). Suite 3–4 is fine. */
export function hasNonContiguousRanks(
  cards: PlacementAwardCardDraft[],
): boolean {
  const ranks = cardsToApiPaths(cards)
    .map((p) => p.rank)
    .sort((a, b) => a - b);
  if (ranks.length < 2) return false;
  for (let i = 1; i < ranks.length; i++) {
    if (ranks[i] !== ranks[i - 1] + 1) return true;
  }
  return false;
}

/**
 * Group attributed cards under round headings (overview order).
 * PairKeys on Cup mono-round → sole round;
 * unknown sources → orphan section.
 */
export function groupCardsByRound(
  cards: PlacementAwardCardDraft[],
  rounds: Pick<StageRound, 'id' | 'name' | 'fixtures'>[],
  bracketPairs: readonly StageBracketPair[] = [],
): PlacementRoundSection[] {
  const sourceToRound = new Map<
    string,
    { roundId: string; roundName: string; sortIndex: number }
  >();

  if (bracketPairs.length > 0 && rounds.length === 1) {
    const round = rounds[0]!;
    [...bracketPairs]
      .sort((a, b) => a.pairKey.localeCompare(b.pairKey))
      .forEach((pair, index) => {
        sourceToRound.set(pair.pairKey, {
          roundId: round.id,
          roundName: round.name,
          sortIndex: index,
        });
      });
  } else if (bracketPairs.length > 0) {
    // Multi-round Cup not modeled: map all pairs to the first round for grouping only.
    const round = rounds[0];
    if (round) {
      [...bracketPairs]
        .sort((a, b) => a.pairKey.localeCompare(b.pairKey))
        .forEach((pair, index) => {
          sourceToRound.set(pair.pairKey, {
            roundId: round.id,
            roundName: round.name,
            sortIndex: index,
          });
        });
    }
  }

  const byRound = new Map<string, PlacementAwardCardDraft[]>();
  const orphans: PlacementAwardCardDraft[] = [];

  for (const card of cards) {
    const key = card.sourcePairKey.trim();
    if (!key) {
      orphans.push(card);
      continue;
    }
    const meta = sourceToRound.get(key);
    if (!meta) {
      orphans.push(card);
      continue;
    }
    const list = byRound.get(meta.roundId) ?? [];
    list.push(card);
    byRound.set(meta.roundId, list);
  }

  const sections: PlacementRoundSection[] = [];
  for (const round of rounds) {
    const list = byRound.get(round.id);
    if (!list || list.length === 0) continue;
    const sorted = [...list].sort((a, b) => {
      const ai = sourceToRound.get(a.sourcePairKey.trim())?.sortIndex ?? 0;
      const bi = sourceToRound.get(b.sourcePairKey.trim())?.sortIndex ?? 0;
      return ai - bi;
    });
    sections.push({
      roundId: round.id,
      roundName: round.name,
      cards: sorted,
    });
  }

  if (orphans.length > 0) {
    sections.push({
      roundId: null,
      roundName: null,
      cards: orphans,
    });
  }

  return sections;
}

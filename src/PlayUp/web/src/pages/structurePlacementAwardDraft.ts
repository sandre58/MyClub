// -----------------------------------------------------------------------
// Attribution des places — confrontation-centric authoring drafts.
// UI: 1 card = fixture + winnerRank? + loserRank? → Domain: 0..2 paths.
// -----------------------------------------------------------------------

import type { StageRound, StructurePlacementAward } from '../types';

/** Visual grouping only — not an authoring grain. */
export type PlacementRoundSection = {
  /** Null = cards without a resolvable round (empty or unknown fixture). */
  roundId: string | null;
  roundName: string | null;
  cards: PlacementAwardCardDraft[];
};

export type PlacementAwardCardDraft = {
  id: string;
  sourceFixtureId: string;
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

export function newPlacementCardId(): string {
  return crypto.randomUUID();
}

export function emptyPlacementCard(): PlacementAwardCardDraft {
  return {
    id: newPlacementCardId(),
    sourceFixtureId: '',
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
 * Group persisted paths by fixture into confrontation cards.
 * Paths without a fixture id are skipped (cannot form a card).
 */
export function cardsFromApiPaths(
  paths: StructurePlacementAward[],
): PlacementAwardCardDraft[] {
  const byFixture = new Map<string, PlacementAwardCardDraft>();
  const order: string[] = [];

  for (const path of paths) {
    const fixtureId = path.sourceFixtureId?.trim() ?? '';
    if (!fixtureId) continue;

    let card = byFixture.get(fixtureId);
    if (!card) {
      card = {
        id: newPlacementCardId(),
        sourceFixtureId: fixtureId,
        winnerRank: '',
        loserRank: '',
      };
      byFixture.set(fixtureId, card);
      order.push(fixtureId);
    }

    if (path.outcome === 'Winner') {
      card.winnerRank = String(path.rank);
    } else if (path.outcome === 'Loser') {
      card.loserRank = String(path.rank);
    }
  }

  return order.map((id) => byFixture.get(id)!);
}

/** Expand cards to Domain paths (stable: Winner then Loser per card). */
export function cardsToApiPaths(
  cards: PlacementAwardCardDraft[],
): StructurePlacementAward[] {
  const paths: StructurePlacementAward[] = [];
  for (const card of cards) {
    const fixtureId = card.sourceFixtureId.trim();
    if (!fixtureId) continue;

    const winner = parseOptionalRank(card.winnerRank);
    if (typeof winner === 'number') {
      paths.push({
        sourceFixtureId: fixtureId,
        outcome: 'Winner',
        rank: winner,
      });
    }

    const loser = parseOptionalRank(card.loserRank);
    if (typeof loser === 'number') {
      paths.push({
        sourceFixtureId: fixtureId,
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
      f: c.sourceFixtureId.trim(),
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
 * first unused fixture (catalogue order) + two lowest free ranks (Winner then Loser).
 * Fixture stays empty when none remain available.
 */
export function createNextPlacementCard(
  existing: PlacementAwardCardDraft[],
  fixtureIdsInOrder: readonly string[],
): PlacementAwardCardDraft {
  const usedFixtures = new Set<string>();
  const usedRanks = new Set<number>();
  for (const card of existing) {
    const fixtureId = card.sourceFixtureId.trim();
    if (fixtureId) usedFixtures.add(fixtureId);
    for (const rank of cardRanks(card)) usedRanks.add(rank);
  }

  const sourceFixtureId =
    fixtureIdsInOrder.find((id) => id && !usedFixtures.has(id)) ?? '';

  const winner = nextUnusedRank(usedRanks, 1);
  usedRanks.add(winner);
  const loser = nextUnusedRank(usedRanks, winner + 1);

  return {
    id: newPlacementCardId(),
    sourceFixtureId,
    winnerRank: String(winner),
    loserRank: String(loser),
  };
}

export function incompleteCardReason(
  card: PlacementAwardCardDraft,
  all: PlacementAwardCardDraft[],
  knownFixtureIds: ReadonlySet<string>,
): PlacementIncompleteReason | null {
  const fixtureId = card.sourceFixtureId.trim();
  if (!fixtureId) return 'Fixture';

  if (knownFixtureIds.size > 0 && !knownFixtureIds.has(fixtureId)) {
    return 'UnknownFixture';
  }

  const winner = parseOptionalRank(card.winnerRank);
  const loser = parseOptionalRank(card.loserRank);
  if (winner === 'invalid' || loser === 'invalid') return 'Rank';
  if (winner == null && loser == null) return 'Placement';

  const duplicateFixture = all.some(
    (other) =>
      other.id !== card.id && other.sourceFixtureId.trim() === fixtureId,
  );
  if (duplicateFixture) return 'DuplicateFixture';

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
  knownFixtureIds: ReadonlySet<string>,
): boolean {
  return incompleteCardReason(card, all, knownFixtureIds) == null;
}

export function areCardsComplete(
  cards: PlacementAwardCardDraft[],
  knownFixtureIds: ReadonlySet<string>,
): boolean {
  return cards.every((c) => isCardComplete(c, cards, knownFixtureIds));
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

export function summarizeCardWho(
  card: PlacementAwardCardDraft,
  fixtureLabel: string | null,
  t: (key: string, opts?: Record<string, unknown>) => string,
): string {
  if (!card.sourceFixtureId.trim()) {
    return t('attribution.newCard');
  }
  return fixtureLabel?.trim() || t('attribution.unknownFixture');
}

export function summarizeCardWhere(
  card: PlacementAwardCardDraft,
  t: (key: string, opts?: Record<string, unknown>) => string,
): string | null {
  const winner = parseOptionalRank(card.winnerRank);
  const loser = parseOptionalRank(card.loserRank);
  const parts: string[] = [];
  if (typeof winner === 'number') {
    parts.push(t('attribution.summary.winnerRank', { rank: winner }));
  }
  if (typeof loser === 'number') {
    parts.push(t('attribution.summary.loserRank', { rank: loser }));
  }
  if (parts.length === 0) return null;
  return parts.join(' · ');
}

/**
 * Group attributed cards under round headings (overview order).
 * Only cards present in `cards` appear — never the full fixture catalogue.
 * Empty / unknown fixtures land in a trailing orphan section (`roundId: null`).
 */
export function groupCardsByRound(
  cards: PlacementAwardCardDraft[],
  rounds: Pick<StageRound, 'id' | 'name' | 'fixtures'>[],
): PlacementRoundSection[] {
  const fixtureToRound = new Map<
    string,
    { roundId: string; roundName: string; fixtureIndex: number }
  >();
  for (const round of rounds) {
    round.fixtures.forEach((fixture, index) => {
      fixtureToRound.set(fixture.id, {
        roundId: round.id,
        roundName: round.name,
        fixtureIndex: index,
      });
    });
  }

  const byRound = new Map<string, PlacementAwardCardDraft[]>();
  const orphans: PlacementAwardCardDraft[] = [];

  for (const card of cards) {
    const fixtureId = card.sourceFixtureId.trim();
    if (!fixtureId) {
      orphans.push(card);
      continue;
    }
    const meta = fixtureToRound.get(fixtureId);
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
      const ai =
        fixtureToRound.get(a.sourceFixtureId.trim())?.fixtureIndex ?? 0;
      const bi =
        fixtureToRound.get(b.sourceFixtureId.trim())?.fixtureIndex ?? 0;
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

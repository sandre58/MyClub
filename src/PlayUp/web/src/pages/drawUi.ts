import type {
  DrawResolutionState,
  DrawStatus,
  StageDraw,
  StageDrawGroupPlacement,
  StageDrawSlotPlacement,
  StageFixture,
  StageRound,
  StageSlot,
} from '../types';

export type GroupPlacementEntry = {
  entryId: string;
  displayName: string;
  logoMediaId?: string | null;
  primaryColor?: string | null;
};

export type GroupPlacementRow = {
  groupId: string;
  groupLabel: string;
  entries: GroupPlacementEntry[];
};

export type SlotConfrontationSide = {
  slotKey: string;
  entryId: string;
  displayName: string;
  shortName?: string | null;
  logoMediaId?: string | null;
  primaryColor?: string | null;
};

/** Dense Slot result row — confrontation product, not fixture / Match #. */
export type SlotConfrontationRow = {
  key: string;
  sideA: SlotConfrontationSide;
  sideB: SlotConfrontationSide;
};

/** Collapse flat groupPlacements into one row per group (list density, not nested cards). */
export function groupPlacementRows(
  placements: StageDrawGroupPlacement[],
  unknownEntry: string,
  unknownGroup: string,
): GroupPlacementRow[] {
  const byGroup = new Map<string, GroupPlacementRow>();
  for (const placement of placements) {
    let row = byGroup.get(placement.groupId);
    if (!row) {
      row = {
        groupId: placement.groupId,
        groupLabel: placement.groupDisplayName?.trim() || unknownGroup,
        entries: [],
      };
      byGroup.set(placement.groupId, row);
    }
    row.entries.push({
      entryId: placement.entryId,
      displayName: placement.displayName?.trim() || unknownEntry,
      logoMediaId: placement.logoMediaId,
      primaryColor: placement.primaryColor,
    });
  }
  return [...byGroup.values()].sort((a, b) =>
    a.groupLabel.localeCompare(b.groupLabel, undefined, {
      numeric: true,
      sensitivity: 'base',
    }),
  );
}

function toSlotConfrontationSide(
  placement: StageDrawSlotPlacement,
  unknownEntry: string,
): SlotConfrontationSide {
  return {
    slotKey: placement.slotKey.trim(),
    entryId: placement.entryId,
    displayName: placement.displayName?.trim() || unknownEntry,
    shortName: placement.shortName,
    logoMediaId: placement.logoMediaId,
    primaryColor: placement.primaryColor,
  };
}

/** `R16-1-A` → stem `R16-1`, side A — Cup slot pairing convention. */
function slotPairParts(
  slotKey: string,
): { stem: string; side: 'A' | 'B' } | null {
  const match = /^(.+)-([ABab])$/.exec(slotKey.trim());
  if (!match) {
    return null;
  }
  return {
    stem: match[1]!,
    side: match[2]!.toUpperCase() as 'A' | 'B',
  };
}

/**
 * Slot draw product for Exécutions: dense A vs B confrontations (no Match #).
 * Prefer fixture slot pairs from StageOverview; fallback to `*-A`/`*-B` stems.
 */
export function slotConfrontationRows(
  placements: StageDrawSlotPlacement[],
  rounds: StageRound[],
  unknownEntry: string,
): SlotConfrontationRow[] {
  const byKey = new Map(
    placements.map((p) => [p.slotKey.trim(), p] as const),
  );
  const used = new Set<string>();
  const rows: SlotConfrontationRow[] = [];

  for (const fixture of listStageFixturesInOrder(rounds)) {
    const aKey = fixture.slotAKey?.trim() || '';
    const bKey = fixture.slotBKey?.trim() || '';
    if (!aKey || !bKey) {
      continue;
    }
    const placementA = byKey.get(aKey);
    const placementB = byKey.get(bKey);
    if (!placementA || !placementB) {
      continue;
    }
    used.add(aKey);
    used.add(bKey);
    rows.push({
      key: `${aKey}|${bKey}`,
      sideA: toSlotConfrontationSide(placementA, unknownEntry),
      sideB: toSlotConfrontationSide(placementB, unknownEntry),
    });
  }

  const byStem = new Map<string, { a?: StageDrawSlotPlacement; b?: StageDrawSlotPlacement }>();
  for (const placement of placements) {
    const key = placement.slotKey.trim();
    if (used.has(key)) {
      continue;
    }
    const parts = slotPairParts(key);
    if (!parts) {
      continue;
    }
    let pair = byStem.get(parts.stem);
    if (!pair) {
      pair = {};
      byStem.set(parts.stem, pair);
    }
    if (parts.side === 'A') {
      pair.a = placement;
    } else {
      pair.b = placement;
    }
  }

  const stemKeys = [...byStem.keys()].sort((a, b) =>
    a.localeCompare(b, undefined, { numeric: true, sensitivity: 'base' }),
  );
  for (const stem of stemKeys) {
    const pair = byStem.get(stem)!;
    if (!pair.a || !pair.b) {
      continue;
    }
    const aKey = pair.a.slotKey.trim();
    const bKey = pair.b.slotKey.trim();
    used.add(aKey);
    used.add(bKey);
    rows.push({
      key: `${aKey}|${bKey}`,
      sideA: toSlotConfrontationSide(pair.a, unknownEntry),
      sideB: toSlotConfrontationSide(pair.b, unknownEntry),
    });
  }

  return rows;
}

/**
 * Why « Nouveau tirage » (G2) cannot run — mirrors Domain / DrawInputsFactory fail-closed
 * checks the SPA can see without calling Generate (avoids 400 + orphan Draft).
 */
export type DrawCreateBlockReason =
  | 'unsupported'
  | 'active'
  | 'emptyPool'
  | 'emptyPoolUpstream'
  | 'oddPool'
  | 'missingPots'
  | 'groupShape';

export type DrawCreateGate =
  | { ok: true }
  | { ok: false; reason: DrawCreateBlockReason };

/**
 * Client gate for Create+Generate. Encoding F: pool = CompositionEntries only.
 * Pairing: even pool. Group: pots ≥ 2 and entries = pots × groupCount.
 */
export function resolveDrawCreateGate(input: {
  kind: 'Group' | 'Pairing' | 'Slot' | null;
  hasActiveDraw: boolean;
  compositionEntryCount: number;
  /** False / undefined when the phase is fed by Qual/Prog (teams arrive from upstream). */
  isRootComposition?: boolean;
  numberOfPots: number | null | undefined;
  groupCount: number | null | undefined;
}): DrawCreateGate {
  if (input.kind == null) {
    return { ok: false, reason: 'unsupported' };
  }
  if (input.hasActiveDraw) {
    return { ok: false, reason: 'active' };
  }

  const pool = input.compositionEntryCount;
  if (pool <= 0) {
    return {
      ok: false,
      reason:
        input.isRootComposition === false ? 'emptyPoolUpstream' : 'emptyPool',
    };
  }

  if (input.kind === 'Pairing') {
    return pool % 2 === 0
      ? { ok: true }
      : { ok: false, reason: 'oddPool' };
  }

  if (input.kind === 'Group') {
    const pots = input.numberOfPots ?? null;
    const groups = input.groupCount ?? 0;
    if (pots == null || pots < 2) {
      return { ok: false, reason: 'missingPots' };
    }
    if (groups < 1 || pool !== pots * groups) {
      return { ok: false, reason: 'groupShape' };
    }
    return { ok: true };
  }

  // Slot (Cup Nouveau): Encoding F — pool non-empty; destinations = stage slots (Host).
  return { ok: true };
}

/**
 * Synthetic chrome flags — projection keeps three axes; UI does not teach the matrix
 * via three systematic badges (PublishAndApply decision).
 */
export type DrawChromeFlags = {
  showStatus: boolean;
  showResolution: boolean;
  showApplied: boolean;
};

/** Single master-rail chip — principal observable status only (or none). */
export type DrawMasterChip =
  | { kind: 'lifecycle'; status: Extract<DrawStatus, 'Cancelled' | 'Published'> }
  | { kind: 'resolution'; state: Extract<DrawResolutionState, 'Resolved' | 'NoSolution'> }
  | { kind: 'applied' }
  | null;

/**
 * UI-only projection of DrawStatus × DrawResolutionState (+ derived Applied).
 * Status colours come from the shared badge tones (see ui.tsx).
 */
export type DrawUiProjection = {
  /** i18n key under the `draw` namespace (SPA owns copy). */
  messageKey: string;
  showResults: boolean;
  isApplied: boolean;
  chrome: DrawChromeFlags;
  /**
   * One optional chip for history tiles (rail V1).
   * Never teaches the full 3-axis matrix — detail carries phrase + chrome.
   */
  masterChip: DrawMasterChip;
};

/** Topology badge states — mirrors server StructureDrawExecutionBadge. */
export type TopologyDrawExecutionBadge =
  | 'ToLaunch'
  | 'InProgress'
  | 'ToApply'
  | 'Applied';

/**
 * Draw ids are UUID v7 in production — lexicographic order ≈ creation order.
 * Do not trust API array order alone (Include without OrderBy can invert / scramble).
 */
function compareDrawCreationOrder(a: StageDraw, b: StageDraw): number {
  return a.id < b.id ? -1 : a.id > b.id ? 1 : 0;
}

/** Oldest execution first (stable chronological identity). */
export function sortDrawsOldestFirst(draws: StageDraw[]): StageDraw[] {
  if (draws.length <= 1) {
    return draws;
  }
  return [...draws].sort(compareDrawCreationOrder);
}

/**
 * Newest execution first — rail / default selection.
 * Sorted by draw id (UUID v7), not by a blind reverse of the payload order.
 */
export function sortDrawsNewestFirst(draws: StageDraw[]): StageDraw[] {
  if (draws.length <= 1) {
    return draws;
  }
  return [...draws].sort((a, b) => compareDrawCreationOrder(b, a));
}

/** Chronological label #1…#N (oldest = 1), independent of rail display order. */
export function drawExecutionNumber(
  draws: StageDraw[],
  drawId: string,
): number {
  const index = sortDrawsOldestFirst(draws).findIndex((d) => d.id === drawId);
  return index < 0 ? 0 : index + 1;
}

/**
 * Current non-cancelled draw for Structure chrome (newest first).
 * Cancelled-only history → null (capacity / config, not engagement).
 */
export function pickActiveDraw(draws: StageDraw[]): StageDraw | null {
  if (draws.length === 0) {
    return null;
  }
  return (
    sortDrawsNewestFirst(draws).find((d) => d.status !== 'Cancelled') ?? null
  );
}

/** Default selection id for the Tirage dialog (prefers active, else newest). */
export function pickDefaultDrawId(draws: StageDraw[]): string | null {
  if (draws.length === 0) {
    return null;
  }
  const newestFirst = sortDrawsNewestFirst(draws);
  const active = newestFirst.find((d) => d.status !== 'Cancelled');
  return (active ?? newestFirst[0])?.id ?? null;
}

/**
 * Rail V1 identity chip — observable principal status only.
 * Priority: Cancelled > NoSolution > Applied > Published > Resolved.
 * Draft / NotResolved → no chip (G2 makes that state rare; detail can still show it).
 */
export function resolveDrawMasterChip(
  draw: StageDraw,
  isApplied: boolean,
): DrawMasterChip {
  if (draw.status === 'Cancelled') {
    return { kind: 'lifecycle', status: 'Cancelled' };
  }
  if (draw.resolutionState === 'NoSolution') {
    return { kind: 'resolution', state: 'NoSolution' };
  }
  if (isApplied) {
    return { kind: 'applied' };
  }
  if (draw.status === 'Published') {
    return { kind: 'lifecycle', status: 'Published' };
  }
  if (draw.resolutionState === 'Resolved') {
    return { kind: 'resolution', state: 'Resolved' };
  }
  return null;
}

/**
 * Detail header chips — synthetic business state (not the rail’s single principal chip).
 * Applied keeps Publié · Appliqué so Publish ≠ Apply stays readable in the detail.
 */
export type DrawDetailHeaderChip =
  | { kind: 'lifecycle'; status: Extract<DrawStatus, 'Published' | 'Cancelled'> }
  | { kind: 'resolution'; state: Extract<DrawResolutionState, 'Resolved' | 'NoSolution'> }
  | { kind: 'applied' };

export function resolveDrawDetailHeaderChips(
  draw: StageDraw,
  isApplied: boolean,
): DrawDetailHeaderChip[] {
  if (draw.status === 'Cancelled') {
    return [{ kind: 'lifecycle', status: 'Cancelled' }];
  }
  if (draw.resolutionState === 'NoSolution') {
    return [{ kind: 'resolution', state: 'NoSolution' }];
  }
  if (draw.status === 'Published' && isApplied) {
    return [
      { kind: 'lifecycle', status: 'Published' },
      { kind: 'applied' },
    ];
  }
  if (draw.status === 'Published') {
    return [{ kind: 'lifecycle', status: 'Published' }];
  }
  if (draw.resolutionState === 'Resolved') {
    return [{ kind: 'resolution', state: 'Resolved' }];
  }
  return [];
}

/**
 * Detail guidance under the header — phrase (calm) or Alert (needs attention).
 * Never Alert success for Applied.
 */
export type DrawDetailGuidance =
  | { kind: 'phrase'; messageKey: string }
  | { kind: 'alert'; tone: 'warning' | 'danger'; messageKey: string }
  | null;

export function resolveDrawDetailGuidance(
  ui: DrawUiProjection,
): DrawDetailGuidance {
  if (
    ui.messageKey === 'cancelled' ||
    ui.messageKey === 'draftNotResolved' ||
    ui.messageKey === 'fallback'
  ) {
    return null;
  }
  if (ui.messageKey === 'noSolution') {
    return { kind: 'alert', tone: 'danger', messageKey: ui.messageKey };
  }
  if (ui.messageKey === 'published') {
    return { kind: 'alert', tone: 'warning', messageKey: ui.messageKey };
  }
  if (ui.isApplied || ui.messageKey === 'draftResolved') {
    // Calm phrases live on the matching chip tooltip — chips already carry the status.
    return null;
  }
  return { kind: 'phrase', messageKey: ui.messageKey };
}

/**
 * Client mirror of StructureViewAssembler.ResolveDrawExecutionBadge.
 * Prefer server `stage.drawExecutionBadge` on the hub; use this for unit tests / SPA-only paths.
 */
export function resolveTopologyDrawExecutionBadge(
  hasDrawRules: boolean,
  draw: StageDraw | null,
  slots: StageSlot[],
  rounds: StageRound[] = [],
): TopologyDrawExecutionBadge | null {
  if (!hasDrawRules) {
    return null;
  }
  if (!draw) {
    return 'ToLaunch';
  }
  if (draw.status === 'Draft') {
    return 'InProgress';
  }
  if (draw.status === 'Published') {
    const applied = getDrawUiProjection(draw, slots, rounds).isApplied;
    return applied ? 'Applied' : 'ToApply';
  }
  return 'ToLaunch';
}

/**
 * Pure projection: server enums → message key + flags for conditional rendering.
 * Not a Domain state machine — only helps the component avoid nested if spaghetti.
 *
 * Prefer server `draw.isApplied` (DrawAppliedState) when present; else Slot/Pairing heuristics.
 * Applied is only meaningful for Published draws (Cancel does not clear stage occupancy).
 */
export function getDrawUiProjection(
  draw: StageDraw,
  slots: StageSlot[],
  rounds: StageRound[] = [],
): DrawUiProjection {
  const occupancyApplied =
    draw.resolutionState === 'Resolved' &&
    (typeof draw.isApplied === 'boolean'
      ? draw.isApplied
      : (draw.kind === 'Slot' && isSlotDrawApplied(draw, slots)) ||
        (draw.kind === 'Pairing' && isPairingDrawApplied(draw, rounds)));
  const isApplied = draw.status === 'Published' && occupancyApplied;
  const masterChip = resolveDrawMasterChip(draw, isApplied);

  if (draw.status === 'Cancelled') {
    return {
      messageKey: 'cancelled',
      showResults: draw.resolutionState === 'Resolved',
      isApplied: false,
      chrome: { showStatus: true, showResolution: false, showApplied: false },
      masterChip,
    };
  }

  if (draw.status === 'Draft' && draw.resolutionState === 'NotResolved') {
    return {
      messageKey: 'draftNotResolved',
      showResults: false,
      isApplied: false,
      chrome: { showStatus: true, showResolution: true, showApplied: false },
      masterChip,
    };
  }

  if (draw.status === 'Draft' && draw.resolutionState === 'Resolved') {
    return {
      messageKey: 'draftResolved',
      showResults: true,
      isApplied: false,
      // Brouillon · Résolu — phrase carries “ready to apply”
      chrome: { showStatus: true, showResolution: true, showApplied: false },
      masterChip,
    };
  }

  if (draw.status === 'Draft' && draw.resolutionState === 'NoSolution') {
    return {
      messageKey: 'noSolution',
      showResults: false,
      isApplied: false,
      chrome: { showStatus: true, showResolution: true, showApplied: false },
      masterChip,
    };
  }

  if (draw.status === 'Published' && draw.resolutionState === 'Resolved') {
    let messageKey = 'published';
    if (isApplied && draw.kind === 'Slot') {
      messageKey = 'publishedSlotApplied';
    } else if (isApplied && draw.kind === 'Group') {
      messageKey = 'publishedGroupApplied';
    } else if (isApplied && draw.kind === 'Pairing') {
      messageKey = 'publishedPairingApplied';
    }

    return {
      messageKey,
      showResults: true,
      isApplied,
      // Publié · (Appliqué when done) — no “Non appliqué” badge on happy recovery path
      chrome: {
        showStatus: true,
        showResolution: false,
        showApplied: isApplied,
      },
      masterChip,
    };
  }

  return {
    messageKey: 'fallback',
    showResults: draw.resolutionState === 'Resolved',
    isApplied: false,
    chrome: { showStatus: true, showResolution: true, showApplied: false },
    masterChip,
  };
}

/**
 * Derived UI (Slot only): placements already occupy the matching stage slots.
 * Compares entry identities from the same StageOverview — not a Domain status.
 */
export function isSlotDrawApplied(
  draw: StageDraw,
  slots: StageSlot[],
): boolean {
  if (draw.kind !== 'Slot' || draw.resolutionState !== 'Resolved') {
    return false;
  }

  if (draw.slotPlacements.length === 0) {
    return false;
  }

  const byKey = new Map(slots.map((slot) => [slot.slotKey, slot]));

  return draw.slotPlacements.every((placement) => {
    const slot = byKey.get(placement.slotKey);
    return slot?.entryId != null && slot.entryId === placement.entryId;
  });
}

/**
 * Fixtures in StageOverview order: rounds then fixtures within each round.
 * Host/repository reorder by SortOrder on load — this is the stable 1:1 source for Pairing Apply.
 */
export function listStageFixturesInOrder(rounds: StageRound[]): StageFixture[] {
  return rounds.flatMap((round) => round.fixtures);
}

/**
 * Strict automap: pairing[i] → fixture[i] only when counts match and both > 0.
 * Returns null when the UI must block Apply (ambiguous / incomplete mapping).
 */
export function resolvePairingFixtureIds(
  draw: StageDraw,
  rounds: StageRound[],
): string[] | null {
  if (draw.kind !== 'Pairing' || draw.resolutionState !== 'Resolved') {
    return null;
  }

  const fixtures = listStageFixturesInOrder(rounds);
  if (
    draw.pairings.length === 0 ||
    fixtures.length === 0 ||
    draw.pairings.length !== fixtures.length
  ) {
    return null;
  }

  return fixtures.map((fixture) => fixture.id);
}

/**
 * Lightweight Pairing “applied” heuristic from StageOverview only:
 * same count as automap + every target fixture already has at least one attachment.
 * Does not verify entry identities (would need the matches query).
 */
export function isPairingDrawApplied(
  draw: StageDraw,
  rounds: StageRound[],
): boolean {
  const fixtureIds = resolvePairingFixtureIds(draw, rounds);
  if (fixtureIds === null) {
    return false;
  }

  const fixtures = listStageFixturesInOrder(rounds);
  return fixtures.every((fixture) => fixture.attachments.length > 0);
}

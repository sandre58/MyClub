import type {
  DrawResolutionState,
  DrawStatus,
  StageBracketPair,
  StageDraw,
  StageDrawGroupPlacement,
  StageDrawSlotPlacement,
  StageSlot,
  StructureFormatKind,
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

/**
 * Slot draw product for Executions: dense A vs B from Domain BracketPairs.
 * Leftovers (placement without a covering pair) stay as flat Place → Entry —
 * never drop Resolved placements. No *-A/*-B stem heuristic; no Fixture pairing.
 */
export type SlotDrawResultProjection = {
  confrontations: SlotConfrontationRow[];
  unpaired: SlotConfrontationSide[];
};

export function projectSlotDrawResult(
  placements: StageDrawSlotPlacement[],
  bracketPairs: ReadonlyArray<StageBracketPair>,
  unknownEntry: string,
): SlotDrawResultProjection {
  const byKey = new Map(placements.map((p) => [p.slotKey.trim(), p] as const));
  const used = new Set<string>();
  const confrontations: SlotConfrontationRow[] = [];

  for (const pair of bracketPairs) {
    const aKey = pair.slotAKey.trim();
    const bKey = pair.slotBKey.trim();
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
    confrontations.push({
      key: pair.pairKey.trim() || `${aKey}|${bKey}`,
      sideA: toSlotConfrontationSide(placementA, unknownEntry),
      sideB: toSlotConfrontationSide(placementB, unknownEntry),
    });
  }

  const unpaired = placements
    .filter((placement) => !used.has(placement.slotKey.trim()))
    .map((placement) => toSlotConfrontationSide(placement, unknownEntry))
    .sort((a, b) =>
      a.slotKey.localeCompare(b.slotKey, undefined, {
        numeric: true,
        sensitivity: 'base',
      }),
    );

  return { confrontations, unpaired };
}

/** Confrontations only — prefer {@link projectSlotDrawResult} when unpaired matter. */
export function slotConfrontationRows(
  placements: StageDrawSlotPlacement[],
  bracketPairs: ReadonlyArray<StageBracketPair>,
  unknownEntry: string,
): SlotConfrontationRow[] {
  return projectSlotDrawResult(placements, bracketPairs, unknownEntry)
    .confrontations;
}

/**
 * Why a new-draw gesture cannot run — UI gate to start Rerun only.
 * Domain Generate/Apply remain the final invariants.
 */
export type DrawCreateBlockReason =
  | 'unsupported'
  | 'active'
  | 'emptyPool'
  | 'emptyPoolUpstream'
  | 'missingPots'
  | 'groupShape'
  | 'belowMinimumTeams'
  | 'countMismatch'
  | 'directAssignment'
  | 'occupiedSlots';

export type DrawCreateGate =
  { ok: true } | { ok: false; reason: DrawCreateBlockReason };

/**
 * Client gate for Create+Generate (Rerun workflow start).
 * Encoding F: pool = CompositionEntries only.
 * Group: pots ≥ 2 and entries = pots × groupCount.
 * Slot (Cup): pool non-empty, composition == Places, no DirectAssignment,
 * all Places free (entryId null). Priority is fixed — do not reorder without product decision.
 */
export function resolveDrawCreateGate(input: {
  kind: 'Group' | 'Slot' | null;
  hasActiveDraw: boolean;
  compositionEntryCount: number;
  /** False / undefined when the phase is fed by Qual/Prog (teams arrive from upstream). */
  isRootComposition?: boolean;
  numberOfPots: number | null | undefined;
  groupCount: number | null | undefined;
  /** Cup Places N (compositionCapacity / entry places). Required for Slot equality. */
  slotCount?: number | null;
  /** Competition EntryRules.MinimumTeams — blocks when composition is below. */
  minimumTeams?: number | null;
  /** True when any Place has a DirectAssignment configured. */
  hasDirectAssignment?: boolean;
  /**
   * True when any Place has entryId set (occupant).
   * Fed-but-vacant Qual/Prog (entryId null) must NOT set this.
   */
  hasOccupiedSlots?: boolean;
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

  const minimumTeams = input.minimumTeams ?? null;
  if (minimumTeams != null && minimumTeams > 0 && pool < minimumTeams) {
    return { ok: false, reason: 'belowMinimumTeams' };
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

  // Slot (Cup) — Rerun only on a free Places grid with exact population coverage.
  const places = input.slotCount ?? null;
  if (places == null || places <= 0 || pool !== places) {
    return { ok: false, reason: 'countMismatch' };
  }
  if (input.hasDirectAssignment) {
    return { ok: false, reason: 'directAssignment' };
  }
  if (input.hasOccupiedSlots) {
    return { ok: false, reason: 'occupiedSlots' };
  }
  return { ok: true };
}

/** Occupant = entryId present; vacant Qual/Prog feed does not count. */
export function slotsHaveOccupants(
  slots: ReadonlyArray<{ entryId: string | null | undefined }>,
): boolean {
  return slots.some((slot) => slot.entryId != null);
}

/**
 * How to surface a primary DrawCreateBlockReason.
 * Quiet = tooltip only; inline = visible Alert (info|warning). Never danger —
 * these are journey / config / authoring states, not system errors.
 */
export type DrawCreateBlockPresentation =
  | { kind: 'quiet' }
  | { kind: 'inline'; tone: 'info' | 'warning'; action: boolean };

export function resolveDrawCreateBlockPresentation(
  reason: DrawCreateBlockReason,
): DrawCreateBlockPresentation {
  switch (reason) {
    case 'active':
    case 'emptyPoolUpstream':
    case 'unsupported':
      return { kind: 'quiet' };
    case 'emptyPool':
      return { kind: 'inline', tone: 'info', action: false };
    case 'countMismatch':
    case 'belowMinimumTeams':
    case 'groupShape':
    case 'missingPots':
      return { kind: 'inline', tone: 'warning', action: false };
    case 'occupiedSlots':
    case 'directAssignment':
      return { kind: 'inline', tone: 'warning', action: true };
  }
}

/** Shared Structure + Draw gate from stage / overview payloads. */
export function resolveStageDrawCreateGate(input: {
  formatKind: StructureFormatKind | null | undefined;
  draws: ReadonlyArray<{ status: string }>;
  slots: ReadonlyArray<{ entryId: string | null | undefined }>;
  compositionEntryCount: number;
  isRootComposition?: boolean;
  numberOfPots: number | null | undefined;
  groupCount: number | null | undefined;
  /** Places N (compositionCapacity / entry places) — not full bracket slotCount. */
  placesN: number | null | undefined;
  minimumTeams?: number | null;
  directAssignmentCount?: number | null;
}): DrawCreateGate {
  const kind =
    input.formatKind === 'Groups'
      ? 'Group'
      : input.formatKind === 'Cup'
        ? 'Slot'
        : null;
  return resolveDrawCreateGate({
    kind,
    hasActiveDraw: input.draws.some((d) => d.status !== 'Cancelled'),
    compositionEntryCount: input.compositionEntryCount,
    isRootComposition: input.isRootComposition,
    numberOfPots: input.numberOfPots,
    groupCount: input.groupCount,
    slotCount: input.placesN,
    minimumTeams: input.minimumTeams,
    hasDirectAssignment: (input.directAssignmentCount ?? 0) > 0,
    hasOccupiedSlots: slotsHaveOccupants(input.slots),
  });
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
  | {
      kind: 'lifecycle';
      status: Extract<DrawStatus, 'Cancelled' | 'Published'>;
    }
  | {
      kind: 'resolution';
      state: Extract<DrawResolutionState, 'Resolved' | 'NoSolution'>;
    }
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
   * One optional chip for history tiles (draw rail).
   * Never teaches the full 3-axis matrix — detail carries phrase + chrome.
   */
  masterChip: DrawMasterChip;
};

/** Topology badge states — mirrors server StructureDrawExecutionBadge. */
export type TopologyDrawExecutionBadge =
  'ToLaunch' | 'InProgress' | 'ToApply' | 'Applied';

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
 * L2 lock: DrawRules become immutable once a non-cancelled Draw has left
 * NotResolved (Generate consumed the rules — Resolved or NoSolution).
 * Draft+NotResolved remains editable; Cancel unlocks for a new cycle.
 */
export function areDrawRulesLockedByExecution(draws: StageDraw[]): boolean {
  return draws.some(
    (d) => d.status !== 'Cancelled' && d.resolutionState !== 'NotResolved',
  );
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

/** Default selection id for the Draw dialog (prefers active, else newest). */
export function pickDefaultDrawId(draws: StageDraw[]): string | null {
  if (draws.length === 0) {
    return null;
  }
  const newestFirst = sortDrawsNewestFirst(draws);
  const active = newestFirst.find((d) => d.status !== 'Cancelled');
  return (active ?? newestFirst[0])?.id ?? null;
}

/**
 * Rail identity chip — observable principal status only.
 * Priority: Cancelled > NoSolution > Applied > Published > Resolved.
 * Draft / NotResolved → no chip (Create+Generate makes that state rare; detail can still show it).
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
 * Applied keeps "Publié" · "Appliqué" so Publish ≠ Apply stays readable in the detail.
 */
export type DrawDetailHeaderChip =
  | {
      kind: 'lifecycle';
      status: Extract<DrawStatus, 'Published' | 'Cancelled'>;
    }
  | {
      kind: 'resolution';
      state: Extract<DrawResolutionState, 'Resolved' | 'NoSolution'>;
    }
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
    return [{ kind: 'lifecycle', status: 'Published' }, { kind: 'applied' }];
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
  | {
      kind: 'alert';
      tone: 'warning' | 'danger';
      messageKey: string;
      /** Optional secondary line under the alert title (e.g. generation interrupted). */
      bodyMessageKey?: string;
    }
  | null;

export function resolveDrawDetailGuidance(
  ui: DrawUiProjection,
): DrawDetailGuidance {
  if (ui.messageKey === 'fallback') {
    return null;
  }
  if (ui.messageKey === 'cancelled') {
    // Badge carries Cancelled; detail body is EmptyState when there is no result.
    return null;
  }
  if (ui.messageKey === 'generationInterrupted') {
    return {
      kind: 'alert',
      tone: 'warning',
      messageKey: 'generationInterrupted',
      bodyMessageKey: 'generationInterruptedBody',
    };
  }
  if (ui.messageKey === 'noSolution') {
    return { kind: 'alert', tone: 'warning', messageKey: 'noSolution' };
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
    const applied = getDrawUiProjection(draw, slots).isApplied;
    return applied ? 'Applied' : 'ToApply';
  }
  return 'ToLaunch';
}

/**
 * Pure projection: server enums → message key + flags for conditional rendering.
 * Not a Domain state machine — only helps the component avoid nested if spaghetti.
 *
 * Prefer server `draw.isApplied` (DrawAppliedState) when present; else Slot/Group heuristics.
 * Applied is only meaningful for Published draws (Cancel does not clear stage occupancy).
 */
export function getDrawUiProjection(
  draw: StageDraw,
  slots: StageSlot[],
): DrawUiProjection {
  const occupancyApplied =
    draw.resolutionState === 'Resolved' &&
    (typeof draw.isApplied === 'boolean'
      ? draw.isApplied
      : draw.kind === 'Slot' && isSlotDrawApplied(draw, slots));
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
    // New draw = Create+Generate; a lasting Draft/NotResolved is an interrupted generate.
    return {
      messageKey: 'generationInterrupted',
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
      // "Brouillon" · "Résolu" — phrase carries "ready to apply"
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
    }

    return {
      messageKey,
      showResults: true,
      isApplied,
      // "Publié" · ("Appliqué" when done) — no "Non appliqué" badge on happy recovery path
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
 * How many Places still carry exactly this draw's SlotResults.
 * Does not inspect DirectAssignment (server skips DA); SPA uses this for CTA visibility.
 */
export function countAlignedSlotPlacements(
  draw: StageDraw,
  slots: StageSlot[],
): number {
  if (draw.kind !== 'Slot' || draw.resolutionState !== 'Resolved') {
    return 0;
  }

  const byKey = new Map(slots.map((slot) => [slot.slotKey, slot]));
  let count = 0;
  for (const placement of draw.slotPlacements) {
    const slot = byKey.get(placement.slotKey);
    if (slot?.entryId != null && slot.entryId === placement.entryId) {
      count += 1;
    }
  }
  return count;
}

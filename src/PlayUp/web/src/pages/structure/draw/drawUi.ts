import type {
  DrawResolutionState,
  DrawStatus,
  SchematicCase,
  SchematicFeedKind,
  StageBracketPair,
  StageDraw,
  StageDrawGroupPlacement,
  StageDrawSlotPlacement,
  StageSlot,
  StageStatus,
  StructureFormatKind,
  StageDrawBracketLaunchFacts,
} from '../../../types';

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

/** Stable group card order (A before B, numeric-aware). */
export function compareGroupLabels(a: string, b: string): number {
  return a.localeCompare(b, undefined, {
    numeric: true,
    sensitivity: 'base',
  });
}

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
    compareGroupLabels(a.groupLabel, b.groupLabel),
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
 * Display vocabulary for a server `drawCreateLaunch.blockReasonCode`.
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
  | 'occupiedSlots'
  | 'noFreeSlot'
  | 'notEnoughTeams'
  | 'tooManyTeams';

/** Occupant = entryId present on StageOverview slots (overview-only; see countBracketPlacesBlockingDraw). */
export function slotsHaveOccupants(
  slots: ReadonlyArray<{ entryId: string | null | undefined }>,
): boolean {
  return countOccupiedSlots(slots) > 0;
}

export function countOccupiedSlots(
  slots: ReadonlyArray<{ entryId: string | null | undefined }>,
): number {
  return slots.reduce(
    (count, slot) => (slot.entryId != null ? count + 1 : count),
    0,
  );
}

const FEEDS_BLOCKING_CUP_DRAW: ReadonlySet<SchematicFeedKind> = new Set([
  'Qualification',
  'Progression',
  'Direct',
  'Draw',
]);

/** Cup / Groups place reserved for draw when an entry or structural feed is present. */
export function schematicCaseBlocksDraw(place: SchematicCase): boolean {
  if (place.entry?.entryId) {
    return true;
  }
  const kind = place.feedOrigin?.kind;
  return kind != null && FEEDS_BLOCKING_CUP_DRAW.has(kind);
}

/**
 * Places that block a Cup/Slot draw rerun — aligns with schematic (Qual/Prog feeds)
 * and falls back to overview slot entryId when schematic is unavailable.
 */
export function countBracketPlacesBlockingDraw(
  formatKind: StructureFormatKind | null | undefined,
  slots: ReadonlyArray<{ entryId: string | null | undefined }>,
  drawBracketLaunchFacts?: StageDrawBracketLaunchFacts | null,
  schematicCases?: ReadonlyArray<SchematicCase> | null,
): number {
  if (drawBracketLaunchFacts) {
    return drawBracketLaunchFacts.blockingPlaceCount;
  }
  if (schematicCases && schematicCases.length > 0) {
    const formKind =
      formatKind === 'Cup'
        ? 'CupSlot'
        : formatKind === 'Groups'
          ? 'GroupPlace'
          : null;
    if (formKind) {
      return schematicCases.filter(
        (place) =>
          place.formPosition.kind === formKind &&
          schematicCaseBlocksDraw(place),
      ).length;
    }
  }
  return countOccupiedSlots(slots);
}

/**
 * Free places for a partial Cup draw. Null when the grid is unknown
 * (no facts, no schematic form, and no overview slots).
 */
function countVacantBracketPlaces(
  formatKind: StructureFormatKind | null | undefined,
  slots: ReadonlyArray<{ entryId: string | null | undefined }>,
  drawBracketLaunchFacts?: StageDrawBracketLaunchFacts | null,
  schematicCases?: ReadonlyArray<SchematicCase> | null,
): number | null {
  if (drawBracketLaunchFacts) {
    return drawBracketLaunchFacts.vacantForDrawLaunchCount;
  }
  if (schematicCases && schematicCases.length > 0) {
    const formKind =
      formatKind === 'Cup'
        ? 'CupSlot'
        : formatKind === 'Groups'
          ? 'GroupPlace'
          : null;
    if (formKind) {
      const places = schematicCases.filter(
        (place) => place.formPosition.kind === formKind,
      );
      if (places.length > 0) {
        return places.filter((place) => !schematicCaseBlocksDraw(place)).length;
      }
    }
  }
  if (slots.length === 0) {
    return null;
  }
  return slots.reduce(
    (count, slot) => (slot.entryId == null ? count + 1 : count),
    0,
  );
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
    case 'noFreeSlot':
    case 'notEnoughTeams':
    case 'tooManyTeams':
      return { kind: 'inline', tone: 'warning', action: true };
  }
}

/**
 * Display map from the hub `blockReasonCode` onto existing fiche copy.
 * The allowed/blocked decision stays on the server field.
 */
const DRAW_CREATE_LAUNCH_BLOCK_COPY: Record<
  string,
  {
    reason?: DrawCreateBlockReason;
    shortKey: string;
    detailKey: string;
    presentation?: DrawCreateBlockPresentation;
  }
> = {
  UnsupportedFormat: {
    reason: 'unsupported',
    shortKey: 'fiche.drawWorkflow.createBlocked.short.unsupported',
    detailKey: 'fiche.drawWorkflow.createBlocked.detail.unsupported',
  },
  DrawRulesMissing: {
    reason: 'unsupported',
    shortKey: 'fiche.drawWorkflow.createBlocked.short.unsupported',
    detailKey: 'fiche.drawWorkflow.createBlocked.detail.unsupported',
  },
  ActiveDrawPresent: {
    reason: 'active',
    shortKey: 'fiche.drawWorkflow.createBlocked.short.active',
    detailKey: 'fiche.drawWorkflow.createBlocked.detail.active',
  },
  BadgeNotToLaunch: {
    reason: 'active',
    shortKey: 'fiche.drawWorkflow.createBlocked.short.active',
    detailKey: 'fiche.drawWorkflow.createBlocked.detail.active',
  },
  EmptyCompositionPool: {
    reason: 'emptyPool',
    shortKey: 'fiche.drawWorkflow.createBlocked.short.emptyPool',
    detailKey: 'fiche.drawWorkflow.createBlocked.detail.emptyPool',
  },
  UpstreamCompositionPoolEmpty: {
    reason: 'emptyPoolUpstream',
    shortKey: 'fiche.drawWorkflow.createBlocked.short.emptyPoolUpstream',
    detailKey: 'fiche.drawWorkflow.createBlocked.detail.emptyPoolUpstream',
  },
  BelowMinimumTeams: {
    reason: 'belowMinimumTeams',
    shortKey: 'fiche.drawWorkflow.createBlocked.short.belowMinimumTeams',
    detailKey: 'fiche.drawWorkflow.createBlocked.detail.belowMinimumTeams',
  },
  MissingPots: {
    reason: 'missingPots',
    shortKey: 'fiche.drawWorkflow.createBlocked.short.missingPots',
    detailKey: 'fiche.drawWorkflow.createBlocked.detail.missingPots',
  },
  GroupShapeInvalid: {
    reason: 'groupShape',
    shortKey: 'fiche.drawWorkflow.createBlocked.short.groupShape',
    detailKey: 'fiche.drawWorkflow.createBlocked.detail.groupShape',
  },
  PlacesCountMismatch: {
    reason: 'countMismatch',
    shortKey: 'fiche.drawWorkflow.createBlocked.short.countMismatch',
    detailKey: 'fiche.drawWorkflow.createBlocked.detail.countMismatch',
  },
  DirectAssignmentPresent: {
    reason: 'directAssignment',
    shortKey: 'fiche.drawWorkflow.createBlocked.short.directAssignment',
    detailKey: 'fiche.drawWorkflow.createBlocked.detail.directAssignment',
  },
  BracketPlacesOccupied: {
    reason: 'occupiedSlots',
    shortKey: 'fiche.drawWorkflow.createBlocked.short.occupiedSlots',
    detailKey: 'fiche.drawWorkflow.createBlocked.detail.occupiedSlots',
  },
  NoFreeSlot: {
    reason: 'noFreeSlot',
    shortKey: 'fiche.drawWorkflow.createBlocked.short.noFreeSlot',
    detailKey: 'fiche.drawWorkflow.createBlocked.detail.noFreeSlot',
  },
  NotEnoughTeams: {
    reason: 'notEnoughTeams',
    shortKey: 'fiche.drawWorkflow.createBlocked.short.notEnoughTeams',
    detailKey: 'fiche.drawWorkflow.createBlocked.detail.notEnoughTeams',
  },
  TooManyTeams: {
    reason: 'tooManyTeams',
    shortKey: 'fiche.drawWorkflow.createBlocked.short.tooManyTeams',
    detailKey: 'fiche.drawWorkflow.createBlocked.detail.tooManyTeams',
  },
  DrawObsoleteForConsumption: {
    shortKey: 'lifecycle.disabled.DrawObsoleteForConsumption',
    detailKey: 'lifecycle.disabled.DrawObsoleteForConsumption',
    presentation: { kind: 'inline', tone: 'warning', action: false },
  },
  OperationalExitEditBlocked: {
    shortKey: 'fiche.materialize.disabledReason.StructureNotMutable',
    detailKey: 'fiche.materialize.disabledReason.StructureNotMutable',
    presentation: { kind: 'inline', tone: 'warning', action: false },
  },
};

export type DrawCreateLaunchBlockCopy = {
  reason: DrawCreateBlockReason | null;
  presentation: DrawCreateBlockPresentation | null;
  shortKey: string | null;
  detailKey: string | null;
};

export function drawCreateLaunchBlockCopy(
  blockReasonCode: string | null | undefined,
): DrawCreateLaunchBlockCopy {
  if (blockReasonCode == null || blockReasonCode.length === 0) {
    return {
      reason: null,
      presentation: null,
      shortKey: null,
      detailKey: null,
    };
  }

  const mapped = DRAW_CREATE_LAUNCH_BLOCK_COPY[blockReasonCode];
  if (!mapped) {
    return {
      reason: null,
      presentation: { kind: 'quiet' },
      shortKey: 'lifecycle.disabled._unknown',
      detailKey: 'lifecycle.disabled._unknown',
    };
  }

  return {
    reason: mapped.reason ?? null,
    presentation:
      mapped.presentation ??
      (mapped.reason != null
        ? resolveDrawCreateBlockPresentation(mapped.reason)
        : { kind: 'quiet' }),
    shortKey: mapped.shortKey,
    detailKey: mapped.detailKey,
  };
}

export type DrawReadinessCriterionId =
  | 'teamsFillPlaces'
  | 'teamsAllKnown'
  /**
   * Cup: met when at least one place is free for a partial draw.
   * Groups: met only when no place blocks a full reshuffle.
   */
  | 'bracketPlacesFree';

export type DrawReadinessChecklistItem = {
  id: DrawReadinessCriterionId;
  met: boolean;
  current?: number;
  capacity?: number;
};

/** Composition-band checklist before running a draw (2–3 criteria; amont line only when configured). */
export function resolveDrawReadinessChecklist(input: {
  compositionEntryCount: number;
  placesN: number | null | undefined;
  inboundResolvedCount: number;
  inboundConfiguredVolume: number;
  /** False when inbound Qual/Prog feeds the phase — line 1 reads configured vs Places N. */
  isRootComposition?: boolean;
  formatKind?: StructureFormatKind | null;
  drawBracketLaunchFacts?: StageDrawBracketLaunchFacts | null;
  schematicCases?: ReadonlyArray<SchematicCase> | null;
  slots: ReadonlyArray<{ entryId: string | null | undefined }>;
}): DrawReadinessChecklistItem[] {
  const pool = input.compositionEntryCount;
  const places = input.placesN ?? 0;
  const resolved = input.inboundResolvedCount;
  const configured = input.inboundConfiguredVolume;
  const useInboundPlacesFill =
    input.isRootComposition === false && configured > 0 && places > 0;

  const teamsFillPlaces = useInboundPlacesFill
    ? configured === places
    : places > 0 && pool === places;
  const teamsFillPlacesCurrent = useInboundPlacesFill ? configured : pool;
  const teamsFillPlacesCapacity = places > 0 ? places : undefined;

  const showInboundKnown = configured > 0;
  const teamsAllKnown = !showInboundKnown || resolved >= configured;

  const occupiedBracketPlaces = countBracketPlacesBlockingDraw(
    input.formatKind,
    input.slots,
    input.drawBracketLaunchFacts,
    input.schematicCases,
  );
  const vacantBracketPlaces = countVacantBracketPlaces(
    input.formatKind,
    input.slots,
    input.drawBracketLaunchFacts,
    input.schematicCases,
  );
  const bracketPlacesFree =
    input.formatKind === 'Cup'
      ? vacantBracketPlaces === null || vacantBracketPlaces > 0
      : occupiedBracketPlaces === 0;

  const items: DrawReadinessChecklistItem[] = [
    {
      id: 'teamsFillPlaces',
      met: teamsFillPlaces,
      current: teamsFillPlacesCurrent,
      capacity: teamsFillPlacesCapacity,
    },
  ];

  if (showInboundKnown) {
    items.push({
      id: 'teamsAllKnown',
      met: teamsAllKnown,
      current: resolved,
      capacity: configured,
    });
  }

  items.push({
    id: 'bracketPlacesFree',
    met: bracketPlacesFree,
    current: occupiedBracketPlaces,
  });

  return items;
}

/**
 * Synthetic chrome flags — projection keeps three axes; UI does not teach the matrix
 * via three systematic badges.
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

/** Placement strip at 0/N — launch CTA copy + brand pool when draw rules are engaged. */
export function isPlacementDrawLaunchSignal(input: {
  drawRulesEngaged: boolean;
  drawLaunchable: boolean;
  drawExecutionBadge?: TopologyDrawExecutionBadge | null;
  occupiedPlacesCount: number | null;
}): boolean {
  if (!input.drawRulesEngaged || input.occupiedPlacesCount !== 0) {
    return false;
  }

  return input.drawLaunchable || input.drawExecutionBadge === 'ToLaunch';
}

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
 * DrawRules become immutable once a non-cancelled Draw has left
 * NotResolved (Generate consumed the rules — Resolved or NoSolution).
 * Draft+NotResolved remains editable; Cancel unlocks for a new cycle.
 */
export function areDrawRulesLockedByExecution(draws: StageDraw[]): boolean {
  return draws.some(
    (d) => d.status !== 'Cancelled' && d.resolutionState !== 'NotResolved',
  );
}

/**
 * Draw-rules authoring (activate / params) is Draft-only.
 * Distinct from draw execution workflow (`openDrawWorkflow`).
 */
export function canAuthorStageDrawRules(
  stageStatus: StageStatus,
  canReplaceDrawRules: boolean,
): boolean {
  return canReplaceDrawRules && stageStatus === 'Draft';
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
    // Failed resolution — same danger tone as the Sans solution chip, not pending work.
    return { kind: 'alert', tone: 'danger', messageKey: 'noSolution' };
  }
  if (ui.messageKey === 'published') {
    return { kind: 'alert', tone: 'warning', messageKey: ui.messageKey };
  }
  if (ui.isApplied) {
    // Stage effect lives on the Appliqué chip tooltip — no phrase under the title.
    return null;
  }
  if (ui.messageKey === 'draftResolved') {
    // "Résolu" does not say the result is ready to publish — that phrase stays visible.
    return { kind: 'phrase', messageKey: 'draftResolved' };
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
  slots: StageSlot[] = [],
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
      // Header chip is Résolu; the visible phrase says the result is ready to publish.
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
/** True when any Published draw is applied on the stage (server DrawAppliedState). */
export function stageHasPublishedAppliedDraw(
  draws: ReadonlyArray<StageDraw>,
): boolean {
  return draws.some((d) => d.status === 'Published' && d.isApplied === true);
}

/** Resolved placement rows in this draw's resolution (Slot or Group). */
export function drawResolutionPlacementCount(draw: StageDraw): number {
  if (draw.kind === 'Group') {
    return draw.groupPlacements?.length ?? 0;
  }
  return draw.slotPlacements.length;
}

/** Footer focus after Libérer — enabled release secondary, else primary, else header X. */
export function pickDrawFooterFocusAfterRelease(
  dialog: Element,
): HTMLElement | undefined {
  const footer = dialog.querySelector('.structure-draw-footer');
  if (!footer) {
    return undefined;
  }
  const release = footer.querySelector<HTMLButtonElement>(
    'button[data-action="release-draw-placements"]:not(:disabled)',
  );
  const primary = footer.querySelector<HTMLElement>(
    '.ds-btn--primary:not(:disabled)',
  );
  const closeButton = dialog.querySelector<HTMLElement>(
    'button.ds-icon-button:not(:disabled)',
  );
  return release ?? primary ?? closeButton ?? undefined;
}

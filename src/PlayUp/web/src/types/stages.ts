/**
 * Stage overview, draws on stage, schematic projection.
 */
import type {
  DrawResolutionKind,
  DrawResolutionState,
  DrawStatus,
  EntryStatus,
  ProgressionOutcome,
  RankingScope,
  SelectionMode,
  StageStatus,
  StructureFormatKind,
} from './enums';

/** Apply / publish-and-apply take no body (Host owns Slot/Group mapping). */

export interface StageFixtureAttachment {
  matchId: string;
  legIndex: number;
}

export interface StageFixture {
  id: string;
  slotAKey: string | null;
  slotBKey: string | null;
  attachments: StageFixtureAttachment[];
}

export interface StageRound {
  id: string;
  name: string;
  fixtures: StageFixture[];
}

export interface StageSlot {
  slotKey: string;
  entryId: string | null;
  displayName: string | null;
  /** True when a complete Fixture already covers this slot (exclude from from-slots pairing). */
  coveredByCompleteFixture: boolean;
}

export type DrawInputsIntent = 'Default' | 'Rerun' | 'Partial';

export type DrawPlacementOrigin = 'Draw' | 'Feed' | 'Manual';

/** Wire names. The C# member for `Reserved` is `HeldByFeed`. */
export type SlotDrawCellState =
  'Drawn' | 'AlreadyPlaced' | 'Reserved' | 'Withdrawn' | 'Free' | 'OutsideDraw';

export interface StageDrawSlotPlacement {
  slotKey: string;
  entryId: string;
  displayName: string | null;
  shortName?: string | null;
  logoMediaId?: string | null;
  primaryColor?: string | null;
  /** Display origin of this resolution line. Resolution lines are Draw. */
  origin?: DrawPlacementOrigin | null;
}

export interface StageDrawSlotDisplayCell {
  slotKey: string;
  cellState: SlotDrawCellState;
  entryId: string | null;
  displayName: string | null;
  shortName?: string | null;
  logoMediaId?: string | null;
  primaryColor?: string | null;
  origin?: DrawPlacementOrigin | null;
}

export interface StageDrawDisplayMetrics {
  unplacedEntryCount: number;
  vacantSlotKeyCount: number;
  vacantSlotKeys?: string[];
  alreadyPlacedEntryCount: number;
  entryCount: number;
  placeCount: number;
}

export interface StageDrawGroupPlacement {
  groupId: string;
  /** Present when resolved from stage groups; may be absent on older payloads. */
  groupDisplayName?: string | null;
  entryId: string;
  displayName: string | null;
  shortName?: string | null;
  logoMediaId?: string | null;
  primaryColor?: string | null;
}

export interface StageDraw {
  id: string;
  kind: DrawResolutionKind;
  status: DrawStatus;
  resolutionState: DrawResolutionState;
  slotPlacements: StageDrawSlotPlacement[];
  /** Present when resolved as Group; may be empty on older payloads. */
  groupPlacements?: StageDrawGroupPlacement[];
  /** Server-derived occupancy (DrawAppliedState). Prefer over local heuristics. */
  isApplied?: boolean;
  /** Resolution obsolete for Apply / Materialize consumption. */
  isObsoleteForConsumption?: boolean;
  /** Server projection. Missing actions are Hidden. Available is not a success guarantee. */
  actions?: StageDrawActions;
  /** Intent recorded when inputs were configured. Omitted configuration stays Default. */
  inputsIntent?: DrawInputsIntent;
  /** Draft slot metrics. Null for group draws and for Published or Cancelled draws. */
  displayMetrics?: StageDrawDisplayMetrics | null;
  /** Cup grid. Null for group draws. */
  slotDisplayCells?: StageDrawSlotDisplayCell[] | null;
}

export type StageDrawActionState = 'Hidden' | 'Available' | 'Blocked';

export interface StageDrawAction {
  state: StageDrawActionState;
  blockCode?: string | null;
}

export interface StageDrawReleaseAction extends StageDrawAction {
  releasableCount: number;
}

export interface StageDrawActions {
  cancel: StageDrawAction;
  release: StageDrawReleaseAction;
  apply: StageDrawAction;
  publishAndApply: StageDrawAction;
  redo: StageDrawAction;
}

export interface StageGroupOverview {
  id: string;
  name: string;
  entryIds: string[];
}

export interface StageDrawBracketLaunchFacts {
  entryPlaceCount: number;
  occupiedByEntryCount: number;
  reservedByStructuralFeedCount: number;
  feedResolutionInvalidCount: number;
  blockingPlaceCount: number;
  vacantForDrawLaunchCount: number;
}

export interface StageOverview {
  id: string;
  competitionId: string;
  name: string;
  status: StageStatus;
  rounds: StageRound[];
  slots: StageSlot[];
  draws: StageDraw[];
  /** Cup structural confrontations (P1 = S1↔S2). Empty for Groups; omit only in tests. */
  bracketPairs?: StageBracketPair[];
  /** Current group membership; empty for non-Groups formats. */
  groups?: StageGroupOverview[];
  /** Bracket occupancy for Tirage launch; from Application classifier when DrawRules engaged. */
  drawBracketLaunchFacts?: StageDrawBracketLaunchFacts | null;
  /** Most recent Cancelled draw with at least one aligned placement. */
  releaseCandidateDrawId?: string | null;
  /** Current cup partial-draw counts. Null when the stage has no slots. */
  slotDrawDisplayMetrics?: StageDrawDisplayMetrics | null;
}

/** Structural Cup pair from StageOverview — drives Slot draw A vs B (not Fixture #). */
export interface StageBracketPair {
  pairKey: string;
  slotAKey: string;
  slotBKey: string;
}

/** GET /stages/{id}/schematic — form units + placed entries. */
export type SchematicFormPositionKind =
  'CupSlot' | 'GroupPlace' | 'RosterPlace';

export type SchematicFeedKind =
  'Qualification' | 'Progression' | 'Direct' | 'Draw';

export interface SchematicFormPosition {
  kind: SchematicFormPositionKind;
  slotKey?: string | null;
  groupId?: string | null;
  groupName?: string | null;
  index?: number | null;
  /** Backing fixture when the unit is a Cup bracket side without Place binding. */
  fixtureId?: string | null;
  /** Bracket side ('A' | 'B') when known for Cup address. */
  side?: string | null;
  /** 0-based Cup round order when the unit belongs to a fixture. */
  roundOrder?: number | null;
  /** Domain round display name (organizer-authored). */
  roundName?: string | null;
  /** 1-based pair ordinal within the round; null for a single-pair round. */
  pairOrdinal?: number | null;
}

export interface SchematicFeedOrigin {
  kind: SchematicFeedKind;
  sourceStageId?: string | null;
  /** Source phase display name when Qual/Prog (read-model chrome / tooltip). */
  sourceStageName?: string | null;
  pathOrder?: number | null;
  selectionMode?: SelectionMode | null;
  selectionValue?: number | null;
  selectionEndValue?: number | null;
  rankingScope?: RankingScope | null;
  groupId?: string | null;
  groupName?: string | null;
  acrossGroupsPosition?: number | null;
  /** Structural progression source (Cup = BracketPair.PairKey). */
  sourcePairKey?: string | null;
  sourceFixtureNumber?: number | null;
  outcome?: ProgressionOutcome | null;
  drawId?: string | null;
  configuredEntryId?: string | null;
  slotKey?: string | null;
  /** Groups A1 destination group when the feed targets a poule (not a Cup slot). */
  destinationGroupId?: string | null;
}

export interface SchematicEntryRef {
  entryId: string;
  displayName?: string | null;
  status?: EntryStatus | null;
}

export interface SchematicParticipantRef {
  entryId: string;
  displayName?: string | null;
  shortName?: string | null;
  logoMediaId?: string | null;
  primaryColor?: string | null;
  secondaryColor?: string | null;
}

export interface SchematicCase {
  formPosition: SchematicFormPosition;
  feedOrigin?: SchematicFeedOrigin | null;
  entry?: SchematicEntryRef | null;
  assignment?: SchematicParticipantRef | null;
}

export interface SchematicConnection {
  fixtureId?: string | null;
  roundOrder: number;
  slotAKey?: string | null;
  slotBKey?: string | null;
  matchNumber: number;
  pairKey?: string | null;
}

/** Groups A1 — feed at Groupe grain (under group title), never Place k. */
export interface SchematicGroupFeed {
  groupId: string;
  feedOrigin: SchematicFeedOrigin;
}

export interface ExpectedResolvedFormParticipant {
  entry: SchematicEntryRef;
  assignment?: SchematicParticipantRef | null;
}

/** Championship/Swiss expected form bag (Structure) — not RosterPlace addressing. */
export interface ExpectedFormParticipants {
  resolved: ExpectedResolvedFormParticipant[];
  pending: SchematicFeedOrigin[];
}

export interface StageSchematic {
  stageId: string;
  competitionId: string;
  name: string;
  status: StageStatus;
  formatKind?: StructureFormatKind | null;
  cases: SchematicCase[];
  connections: SchematicConnection[];
  /** Planned Swiss rounds (structural K) when formatKind is Swiss. */
  swissRoundCount?: number | null;
  /** Cup rounds in this phase when formatKind is Cup. */
  cupRoundCount?: number | null;
  groupFeeds?: SchematicGroupFeed[] | null;
  /**
   * Championship/Swiss expected participants (Composition + pending ForForm).
   * Cases are the N-cell bag projection; no separate alimentation zone.
   */
  expectedFormParticipants?: ExpectedFormParticipants | null;
}

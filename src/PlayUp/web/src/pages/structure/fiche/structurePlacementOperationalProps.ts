import type { ReactNode } from 'react';

import type { StageStatus } from '../../../types';
import type { StructureDiagnosticPresentationContext } from '../issues/structureDiagnosticPresentationPolicy';
import type { DrawReadinessChecklistItem } from '../draw/drawUi';
import type { CompositionTileMode } from '../composition/compositionTilePresentation';

/** Props for Places / Draw columns in `StructurePhaseOperationalStrip`. */
export type StructurePlacementOperationalSectionProps = {
  competitionId: string;
  placesN: number | null;
  poolFilled: number;
  occupiedPlacesCount: number | null;
  occupancyReady: boolean;
  showDrawCta: boolean;
  showActivateDrawCta: boolean;
  showResolvedPoolGauge: boolean;
  canManualPlace: boolean;
  canEditDraw: boolean;
  showReleaseCta: boolean;
  hasNonCancelledDraw: boolean;
  activeDraw: boolean;
  blockPerformDrawCta: boolean;
  createBlockedShort: string | null;
  createBlockedReason: string | null;
  showCreateBlockedCaption: boolean;
  showPoolHint: boolean;
  releasableAlignedCount: number;
  releasableTotal: number;
  releasePending: boolean;
  /** Server release action is Blocked. Button stays visible with the block reason. */
  releaseDisabled?: boolean;
  releaseDisabledReason?: string | null;
  deactivateDrawPending: boolean;
  /** Host reports SlotFeedMissing — strip meter copy carries the gap (not the banner). */
  slotFeedMissingActive?: boolean;
  /** Same predicate as Create + Generate (`resolveStageDrawCreateGate`). Only when `showDrawCta`. */
  drawLaunchable?: boolean;
  drawExecutionBadge?: 'ToLaunch' | 'InProgress' | 'ToApply' | 'Applied' | null;
  stageStatus?: StageStatus;
  slotFeedPresentationContext?: StructureDiagnosticPresentationContext;
  /** Draw pool / create gate needs draw overview (Cup) before showing Tirage strip body. */
  drawOperationalReady?: boolean;
  drawReadinessChecklist?: readonly DrawReadinessChecklistItem[] | null;
  compositionTileMode?: CompositionTileMode;
  placesDiagnostics?: ReactNode;
  onOpenDrawWorkflow: () => void;
  onActivateDraw: () => void;
  onEditDrawParams: () => void;
  onManualPlace: () => void;
  onReleasePlacements: () => void;
  onDeactivateDraw: () => void;
  structureHubRefreshing?: boolean;
  embeddedInSection?: boolean;
};

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useOpenEdge, useSyncWhen } from '../../../design-system/useRenderSync';
import {
  useId,
  useLayoutEffect,
  useMemo,
  useRef,
  useState,
  type KeyboardEvent,
  type ReactElement,
} from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import {
  applyDraw,
  cancelDraw,
  createAndGenerateDraw,
  fetchStageOverview,
  isDrawGenerateFailedError,
  publishAndApplyDraw,
  releaseDrawAlignedPlacements,
  rerunDrawGeneration,
} from '../../../api';
import { Alert } from '../../../design-system/components/Alert';
import { ConfirmDialog } from '../../../design-system/components/ConfirmDialog';
import { WaitMark } from '../../../design-system/components/WaitMark';
import { Status } from '../../../design-system/components/Status';
import { Dialog } from '../../../design-system/components/Dialog';
import { SelectableTile } from '../../../design-system/components/SelectableTile';
import { Tooltip } from '../../../design-system/components/Tooltip';
import {
  CheckIcon,
  RandomIcon,
  UndoIcon,
  UnlockIcon,
} from '../../../design-system/icons/contentIcons';
import { CloseIcon } from '../../../design-system/icons/shellIcons';
import { notify } from '../../../design-system/toastStore';
import { useClearMutationsOnOpen } from '../../../design-system/useClearMutationsOnOpen';
import { queryKeys } from '../../../queryKeys';
import type {
  CompetitionStatus,
  StageBracketPair,
  StageDraw,
  StageGroupOverview,
  StageSlot,
  StructureFormatKind,
  StructureStageHubSummary,
} from '../../../types';
import { resolveLifecycleMutationMessage } from '../problems/resolveStructureProblemMessage';
import {
  countAlignedDrawPlacements,
  drawExecutionNumber,
  drawResolutionPlacementCount,
  getDrawUiProjection,
  pickActiveDraw,
  pickDefaultDrawId,
  projectSlotDrawResult,
  resolveDrawCreateBlockPresentation,
  resolveDrawDetailGuidance,
  resolveStageDrawCreateGate,
  sortDrawsNewestFirst,
  stageHasPublishedAppliedDraw,
} from './drawUi';
import { DrawSlotConfrontationGrid } from './DrawSlotConfrontationGrid';
import { DrawRedoStoppedError, resolveAppliedDrawRedo } from './drawRedo';
import {
  canCancelDrawExecution,
  canReleaseDrawAlignedPlacements,
  groupReleaseBlockedByAttachedMatches,
  releaseDrawPlacementsVisible,
} from '../../stage/lifecycleGates';
import { scheduleStructureRefresh } from '../structureInvalidation';
import { resolvePlacesN } from '../composition/structurePlaces';
import { functionalProblemPresentation } from '../../../shell/functionalProblemPresentation';
import { GroupsSchematicView } from '../schematic/GroupsSchematicView';
import { groupsSchematicFromDrawDraft } from '../schematic/groupsSchematic';
import {
  DrawStatusBadge,
  EmptyState,
  MutationError,
  PendingLabel,
} from '../../../ui';
import { ApiError } from '../../../api/http';
import { persistentStructureMutationSotHref } from '../../../persistentStructureMutationSot';

function resolveDrawKindForFormat(
  format: StructureFormatKind | null | undefined,
): 'Group' | 'Slot' | null {
  if (format === 'Groups') {
    return 'Group';
  }
  if (format === 'Cup') {
    return 'Slot';
  }
  return null;
}

function stickyMutationError(error: unknown): unknown {
  if (error == null || isDrawGenerateFailedError(error)) {
    return null;
  }
  if (error instanceof DrawRedoStoppedError) {
    if (isDrawGenerateFailedError(error.cause)) {
      return null;
    }
    return error.cause ?? error;
  }
  return error;
}

/** Work dialog — one selected execution, with a run selector when history exists. */
export type StructureDrawDialogProps = {
  open: boolean;
  onClose: () => void;
  competitionId: string;
  /** Host lifecycle gate — Completed|Archived → Cancel hidden. */
  competitionStatus: CompetitionStatus;
  /** Competition EntryRules.MinimumTeams — gate belowMinimumTeams. */
  minimumTeams?: number | null;
  stage: StructureStageHubSummary;
  structureStages: readonly StructureStageHubSummary[];
};

const EMPTY_DRAWS: StageDraw[] = [];
const EMPTY_SLOTS: StageSlot[] = [];
const EMPTY_GROUPS: StageGroupOverview[] = [];

type ConfirmKind = 'apply' | 'abandon' | 'release' | 'redo' | 'cancelApplied';

/**
 * Work dialog — stage draw executions.
 * The selected execution fills the view. Cancelled runs stay selectable.
 */
export function StructureDrawDialog({
  open,
  onClose,
  competitionId,
  structureStages,
  competitionStatus,
  minimumTeams = null,
  stage,
}: StructureDrawDialogProps) {
  const { t } = useTranslation('structure');
  const { t: tDraw } = useTranslation('draw');
  const { t: tCommon } = useTranslation('common');
  const queryClient = useQueryClient();
  const stageId = stage.stageId;
  const drawKind = resolveDrawKindForFormat(stage.formatKind);

  const overviewQuery = useQuery({
    queryKey: queryKeys.stages.detail(stageId),
    queryFn: () => fetchStageOverview(stageId),
    enabled: open && Boolean(stageId),
  });

  const draws = overviewQuery.data?.draws;
  const slots = overviewQuery.data?.slots ?? EMPTY_SLOTS;
  const overviewGroups = overviewQuery.data?.groups ?? EMPTY_GROUPS;
  const bracketPairs = overviewQuery.data?.bracketPairs ?? [];
  const rounds = overviewQuery.data?.rounds ?? [];
  const drawList = draws ?? EMPTY_DRAWS;
  const stageStatus = overviewQuery.data?.status ?? stage.status;
  const stageHasAppliedDraw = stageHasPublishedAppliedDraw(drawList);
  const poolCount = stage.compositionEntryCount ?? 0;
  const placesN = stage.compositionCapacity ?? resolvePlacesN(stage);
  const placesPerGroup = stage.placesPerGroup ?? stage.numberOfPots ?? null;

  const [confirm, setConfirm] = useState<ConfirmKind | null>(null);
  const [confirmDrawId, setConfirmDrawId] = useState<string | null>(null);
  const [selectedDrawId, setSelectedDrawId] = useState<string | null>(null);
  const focusedFooterRef = useRef<HTMLButtonElement | null>(null);
  const focusTabIdRef = useRef<string | null>(null);
  const runsDomId = useId();

  useOpenEdge(open, {
    onClose: () => {
      setConfirm(null);
      setConfirmDrawId(null);
      setSelectedDrawId(null);
      focusedFooterRef.current = null;
    },
  });

  const activeDraw = useMemo(() => pickActiveDraw(drawList), [drawList]);
  const cancelledDraws = useMemo(
    () =>
      sortDrawsNewestFirst(drawList).filter(
        (draw) => draw.status === 'Cancelled',
      ),
    [drawList],
  );
  const runList = useMemo(
    () => (activeDraw ? [activeDraw, ...cancelledDraws] : cancelledDraws),
    [activeDraw, cancelledDraws],
  );

  function markSelectionDefault() {
    setSelectedDrawId(null);
  }

  useSyncWhen(
    open && draws != null,
    runList.map((draw) => draw.id).join(','),
    () => {
      setSelectedDrawId((current) => {
        if (current != null && runList.some((draw) => draw.id === current)) {
          return current;
        }
        return pickDefaultDrawId(drawList);
      });
    },
  );

  const resolvedSelectedId =
    selectedDrawId != null && runList.some((draw) => draw.id === selectedDrawId)
      ? selectedDrawId
      : pickDefaultDrawId(drawList);
  const displayedDraw =
    runList.find((draw) => draw.id === resolvedSelectedId) ?? null;
  const showingActive =
    displayedDraw != null &&
    activeDraw != null &&
    displayedDraw.id === activeDraw.id;
  const showRunSelector = cancelledDraws.length > 0;
  const attachedMatchCount = stage.matchCount ?? 0;

  function alignedCountFor(draw: StageDraw): number {
    return countAlignedDrawPlacements(draw, slots, overviewGroups);
  }

  function releaseVisible(draw: StageDraw): boolean {
    if (draw.kind !== 'Slot' && draw.kind !== 'Group') {
      return false;
    }
    return releaseDrawPlacementsVisible({
      competitionStatus,
      stageStatus,
      drawStatus: draw.status,
      drawKind: draw.kind,
      stageHasPublishedAppliedDraw: stageHasAppliedDraw,
      alignedPlacementCount: alignedCountFor(draw),
    });
  }

  function releaseHoldFor(
    draw: StageDraw,
  ): 'publishedDraw' | 'attachedMatches' | null {
    if (draw.status !== 'Cancelled' || alignedCountFor(draw) <= 0) {
      return null;
    }
    if (stageHasAppliedDraw) {
      return 'publishedDraw';
    }
    if (
      draw.kind === 'Group' &&
      groupReleaseBlockedByAttachedMatches({
        drawKind: 'Group',
        stageStatus,
        attachedMatchCount,
      })
    ) {
      return 'attachedMatches';
    }
    return null;
  }

  function releaseEnabled(draw: StageDraw): boolean {
    if (draw.kind !== 'Slot' && draw.kind !== 'Group') {
      return false;
    }
    return canReleaseDrawAlignedPlacements({
      competitionStatus,
      stageStatus,
      drawStatus: draw.status,
      drawKind: draw.kind,
      stageHasPublishedAppliedDraw: stageHasAppliedDraw,
      alignedPlacementCount: alignedCountFor(draw),
      attachedMatchCount,
    });
  }

  const confirmDraw =
    drawList.find((draw) => draw.id === confirmDrawId) ?? null;

  const createGateInput = {
    formatKind: stage.formatKind,
    draws: drawList,
    slots,
    drawBracketLaunchFacts: overviewQuery.data?.drawBracketLaunchFacts,
    compositionEntryCount: poolCount,
    isRootComposition: stage.isRootComposition,
    numberOfPots: stage.numberOfPots,
    groupCount: stage.groupCount,
    placesN,
    minimumTeams,
    directAssignmentCount: stage.directAssignmentCount,
  };
  const createGate = resolveStageDrawCreateGate(createGateInput);
  const showLaunch =
    drawKind != null && !overviewQuery.isLoading && !activeDraw;
  const canLaunch = showLaunch && createGate.ok;
  const createBlockedReason =
    showLaunch && !createGate.ok ? createGate.reason : null;
  const createBlockPresentation =
    createBlockedReason != null
      ? resolveDrawCreateBlockPresentation(createBlockedReason)
      : null;
  const createBlockEcho =
    createBlockedReason != null
      ? functionalProblemPresentation({
          source: createBlockedReason,
          competitionId,
          stageId: stage.stageId,
        })
      : null;
  const createBlockedShort =
    createBlockedReason != null
      ? t(`fiche.drawWorkflow.createBlocked.short.${createBlockedReason}`)
      : null;
  const createBlockedDetail =
    createBlockedReason != null && createBlockPresentation?.kind === 'inline'
      ? createBlockedReason === 'countMismatch'
        ? t('fiche.drawWorkflow.createBlocked.detail.countMismatch', {
            count: poolCount,
            places: placesN ?? 0,
          })
        : t(`fiche.drawWorkflow.createBlocked.detail.${createBlockedReason}`)
      : null;

  /** Draft create / generate — hub + schematic unchanged until publish/apply. */
  function scheduleDrawOverviewRefresh() {
    scheduleStructureRefresh(queryClient, competitionId, ['stageOverview'], {
      stageId,
    });
  }

  function scheduleDrawHubRefresh() {
    scheduleStructureRefresh(queryClient, competitionId, ['hubStageShell'], {
      stageId,
    });
  }

  function openConfirm(kind: ConfirmKind, drawId: string) {
    setConfirm(kind);
    setConfirmDrawId(drawId);
  }

  function closeConfirm() {
    setConfirm(null);
    setConfirmDrawId(null);
  }

  const createMutation = useMutation({
    mutationFn: () => {
      if (!drawKind) {
        throw new Error('Draw kind unavailable for this format.');
      }
      const gate = resolveStageDrawCreateGate(createGateInput);
      if (!gate.ok) {
        throw new Error(`Draw create blocked: ${gate.reason}`);
      }
      return createAndGenerateDraw(stageId, drawKind);
    },
    onSuccess: (result) => {
      markSelectionDefault();
      scheduleDrawOverviewRefresh();
      if (result.isNoSolution) {
        notify.attention(tDraw('toastCreatedNoSolution'));
      } else {
        notify.success(tDraw('toastCreated'));
      }
    },
    onError: async (error) => {
      if (isDrawGenerateFailedError(error)) {
        scheduleDrawOverviewRefresh();
        notify.error(tDraw('toastGenerationFailed'));
        return;
      }
      if (
        error instanceof ApiError &&
        error.code === 'Stage.NonCancelledDrawAlreadyExists'
      ) {
        notify.error(resolveLifecycleMutationMessage(t, error));
        return;
      }
      notify.error(tDraw('toastActionFailed'));
    },
  });

  const rerunMutation = useMutation({
    mutationFn: (drawId: string) => rerunDrawGeneration(stageId, drawId),
    onSuccess: (result) => {
      markSelectionDefault();
      scheduleDrawOverviewRefresh();
      if (result.isNoSolution) {
        notify.attention(tDraw('toastCreatedNoSolution'));
      } else {
        notify.success(tDraw('toastRerun'));
      }
    },
    onError: (error) => {
      scheduleDrawOverviewRefresh();
      if (isDrawGenerateFailedError(error)) {
        notify.error(tDraw('toastGenerationFailed'));
      }
    },
  });

  const publishAndApplyMutation = useMutation({
    mutationFn: async (draw: StageDraw) => {
      return publishAndApplyDraw(stageId, draw.id);
    },
    onSuccess: () => {
      notify.success(tDraw('toastPublishedAndApplied'));
    },
    onSettled: () => {
      scheduleDrawHubRefresh();
    },
  });

  const cancelMutation = useMutation({
    mutationFn: (drawId: string) => cancelDraw(stageId, drawId),
    onSuccess: () => {
      markSelectionDefault();
      closeConfirm();
      scheduleDrawHubRefresh();
      notify.success(tDraw('toastCancelled'));
    },
  });

  const releaseMutation = useMutation({
    mutationFn: (drawId: string) =>
      releaseDrawAlignedPlacements(stageId, drawId),
    onSuccess: (result) => {
      markSelectionDefault();
      closeConfirm();
      scheduleDrawHubRefresh();
      notify.success(tDraw('toastReleased', { count: result.releasedCount }));
    },
  });

  const applyMutation = useMutation({
    mutationFn: async (draw: StageDraw) => {
      return applyDraw(stageId, draw.id);
    },
    onSuccess: () => {
      closeConfirm();
      scheduleDrawHubRefresh();
      notify.success(tDraw('toastApplied'));
    },
  });

  const redoMutation = useMutation({
    mutationFn: async (draw: StageDraw) => {
      await cancelDraw(stageId, draw.id);
      try {
        await releaseDrawAlignedPlacements(stageId, draw.id);
      } catch (cause) {
        throw new DrawRedoStoppedError('release', cause);
      }
      if (!drawKind) {
        throw new DrawRedoStoppedError(
          'preview',
          new Error('Draw kind unavailable for this format.'),
        );
      }
      try {
        return await createAndGenerateDraw(stageId, drawKind);
      } catch (cause) {
        throw new DrawRedoStoppedError('preview', cause);
      }
    },
    onSuccess: (result) => {
      markSelectionDefault();
      closeConfirm();
      scheduleDrawHubRefresh();
      if (result.isNoSolution) {
        notify.attention(tDraw('toastCreatedNoSolution'));
      } else {
        notify.success(tDraw('toastCreated'));
      }
    },
    onError: (error) => {
      closeConfirm();
      scheduleDrawHubRefresh();
      if (
        error instanceof DrawRedoStoppedError &&
        isDrawGenerateFailedError(error.cause)
      ) {
        notify.error(tDraw('toastGenerationFailed'));
      }
    },
  });

  useClearMutationsOnOpen(
    open,
    publishAndApplyMutation,
    applyMutation,
    cancelMutation,
    releaseMutation,
    rerunMutation,
    redoMutation,
  );
  useClearMutationsOnOpen(confirm === 'apply', applyMutation);
  useClearMutationsOnOpen(confirm === 'abandon', cancelMutation);
  useClearMutationsOnOpen(confirm === 'cancelApplied', cancelMutation);
  useClearMutationsOnOpen(confirm === 'release', releaseMutation);
  useClearMutationsOnOpen(confirm === 'redo', redoMutation);

  const busy =
    createMutation.isPending ||
    rerunMutation.isPending ||
    publishAndApplyMutation.isPending ||
    cancelMutation.isPending ||
    releaseMutation.isPending ||
    applyMutation.isPending ||
    redoMutation.isPending;

  const mutationError =
    stickyMutationError(publishAndApplyMutation.error) ??
    stickyMutationError(cancelMutation.error) ??
    stickyMutationError(releaseMutation.error) ??
    stickyMutationError(applyMutation.error) ??
    stickyMutationError(rerunMutation.error) ??
    stickyMutationError(redoMutation.error);

  const activeUi =
    activeDraw != null ? getDrawUiProjection(activeDraw, slots) : null;
  const activeRedo =
    activeDraw != null
      ? resolveAppliedDrawRedo({
          formatKind: stage.formatKind,
          competitionStatus,
          stageStatus,
          isApplied: activeUi?.isApplied === true,
          draw: activeDraw,
          slots,
          bracketPairs,
          rounds,
          attachedMatchCount,
        })
      : { kind: 'hidden' as const };

  const canValidate =
    activeDraw != null &&
    activeDraw.status === 'Draft' &&
    activeDraw.resolutionState === 'Resolved' &&
    (activeDraw.kind === 'Slot' || activeDraw.kind === 'Group');
  const canApplyResume =
    activeDraw != null &&
    activeUi != null &&
    activeDraw.status === 'Published' &&
    activeDraw.resolutionState === 'Resolved' &&
    !activeUi.isApplied &&
    (activeDraw.kind === 'Slot' || activeDraw.kind === 'Group');
  const canRerun =
    activeDraw != null &&
    activeDraw.status === 'Draft' &&
    (activeDraw.kind === 'Slot' || activeDraw.kind === 'Group');
  const canAbandon =
    activeDraw != null &&
    activeUi != null &&
    !activeUi.isApplied &&
    canCancelDrawExecution({
      competitionStatus,
      stageStatus,
      drawStatus: activeDraw.status,
    });
  const canRedo = activeRedo.kind === 'available';
  const canCancelApplied =
    activeDraw != null &&
    activeUi?.isApplied === true &&
    activeRedo.kind === 'blocked' &&
    canCancelDrawExecution({
      competitionStatus,
      stageStatus,
      drawStatus: activeDraw.status,
    });

  const selectedRelease =
    displayedDraw != null && !showingActive && releaseVisible(displayedDraw)
      ? displayedDraw
      : null;
  const releaseBlocked =
    selectedRelease != null &&
    selectedRelease.kind === 'Group' &&
    groupReleaseBlockedByAttachedMatches({
      drawKind: 'Group',
      stageStatus,
      attachedMatchCount,
    });

  const focusKey = [
    overviewQuery.isLoading ? 'loading' : 'ready',
    activeDraw?.id ?? 'none',
    activeDraw?.status ?? '',
    activeDraw?.resolutionState ?? '',
    activeUi?.isApplied ? 'applied' : 'open',
    activeRedo.kind,
    canLaunch ? 'launch' : 'wait',
  ].join('|');

  const footerSignature = [
    showingActive ? 'active' : (displayedDraw?.id ?? 'none'),
    canAbandon && showingActive ? 'abandon' : '',
    canCancelApplied && showingActive ? 'cancelApplied' : '',
    selectedRelease ? `release:${releaseBlocked ? 'blocked' : 'on'}` : '',
    canRerun && showingActive ? 'rerun' : '',
    showLaunch ? 'launch' : '',
    canValidate && showingActive ? 'validate' : '',
    canApplyResume && showingActive ? 'apply' : '',
    canRedo && showingActive ? 'redo' : '',
  ].join('|');

  useLayoutEffect(() => {
    const tabId = focusTabIdRef.current;
    if (!tabId) {
      return;
    }
    focusTabIdRef.current = null;
    document.getElementById(tabId)?.focus();
  }, [resolvedSelectedId]);

  useLayoutEffect(() => {
    if (!open) {
      focusedFooterRef.current = null;
      return;
    }
    if (confirm != null || busy) {
      return;
    }
    const previous = focusedFooterRef.current;
    const buttonLost =
      previous != null && (!previous.isConnected || previous.disabled);
    const dialog = document
      .querySelector('.structure-draw-dialog')
      ?.closest('[role="dialog"]');
    const active = document.activeElement;
    const outsideDialog =
      !(active instanceof Node) ||
      active === document.body ||
      active === document.documentElement ||
      (dialog != null && !dialog.contains(active));
    const landedElsewhere =
      active instanceof HTMLElement &&
      active !== previous &&
      active.isConnected &&
      dialog?.contains(active) === true;
    if (dialog && ((buttonLost && !landedElsewhere) || outsideDialog)) {
      const footer = dialog.querySelector('.structure-draw-footer');
      const primary = footer?.querySelector<HTMLElement>(
        '.ds-btn--primary:not(:disabled)',
      );
      const neutral = footer
        ? [...footer.querySelectorAll<HTMLButtonElement>('button')].find(
            (button) =>
              !button.disabled &&
              !button.classList.contains('ds-btn--primary') &&
              !button.classList.contains('ds-btn--destructive'),
          )
        : undefined;
      const tab = dialog.querySelector<HTMLElement>(
        '[role="tab"][aria-selected="true"]',
      );
      const closeButton = dialog.querySelector<HTMLElement>(
        'button.ds-icon-button:not(:disabled)',
      );
      (primary ?? neutral ?? tab ?? closeButton)?.focus({
        preventScroll: true,
      });
    }
    const now = document.activeElement;
    if (
      now instanceof HTMLButtonElement &&
      !now.disabled &&
      now.closest('.structure-draw-footer')
    ) {
      focusedFooterRef.current = now;
    }
  }, [open, confirm, footerSignature, busy]);

  const footerStatus = overviewQuery.isError ? (
    <Alert tone="danger" role="alert">
      {t('fiche.drawWorkflow.loadError')}
    </Alert>
  ) : mutationError ? (
    <MutationError
      error={mutationError}
      structureStages={structureStages}
      sotHref={persistentStructureMutationSotHref(mutationError, competitionId)}
    />
  ) : showingActive && activeRedo.kind === 'blocked' ? (
    <Alert tone="info" role="status">
      {tDraw(`redoBlocked.${activeRedo.reason}`)}
    </Alert>
  ) : releaseBlocked ? (
    <Alert tone="info" role="status">
      {tDraw('releaseBlockedAttachedMatches')}
    </Alert>
  ) : displayedDraw != null &&
    !showingActive &&
    releaseHoldFor(displayedDraw) === 'publishedDraw' ? (
    <Alert tone="info" role="status">
      {tDraw('releaseBlockedByPublishedDraw')}
    </Alert>
  ) : !overviewQuery.isLoading &&
    createBlockedDetail != null &&
    createBlockPresentation?.kind === 'inline' ? (
    <Alert tone={createBlockPresentation.tone} role="status">
      <p className="ds-body">{createBlockedDetail}</p>
      {createBlockEcho?.cta?.actionCode === 'AddEntry' &&
      createBlockEcho.sotHref ? (
        <p className="ds-body">
          <Link
            className="ds-text-link"
            to={createBlockEcho.sotHref}
            onClick={onClose}
          >
            {t('fiche.drawWorkflow.createBlocked.echoTeams')}
          </Link>
        </p>
      ) : null}
    </Alert>
  ) : null;

  return (
    <>
      <Dialog
        open={open}
        onClose={onClose}
        title={t('fiche.drawWorkflow.title', { phase: stage.name })}
        description={t('regulation.drawExecHeaderHint')}
        size="lg"
        closeLabel={tCommon('close')}
        closeDisabled={busy}
        trapFocus={confirm == null}
        initialFocus="footer"
        focusKey={focusKey}
        footer={
          <div
            className="structure-draw-footer"
            onFocus={(event) => {
              const target = event.target;
              if (
                target instanceof HTMLButtonElement &&
                target.closest('.structure-draw-footer')
              ) {
                focusedFooterRef.current = target;
              }
            }}
          >
            {showingActive && canAbandon ? (
              <button
                type="button"
                className="ds-btn ds-btn--destructive"
                disabled={busy}
                onClick={() => openConfirm('abandon', activeDraw!.id)}
              >
                {cancelMutation.isPending ? (
                  <PendingLabel>{tDraw('abandoning')}</PendingLabel>
                ) : (
                  <>
                    <CloseIcon size="sm" />
                    {tDraw('abandon')}
                  </>
                )}
              </button>
            ) : null}
            {showingActive && canCancelApplied ? (
              <button
                type="button"
                className="ds-btn ds-btn--destructive"
                disabled={busy}
                onClick={() => openConfirm('cancelApplied', activeDraw!.id)}
              >
                {cancelMutation.isPending ? (
                  <PendingLabel>{tDraw('cancelling')}</PendingLabel>
                ) : (
                  <>
                    <CloseIcon size="sm" />
                    {tDraw('cancelAppliedDraw')}
                  </>
                )}
              </button>
            ) : null}
            {selectedRelease ? (
              <button
                type="button"
                className="ds-btn ds-btn--secondary"
                disabled={busy || !releaseEnabled(selectedRelease)}
                onClick={() => openConfirm('release', selectedRelease.id)}
              >
                {releaseMutation.isPending ? (
                  <PendingLabel>{tDraw('releasingPlacements')}</PendingLabel>
                ) : (
                  <>
                    <UnlockIcon size="sm" />
                    {tDraw('releasePlacementsCount', {
                      aligned: alignedCountFor(selectedRelease),
                      total: drawResolutionPlacementCount(selectedRelease),
                    })}
                  </>
                )}
              </button>
            ) : null}
            {showingActive && canRerun ? (
              <button
                type="button"
                className="ds-btn ds-btn--secondary"
                disabled={busy}
                onClick={() => rerunMutation.mutate(activeDraw!.id)}
              >
                {rerunMutation.isPending ? (
                  <PendingLabel>{tDraw('rerunning')}</PendingLabel>
                ) : (
                  <>
                    <RandomIcon size="sm" />
                    {tDraw('rerun')}
                  </>
                )}
              </button>
            ) : null}
            {showLaunch ? (
              createBlockedShort != null ? (
                <Tooltip content={createBlockedShort}>
                  <button
                    type="button"
                    className="ds-btn ds-btn--primary"
                    disabled
                    aria-label={t('fiche.drawWorkflow.create')}
                  >
                    <RandomIcon size="sm" />
                    {t('fiche.drawWorkflow.create')}
                  </button>
                </Tooltip>
              ) : (
                <button
                  type="button"
                  className="ds-btn ds-btn--primary"
                  disabled={busy || !canLaunch}
                  onClick={() => {
                    if (!canLaunch) return;
                    createMutation.mutate(undefined, {
                      onSettled: () => {
                        createMutation.reset();
                      },
                    });
                  }}
                >
                  {createMutation.isPending ? (
                    <PendingLabel>
                      {t('fiche.drawWorkflow.creating')}
                    </PendingLabel>
                  ) : (
                    <>
                      <RandomIcon size="sm" />
                      {t('fiche.drawWorkflow.create')}
                    </>
                  )}
                </button>
              )
            ) : null}
            {showingActive && canValidate ? (
              <button
                type="button"
                className="ds-btn ds-btn--primary"
                disabled={busy}
                onClick={() => {
                  applyMutation.reset();
                  publishAndApplyMutation.mutate(activeDraw!);
                }}
              >
                {publishAndApplyMutation.isPending ? (
                  <PendingLabel>{tDraw('validating')}</PendingLabel>
                ) : (
                  <>
                    <CheckIcon size="sm" />
                    {tDraw('validate')}
                  </>
                )}
              </button>
            ) : null}
            {showingActive && canApplyResume ? (
              <button
                type="button"
                className="ds-btn ds-btn--primary"
                disabled={busy}
                onClick={() => openConfirm('apply', activeDraw!.id)}
              >
                {applyMutation.isPending ? (
                  <PendingLabel>{tDraw('applying')}</PendingLabel>
                ) : (
                  <>
                    <CheckIcon size="sm" />
                    {tDraw('apply')}
                  </>
                )}
              </button>
            ) : null}
            {showingActive && canRedo ? (
              <button
                type="button"
                className="ds-btn ds-btn--primary"
                disabled={busy}
                onClick={() => openConfirm('redo', activeDraw!.id)}
              >
                {redoMutation.isPending ? (
                  <PendingLabel>{tDraw('redoing')}</PendingLabel>
                ) : (
                  <>
                    <UndoIcon size="sm" />
                    {tDraw('redo')}
                  </>
                )}
              </button>
            ) : null}
          </div>
        }
        footerStatus={footerStatus}
      >
        <div className="structure-draw-dialog">
          <div className="structure-draw-chrome">
            <DrawHeroFacts
              formatKind={stage.formatKind}
              groupCount={stage.groupCount ?? 0}
              teamCount={poolCount}
              placesCount={placesN ?? 0}
            />
            {showRunSelector ? (
              <div
                className="structure-draw-runs"
                role="tablist"
                aria-label={tDraw('runsLabel')}
                onKeyDown={(event) =>
                  onRunKeyDown(event, runList, resolvedSelectedId, (drawId) => {
                    focusTabIdRef.current = runTabId(runsDomId, drawId);
                    setSelectedDrawId(drawId);
                  })
                }
              >
                {runList.map((draw) => {
                  const selected = draw.id === resolvedSelectedId;
                  return (
                    <SelectableTile
                      key={draw.id}
                      id={runTabId(runsDomId, draw.id)}
                      type="button"
                      className="structure-draw-run__tile"
                      role="tab"
                      selected={selected}
                      aria-selected={selected}
                      aria-controls={runPanelId(runsDomId, draw.id)}
                      tabIndex={selected ? 0 : -1}
                      onClick={() => setSelectedDrawId(draw.id)}
                    >
                      <span className="structure-draw-run__line">
                        <span>#{drawExecutionNumber(drawList, draw.id)}</span>
                        <DrawStatusBadge
                          status={draw.status}
                          density="compact"
                        />
                      </span>
                      <DrawRunMeta
                        draw={draw}
                        isActive={activeDraw?.id === draw.id}
                        aligned={alignedCountFor(draw)}
                        total={drawResolutionPlacementCount(draw)}
                        releaseHold={releaseHoldFor(draw)}
                      />
                    </SelectableTile>
                  );
                })}
              </div>
            ) : null}
          </div>

          {overviewQuery.isLoading ? (
            <WaitMark size="region">{t('fiche.drawWorkflow.loading')}</WaitMark>
          ) : null}

          {!overviewQuery.isLoading &&
          !overviewQuery.isError &&
          drawList.length === 0 ? (
            <EmptyState
              variant="idle"
              icon={<RandomIcon size="lg" />}
              title={t('fiche.drawWorkflow.emptyTitle')}
            >
              {drawKind == null
                ? t('fiche.drawWorkflow.kindUnsupported')
                : createBlockPresentation?.kind === 'inline'
                  ? null
                  : t('fiche.drawWorkflow.emptyBody')}
            </EmptyState>
          ) : null}

          {displayedDraw ? (
            <DrawExecutionView
              draw={displayedDraw}
              executionNumber={drawExecutionNumber(drawList, displayedDraw.id)}
              readOnly={displayedDraw.status === 'Cancelled'}
              showObsolete={
                showingActive &&
                displayedDraw.status === 'Published' &&
                displayedDraw.isObsoleteForConsumption === true
              }
              tabbed={showRunSelector}
              panelId={runPanelId(runsDomId, displayedDraw.id)}
              labelledBy={runTabId(runsDomId, displayedDraw.id)}
              slots={slots}
              groups={overviewGroups}
              placesPerGroup={placesPerGroup}
              bracketPairs={bracketPairs}
            />
          ) : null}
        </div>
      </Dialog>

      <ConfirmDialog
        open={confirm === 'apply'}
        title={tDraw('confirmApplyTitle')}
        message={tDraw('confirmApply')}
        confirmLabel={tDraw('apply')}
        cancelLabel={tCommon('cancel')}
        closeLabel={tCommon('close')}
        confirmDisabled={applyMutation.isPending}
        confirmPending={applyMutation.isPending}
        confirmPendingLabel={tDraw('applying')}
        footerStatus={
          applyMutation.isError ? (
            <MutationError
              error={applyMutation.error}
              structureStages={structureStages}
              sotHref={persistentStructureMutationSotHref(
                applyMutation.error,
                competitionId,
              )}
            />
          ) : null
        }
        onCancel={() => {
          if (applyMutation.isPending) {
            return;
          }
          closeConfirm();
        }}
        onConfirm={() => {
          if (!confirmDraw || applyMutation.isPending) {
            return;
          }
          publishAndApplyMutation.reset();
          applyMutation.mutate(confirmDraw);
        }}
      />

      <ConfirmDialog
        open={confirm === 'abandon'}
        title={tDraw('confirmAbandonTitle')}
        message={tDraw('confirmAbandon')}
        confirmLabel={tDraw('abandon')}
        confirmIcon={<CloseIcon size="sm" />}
        cancelLabel={tCommon('close')}
        closeLabel={tCommon('close')}
        danger
        confirmDisabled={cancelMutation.isPending}
        confirmPending={cancelMutation.isPending}
        confirmPendingLabel={tDraw('abandoning')}
        footerStatus={
          cancelMutation.isError ? (
            <MutationError
              error={cancelMutation.error}
              structureStages={structureStages}
              sotHref={persistentStructureMutationSotHref(
                cancelMutation.error,
                competitionId,
              )}
            />
          ) : null
        }
        onCancel={() => {
          if (cancelMutation.isPending) {
            return;
          }
          closeConfirm();
        }}
        onConfirm={() => {
          if (!confirmDraw || cancelMutation.isPending) {
            return;
          }
          cancelMutation.mutate(confirmDraw.id);
        }}
      />

      <ConfirmDialog
        open={confirm === 'cancelApplied'}
        title={tDraw('confirmCancelAppliedTitle')}
        message={tDraw('confirmCancelApplied')}
        confirmLabel={tDraw('cancelAppliedDraw')}
        confirmIcon={<CloseIcon size="sm" />}
        cancelLabel={tCommon('close')}
        closeLabel={tCommon('close')}
        danger
        confirmDisabled={cancelMutation.isPending}
        confirmPending={cancelMutation.isPending}
        confirmPendingLabel={tDraw('cancelling')}
        footerStatus={
          cancelMutation.isError ? (
            <MutationError
              error={cancelMutation.error}
              structureStages={structureStages}
              sotHref={persistentStructureMutationSotHref(
                cancelMutation.error,
                competitionId,
              )}
            />
          ) : null
        }
        onCancel={() => {
          if (cancelMutation.isPending) {
            return;
          }
          closeConfirm();
        }}
        onConfirm={() => {
          if (!confirmDraw || cancelMutation.isPending) {
            return;
          }
          cancelMutation.mutate(confirmDraw.id);
        }}
      />

      <ConfirmDialog
        open={confirm === 'release'}
        title={tDraw('confirmReleaseTitle')}
        message={tDraw('confirmRelease')}
        confirmLabel={tDraw('releasePlacements')}
        confirmIcon={<UnlockIcon size="sm" />}
        cancelLabel={tCommon('close')}
        closeLabel={tCommon('close')}
        confirmDisabled={releaseMutation.isPending}
        confirmPending={releaseMutation.isPending}
        confirmPendingLabel={tDraw('releasingPlacements')}
        footerStatus={
          releaseMutation.isError ? (
            <MutationError
              error={releaseMutation.error}
              structureStages={structureStages}
              sotHref={persistentStructureMutationSotHref(
                releaseMutation.error,
                competitionId,
              )}
            />
          ) : null
        }
        onCancel={() => {
          if (releaseMutation.isPending) {
            return;
          }
          closeConfirm();
        }}
        onConfirm={() => {
          if (!confirmDraw || releaseMutation.isPending) {
            return;
          }
          releaseMutation.mutate(confirmDraw.id);
        }}
      />

      <ConfirmDialog
        open={confirm === 'redo'}
        title={tDraw('confirmRedoTitle')}
        message={tDraw('confirmRedo')}
        confirmLabel={tDraw('redo')}
        confirmIcon={<UndoIcon size="sm" />}
        cancelLabel={tCommon('close')}
        closeLabel={tCommon('close')}
        danger
        confirmDisabled={redoMutation.isPending}
        confirmPending={redoMutation.isPending}
        confirmPendingLabel={tDraw('redoing')}
        footerStatus={
          redoMutation.isError ? (
            <MutationError
              error={
                stickyMutationError(redoMutation.error) ?? redoMutation.error
              }
              structureStages={structureStages}
              sotHref={persistentStructureMutationSotHref(
                redoMutation.error,
                competitionId,
              )}
            />
          ) : null
        }
        onCancel={() => {
          if (redoMutation.isPending) {
            return;
          }
          closeConfirm();
        }}
        onConfirm={() => {
          if (!confirmDraw || redoMutation.isPending) {
            return;
          }
          redoMutation.mutate(confirmDraw);
        }}
      />
    </>
  );
}

function runTabId(prefix: string, drawId: string): string {
  return `${prefix}-tab-${drawId}`;
}

function runPanelId(prefix: string, drawId: string): string {
  return `${prefix}-panel-${drawId}`;
}

function onRunKeyDown(
  event: KeyboardEvent<HTMLDivElement>,
  runs: readonly StageDraw[],
  selectedId: string | null,
  select: (drawId: string) => void,
): void {
  if (
    event.key !== 'ArrowLeft' &&
    event.key !== 'ArrowRight' &&
    event.key !== 'Home' &&
    event.key !== 'End'
  ) {
    return;
  }
  if (runs.length === 0) {
    return;
  }
  event.preventDefault();
  const index = Math.max(
    0,
    runs.findIndex((draw) => draw.id === selectedId),
  );
  let next = index;
  if (event.key === 'ArrowRight') {
    next = (index + 1) % runs.length;
  } else if (event.key === 'ArrowLeft') {
    next = (index - 1 + runs.length) % runs.length;
  } else if (event.key === 'Home') {
    next = 0;
  } else {
    next = runs.length - 1;
  }
  const draw = runs[next];
  if (draw) {
    select(draw.id);
  }
}

function DrawHeroFacts({
  formatKind,
  groupCount,
  teamCount,
  placesCount,
}: {
  formatKind: StructureFormatKind | null | undefined;
  groupCount: number;
  teamCount: number;
  placesCount: number;
}): ReactElement {
  const { t } = useTranslation('draw');
  const secondary =
    formatKind === 'Groups'
      ? { value: groupCount, label: t('heroGroups') }
      : { value: placesCount, label: t('heroPlaces') };
  const primary = { value: teamCount, label: t('heroTeams') };
  return (
    <div className="structure-qualification__facts" aria-live="polite">
      <span
        className="structure-qualification__fact structure-qualification__fact--secondary"
        data-fact="secondary"
      >
        <span className="structure-qualification__fact-value">
          {secondary.value}
        </span>
        <span className="structure-qualification__fact-label">
          {secondary.label}
        </span>
      </span>
      <span className="structure-qualification__fact-rule" aria-hidden="true" />
      <span
        className="structure-qualification__fact structure-qualification__fact--primary"
        data-fact="primary"
      >
        <span className="structure-qualification__fact-value">
          {primary.value}
        </span>
        <span className="structure-qualification__fact-label">
          {primary.label}
        </span>
      </span>
    </div>
  );
}

function DrawRunMeta({
  draw,
  isActive,
  aligned,
  total,
  releaseHold,
}: {
  draw: StageDraw;
  isActive: boolean;
  aligned: number;
  total: number;
  releaseHold: 'publishedDraw' | 'attachedMatches' | null;
}): ReactElement {
  const { t } = useTranslation('draw');
  if (
    isActive &&
    draw.status === 'Published' &&
    draw.isObsoleteForConsumption
  ) {
    return (
      <span className="structure-draw-run__meta">{t('runMetaObsolete')}</span>
    );
  }
  if (isActive) {
    return (
      <span className="structure-draw-run__meta">{t('runMetaActive')}</span>
    );
  }
  if (aligned > 0) {
    const reason =
      releaseHold === 'publishedDraw'
        ? t('releaseBlockedByPublishedDraw')
        : releaseHold === 'attachedMatches'
          ? t('releaseBlockedAttachedMatches')
          : null;
    return (
      <>
        <span className="structure-draw-run__meta" data-tone="attention">
          {t('runMetaUnreleased', { aligned, total })}
        </span>
        {reason != null ? (
          <span className="structure-draw-run__meta">{reason}</span>
        ) : null}
      </>
    );
  }
  return (
    <span className="structure-draw-run__meta">{t('runMetaReleased')}</span>
  );
}

function DrawExecutionView({
  draw,
  executionNumber,
  readOnly,
  showObsolete,
  tabbed,
  panelId,
  labelledBy,
  slots,
  groups,
  placesPerGroup,
  bracketPairs,
}: {
  draw: StageDraw;
  executionNumber: number;
  readOnly: boolean;
  showObsolete: boolean;
  tabbed: boolean;
  panelId: string;
  labelledBy: string;
  slots: StageSlot[];
  groups: StageGroupOverview[];
  placesPerGroup: number | null;
  bracketPairs: StageBracketPair[];
}): ReactElement {
  const { t } = useTranslation('draw');
  const { t: tCommon } = useTranslation('common');
  const { t: tStructure } = useTranslation('structure');
  const ui = getDrawUiProjection(draw, slots);
  const guidance = readOnly ? null : resolveDrawDetailGuidance(ui);
  const showPhrase =
    guidance?.kind === 'phrase' && guidance.messageKey !== 'draftResolved';
  const groupModel = useMemo(() => {
    if (draw.kind !== 'Group' || (draw.groupPlacements?.length ?? 0) === 0) {
      return [];
    }
    return groupsSchematicFromDrawDraft({
      placements: draw.groupPlacements ?? [],
      stageGroups: groups.map((group) => ({ id: group.id, name: group.name })),
      placesPerGroup,
      unknownEntry: tCommon('unknownEntry'),
      unknownGroup: t('unknownGroup'),
      groupLabel: (name) => tStructure('place.group', { name }),
    });
  }, [draw, groups, placesPerGroup, t, tCommon, tStructure]);

  const slotResult =
    draw.kind === 'Slot' && draw.slotPlacements.length > 0
      ? projectSlotDrawResult(
          draw.slotPlacements,
          bracketPairs,
          tCommon('unknownEntry'),
        )
      : { confrontations: [], unpaired: [] };
  const hasGroups = ui.showResults && groupModel.length > 0;
  const hasSlots =
    ui.showResults &&
    (slotResult.confrontations.length > 0 || slotResult.unpaired.length > 0);
  const showResults = hasGroups || hasSlots;
  const showEmptyResult = !showResults && guidance?.kind !== 'alert';

  return (
    <div
      className="structure-draw-execution"
      role={tabbed ? 'tabpanel' : undefined}
      id={tabbed ? panelId : undefined}
      aria-labelledby={tabbed ? labelledBy : undefined}
      tabIndex={tabbed ? 0 : undefined}
    >
      <div className="structure-draw-title">
        <h4 className="structure-draw-title__heading">
          {t('executionTitle', { n: executionNumber })}
        </h4>
        <DrawStatusBadge status={draw.status} density="compact" />
        {showObsolete ? (
          <Status density="compact" tone="attention">
            {t('obsolete')}
          </Status>
        ) : null}
      </div>
      <div className="structure-draw-view">
        {showPhrase ? (
          <p className="structure-draw-detail__phrase" role="status">
            {t(guidance.messageKey)}
          </p>
        ) : null}
        {guidance?.kind === 'alert' ? (
          <Alert tone={guidance.tone} role="status">
            {guidance.bodyMessageKey ? (
              <>
                <p className="ds-body">
                  <strong>{t(guidance.messageKey)}</strong>
                </p>
                <p className="ds-body">{t(guidance.bodyMessageKey)}</p>
              </>
            ) : (
              t(guidance.messageKey)
            )}
          </Alert>
        ) : null}
        {showEmptyResult ? (
          <EmptyState
            variant="idle"
            icon={<RandomIcon size="lg" />}
            title={t('noResultTitle')}
          >
            {t('noResultBody')}
          </EmptyState>
        ) : null}
        {hasGroups ? (
          <div className="structure-schematic-viewport">
            <GroupsSchematicView
              groups={groupModel}
              ariaLabel={t('result')}
              ghostLabel={t('ghostPlace')}
            />
          </div>
        ) : null}
        {hasSlots ? (
          <DrawSlotConfrontationGrid
            rows={slotResult.confrontations}
            unpaired={slotResult.unpaired}
            ariaLabel={t('result')}
            unpairedLabel={t('placements')}
          />
        ) : null}
      </div>
    </div>
  );
}

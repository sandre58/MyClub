import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useOpenEdge, useSyncWhen } from '../../../design-system/useRenderSync';
import {
  Fragment,
  useId,
  useLayoutEffect,
  useMemo,
  useRef,
  useState,
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
import {
  CloseIcon,
  ChevronDownIcon,
} from '../../../design-system/icons/shellIcons';
import { ToastToneIcon } from '../../../design-system/icons/toastIcons';
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
import { resolveLifecycleMutationMessage } from '../problems';
import {
  buildCupDrawDisplayFromDto,
  drawExecutionNumber,
  drawResolutionPlacementCount,
  getDrawUiProjection,
  pickActiveDraw,
  drawCreateLaunchBlockCopy,
  pickDrawFooterFocusAfterRelease,
  slotsHaveOccupants,
  projectSlotDrawResult,
  resolveDrawDetailGuidance,
  resolveDrawHeroModel,
  sortDrawsNewestFirst,
  type CupDrawCellLabels,
} from './drawUi';
import { DrawSlotConfrontationGrid } from './DrawSlotConfrontationGrid';
import { redoAppliedDraw } from './drawRedo';
import {
  drawActionsOf,
  isDrawActionAvailable,
  releaseBlockReason,
  redoPresentation,
  releaseFooterMode,
  releaseHoldForDraw,
} from './drawActions';
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
  return error;
}

/** Work dialog — active execution is primary; cancelled runs disclose below. */
export type StructureDrawDialogProps = {
  open: boolean;
  onClose: () => void;
  competitionId: string;
  /** Callers still pass host status; draw CTAs are projected on the stage overview. */
  competitionStatus: CompetitionStatus;
  stage: StructureStageHubSummary;
  structureStages: readonly StructureStageHubSummary[];
};

const EMPTY_DRAWS: StageDraw[] = [];
const EMPTY_SLOTS: StageSlot[] = [];
const EMPTY_GROUPS: StageGroupOverview[] = [];

type ConfirmKind = 'apply' | 'abandon' | 'release' | 'redo' | 'cancelApplied';

/**
 * Work dialog — stage draw executions.
 * Footer actions always target the active execution; cancelled runs disclose inline.
 */
export function StructureDrawDialog({
  open,
  onClose,
  competitionId,
  structureStages,
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
  const drawList = draws ?? EMPTY_DRAWS;
  const poolCount = stage.compositionEntryCount ?? 0;
  const placesN = stage.compositionCapacity ?? resolvePlacesN(stage);
  const placesPerGroup = stage.placesPerGroup ?? stage.numberOfPots ?? null;

  const [confirm, setConfirm] = useState<ConfirmKind | null>(null);
  const [confirmDrawId, setConfirmDrawId] = useState<string | null>(null);
  const [expandedDrawId, setExpandedDrawId] = useState<string | null>(null);
  const focusedFooterRef = useRef<HTMLButtonElement | null>(null);
  const pendingReleaseFocusRef = useRef(false);
  const dialogBodyRef = useRef<HTMLDivElement>(null);
  const runsDomId = useId();
  const previousRunsSectionId = `${runsDomId}-previous-runs`;

  useOpenEdge(open, {
    onClose: () => {
      setConfirm(null);
      setConfirmDrawId(null);
      setExpandedDrawId(null);
      focusedFooterRef.current = null;
      pendingReleaseFocusRef.current = false;
    },
  });

  const activeDraw = useMemo(() => pickActiveDraw(drawList), [drawList]);
  const stageDrawMetrics = overviewQuery.data?.slotDrawDisplayMetrics ?? null;
  // Group draws ignore cup metrics. A slot draft prefers its displayMetrics.
  // When that field is absent, stage metrics are used. When both are absent,
  // resolveDrawHeroModel falls back to the stage team and place counts.
  const heroMetrics =
    activeDraw?.kind === 'Group'
      ? null
      : activeDraw?.kind === 'Slot'
        ? (activeDraw.displayMetrics ?? stageDrawMetrics)
        : stageDrawMetrics;
  const heroModel = resolveDrawHeroModel({
    formatKind: stage.formatKind,
    groupCount: stage.groupCount ?? 0,
    teamCount: poolCount,
    placesCount: placesN ?? 0,
    metrics: heroMetrics,
    inputsIntent: activeDraw?.inputsIntent ?? null,
    hasActiveDraw: activeDraw != null,
  });
  const cancelledDraws = useMemo(
    () =>
      sortDrawsNewestFirst(drawList).filter(
        (draw) => draw.status === 'Cancelled',
      ),
    [drawList],
  );

  useSyncWhen(
    open && draws != null,
    cancelledDraws.map((draw) => draw.id).join(','),
    () => {
      setExpandedDrawId((current) => {
        if (current == null) {
          return null;
        }
        return cancelledDraws.some((draw) => draw.id === current)
          ? current
          : null;
      });
    },
  );

  function alignedCountFor(draw: StageDraw): number {
    return drawActionsOf(draw).release.releasableCount;
  }

  const confirmDraw =
    drawList.find((draw) => draw.id === confirmDrawId) ?? null;

  const showLaunch =
    drawKind != null && !overviewQuery.isLoading && !activeDraw;
  const launchAllowed = stage.drawCreateLaunch?.allowed === true;
  const canLaunch = showLaunch && launchAllowed;
  const launchBlock =
    showLaunch && !launchAllowed
      ? drawCreateLaunchBlockCopy(stage.drawCreateLaunch?.blockReasonCode)
      : null;
  const createBlockedReason = launchBlock?.reason ?? null;
  const createBlockPresentation = launchBlock?.presentation ?? null;
  const createBlockEcho =
    createBlockedReason != null
      ? functionalProblemPresentation({
          source: createBlockedReason,
          competitionId,
          stageId: stage.stageId,
        })
      : null;
  const createBlockedShort = launchBlock?.shortKey
    ? t(launchBlock.shortKey)
    : null;
  const createBlockedDetail =
    createBlockedReason != null &&
    createBlockPresentation?.kind === 'inline' &&
    launchBlock?.detailKey
      ? createBlockedReason === 'countMismatch'
        ? t(launchBlock.detailKey, {
            count: poolCount,
            places: placesN ?? 0,
          })
        : t(launchBlock.detailKey)
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
      if (stage.drawCreateLaunch?.allowed !== true) {
        throw new Error(
          `Draw create blocked: ${stage.drawCreateLaunch?.blockReasonCode ?? 'denied'}`,
        );
      }
      return createAndGenerateDraw(stageId, drawKind);
    },
    onSuccess: (result) => {
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
    mutationFn: (drawId: string) =>
      rerunDrawGeneration(
        stageId,
        drawId,
        drawKind === 'Slot' ? 'Slot' : 'Group',
      ),
    onSuccess: (result) => {
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
    onError: (error) => {
      if (error instanceof ApiError && error.status === 409) {
        scheduleDrawHubRefresh();
      }
    },
    onSettled: () => {
      scheduleDrawHubRefresh();
    },
  });

  const cancelMutation = useMutation({
    mutationFn: (drawId: string) => cancelDraw(stageId, drawId),
    onSuccess: () => {
      closeConfirm();
      scheduleDrawHubRefresh();
      notify.success(tDraw('toastCancelled'));
    },
    onError: (error) => {
      if (error instanceof ApiError && error.status === 409) {
        scheduleDrawHubRefresh();
      }
    },
  });

  const releaseMutation = useMutation({
    mutationFn: (drawId: string) =>
      releaseDrawAlignedPlacements(stageId, drawId),
    onSuccess: (result) => {
      closeConfirm();
      scheduleDrawHubRefresh();
      notify.success(tDraw('toastReleased', { count: result.releasedCount }));
    },
    onError: (error) => {
      if (error instanceof ApiError && error.status === 409) {
        scheduleDrawHubRefresh();
      }
    },
    onSettled: () => {
      pendingReleaseFocusRef.current = true;
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
    onError: (error) => {
      if (error instanceof ApiError && error.status === 409) {
        scheduleDrawHubRefresh();
      }
    },
  });

  const redoMutation = useMutation({
    mutationFn: (draw: StageDraw) => redoAppliedDraw(stageId, draw.id),
    onSuccess: (result) => {
      closeConfirm();
      scheduleDrawHubRefresh();
      if (result.isNoSolution) {
        notify.attention(tDraw('toastCreatedNoSolution'));
      } else {
        notify.success(tDraw('toastCreated'));
      }
    },
    onError: () => {
      // Atomic redo: any failure (409 refusal or otherwise) leaves the server state ahead of what
      // the dialog last fetched, so refresh regardless of status code.
      scheduleDrawHubRefresh();
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

  const releaseBodyError = stickyMutationError(releaseMutation.error);

  const mutationError =
    stickyMutationError(publishAndApplyMutation.error) ??
    stickyMutationError(cancelMutation.error) ??
    stickyMutationError(applyMutation.error) ??
    stickyMutationError(rerunMutation.error) ??
    stickyMutationError(redoMutation.error);

  const activeUi =
    activeDraw != null ? getDrawUiProjection(activeDraw, slots) : null;
  const activeRedo =
    activeDraw != null
      ? redoPresentation(activeDraw)
      : { kind: 'hidden' as const };

  const canValidate =
    activeDraw != null &&
    isDrawActionAvailable(drawActionsOf(activeDraw).publishAndApply);
  const canApplyResume =
    activeDraw != null &&
    isDrawActionAvailable(drawActionsOf(activeDraw).apply);
  const canRerun =
    activeDraw != null &&
    activeDraw.status === 'Draft' &&
    (activeDraw.kind === 'Slot' || activeDraw.kind === 'Group');
  const canAbandon =
    activeDraw != null &&
    activeUi != null &&
    !activeUi.isApplied &&
    isDrawActionAvailable(drawActionsOf(activeDraw).cancel);
  const canRedo = activeRedo.kind === 'available';
  const canCancelApplied =
    activeDraw != null &&
    activeUi?.isApplied === true &&
    activeRedo.kind === 'blocked' &&
    isDrawActionAvailable(drawActionsOf(activeDraw).cancel);

  const releaseTarget = (() => {
    if (activeDraw != null) {
      return null;
    }
    const candidateId = overviewQuery.data?.releaseCandidateDrawId;
    if (candidateId == null) {
      return null;
    }
    const draw = cancelledDraws.find((item) => item.id === candidateId) ?? null;
    return draw != null && releaseFooterMode(draw) !== 'hidden' ? draw : null;
  })();

  const releaseFooterBlocked =
    releaseTarget != null && releaseFooterMode(releaseTarget) === 'disabled';
  const releaseFooterBlockedReason =
    releaseTarget != null ? releaseBlockReason(releaseTarget) : null;

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
    activeDraw?.id ?? 'none',
    canAbandon ? 'abandon' : '',
    canCancelApplied ? 'cancelApplied' : '',
    releaseTarget ? `release:${releaseFooterBlocked ? 'blocked' : 'on'}` : '',
    canRerun ? 'rerun' : '',
    showLaunch ? 'launch' : '',
    canValidate ? 'validate' : '',
    canApplyResume ? 'apply' : '',
    canRedo ? 'redo' : '',
  ].join('|');

  useLayoutEffect(() => {
    if (expandedDrawId == null) {
      return;
    }
    dialogBodyRef.current
      ?.querySelector<HTMLElement>(
        `[data-draw-run-trigger="${expandedDrawId}"]`,
      )
      ?.scrollIntoView?.({ block: 'nearest' });
  }, [expandedDrawId]);

  useLayoutEffect(() => {
    if (!open || !pendingReleaseFocusRef.current) {
      return;
    }
    if (confirm != null || busy || overviewQuery.isFetching) {
      return;
    }
    const timer = window.setTimeout(() => {
      pendingReleaseFocusRef.current = false;
      const dialog = document
        .querySelector('.structure-draw-dialog')
        ?.closest('[role="dialog"]');
      if (!dialog) {
        return;
      }
      const target = pickDrawFooterFocusAfterRelease(dialog);
      target?.focus({ preventScroll: true });
      const now = document.activeElement;
      if (
        now instanceof HTMLButtonElement &&
        !now.disabled &&
        now.closest('.structure-draw-footer')
      ) {
        focusedFooterRef.current = now;
      }
    }, 160);
    return () => window.clearTimeout(timer);
  }, [open, confirm, busy, footerSignature, overviewQuery.isFetching]);

  useLayoutEffect(() => {
    if (!open) {
      focusedFooterRef.current = null;
      return;
    }
    if (confirm != null || busy || pendingReleaseFocusRef.current) {
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
      const closeButton = dialog.querySelector<HTMLElement>(
        'button.ds-icon-button:not(:disabled)',
      );
      (primary ?? neutral ?? closeButton)?.focus({
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
  ) : activeDraw != null && activeRedo.kind === 'blocked' ? (
    <Alert tone="info" role="status">
      {tDraw(`redoBlocked.${activeRedo.reason}`)}
    </Alert>
  ) : releaseFooterBlocked ? (
    <Alert tone="info" role="status">
      {releaseFooterBlockedReason}
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
            {activeDraw != null && canAbandon ? (
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
            {activeDraw != null && canCancelApplied ? (
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
            {releaseTarget ? (
              <button
                type="button"
                className="ds-btn ds-btn--secondary"
                data-action="release-draw-placements"
                disabled={busy || releaseFooterBlocked}
                onClick={() => openConfirm('release', releaseTarget.id)}
              >
                {releaseMutation.isPending ? (
                  <PendingLabel>{tDraw('releasingPlacements')}</PendingLabel>
                ) : (
                  <>
                    <UnlockIcon size="sm" />
                    {releaseTarget.kind === 'Slot'
                      ? tDraw('releaseDrawnTeams', {
                          n: alignedCountFor(releaseTarget),
                        })
                      : tDraw('releaseRun', {
                          n: drawExecutionNumber(drawList, releaseTarget.id),
                          aligned: alignedCountFor(releaseTarget),
                          total: drawResolutionPlacementCount(releaseTarget),
                        })}
                  </>
                )}
              </button>
            ) : null}
            {activeDraw != null && canRerun ? (
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
            {activeDraw != null && canValidate ? (
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
            {activeDraw != null && canApplyResume ? (
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
            {activeDraw != null && canRedo ? (
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
        <div className="structure-draw-dialog" ref={dialogBodyRef}>
          <DrawHeroFacts model={heroModel} />

          {/*
            slotDrawDisplayMetrics is the server count. When that payload is
            absent, the hint falls back to whether any slot already has an occupant.
          */}
          {canLaunch &&
          drawKind === 'Slot' &&
          (stageDrawMetrics
            ? stageDrawMetrics.alreadyPlacedEntryCount > 0 &&
              stageDrawMetrics.vacantSlotKeyCount > 0
            : slotsHaveOccupants(slots)) ? (
            <Alert tone="info" role="status">
              <p className="ds-body">
                {t('fiche.drawWorkflow.partialFillHint')}
              </p>
            </Alert>
          ) : null}

          {overviewQuery.isLoading ? (
            <WaitMark size="region">{t('fiche.drawWorkflow.loading')}</WaitMark>
          ) : null}

          {!overviewQuery.isLoading && releaseBodyError ? (
            <MutationError
              error={releaseBodyError}
              structureStages={structureStages}
              sotHref={persistentStructureMutationSotHref(
                releaseBodyError,
                competitionId,
              )}
            />
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

          {!overviewQuery.isLoading &&
          !overviewQuery.isError &&
          activeDraw == null &&
          cancelledDraws.length > 0 ? (
            <EmptyState
              variant="idle"
              icon={<RandomIcon size="lg" />}
              title={tDraw('noActiveTitle')}
            >
              {releaseTarget != null && !releaseFooterBlocked
                ? tDraw('noActiveReleaseFirst', {
                    n: drawExecutionNumber(drawList, releaseTarget.id),
                  })
                : tDraw('noActiveBody')}
            </EmptyState>
          ) : null}

          {activeDraw != null ? (
            <>
              <div className="structure-draw-title">
                <h4 className="structure-draw-title__heading">
                  {tDraw('executionTitle', {
                    n: drawExecutionNumber(drawList, activeDraw.id),
                  })}
                </h4>
                <DrawStatusBadge status={activeDraw.status} density="compact" />
                {activeDraw.status === 'Published' &&
                activeDraw.isObsoleteForConsumption === true ? (
                  <Status density="compact" tone="attention">
                    {tDraw('obsolete')}
                  </Status>
                ) : null}
              </div>
              <DrawExecutionView
                draw={activeDraw}
                readOnly={false}
                slots={slots}
                groups={overviewGroups}
                placesPerGroup={placesPerGroup}
                bracketPairs={bracketPairs}
              />
            </>
          ) : null}

          {cancelledDraws.length > 0 ? (
            <section
              className="structure-draw-previous"
              id={previousRunsSectionId}
              aria-labelledby={`${previousRunsSectionId}-title`}
            >
              <h4
                className="structure-draw-previous__heading"
                id={`${previousRunsSectionId}-title`}
              >
                {tDraw('previousRunsTitle', { count: cancelledDraws.length })}
              </h4>
              <ul className="structure-draw-previous__list">
                {cancelledDraws.map((draw) => {
                  const expanded = draw.id === expandedDrawId;
                  const aligned = alignedCountFor(draw);
                  const total = drawResolutionPlacementCount(draw);
                  const panelId = previousRunDetailId(runsDomId, draw.id);
                  const executionNumber = drawExecutionNumber(
                    drawList,
                    draw.id,
                  );
                  return (
                    <li key={draw.id} className="structure-draw-previous__item">
                      <SelectableTile
                        as="div"
                        selected={expanded}
                        className="structure-draw-previous__tile"
                      >
                        <h5 className="structure-draw-previous__header">
                          <button
                            type="button"
                            className="ds-selectable-tile__trigger structure-draw-previous__trigger"
                            data-draw-run-trigger={draw.id}
                            aria-expanded={expanded}
                            aria-controls={expanded ? panelId : undefined}
                            onClick={() =>
                              setExpandedDrawId((current) =>
                                current === draw.id ? null : draw.id,
                              )
                            }
                          >
                            <span className="structure-draw-previous__lead">
                              <span className="structure-draw-previous__title">
                                {tDraw('executionTitle', {
                                  n: executionNumber,
                                })}
                              </span>
                              <span className="structure-draw-previous__meta">
                                {total > 0 && aligned === 0 ? (
                                  <Status
                                    density="compact"
                                    tone="success"
                                    variant="outline"
                                    shape="pill"
                                  >
                                    <span className="ds-status-inline">
                                      <span
                                        className="ds-status-inline__icon"
                                        aria-hidden="true"
                                      >
                                        <ToastToneIcon
                                          tone="success"
                                          size="sm"
                                        />
                                      </span>
                                      {tDraw('runMetaReleased')}
                                    </span>
                                  </Status>
                                ) : null}
                                {aligned > 0 ? (
                                  <Status
                                    density="compact"
                                    tone="neutral"
                                    variant="outline"
                                    shape="pill"
                                  >
                                    {tDraw('runMetaUnreleased', {
                                      aligned,
                                      total,
                                    })}
                                  </Status>
                                ) : null}
                                <DrawStatusBadge
                                  status="Cancelled"
                                  density="compact"
                                  variant="outline"
                                />
                              </span>
                            </span>
                            <span
                              className="ds-selectable-tile__chevron"
                              aria-hidden="true"
                            >
                              <ChevronDownIcon size="sm" />
                            </span>
                          </button>
                        </h5>
                        {expanded ? (
                          <div
                            id={panelId}
                            className="structure-draw-previous__panel"
                          >
                            <CancelledDrawDetail
                              draw={draw}
                              executionNumber={executionNumber}
                              activeDraw={activeDraw}
                              aligned={aligned}
                              releaseHold={releaseHoldForDraw(draw)}
                              slots={slots}
                              groups={overviewGroups}
                              placesPerGroup={placesPerGroup}
                              bracketPairs={bracketPairs}
                            />
                          </div>
                        ) : null}
                      </SelectableTile>
                    </li>
                  );
                })}
              </ul>
            </section>
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
        confirmLabel={
          confirmDraw?.kind === 'Slot'
            ? tDraw('releaseDrawnTeams', {
                n: alignedCountFor(confirmDraw),
              })
            : tDraw('releasePlacements')
        }
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

function previousRunDetailId(prefix: string, drawId: string): string {
  return `${prefix}-detail-${drawId}`;
}

function cancelledDrawReleaseReason(
  executionNumber: number,
  activeDraw: StageDraw | null,
  releaseHold: 'publishedDraw' | 'attachedMatches' | null,
  tDraw: (key: string, options?: Record<string, unknown>) => string,
): string {
  if (releaseHold === 'publishedDraw') {
    return tDraw('releaseBlockedByPublishedDraw');
  }
  if (releaseHold === 'attachedMatches') {
    return tDraw('releaseBlockedAttachedMatches');
  }
  if (activeDraw != null) {
    return tDraw('releaseHeldByActive');
  }
  return tDraw('noActiveReleaseFirst', { n: executionNumber });
}

function CancelledDrawDetail({
  draw,
  executionNumber,
  activeDraw,
  aligned,
  releaseHold,
  slots,
  groups,
  placesPerGroup,
  bracketPairs,
}: {
  draw: StageDraw;
  executionNumber: number;
  activeDraw: StageDraw | null;
  aligned: number;
  releaseHold: 'publishedDraw' | 'attachedMatches' | null;
  slots: StageSlot[];
  groups: StageGroupOverview[];
  placesPerGroup: number | null;
  bracketPairs: StageBracketPair[];
}): ReactElement {
  const { t: tDraw } = useTranslation('draw');

  return (
    <div className="structure-draw-previous__body">
      {aligned > 0 ? (
        <Alert tone="info" role="status">
          <p className="ds-body">
            {cancelledDrawReleaseReason(
              executionNumber,
              activeDraw,
              releaseHold,
              tDraw,
            )}
          </p>
        </Alert>
      ) : null}
      <DrawExecutionView
        draw={draw}
        readOnly
        omitResultAriaLabel={Boolean(activeDraw)}
        slots={slots}
        groups={groups}
        placesPerGroup={placesPerGroup}
        bracketPairs={bracketPairs}
      />
    </div>
  );
}

function DrawHeroFacts({
  model,
}: {
  model: ReturnType<typeof resolveDrawHeroModel>;
}): ReactElement {
  const { t } = useTranslation('draw');
  if (model.layout === 'triplet') {
    const facts = [model.toPlace, model.freePlaces, model.alreadyPlaced];
    return (
      <div className="structure-qualification__facts" aria-live="polite">
        {facts.map((fact, index) => (
          <Fragment key={fact.label}>
            {index > 0 ? (
              <span
                className="structure-qualification__fact-rule"
                aria-hidden="true"
              />
            ) : null}
            <span
              className="structure-qualification__fact"
              data-fact={fact.label}
            >
              <span className="structure-qualification__fact-value">
                {fact.value}
              </span>
              <span className="structure-qualification__fact-label">
                {t(fact.label)}
              </span>
            </span>
          </Fragment>
        ))}
      </div>
    );
  }
  return (
    <div className="structure-qualification__facts" aria-live="polite">
      <span
        className="structure-qualification__fact structure-qualification__fact--secondary"
        data-fact="secondary"
      >
        <span className="structure-qualification__fact-value">
          {model.secondary.value}
        </span>
        <span className="structure-qualification__fact-label">
          {t(model.secondary.label)}
        </span>
      </span>
      <span className="structure-qualification__fact-rule" aria-hidden="true" />
      <span
        className="structure-qualification__fact structure-qualification__fact--primary"
        data-fact="primary"
      >
        <span className="structure-qualification__fact-value">
          {model.primary.value}
        </span>
        <span className="structure-qualification__fact-label">
          {t(model.primary.label)}
        </span>
      </span>
    </div>
  );
}

function DrawExecutionView({
  draw,
  readOnly,
  omitResultAriaLabel = false,
  slots,
  groups,
  placesPerGroup,
  bracketPairs,
}: {
  draw: StageDraw;
  readOnly: boolean;
  omitResultAriaLabel?: boolean;
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

  const cellLabels: CupDrawCellLabels = {
    unknownEntry: tCommon('unknownEntry'),
    alreadyPlaced: t('alreadyPlaced'),
    freePlace: t('freePlace'),
    reserved: t('reserved'),
    withdrawn: t('withdrawn'),
    outsideDraw: t('outsideDraw'),
    originManualHint: t('originManualHint'),
  };
  // Cells are the cup read model. projectSlotDrawResult stays for payloads
  // that only carry resolution placements (no slotDisplayCells).
  const slotResult =
    draw.kind !== 'Slot'
      ? { confrontations: [], unpaired: [] }
      : draw.slotDisplayCells && draw.slotDisplayCells.length > 0
        ? buildCupDrawDisplayFromDto(
            draw.slotDisplayCells,
            bracketPairs,
            cellLabels,
          )
        : draw.slotPlacements.length > 0
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
    <div className="structure-draw-execution">
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
              ariaLabel={omitResultAriaLabel ? undefined : t('result')}
              ghostLabel={t('ghostPlace')}
            />
          </div>
        ) : null}
        {hasSlots ? (
          <DrawSlotConfrontationGrid
            rows={slotResult.confrontations}
            unpaired={slotResult.unpaired}
            ariaLabel={omitResultAriaLabel ? undefined : t('result')}
            unpairedLabel={t('placements')}
          />
        ) : null}
      </div>
    </div>
  );
}

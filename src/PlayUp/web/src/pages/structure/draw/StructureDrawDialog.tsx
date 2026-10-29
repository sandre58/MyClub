import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useOpenEdge } from '../../../design-system/useRenderSync';
import { useMemo, useState, type ReactElement } from 'react';
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
import { Tooltip } from '../../../design-system/components/Tooltip';
import {
  CheckIcon,
  RandomIcon,
  UndoIcon,
  UnlockIcon,
} from '../../../design-system/icons/contentIcons';
import {
  ChevronDownIcon,
  CloseIcon,
} from '../../../design-system/icons/shellIcons';
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

/** Work dialog — one current execution, cancelled history folded away. */
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
 * Current non-cancelled execution is full width. Cancelled runs fold into
 * one disclosure; the releasable one keeps Libérer.
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
  const [cancelledOpen, setCancelledOpen] = useState(false);
  const [openHistoryId, setOpenHistoryId] = useState<string | null>(null);

  useOpenEdge(open, {
    onClose: () => {
      setConfirm(null);
      setConfirmDrawId(null);
      setCancelledOpen(false);
      setOpenHistoryId(null);
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
  const forceCancelledOpen = activeDraw == null && cancelledDraws.length > 0;

  function alignedCountFor(draw: StageDraw): number {
    return countAlignedDrawPlacements(draw, slots, overviewGroups);
  }

  function releaseAllowed(draw: StageDraw): boolean {
    return canReleaseDrawAlignedPlacements({
      competitionStatus,
      stageStatus,
      drawStatus: draw.status,
      drawKind: draw.kind,
      stageHasPublishedAppliedDraw: stageHasAppliedDraw,
      alignedPlacementCount: alignedCountFor(draw),
    });
  }

  const footerRelease =
    activeDraw == null
      ? (cancelledDraws.find((draw) => releaseAllowed(draw)) ?? null)
      : null;

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
      closeConfirm();
      scheduleDrawHubRefresh();
      notify.success(tDraw('toastCancelled'));
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

  const focusKey = [
    overviewQuery.isLoading ? 'loading' : 'ready',
    activeDraw?.id ?? 'none',
    activeDraw?.status ?? '',
    activeDraw?.resolutionState ?? '',
    activeUi?.isApplied ? 'applied' : 'open',
    activeRedo.kind,
    footerRelease?.id ?? '',
    canLaunch ? 'launch' : 'wait',
  ].join('|');

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
  ) : activeRedo.kind === 'blocked' ? (
    <Alert tone="info" role="status">
      {tDraw(`redoBlocked.${activeRedo.reason}`)}
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
          <div className="structure-draw-footer">
            <div className="structure-draw-footer__start">
              {canAbandon ? (
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
              {canCancelApplied ? (
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
            </div>
            <div className="structure-draw-footer__end">
              {footerRelease ? (
                <button
                  type="button"
                  className="ds-btn ds-btn--secondary"
                  disabled={busy}
                  onClick={() => openConfirm('release', footerRelease.id)}
                >
                  {releaseMutation.isPending ? (
                    <PendingLabel>{tDraw('releasingPlacements')}</PendingLabel>
                  ) : (
                    <>
                      <UnlockIcon size="sm" />
                      {tDraw('releasePlacementsCount', {
                        aligned: alignedCountFor(footerRelease),
                        total: drawResolutionPlacementCount(footerRelease),
                      })}
                    </>
                  )}
                </button>
              ) : null}
              {canRerun ? (
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
              {canValidate ? (
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
              {canApplyResume ? (
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
              {canRedo ? (
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
          </div>
        }
        footerStatus={footerStatus}
      >
        <div className="structure-draw-dialog">
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

          {activeDraw ? (
            <DrawActiveBody
              draw={activeDraw}
              executionNumber={drawExecutionNumber(drawList, activeDraw.id)}
              teamCount={poolCount}
              slots={slots}
              groups={overviewGroups}
              placesPerGroup={placesPerGroup}
              bracketPairs={bracketPairs}
            />
          ) : null}

          {cancelledDraws.length > 0 ? (
            <details
              className="structure-draw-cancelled"
              open={forceCancelledOpen || cancelledOpen}
              onToggle={(event) => {
                if (forceCancelledOpen) {
                  return;
                }
                setCancelledOpen(event.currentTarget.open);
              }}
            >
              <summary className="structure-draw-cancelled__summary">
                <ChevronDownIcon size="sm" />
                {tDraw('previousExecutions', { count: cancelledDraws.length })}
              </summary>
              <div className="structure-draw-cancelled__body">
                {cancelledDraws.map((draw) => (
                  <CancelledDrawRow
                    key={draw.id}
                    draw={draw}
                    executionNumber={drawExecutionNumber(drawList, draw.id)}
                    aligned={alignedCountFor(draw)}
                    total={drawResolutionPlacementCount(draw)}
                    canRelease={releaseAllowed(draw)}
                    open={openHistoryId === draw.id}
                    busy={busy}
                    releasePending={
                      releaseMutation.isPending && confirmDrawId === draw.id
                    }
                    slots={slots}
                    groups={overviewGroups}
                    placesPerGroup={placesPerGroup}
                    bracketPairs={bracketPairs}
                    onToggle={() =>
                      setOpenHistoryId((current) =>
                        current === draw.id ? null : draw.id,
                      )
                    }
                    onRelease={() => openConfirm('release', draw.id)}
                  />
                ))}
              </div>
            </details>
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

function DrawActiveBody({
  draw,
  executionNumber,
  teamCount,
  slots,
  groups,
  placesPerGroup,
  bracketPairs,
}: {
  draw: StageDraw;
  executionNumber: number;
  teamCount: number;
  slots: StageSlot[];
  groups: StageGroupOverview[];
  placesPerGroup: number | null;
  bracketPairs: StageBracketPair[];
}): ReactElement {
  const { t } = useTranslation('draw');
  const { t: tCommon } = useTranslation('common');
  const { t: tStructure } = useTranslation('structure');
  const ui = getDrawUiProjection(draw, slots);
  const guidance = resolveDrawDetailGuidance(ui);
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
    <div className="structure-draw-active">
      <div className="structure-draw-status">
        <DrawStatusBadge status={draw.status} density="compact" />
        <p className="structure-draw-status__line">
          {t('executionStatus', { count: teamCount, n: executionNumber })}
        </p>
        {draw.isObsoleteForConsumption ? (
          <Status density="compact" tone="attention">
            {t('obsolete')}
          </Status>
        ) : null}
      </div>
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
  );
}

function CancelledDrawRow({
  draw,
  executionNumber,
  aligned,
  total,
  canRelease,
  open,
  busy,
  releasePending,
  slots,
  groups,
  placesPerGroup,
  bracketPairs,
  onToggle,
  onRelease,
}: {
  draw: StageDraw;
  executionNumber: number;
  aligned: number;
  total: number;
  canRelease: boolean;
  open: boolean;
  busy: boolean;
  releasePending: boolean;
  slots: StageSlot[];
  groups: StageGroupOverview[];
  placesPerGroup: number | null;
  bracketPairs: StageBracketPair[];
  onToggle: () => void;
  onRelease: () => void;
}): ReactElement {
  const { t } = useTranslation('draw');
  const { t: tCommon } = useTranslation('common');
  const { t: tStructure } = useTranslation('structure');
  const lines = cancelledHistoryLines({
    draw,
    slots,
    groups,
    placesPerGroup,
    bracketPairs,
    unknownEntry: tCommon('unknownEntry'),
    unknownGroup: t('unknownGroup'),
    groupLabel: (name) => tStructure('place.group', { name }),
    format: t,
  });
  const summary =
    aligned > 0
      ? t('historyRowUnreleased', {
          n: executionNumber,
          aligned,
          total,
        })
      : t('historyRowReleased', { n: executionNumber });

  return (
    <details className="structure-draw-history-row" open={open}>
      <summary
        className="structure-draw-history-row__summary"
        onClick={(event) => {
          event.preventDefault();
          onToggle();
        }}
      >
        <span>{summary}</span>
        {canRelease ? (
          <button
            type="button"
            className="ds-btn ds-btn--secondary"
            disabled={busy}
            onClick={(event) => {
              event.preventDefault();
              event.stopPropagation();
              onRelease();
            }}
          >
            {releasePending ? (
              <PendingLabel>{t('releasingPlacements')}</PendingLabel>
            ) : (
              <>
                <UnlockIcon size="sm" />
                {t('releasePlacementsCount', { aligned, total })}
              </>
            )}
          </button>
        ) : null}
      </summary>
      {open && lines.length > 0 ? (
        <ul className="structure-draw-history-row__detail">
          {lines.map((line) => (
            <li key={line}>{line}</li>
          ))}
        </ul>
      ) : null}
    </details>
  );
}

function cancelledHistoryLines(input: {
  draw: StageDraw;
  slots: StageSlot[];
  groups: StageGroupOverview[];
  placesPerGroup: number | null;
  bracketPairs: StageBracketPair[];
  unknownEntry: string;
  unknownGroup: string;
  groupLabel: (name: string) => string;
  format: (key: string, options?: Record<string, unknown>) => string;
}): string[] {
  if (
    input.draw.kind === 'Group' &&
    (input.draw.groupPlacements?.length ?? 0) > 0
  ) {
    return groupsSchematicFromDrawDraft({
      placements: input.draw.groupPlacements ?? [],
      stageGroups: input.groups.map((group) => ({
        id: group.id,
        name: group.name,
      })),
      placesPerGroup: input.placesPerGroup,
      unknownEntry: input.unknownEntry,
      unknownGroup: input.unknownGroup,
      groupLabel: input.groupLabel,
    })
      .filter((group) => group.entries.length > 0)
      .map((group) =>
        input.format('historyGroupLine', {
          group: group.name,
          teams: group.entries.map((entry) => entry.displayName).join(', '),
        }),
      );
  }

  if (input.draw.kind !== 'Slot' || input.draw.slotPlacements.length === 0) {
    return [];
  }

  const result = projectSlotDrawResult(
    input.draw.slotPlacements,
    input.bracketPairs,
    input.unknownEntry,
  );
  return [
    ...result.confrontations.map((row) =>
      input.format('historyPairLine', {
        pair: row.key,
        slots: `${row.sideA.slotKey} vs ${row.sideB.slotKey}`,
        home: row.sideA.displayName,
        away: row.sideB.displayName,
      }),
    ),
    ...result.unpaired.map((side) =>
      input.format('historyPlaceLine', {
        slot: side.slotKey,
        team: side.displayName,
      }),
    ),
  ];
}

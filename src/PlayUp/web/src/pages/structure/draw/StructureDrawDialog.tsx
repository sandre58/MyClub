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
import { statusToneFromBadgeTone } from '../../../design-system/components/statusTone';
import { Dialog } from '../../../design-system/components/Dialog';
import { PanelHead } from '../../../design-system/components/PanelHead';
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
import { TeamCrest } from '../../../design-system/TeamCrest';
import { notify } from '../../../design-system/toastStore';
import { useClearMutationsOnOpen } from '../../../design-system/useClearMutationsOnOpen';
import { drawResolutionKindLabel } from '../../../i18n/enumLabels';
import { queryKeys } from '../../../queryKeys';
import type {
  CompetitionStatus,
  StageBracketPair,
  StageDraw,
  StageGroupOverview,
  StageSlot,
  StageStatus,
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
  pickReleasableCancelledDraw,
  projectSlotDrawResult,
  resolveDrawCreateBlockPresentation,
  resolveDrawDetailGuidance,
  resolveDrawDetailHeaderChips,
  resolveStageDrawCreateGate,
  sortDrawsNewestFirst,
  stageHasPublishedAppliedDraw,
  type DrawDetailHeaderChip,
} from './drawUi';
import { DrawSlotConfrontationGrid } from './DrawSlotConfrontationGrid';
import { resolveDrawParameterFacts } from './drawParameters';
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
  DrawResolutionBadge,
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
  const { t: tRegulation } = useTranslation('regulation');
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

  useOpenEdge(open, {
    onClose: () => {
      setConfirm(null);
      setConfirmDrawId(null);
      setCancelledOpen(false);
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
  const releasable = useMemo(
    () =>
      pickReleasableCancelledDraw({
        draws: drawList,
        slots,
        groups: overviewGroups,
        competitionStatus,
        stageStatus,
        stageHasPublishedAppliedDraw: stageHasAppliedDraw,
      }),
    [
      competitionStatus,
      drawList,
      overviewGroups,
      slots,
      stageHasAppliedDraw,
      stageStatus,
    ],
  );
  const forceCancelledOpen = activeDraw == null && cancelledDraws.length > 0;

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

  const parameterFacts = resolveDrawParameterFacts({
    formatKind: stage.formatKind,
    numberOfPots: stage.numberOfPots,
    groupCount: stage.groupCount,
    numberOfSeeds: stage.numberOfSeeds,
    constraints: stage.drawConstraints,
  });

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
        footer={
          <div className="button-row">
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
            <button
              type="button"
              className="ds-btn ds-btn--ghost"
              disabled={busy}
              onClick={onClose}
            >
              <CloseIcon size="sm" />
              {tCommon('close')}
            </button>
          </div>
        }
        footerStatus={
          overviewQuery.isError ||
          mutationError ||
          (createBlockedDetail != null &&
            createBlockPresentation?.kind === 'inline') ? (
            <>
              {overviewQuery.isError ? (
                <Alert tone="danger" role="alert">
                  {t('fiche.drawWorkflow.loadError')}
                </Alert>
              ) : null}
              {mutationError ? (
                <MutationError
                  error={mutationError}
                  structureStages={structureStages}
                  sotHref={persistentStructureMutationSotHref(
                    mutationError,
                    competitionId,
                  )}
                />
              ) : null}
              {!overviewQuery.isLoading &&
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
              ) : null}
            </>
          ) : null
        }
      >
        <div className="structure-draw-dialog">
          {!overviewQuery.isLoading ? (
            <div className="structure-draw-summary" aria-live="polite">
              <div className="structure-qualification__facts">
                <div className="structure-qualification__fact structure-qualification__fact--primary">
                  <span className="structure-qualification__fact-value">
                    {poolCount}
                  </span>
                  <span className="structure-qualification__fact-label">
                    {t('fiche.drawWorkflow.factPool', { count: poolCount })}
                  </span>
                </div>
              </div>
            </div>
          ) : null}

          {!overviewQuery.isLoading && parameterFacts.length > 0 ? (
            <DrawParameters
              facts={parameterFacts}
              labelFor={(fact) => drawParameterLabel(fact, tDraw, tRegulation)}
              notAppliedLabel={tDraw('parametersNotApplied')}
              title={tDraw('parametersTitle')}
            />
          ) : null}

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
            <DrawExecutionPanel
              draw={activeDraw}
              title={tDraw('title', {
                kind: drawResolutionKindLabel(activeDraw.kind),
              })}
              slots={slots}
              groups={overviewGroups}
              placesPerGroup={placesPerGroup}
              bracketPairs={bracketPairs}
              stageHasPublishedAppliedDraw={stageHasAppliedDraw}
              competitionStatus={competitionStatus}
              stageStatus={stageStatus}
              busy={busy}
              stale={activeDraw.isObsoleteForConsumption === true}
              redo={activeRedo}
              onValidate={() => {
                applyMutation.reset();
                publishAndApplyMutation.mutate(activeDraw);
              }}
              onRerun={() => rerunMutation.mutate(activeDraw.id)}
              onAbandon={() => openConfirm('abandon', activeDraw.id)}
              onApply={() => openConfirm('apply', activeDraw.id)}
              onRedo={() => openConfirm('redo', activeDraw.id)}
              onCancelApplied={() =>
                openConfirm('cancelApplied', activeDraw.id)
              }
              validatePending={publishAndApplyMutation.isPending}
              rerunPending={rerunMutation.isPending}
              abandonPending={cancelMutation.isPending}
              applyPending={applyMutation.isPending}
              redoPending={redoMutation.isPending}
              cancelAppliedPending={
                cancelMutation.isPending && confirm === 'cancelApplied'
              }
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
                {cancelledDraws.map((draw) => {
                  const alignedCount = countAlignedDrawPlacements(
                    draw,
                    slots,
                    overviewGroups,
                  );
                  const canRelease =
                    releasable?.draw.id === draw.id &&
                    canReleaseDrawAlignedPlacements({
                      competitionStatus,
                      stageStatus,
                      drawStatus: draw.status,
                      drawKind: draw.kind,
                      stageHasPublishedAppliedDraw: stageHasAppliedDraw,
                      alignedPlacementCount: alignedCount,
                    });
                  return (
                    <DrawExecutionPanel
                      key={draw.id}
                      draw={draw}
                      title={t('fiche.drawWorkflow.execution', {
                        n: drawExecutionNumber(drawList, draw.id),
                      })}
                      slots={slots}
                      groups={overviewGroups}
                      placesPerGroup={placesPerGroup}
                      bracketPairs={bracketPairs}
                      stageHasPublishedAppliedDraw={stageHasAppliedDraw}
                      competitionStatus={competitionStatus}
                      stageStatus={stageStatus}
                      busy={busy}
                      stale={draw.isObsoleteForConsumption === true}
                      redo={{ kind: 'hidden' }}
                      cancelledRecovery={canRelease}
                      onRelease={
                        canRelease
                          ? () => openConfirm('release', draw.id)
                          : undefined
                      }
                      releasePending={
                        releaseMutation.isPending && confirmDrawId === draw.id
                      }
                      alignedCount={alignedCount}
                    />
                  );
                })}
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

function drawParameterLabel(
  fact: ReturnType<typeof resolveDrawParameterFacts>[number],
  tDraw: (key: string, options?: Record<string, unknown>) => string,
  tRegulation: (key: string, options?: Record<string, unknown>) => string,
): string {
  if (fact.kind === 'applied') {
    return tDraw('parametersPotsGroups', {
      pots: fact.pots,
      groups: fact.groups,
    });
  }
  if ('seeds' in fact) {
    return tRegulation('tokens.drawSeedsTip', { count: fact.seeds });
  }
  const name = tRegulation(`tokens.drawConstraint.${fact.constraintType}`);
  if (fact.maxPerGroup != null && fact.maxPerGroup > 0) {
    return `${name} (${tRegulation('tokens.drawConstraintMax', {
      count: fact.maxPerGroup,
    })})`;
  }
  return name;
}

function DrawParameters({
  facts,
  labelFor,
  notAppliedLabel,
  title,
}: {
  facts: ReturnType<typeof resolveDrawParameterFacts>;
  labelFor: (
    fact: ReturnType<typeof resolveDrawParameterFacts>[number],
  ) => string;
  notAppliedLabel: string;
  title: string;
}): ReactElement {
  return (
    <section
      className="ds-panel structure-draw-parameters"
      aria-labelledby="draw-parameters-title"
    >
      <PanelHead id="draw-parameters-title" title={title} />
      <ul className="structure-draw-parameters__list">
        {facts.map((fact) => (
          <li key={fact.id} className="structure-draw-parameters__fact">
            <span>{labelFor(fact)}</span>
            {fact.kind === 'notApplied' ? (
              <span className="structure-draw-parameters__mark">
                {' — '}
                {notAppliedLabel}
              </span>
            ) : null}
          </li>
        ))}
      </ul>
    </section>
  );
}

function DrawDetailHeaderChipBadge({
  chip,
  tooltip,
}: {
  chip: DrawDetailHeaderChip;
  tooltip?: string;
}) {
  const { t } = useTranslation('draw');
  let badge: ReactElement;
  if (chip.kind === 'lifecycle') {
    badge = <DrawStatusBadge status={chip.status} density="compact" />;
  } else if (chip.kind === 'resolution') {
    badge = <DrawResolutionBadge state={chip.state} density="compact" />;
  } else {
    badge = (
      <Status density="compact" tone={statusToneFromBadgeTone('ok')}>
        {t('applied')}
      </Status>
    );
  }
  if (!tooltip) {
    return badge;
  }
  return <Tooltip content={tooltip}>{badge}</Tooltip>;
}

function DrawExecutionPanel({
  draw,
  title,
  slots,
  groups,
  placesPerGroup,
  bracketPairs,
  stageHasPublishedAppliedDraw: publishedApplied,
  competitionStatus,
  stageStatus,
  busy,
  stale,
  redo,
  cancelledRecovery = false,
  onValidate,
  onRerun,
  onAbandon,
  onApply,
  onRedo,
  onCancelApplied,
  onRelease,
  validatePending = false,
  rerunPending = false,
  abandonPending = false,
  applyPending = false,
  redoPending = false,
  cancelAppliedPending = false,
  releasePending = false,
  alignedCount: alignedCountProp,
}: {
  draw: StageDraw;
  title: string;
  slots: StageSlot[];
  groups: StageGroupOverview[];
  placesPerGroup: number | null;
  bracketPairs: StageBracketPair[];
  stageHasPublishedAppliedDraw: boolean;
  competitionStatus: CompetitionStatus;
  stageStatus: StageStatus;
  busy: boolean;
  stale: boolean;
  redo: ReturnType<typeof resolveAppliedDrawRedo>;
  cancelledRecovery?: boolean;
  onValidate?: () => void;
  onRerun?: () => void;
  onAbandon?: () => void;
  onApply?: () => void;
  onRedo?: () => void;
  onCancelApplied?: () => void;
  onRelease?: () => void;
  validatePending?: boolean;
  rerunPending?: boolean;
  abandonPending?: boolean;
  applyPending?: boolean;
  redoPending?: boolean;
  cancelAppliedPending?: boolean;
  releasePending?: boolean;
  alignedCount?: number;
}): ReactElement {
  const { t } = useTranslation('draw');
  const { t: tCommon } = useTranslation('common');
  const { t: tStructure } = useTranslation('structure');
  const ui = getDrawUiProjection(draw, slots);
  const alignedCount =
    alignedCountProp ?? countAlignedDrawPlacements(draw, slots, groups);
  const canPublishAndApply =
    onValidate != null &&
    draw.status === 'Draft' &&
    draw.resolutionState === 'Resolved' &&
    (draw.kind === 'Slot' || draw.kind === 'Group');
  const canApply =
    onApply != null &&
    draw.status === 'Published' &&
    draw.resolutionState === 'Resolved' &&
    !ui.isApplied &&
    (draw.kind === 'Slot' || draw.kind === 'Group');
  const canAbandon =
    onAbandon != null &&
    !ui.isApplied &&
    canCancelDrawExecution({
      competitionStatus,
      stageStatus,
      drawStatus: draw.status,
    });
  const canRerun =
    onRerun != null &&
    draw.status === 'Draft' &&
    (draw.kind === 'Slot' || draw.kind === 'Group');
  const canRedo = redo.kind === 'available' && onRedo != null;
  const canCancelApplied =
    onCancelApplied != null &&
    redo.kind === 'blocked' &&
    ui.isApplied &&
    canCancelDrawExecution({
      competitionStatus,
      stageStatus,
      drawStatus: draw.status,
    });
  const canRelease =
    onRelease != null &&
    canReleaseDrawAlignedPlacements({
      competitionStatus,
      stageStatus,
      drawStatus: draw.status,
      drawKind: draw.kind,
      stageHasPublishedAppliedDraw: publishedApplied,
      alignedPlacementCount: alignedCount,
    });

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

  const hasSlotConfrontations =
    ui.showResults &&
    draw.kind === 'Slot' &&
    slotResult.confrontations.length > 0;
  const hasSlotUnpaired =
    ui.showResults && draw.kind === 'Slot' && slotResult.unpaired.length > 0;
  const hasGroups = ui.showResults && groupModel.length > 0;
  const showResults = hasSlotConfrontations || hasSlotUnpaired || hasGroups;
  const guidance = resolveDrawDetailGuidance(ui);
  const showEmptyResult = !showResults && guidance?.kind !== 'alert';
  const headerChips = resolveDrawDetailHeaderChips(draw, ui.isApplied);
  const titleId = `draw-detail-${draw.id}-title`;

  const actions =
    canPublishAndApply ||
    canApply ||
    canRerun ||
    canAbandon ||
    canRedo ||
    canCancelApplied ||
    canRelease ? (
      <div className="button-row" aria-busy={busy}>
        {canPublishAndApply ? (
          <button
            type="button"
            className="ds-btn ds-btn--primary"
            disabled={busy}
            onClick={onValidate}
          >
            {validatePending ? (
              <PendingLabel>{t('validating')}</PendingLabel>
            ) : (
              <>
                <CheckIcon size="sm" />
                {t('validate')}
              </>
            )}
          </button>
        ) : null}
        {canApply ? (
          <button
            type="button"
            className="ds-btn ds-btn--primary"
            disabled={busy}
            onClick={onApply}
          >
            {applyPending ? (
              <PendingLabel>{t('applying')}</PendingLabel>
            ) : (
              <>
                <CheckIcon size="sm" />
                {t('apply')}
              </>
            )}
          </button>
        ) : null}
        {canRedo ? (
          <button
            type="button"
            className="ds-btn ds-btn--secondary"
            disabled={busy}
            onClick={onRedo}
          >
            {redoPending ? (
              <PendingLabel>{t('redoing')}</PendingLabel>
            ) : (
              <>
                <UndoIcon size="sm" />
                {t('redo')}
              </>
            )}
          </button>
        ) : null}
        {canCancelApplied ? (
          <button
            type="button"
            className="ds-btn ds-btn--secondary"
            disabled={busy}
            onClick={onCancelApplied}
          >
            {cancelAppliedPending ? (
              <PendingLabel>{t('cancelling')}</PendingLabel>
            ) : (
              <>
                <CloseIcon size="sm" />
                {t('cancelAppliedDraw')}
              </>
            )}
          </button>
        ) : null}
        {canRerun ? (
          <button
            type="button"
            className="ds-btn ds-btn--secondary"
            disabled={busy}
            onClick={onRerun}
          >
            {rerunPending ? (
              <PendingLabel>{t('rerunning')}</PendingLabel>
            ) : (
              <>
                <RandomIcon size="sm" />
                {t('rerun')}
              </>
            )}
          </button>
        ) : null}
        {canRelease ? (
          <Tooltip content={t('releasePlacementsHint')}>
            <button
              type="button"
              className="ds-btn ds-btn--ghost"
              disabled={busy}
              onClick={onRelease}
            >
              {releasePending ? (
                <PendingLabel>{t('releasingPlacements')}</PendingLabel>
              ) : (
                <>
                  <UnlockIcon size="sm" />
                  {t('releasePlacementsCount', {
                    aligned: alignedCount,
                    total: drawResolutionPlacementCount(draw),
                  })}
                </>
              )}
            </button>
          </Tooltip>
        ) : null}
        {canAbandon ? (
          <button
            type="button"
            className="ds-btn ds-btn--ghost ds-btn--destructive"
            disabled={busy}
            onClick={onAbandon}
          >
            {abandonPending ? (
              <PendingLabel>{t('abandoning')}</PendingLabel>
            ) : (
              <>
                <CloseIcon size="sm" />
                {t('abandon')}
              </>
            )}
          </button>
        ) : null}
      </div>
    ) : null;

  return (
    <section
      className="ds-panel structure-draw-panel"
      aria-labelledby={titleId}
    >
      <PanelHead
        id={titleId}
        title={title}
        icon={<RandomIcon size="sm" />}
        actions={
          headerChips.length > 0 ? (
            <div className="structure-draw-detail__chips">
              {headerChips.map((chip, index) => (
                <DrawDetailHeaderChipBadge
                  key={`${chip.kind}-${index}`}
                  chip={chip}
                  tooltip={
                    chip.kind === 'applied' ? t(ui.messageKey) : undefined
                  }
                />
              ))}
            </div>
          ) : undefined
        }
      />
      <div className="structure-draw-panel__body">
        {stale ? (
          <Alert tone="warning" role="status">
            {t('staleResult')}
          </Alert>
        ) : null}
        {cancelledRecovery ? (
          <Alert tone="warning" role="status">
            {t('cancelledUnreleased')}
          </Alert>
        ) : null}
        {redo.kind === 'blocked' ? (
          <Alert tone="info" role="status">
            {t(`redoBlocked.${redo.reason}`)}
          </Alert>
        ) : null}
        {guidance?.kind === 'phrase' ? (
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
        {showResults ? (
          <div className="structure-draw-result">
            {hasGroups ? (
              <GroupsSchematicView
                groups={groupModel}
                ariaLabel={t('result')}
                ghostLabel={t('ghostPlace')}
              />
            ) : null}
            {hasSlotConfrontations ? (
              <DrawSlotConfrontationGrid
                rows={slotResult.confrontations}
                vsLabel={t('vs')}
                ariaLabel={t('result')}
              />
            ) : null}
            {hasSlotUnpaired ? (
              <ul
                className="draw-placement-list"
                aria-label={
                  hasSlotConfrontations ? t('placements') : t('result')
                }
              >
                {slotResult.unpaired.map((side) => (
                  <li key={side.slotKey} className="draw-placement">
                    <span className="draw-placement__key">{side.slotKey}</span>
                    <span className="draw-placement__arrow" aria-hidden="true">
                      →
                    </span>
                    <span className="draw-placement__entry">
                      <TeamCrest
                        name={side.displayName}
                        logoMediaId={side.logoMediaId}
                        primaryColor={side.primaryColor}
                        size="sm"
                      />
                      {side.displayName}
                    </span>
                  </li>
                ))}
              </ul>
            ) : null}
          </div>
        ) : null}
      </div>
      {actions ? (
        <div className="structure-draw-panel__footer">{actions}</div>
      ) : null}
    </section>
  );
}

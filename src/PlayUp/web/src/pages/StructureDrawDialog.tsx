import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useMemo, useRef, useState, type ReactElement, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import {
  applyDraw,
  cancelDraw,
  createAndGenerateDraw,
  fetchStageOverview,
  isDrawGenerateFailedError,
  publishAndApplyDraw,
  releaseDrawAlignedPlacements,
} from '../api';
import { Alert } from '../design-system/components/Alert';
import { ConfirmDialog } from '../design-system/components/ConfirmDialog';
import { Dialog } from '../design-system/components/Dialog';
import { Tooltip } from '../design-system/components/Tooltip';
import {
  CheckIcon,
  PlusIcon,
  RandomIcon,
  UnlockIcon,
} from '../design-system/icons/contentIcons';
import { CloseIcon } from '../design-system/icons/shellIcons';
import { TeamCrest } from '../design-system/TeamCrest';
import { notify } from '../design-system/toastStore';
import { drawResolutionKindLabel } from '../i18n/enumLabels';
import { queryKeys } from '../queryKeys';
import type {
  CompetitionStatus,
  StageBracketPair,
  StageDraw,
  StageRound,
  StageSlot,
  StageStatus,
  StructureFormatKind,
  StructureStageHubSummary,
} from '../types';
import { deriveShortName } from './deriveShortName';
import {
  drawExecutionNumber,
  getDrawUiProjection,
  groupPlacementRows,
  pickDefaultDrawId,
  resolveDrawCreateBlockPresentation,
  resolveStageDrawCreateGate,
  resolveDrawDetailGuidance,
  resolveDrawDetailHeaderChips,
  countAlignedSlotPlacements,
  projectSlotDrawResult,
  sortDrawsNewestFirst,
  type DrawDetailHeaderChip,
  type DrawMasterChip,
  type SlotConfrontationSide,
} from './drawUi';
import {
  canCancelDrawExecution,
  canReleaseDrawAlignedPlacements,
} from './lifecycleGates';
import { invalidateAfterStructureMutation } from './structureInvalidation';
import { resolvePlacesN } from './structurePlaces';
import {
  DrawResolutionBadge,
  DrawStatusBadge,
  EmptyState,
  LoadingState,
  MutationError,
  PendingLabel,
  StatusBadge,
} from '../ui';
import './phase-schematic.css';

function pairingSideLabels(
  displayName: string | null | undefined,
  shortName: string | null | undefined,
  fallback: string,
): { full: string; short: string } {
  const full = displayName?.trim() || fallback;
  const short = shortName?.trim() || deriveShortName(full) || full;
  return { full, short };
}

function DrawConfrontationSide({
  side,
  away = false,
}: {
  side: {
    full: string;
    short: string;
    logoMediaId?: string | null;
    primaryColor?: string | null;
    /** Place address (Slot) — secondary chrome; never Match #. */
    address?: string | null;
  };
  away?: boolean;
}): ReactElement {
  const address = side.address?.trim() || null;
  const identity = (
    <span className="draw-pairing__identity">
      <TeamCrest
        name={side.full}
        logoMediaId={side.logoMediaId}
        primaryColor={side.primaryColor}
        size="sm"
        className="draw-pairing__crest"
      />
      <span className="draw-pairing__name">
        <span className="draw-pairing__name-full">{side.full}</span>
        <span className="draw-pairing__name-short" aria-hidden="true">
          {side.short}
        </span>
      </span>
    </span>
  );
  const ariaLabel = address ? `${side.full} · ${address}` : side.full;
  return (
    <span
      className={
        away
          ? 'draw-pairing__side draw-pairing__side--away'
          : 'draw-pairing__side'
      }
      aria-label={ariaLabel}
    >
      {address ? (
        <span className="draw-pairing__address">{address}</span>
      ) : null}
      {identity}
    </span>
  );
}

function confrontationSideFromSlot(
  side: SlotConfrontationSide,
): {
  full: string;
  short: string;
  logoMediaId?: string | null;
  primaryColor?: string | null;
  address: string;
} {
  const labels = pairingSideLabels(
    side.displayName,
    side.shortName,
    side.displayName,
  );
  return {
    ...labels,
    logoMediaId: side.logoMediaId,
    primaryColor: side.primaryColor,
    address: side.slotKey,
  };
}
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

/** One non-Cancelled Draw may own the active execution; rerun = Cancel → Nouveau. */
type StructureDrawDialogProps = {
  open: boolean;
  onClose: () => void;
  competitionId: string;
  /** Host lifecycle gate — Completed|Archived → Cancel hidden. */
  competitionStatus: CompetitionStatus;
  /** Competition EntryRules.MinimumTeams — gate belowMinimumTeams. */
  minimumTeams?: number | null;
  stage: StructureStageHubSummary;
};

/**
 * Work dialog — exécutions de tirage de la phase (H1).
 * Stats pool (comme Qual/Prog) · tuile Historique dès 1 Draw ·
 * une tuile détail (état + résultat) · Nouveau = seul primary.
 */
export function StructureDrawDialog({
  open,
  onClose,
  competitionId,
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
  const slots = overviewQuery.data?.slots ?? [];
  const rounds = overviewQuery.data?.rounds ?? [];
  const drawList = draws ?? [];
  const showHistory = drawList.length >= 1;
  const poolCount = stage.compositionEntryCount ?? 0;
  const placesN = stage.compositionCapacity ?? resolvePlacesN(stage);

  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [applyConfirmOpen, setApplyConfirmOpen] = useState(false);
  const [cancelConfirmOpen, setCancelConfirmOpen] = useState(false);
  const [releaseConfirmOpen, setReleaseConfirmOpen] = useState(false);
  /** After « Nouveau tirage », prefer this id once it appears in overview. */
  const pendingSelectIdRef = useRef<string | null>(null);

  useEffect(() => {
    if (!open) {
      pendingSelectIdRef.current = null;
      setSelectedId(null);
      setApplyConfirmOpen(false);
      setCancelConfirmOpen(false);
      setReleaseConfirmOpen(false);
      return;
    }
    if (!draws || draws.length === 0) {
      setSelectedId(null);
      return;
    }
    const pending = pendingSelectIdRef.current;
    if (pending) {
      if (draws.some((d) => d.id === pending)) {
        pendingSelectIdRef.current = null;
      }
      setSelectedId(pending);
      return;
    }
    setSelectedId((current) => {
      if (current && draws.some((d) => d.id === current)) {
        return current;
      }
      return pickDefaultDrawId(draws);
    });
  }, [open, draws]);

  const selected = useMemo(
    () => drawList.find((d) => d.id === selectedId) ?? null,
    [drawList, selectedId],
  );

  const createGateInput = {
    formatKind: stage.formatKind,
    draws: drawList,
    slots,
    compositionEntryCount: poolCount,
    isRootComposition: stage.isRootComposition,
    numberOfPots: stage.numberOfPots,
    groupCount: stage.groupCount,
    placesN,
    minimumTeams,
    directAssignmentCount: stage.directAssignmentCount,
  };
  const createGate = resolveStageDrawCreateGate(createGateInput);
  const showCreate = drawKind != null && !overviewQuery.isLoading;
  const canCreate = showCreate && createGate.ok;
  const createBlockedReason =
    showCreate && !createGate.ok ? createGate.reason : null;
  const createBlockPresentation =
    createBlockedReason != null
      ? resolveDrawCreateBlockPresentation(createBlockedReason)
      : null;
  const createBlockedShort =
    createBlockedReason != null
      ? t(`fiche.drawWorkflow.createBlocked.short.${createBlockedReason}`)
      : null;
  const createBlockedDetail =
    createBlockedReason != null &&
    createBlockPresentation?.kind === 'inline'
      ? createBlockedReason === 'countMismatch'
        ? t('fiche.drawWorkflow.createBlocked.detail.countMismatch', {
            count: poolCount,
            places: placesN ?? 0,
          })
        : t(`fiche.drawWorkflow.createBlocked.detail.${createBlockedReason}`)
      : null;

  async function invalidateDrawQueries() {
    await invalidateAfterStructureMutation(queryClient, competitionId, {
      stageId,
    });
  }

  const createMutation = useMutation({
    mutationFn: () => {
      if (!drawKind) {
        throw new Error('Draw kind unavailable for this format.');
      }
      // Revalidate before Create+Generate — same gate as CTA (stale overview / race).
      const gate = resolveStageDrawCreateGate(createGateInput);
      if (!gate.ok) {
        throw new Error(`Draw create blocked: ${gate.reason}`);
      }
      return createAndGenerateDraw(stageId, drawKind);
    },
    onSuccess: async (result) => {
      pendingSelectIdRef.current = result.drawId;
      await invalidateDrawQueries();
      setSelectedId(result.drawId);
      if (result.isNoSolution) {
        notify.attention(tDraw('toastCreatedNoSolution'));
      } else {
        notify.success(tDraw('toastCreated'));
      }
    },
    onError: async (error) => {
      if (isDrawGenerateFailedError(error)) {
        // Create succeeded — keep Draft selected; do not sticky-footer the error.
        pendingSelectIdRef.current = error.drawId;
        await invalidateDrawQueries();
        setSelectedId(error.drawId);
        notify.error(tDraw('toastGenerationFailed'));
        return;
      }
      pendingSelectIdRef.current = null;
      notify.error(tDraw('toastActionFailed'));
    },
  });

  const publishAndApplyMutation = useMutation({
    mutationFn: async (draw: StageDraw) => {
      return publishAndApplyDraw(stageId, draw.id, { fixtureIds: [] });
    },
    onSuccess: () => {
      notify.success(tDraw('toastPublishedAndApplied'));
    },
    // Always refresh: Apply may fail after a durable Publish (recovery state).
    onSettled: async () => {
      await invalidateDrawQueries();
    },
  });

  const cancelMutation = useMutation({
    mutationFn: (drawId: string) => cancelDraw(stageId, drawId),
    onSuccess: async () => {
      setCancelConfirmOpen(false);
      await invalidateDrawQueries();
      notify.success(tDraw('toastCancelled'));
    },
  });

  const releaseMutation = useMutation({
    mutationFn: (drawId: string) =>
      releaseDrawAlignedPlacements(stageId, drawId),
    onSuccess: async (result) => {
      setReleaseConfirmOpen(false);
      await invalidateDrawQueries();
      notify.success(
        tDraw('toastReleased', { count: result.releasedCount }),
      );
    },
  });

  const applyMutation = useMutation({
    mutationFn: async (draw: StageDraw) => {
      return applyDraw(stageId, draw.id, { fixtureIds: [] });
    },
    onSuccess: async () => {
      setApplyConfirmOpen(false);
      await invalidateDrawQueries();
      notify.success(tDraw('toastApplied'));
    },
  });

  const busy =
    createMutation.isPending ||
    publishAndApplyMutation.isPending ||
    cancelMutation.isPending ||
    releaseMutation.isPending ||
    applyMutation.isPending;

  // Create path uses toasts only (NoSolution / generation interrupted / hard fail).
  // Do not sticky MutationError in the dialog footer after Create+Generate.
  const mutationError =
    publishAndApplyMutation.error ??
    cancelMutation.error ??
    releaseMutation.error ??
    applyMutation.error;

  const newestFirst = useMemo(
    () => sortDrawsNewestFirst(drawList),
    [drawList],
  );

  const selectedApplied =
    selected != null &&
    getDrawUiProjection(selected, slots, rounds).isApplied;

  return (
    <>
      <Dialog
        open={open}
        onClose={onClose}
        title={t('fiche.drawWorkflow.title')}
        description={stage.name}
        size="lg"
        closeLabel={tCommon('close')}
        closeDisabled={busy}
        trapFocus={
          !applyConfirmOpen && !cancelConfirmOpen && !releaseConfirmOpen
        }
        footer={
          <div className="button-row">
            {showCreate ? (
              createBlockedShort != null ? (
                <Tooltip content={createBlockedShort}>
                  <button
                    type="button"
                    className="ds-btn ds-btn--primary"
                    disabled
                    aria-label={t('fiche.drawWorkflow.create')}
                  >
                    <PlusIcon size="sm" />
                    {t('fiche.drawWorkflow.create')}
                  </button>
                </Tooltip>
              ) : (
                <button
                  type="button"
                  className="ds-btn ds-btn--primary"
                  disabled={busy || !canCreate}
                  onClick={() => {
                    if (!canCreate) return;
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
                      <PlusIcon size="sm" />
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
              {mutationError ? <MutationError error={mutationError} /> : null}
              {!overviewQuery.isLoading &&
              createBlockedDetail != null &&
              createBlockPresentation?.kind === 'inline' ? (
                <Alert tone={createBlockPresentation.tone} role="status">
                  <p className="ds-body">{createBlockedDetail}</p>
                </Alert>
              ) : null}
            </>
          ) : null
        }
      >
        <div className="structure-draw-dialog">
          {!overviewQuery.isLoading ? (
            <div
              className="structure-draw-summary"
              aria-live="polite"
            >
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

          {overviewQuery.isLoading ? (
            <LoadingState
              size="region"
              label={t('fiche.drawWorkflow.loading')}
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

          {showHistory ? (
            <div className="structure-draw-layout structure-draw-layout--split">
              <nav
                className="structure-draw-master"
                aria-label={t('fiche.drawWorkflow.historyAria')}
              >
                <ul className="structure-draw-master__list">
                  {newestFirst.map((draw) => {
                    const execLabel = t('fiche.drawWorkflow.execution', {
                      n: drawExecutionNumber(drawList, draw.id),
                    });
                    const isCurrent = draw.id === selected?.id;
                    const { masterChip } = getDrawUiProjection(
                      draw,
                      slots,
                      rounds,
                    );
                    return (
                      <li key={draw.id}>
                        <button
                          type="button"
                          className="structure-draw-master__card ds-selectable-tile"
                          data-selected={isCurrent ? 'true' : 'false'}
                          aria-current={isCurrent ? 'true' : undefined}
                          onClick={() => setSelectedId(draw.id)}
                        >
                          <span className="structure-draw-master__card-head">
                            <span className="structure-draw-master__card-title">
                              <span className="structure-draw-master__card-icon">
                                <RandomIcon size="sm" aria-hidden="true" />
                              </span>
                              <span className="structure-draw-master__card-name">
                                {execLabel}
                              </span>
                            </span>
                            <span className="structure-draw-master__card-status">
                              <DrawMasterChipBadge chip={masterChip} />
                            </span>
                          </span>
                        </button>
                      </li>
                    );
                  })}
                </ul>
              </nav>

              {selected ? (
                <DrawExecutionDetail
                  draw={selected}
                  slots={slots}
                  rounds={rounds}
                  bracketPairs={overviewQuery.data?.bracketPairs ?? []}
                  competitionStatus={competitionStatus}
                  stageStatus={overviewQuery.data?.status ?? stage.status}
                  busy={busy}
                  onPublishAndApply={() =>
                    publishAndApplyMutation.mutate(selected)
                  }
                  onApply={() => setApplyConfirmOpen(true)}
                  onCancel={() => setCancelConfirmOpen(true)}
                  onRelease={() => setReleaseConfirmOpen(true)}
                  publishAndApplyPending={publishAndApplyMutation.isPending}
                  applyPending={applyMutation.isPending}
                  cancelPending={cancelMutation.isPending}
                  releasePending={releaseMutation.isPending}
                />
              ) : null}
            </div>
          ) : null}
        </div>
      </Dialog>

      <ConfirmDialog
        open={applyConfirmOpen}
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
            <MutationError error={applyMutation.error} />
          ) : null
        }
        onCancel={() => {
          if (applyMutation.isPending) {
            return;
          }
          setApplyConfirmOpen(false);
        }}
        onConfirm={() => {
          if (!selected || applyMutation.isPending) {
            return;
          }
          applyMutation.mutate(selected);
        }}
      />

      <ConfirmDialog
        open={cancelConfirmOpen}
        title={tDraw('confirmCancelTitle')}
        message={
          selectedApplied
            ? tDraw('confirmCancelApplied')
            : tDraw('confirmCancel')
        }
        confirmLabel={tDraw('cancel')}
        confirmIcon={<CloseIcon size="sm" />}
        cancelLabel={tCommon('close')}
        closeLabel={tCommon('close')}
        danger
        confirmDisabled={cancelMutation.isPending}
        confirmPending={cancelMutation.isPending}
        confirmPendingLabel={tDraw('cancelling')}
        footerStatus={
          cancelMutation.isError ? (
            <MutationError error={cancelMutation.error} />
          ) : null
        }
        onCancel={() => {
          if (cancelMutation.isPending) {
            return;
          }
          setCancelConfirmOpen(false);
        }}
        onConfirm={() => {
          if (!selected || cancelMutation.isPending) {
            return;
          }
          cancelMutation.mutate(selected.id);
        }}
      />

      <ConfirmDialog
        open={releaseConfirmOpen}
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
            <MutationError error={releaseMutation.error} />
          ) : null
        }
        onCancel={() => {
          if (releaseMutation.isPending) {
            return;
          }
          setReleaseConfirmOpen(false);
        }}
        onConfirm={() => {
          if (!selected || releaseMutation.isPending) {
            return;
          }
          releaseMutation.mutate(selected.id);
        }}
      />
    </>
  );
}

function DrawMasterChipBadge({ chip }: { chip: DrawMasterChip }) {
  const { t } = useTranslation('draw');
  if (chip == null) {
    return null;
  }
  if (chip.kind === 'lifecycle') {
    return <DrawStatusBadge status={chip.status} density="compact" />;
  }
  if (chip.kind === 'resolution') {
    return <DrawResolutionBadge state={chip.state} density="compact" />;
  }
  return (
    <StatusBadge tone="ok" density="compact">
      {t('applied')}
    </StatusBadge>
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
      <StatusBadge tone="ok" density="compact">
        {t('applied')}
      </StatusBadge>
    );
  }
  if (!tooltip) {
    return badge;
  }
  return <Tooltip content={tooltip}>{badge}</Tooltip>;
}

function DrawSectionTile({
  id,
  title,
  description,
  icon,
  statusChip,
  footer,
  muted = false,
  children,
}: {
  id: string;
  title: string;
  description?: string;
  icon: ReactNode;
  statusChip?: ReactNode;
  footer?: ReactNode;
  /** Light attenuation for Cancelled history — not an error treatment. */
  muted?: boolean;
  children: ReactNode;
}) {
  const titleId = `${id}-title`;
  return (
    <section
      id={id}
      className={`ds-form-section structure-domain-tile structure-draw-tile${
        muted ? ' structure-draw-tile--muted' : ''
      }`}
      aria-labelledby={titleId}
    >
      <header className="ds-form-section__head structure-domain-tile__head">
        <span className="ds-form-section__icon" aria-hidden="true">
          {icon}
        </span>
        <div className="ds-form-section__copy">
          <h3 id={titleId} className="ds-form-section__title">
            {title}
          </h3>
          {description ? (
            <p className="ds-form-section__description">{description}</p>
          ) : null}
        </div>
        {statusChip ? (
          <div className="structure-domain-tile__toolbar">{statusChip}</div>
        ) : null}
      </header>
      <div className="ds-form-section__body structure-domain-tile__content">
        {children}
      </div>
      {footer ? (
        <div className="structure-domain-tile__footer">{footer}</div>
      ) : null}
    </section>
  );
}

function DrawExecutionDetail({
  draw,
  slots,
  rounds,
  bracketPairs,
  competitionStatus,
  stageStatus,
  busy,
  onPublishAndApply,
  onApply,
  onCancel,
  onRelease,
  publishAndApplyPending,
  applyPending,
  cancelPending,
  releasePending,
}: {
  draw: StageDraw;
  slots: StageSlot[];
  rounds: StageRound[];
  bracketPairs: StageBracketPair[];
  competitionStatus: CompetitionStatus;
  stageStatus: StageStatus;
  busy: boolean;
  onPublishAndApply: () => void;
  onApply: () => void;
  onCancel: () => void;
  onRelease: () => void;
  publishAndApplyPending: boolean;
  applyPending: boolean;
  cancelPending: boolean;
  releasePending: boolean;
}) {
  const { t } = useTranslation('draw');
  const { t: tCommon } = useTranslation('common');
  const { t: tStructure } = useTranslation('structure');
  const ui = getDrawUiProjection(draw, slots, rounds);

  const alignedCount = countAlignedSlotPlacements(draw, slots);
  const canPublishAndApply =
    draw.status === 'Draft' &&
    draw.resolutionState === 'Resolved' &&
    (draw.kind === 'Slot' || draw.kind === 'Group');
  const canApply =
    draw.status === 'Published' &&
    draw.resolutionState === 'Resolved' &&
    !ui.isApplied &&
    (draw.kind === 'Slot' || draw.kind === 'Group');
  const canCancel = canCancelDrawExecution({
    competitionStatus,
    stageStatus,
    drawStatus: draw.status,
  });
  const canRelease = canReleaseDrawAlignedPlacements({
    competitionStatus,
    stageStatus,
    drawStatus: draw.status,
    drawKind: draw.kind,
    alignedPlacementCount: alignedCount,
  });

  const groupRows =
    draw.kind === 'Group' && (draw.groupPlacements?.length ?? 0) > 0
      ? groupPlacementRows(
          draw.groupPlacements!,
          tCommon('unknownEntry'),
          t('unknownGroup'),
        )
      : [];

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
  const hasSlots = hasSlotConfrontations || hasSlotUnpaired;
  const hasGroups = ui.showResults && draw.kind === 'Group' && groupRows.length > 0;
  const showResults = hasSlots || hasGroups;

  const stateActions =
    canPublishAndApply || canApply || canCancel || canRelease ? (
      <div className="button-row" aria-busy={busy}>
        {canPublishAndApply ? (
          <button
            type="button"
            className="ds-btn ds-btn--primary"
            disabled={busy}
            onClick={onPublishAndApply}
          >
            {publishAndApplyPending ? (
              <PendingLabel>{t('publishingAndApplying')}</PendingLabel>
            ) : (
              <>
                <CheckIcon size="sm" />
                {t('publishAndApply')}
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
                    total: draw.slotPlacements.length,
                  })}
                </>
              )}
            </button>
          </Tooltip>
        ) : null}
        {canCancel ? (
          <button
            type="button"
            className="ds-btn ds-btn--ghost ds-btn--destructive"
            disabled={busy}
            onClick={onCancel}
          >
            {cancelPending ? (
              <PendingLabel>{t('cancelling')}</PendingLabel>
            ) : (
              <>
                <CloseIcon size="sm" />
                {t('cancel')}
              </>
            )}
          </button>
        ) : null}
      </div>
    ) : null;

  const headerChips = resolveDrawDetailHeaderChips(draw, ui.isApplied);
  const guidance = resolveDrawDetailGuidance(ui);
  const showEmptyResult = !showResults && guidance?.kind !== 'alert';

  return (
    <DrawSectionTile
      id={`draw-detail-${draw.id}`}
      title={t('title', { kind: drawResolutionKindLabel(draw.kind) })}
      icon={<RandomIcon size="sm" />}
      muted={draw.status === 'Cancelled'}
      statusChip={
        headerChips.length > 0 ? (
          <div className="structure-draw-detail__chips">
            {headerChips.map((chip, index) => (
              <DrawDetailHeaderChipBadge
                key={`${chip.kind}-${index}`}
                chip={chip}
                tooltip={
                  chip.kind === 'applied' ||
                  (chip.kind === 'resolution' &&
                    chip.state === 'Resolved' &&
                    ui.messageKey === 'draftResolved')
                    ? t(ui.messageKey)
                    : undefined
                }
              />
            ))}
          </div>
        ) : undefined
      }
      footer={stateActions}
    >
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
          {hasSlotConfrontations ? (
            <ul className="draw-pairing-list" aria-label={t('result')}>
              {slotResult.confrontations.map((row) => (
                <li key={row.key} className="draw-pairing">
                  <DrawConfrontationSide
                    side={confrontationSideFromSlot(row.sideA)}
                  />
                  <span className="draw-pairing__vs">{t('vs')}</span>
                  <DrawConfrontationSide
                    side={confrontationSideFromSlot(row.sideB)}
                    away
                  />
                </li>
              ))}
            </ul>
          ) : null}
          {hasSlotUnpaired ? (
            <ul
              className="draw-placement-list"
              aria-label={hasSlotConfrontations ? t('placements') : t('result')}
            >
              {slotResult.unpaired.map((side) => (
                <li key={side.slotKey} className="draw-placement">
                  <code className="draw-placement__key">{side.slotKey}</code>
                  <span className="draw-placement__arrow" aria-hidden="true">
                    →
                  </span>
                  <span className="draw-placement__entry">
                    <TeamCrest
                      name={side.displayName}
                      logoMediaId={side.logoMediaId}
                      primaryColor={side.primaryColor}
                      size="sm"
                      className="draw-pairing__crest"
                    />
                    {side.displayName}
                  </span>
                </li>
              ))}
            </ul>
          ) : null}

          {hasGroups ? (
            <div
              className="regulation-schematic regulation-schematic--groups structure-draw-result-schematic"
              aria-label={t('result')}
            >
              <div className="regulation-schematic__cards">
                {groupRows.map((row, index) => (
                  <div
                    key={row.groupId}
                    className={`regulation-schematic__card regulation-schematic__card--${index % 4}`}
                  >
                    <span className="regulation-schematic__card-label">
                      {tStructure('place.group', { name: row.groupLabel })}
                    </span>
                    <div className="regulation-schematic__card-slots">
                      {row.entries.map((entry) => (
                        <div
                          key={`${row.groupId}-${entry.entryId}`}
                          className="schematic-slot"
                        >
                          <span className="schematic-slot__body">
                            <TeamCrest
                              name={entry.displayName}
                              logoMediaId={entry.logoMediaId}
                              primaryColor={entry.primaryColor}
                              size="sm"
                              className="schematic-slot__crest"
                            />
                            <span className="schematic-slot__copy">
                              <span className="schematic-slot__primary">
                                {entry.displayName}
                              </span>
                            </span>
                          </span>
                        </div>
                      ))}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          ) : null}
        </div>
      ) : null}
    </DrawSectionTile>
  );
}

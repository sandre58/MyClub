import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useMemo, useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import {
  applyDraw,
  cancelDraw,
  createAndGenerateDraw,
  fetchStageOverview,
  publishAndApplyDraw,
} from '../api';
import { Alert } from '../design-system/components/Alert';
import { ConfirmDialog } from '../design-system/components/ConfirmDialog';
import { Dialog } from '../design-system/components/Dialog';
import { RandomIcon } from '../design-system/icons/contentIcons';
import { TeamCrest } from '../design-system/TeamCrest';
import { drawResolutionKindLabel } from '../i18n/enumLabels';
import { queryKeys } from '../queryKeys';
import type {
  StageDraw,
  StageRound,
  StageSlot,
  StructureFormatKind,
  StructureStageHubSummary,
} from '../types';
import {
  getDrawUiProjection,
  groupPlacementRows,
  pickDefaultDrawId,
  resolveDrawCreateGate,
} from './drawUi';
import { invalidateAfterStructureMutation } from './structureInvalidation';
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

function resolveDrawKindForFormat(
  format: StructureFormatKind | null | undefined,
): 'Group' | 'Pairing' | null {
  if (format === 'Groups') {
    return 'Group';
  }
  if (format === 'Cup') {
    return 'Pairing';
  }
  return null;
}

/** One non-Cancelled Draw may own the active execution; rerun = Cancel → Nouveau. */
function hasActiveDraw(draws: StageDraw[]): boolean {
  return draws.some((d) => d.status !== 'Cancelled');
}

type StructureDrawDialogProps = {
  open: boolean;
  onClose: () => void;
  competitionId: string;
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

  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [applyConfirmOpen, setApplyConfirmOpen] = useState(false);
  const [cancelConfirmOpen, setCancelConfirmOpen] = useState(false);

  useEffect(() => {
    if (!open) {
      setSelectedId(null);
      setApplyConfirmOpen(false);
      setCancelConfirmOpen(false);
      return;
    }
    if (!draws || draws.length === 0) {
      setSelectedId(null);
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

  const activeExists = hasActiveDraw(drawList);
  const createGate = resolveDrawCreateGate({
    kind: drawKind,
    hasActiveDraw: activeExists,
    compositionEntryCount: poolCount,
    isRootComposition: stage.isRootComposition,
    numberOfPots: stage.numberOfPots,
    groupCount: stage.groupCount,
  });
  const showCreate = drawKind != null && !overviewQuery.isLoading;
  const canCreate = showCreate && createGate.ok;
  const createBlockedReason =
    showCreate && !createGate.ok ? createGate.reason : null;

  const createBlockedMessage =
    createBlockedReason != null
      ? t(`fiche.drawWorkflow.createBlocked.${createBlockedReason}`)
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
      return createAndGenerateDraw(stageId, drawKind);
    },
    onSuccess: async (result) => {
      await invalidateDrawQueries();
      setSelectedId(result.drawId);
    },
  });

  const publishAndApplyMutation = useMutation({
    mutationFn: async (draw: StageDraw) => {
      // Pairing: Host EnsurePairingFixtures when fixtureIds empty — no client 1:1 gate.
      return publishAndApplyDraw(stageId, draw.id, { fixtureIds: [] });
    },
    // Always refresh: Apply may fail after a durable Publish (recovery state).
    onSettled: async (_data, _error, draw) => {
      await invalidateDrawQueries();
      if (draw?.kind === 'Pairing') {
        await queryClient.invalidateQueries({
          queryKey: queryKeys.matches.byStage(stageId),
        });
      }
    },
  });

  const cancelMutation = useMutation({
    mutationFn: (drawId: string) => cancelDraw(stageId, drawId),
    onSuccess: async () => {
      setCancelConfirmOpen(false);
      await invalidateDrawQueries();
    },
  });

  const applyMutation = useMutation({
    mutationFn: async (draw: StageDraw) => {
      // Slot/Group/Pairing — Host ensures Pairing fixtures when body is empty.
      return applyDraw(stageId, draw.id, { fixtureIds: [] });
    },
    onSuccess: async (_void, draw) => {
      setApplyConfirmOpen(false);
      await invalidateDrawQueries();
      if (draw.kind === 'Pairing') {
        await queryClient.invalidateQueries({
          queryKey: queryKeys.matches.byStage(stageId),
        });
      }
    },
  });

  const busy =
    createMutation.isPending ||
    publishAndApplyMutation.isPending ||
    cancelMutation.isPending ||
    applyMutation.isPending;

  const mutationError =
    createMutation.error ??
    publishAndApplyMutation.error ??
    cancelMutation.error ??
    applyMutation.error;

  const newestFirst = useMemo(
    () => [...drawList].reverse(),
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
        trapFocus={!applyConfirmOpen && !cancelConfirmOpen}
        footer={
          <div className="button-row">
            {showCreate ? (
              <button
                type="button"
                className="ds-btn ds-btn--primary"
                disabled={busy || !canCreate}
                aria-describedby={
                  createBlockedMessage != null
                    ? 'structure-draw-create-blocked'
                    : undefined
                }
                onClick={() => createMutation.mutate()}
              >
                {createMutation.isPending ? (
                  <PendingLabel>{t('fiche.drawWorkflow.creating')}</PendingLabel>
                ) : (
                  t('fiche.drawWorkflow.create')
                )}
              </button>
            ) : null}
            <button
              type="button"
              className="ds-btn ds-btn--ghost"
              disabled={busy}
              onClick={onClose}
            >
              {tCommon('close')}
            </button>
          </div>
        }
        footerStatus={
          overviewQuery.isError ||
          mutationError ||
          createBlockedMessage != null ? (
            <>
              {createBlockedMessage != null ? (
                <Alert
                  id="structure-draw-create-blocked"
                  tone="warning"
                  role="status"
                >
                  {createBlockedMessage}
                </Alert>
              ) : null}
              {overviewQuery.isError ? (
                <Alert tone="danger" role="alert">
                  {t('fiche.drawWorkflow.loadError')}
                </Alert>
              ) : null}
              {mutationError ? <MutationError error={mutationError} /> : null}
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
                : createBlockedReason === 'emptyPool' ||
                    createBlockedReason === 'emptyPoolUpstream'
                  ? t(
                      createBlockedReason === 'emptyPoolUpstream'
                        ? 'fiche.drawWorkflow.emptyBodyNeedUpstream'
                        : 'fiche.drawWorkflow.emptyBodyNeedPool',
                    )
                  : createBlockedReason != null
                    ? createBlockedMessage
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
                  {newestFirst.map((draw, index) => {
                    const execLabel = t('fiche.drawWorkflow.execution', {
                      n: drawList.length - index,
                    });
                    const isCurrent = draw.id === selected?.id;
                    const chrome = getDrawUiProjection(draw, slots, rounds).chrome;
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
                            {chrome.showStatus ? (
                              <span className="structure-draw-master__card-status">
                                <DrawStatusBadge
                                  status={draw.status}
                                  density="compact"
                                />
                              </span>
                            ) : null}
                          </span>
                          <span className="structure-draw-master__card-facts">
                            {chrome.showResolution ? (
                              <DrawResolutionBadge
                                state={draw.resolutionState}
                                density="compact"
                              />
                            ) : null}
                            {chrome.showApplied ? (
                              <StatusBadge tone="ok" density="compact">
                                {tDraw('applied')}
                              </StatusBadge>
                            ) : null}
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
                  busy={busy}
                  onPublishAndApply={() =>
                    publishAndApplyMutation.mutate(selected)
                  }
                  onApply={() => setApplyConfirmOpen(true)}
                  onCancel={() => setCancelConfirmOpen(true)}
                  publishAndApplyPending={publishAndApplyMutation.isPending}
                  applyPending={applyMutation.isPending}
                  cancelPending={cancelMutation.isPending}
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
        cancelLabel={tCommon('close')}
        closeLabel={tCommon('close')}
        danger
        confirmDisabled={cancelMutation.isPending}
        confirmPending={cancelMutation.isPending}
        confirmPendingLabel={tDraw('cancelling')}
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
    </>
  );
}

function DrawSectionTile({
  id,
  title,
  description,
  icon,
  footer,
  children,
}: {
  id: string;
  title: string;
  description?: string;
  icon: ReactNode;
  footer?: ReactNode;
  children: ReactNode;
}) {
  const titleId = `${id}-title`;
  return (
    <section
      id={id}
      className="ds-form-section structure-domain-tile structure-draw-tile"
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
  busy,
  onPublishAndApply,
  onApply,
  onCancel,
  publishAndApplyPending,
  applyPending,
  cancelPending,
}: {
  draw: StageDraw;
  slots: StageSlot[];
  rounds: StageRound[];
  busy: boolean;
  onPublishAndApply: () => void;
  onApply: () => void;
  onCancel: () => void;
  publishAndApplyPending: boolean;
  applyPending: boolean;
  cancelPending: boolean;
}) {
  const { t } = useTranslation('draw');
  const { t: tCommon } = useTranslation('common');
  const { t: tStructure } = useTranslation('structure');
  const ui = getDrawUiProjection(draw, slots, rounds);

  const canPublishAndApply =
    draw.status === 'Draft' &&
    draw.resolutionState === 'Resolved' &&
    (draw.kind === 'Slot' || draw.kind === 'Group' || draw.kind === 'Pairing');
  const canApply =
    draw.status === 'Published' &&
    draw.resolutionState === 'Resolved' &&
    !ui.isApplied &&
    (draw.kind === 'Slot' || draw.kind === 'Group' || draw.kind === 'Pairing');
  const canCancel =
    draw.status === 'Draft' || draw.status === 'Published';

  const groupRows =
    draw.kind === 'Group' && (draw.groupPlacements?.length ?? 0) > 0
      ? groupPlacementRows(
          draw.groupPlacements!,
          tCommon('unknownEntry'),
          t('unknownGroup'),
        )
      : [];

  const hasPairings =
    ui.showResults && draw.kind === 'Pairing' && draw.pairings.length > 0;
  const hasSlots =
    ui.showResults &&
    draw.kind === 'Slot' &&
    draw.slotPlacements.length > 0;
  const hasGroups = ui.showResults && draw.kind === 'Group' && groupRows.length > 0;
  const showResults = hasPairings || hasSlots || hasGroups;

  const stateActions =
    canPublishAndApply || canApply || canCancel ? (
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
              t('publishAndApply')
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
              t('apply')
            )}
          </button>
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
              t('cancel')
            )}
          </button>
        ) : null}
      </div>
    ) : null;

  return (
    <DrawSectionTile
      id={`draw-detail-${draw.id}`}
      title={t('title', { kind: drawResolutionKindLabel(draw.kind) })}
      icon={<RandomIcon size="sm" />}
      footer={stateActions}
    >
      <p className="badge-row">
        {ui.chrome.showStatus ? (
          <DrawStatusBadge status={draw.status} />
        ) : null}
        {ui.chrome.showResolution ? (
          <DrawResolutionBadge state={draw.resolutionState} />
        ) : null}
        {ui.chrome.showApplied ? (
          <StatusBadge tone="ok">{t('applied')}</StatusBadge>
        ) : null}
      </p>
      <p className="structure-draw-detail__message" role="status">
        {t(ui.messageKey)}
      </p>

      {showResults ? (
        <div className="structure-draw-result">
          <h4 className="structure-draw-result__title">{t('result')}</h4>
          {hasPairings ? (
            <ul className="draw-pairing-list">
              {draw.pairings.map((pairing) => (
                <li
                  key={`${pairing.entryAId}-${pairing.entryBId}`}
                  className="draw-pairing"
                >
                  <span className="draw-pairing__side">
                    {pairing.entryADisplayName?.trim() ||
                      tCommon('unknownEntry')}
                  </span>
                  <span className="draw-pairing__vs">{t('vs')}</span>
                  <span className="draw-pairing__side">
                    {pairing.entryBDisplayName?.trim() ||
                      tCommon('unknownEntry')}
                  </span>
                </li>
              ))}
            </ul>
          ) : null}

          {hasSlots ? (
            <ul className="draw-placement-list">
              {draw.slotPlacements.map((placement) => (
                <li
                  key={`${placement.slotKey}-${placement.entryId}`}
                  className="draw-placement"
                >
                  <code className="draw-placement__key">{placement.slotKey}</code>
                  <span className="draw-placement__arrow" aria-hidden="true">
                    →
                  </span>
                  <span className="draw-placement__entry">
                    {placement.displayName?.trim() || tCommon('unknownEntry')}
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

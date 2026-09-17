import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  applyDraw,
  cancelDraw,
  createAndGenerateDraw,
  fetchStageOverview,
  publishDraw,
} from '../api';
import { ConfirmDialog } from '../design-system/components/ConfirmDialog';
import { Dialog } from '../design-system/components/Dialog';
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
  pickDefaultDrawId,
  resolvePairingFixtureIds,
} from './drawUi';
import { invalidateAfterStructureMutation } from './structureInvalidation';
import {
  DrawResolutionBadge,
  DrawStatusBadge,
  MutationError,
  PendingLabel,
  StatusBadge,
} from '../ui';

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

type StructureDrawDialogProps = {
  open: boolean;
  onClose: () => void;
  competitionId: string;
  stage: StructureStageHubSummary;
};

/**
 * Work dialog — exécutions de tirage de la phase (H1).
 * Master-detail only when draws.length > 1. Create = G2 (create+inputs+generate).
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
  const showMaster = drawList.length > 1;

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

  const hasDraft = drawList.some((d) => d.status === 'Draft');
  const canCreate = drawKind != null && !hasDraft && !overviewQuery.isLoading;

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

  const publishMutation = useMutation({
    mutationFn: (drawId: string) => publishDraw(stageId, drawId),
    onSuccess: async () => {
      await invalidateDrawQueries();
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
      if (draw.kind === 'Slot') {
        return applyDraw(stageId, draw.id, { fixtureIds: [] });
      }
      if (draw.kind === 'Pairing') {
        const fixtureIds = resolvePairingFixtureIds(draw, rounds);
        if (fixtureIds === null) {
          throw new Error(
            'Cannot apply pairing: fixture count must match pairing count for a 1:1 map.',
          );
        }
        return applyDraw(stageId, draw.id, { fixtureIds });
      }
      throw new Error('Apply is not available for this draw kind.');
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
    publishMutation.isPending ||
    cancelMutation.isPending ||
    applyMutation.isPending;

  const mutationError =
    createMutation.error ??
    publishMutation.error ??
    cancelMutation.error ??
    applyMutation.error;

  const newestFirst = useMemo(
    () => [...drawList].reverse(),
    [drawList],
  );

  return (
    <>
      <Dialog
        open={open}
        onClose={onClose}
        title={t('fiche.drawWorkflow.title')}
        description={t('fiche.drawWorkflow.description', {
          phase: stage.name,
        })}
        size="lg"
        closeLabel={tCommon('close')}
        closeDisabled={busy}
        trapFocus={!applyConfirmOpen && !cancelConfirmOpen}
        footer={
          <div className="button-row">
            {canCreate ? (
              <button
                type="button"
                className="ds-btn ds-btn--primary"
                disabled={busy}
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
          overviewQuery.isError || mutationError ? (
            <>
              {overviewQuery.isError ? (
                <p className="ds-notice ds-notice--danger" role="alert">
                  {t('fiche.drawWorkflow.loadError')}
                </p>
              ) : null}
              {mutationError ? <MutationError error={mutationError} /> : null}
            </>
          ) : null
        }
      >
        {(stage.compositionEntryCount ?? 0) > 0 ? (
          <p className="structure-draw-pool" role="note">
            {t('fiche.drawWorkflow.poolFromPopulation', {
              count: stage.compositionEntryCount,
            })}
          </p>
        ) : null}
        {overviewQuery.isLoading ? (
          <p className="structure-panel__muted" role="status">
            {t('fiche.drawWorkflow.loading')}
          </p>
        ) : null}

        {!overviewQuery.isLoading && !overviewQuery.isError && drawList.length === 0 ? (
          <div className="structure-draw-empty">
            <p className="structure-panel__muted">{t('fiche.drawWorkflow.empty')}</p>
            {drawKind == null ? (
              <p className="hint">{t('fiche.drawWorkflow.kindUnsupported')}</p>
            ) : null}
          </div>
        ) : null}

        {drawList.length > 0 ? (
          <div
            className={
              showMaster
                ? 'structure-draw-layout structure-draw-layout--split'
                : 'structure-draw-layout'
            }
          >
            {showMaster ? (
              <nav
                className="structure-draw-master"
                aria-label={t('fiche.drawWorkflow.historyAria')}
              >
                <p className="structure-draw-master__hint">
                  {t('fiche.drawWorkflow.historyHint')}
                </p>
                <ul className="structure-draw-master__list">
                  {newestFirst.map((draw, index) => {
                    const execLabel = t('fiche.drawWorkflow.execution', {
                      n: drawList.length - index,
                    });
                    const isCurrent = draw.id === selected?.id;
                    return (
                      <li key={draw.id}>
                        <button
                          type="button"
                          className={
                            isCurrent
                              ? 'structure-draw-master__item structure-draw-master__item--active'
                              : 'structure-draw-master__item'
                          }
                          aria-current={isCurrent ? 'true' : undefined}
                          onClick={() => setSelectedId(draw.id)}
                        >
                          <span className="structure-draw-master__item-title">
                            {execLabel}
                          </span>
                          <span className="badge-row">
                            <DrawStatusBadge status={draw.status} />
                          </span>
                        </button>
                      </li>
                    );
                  })}
                </ul>
              </nav>
            ) : null}

            {selected ? (
              <DrawExecutionDetail
                draw={selected}
                slots={slots}
                rounds={rounds}
                busy={busy}
                onPublish={() => publishMutation.mutate(selected.id)}
                onApply={() => setApplyConfirmOpen(true)}
                onCancel={() => setCancelConfirmOpen(true)}
                publishPending={publishMutation.isPending}
                applyPending={applyMutation.isPending}
                cancelPending={cancelMutation.isPending}
              />
            ) : null}
          </div>
        ) : null}
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
        message={tDraw('confirmCancel')}
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

function DrawExecutionDetail({
  draw,
  slots,
  rounds,
  busy,
  onPublish,
  onApply,
  onCancel,
  publishPending,
  applyPending,
  cancelPending,
}: {
  draw: StageDraw;
  slots: StageSlot[];
  rounds: StageRound[];
  busy: boolean;
  onPublish: () => void;
  onApply: () => void;
  onCancel: () => void;
  publishPending: boolean;
  applyPending: boolean;
  cancelPending: boolean;
}) {
  const { t } = useTranslation('draw');
  const ui = getDrawUiProjection(draw, slots, rounds);

  const canPublish =
    draw.status === 'Draft' && draw.resolutionState === 'Resolved';
  const canApply =
    draw.status === 'Published' &&
    draw.resolutionState === 'Resolved' &&
    !ui.isApplied &&
    (draw.kind === 'Slot' || draw.kind === 'Pairing');
  const canCancel =
    draw.status === 'Draft' || draw.status === 'Published';

  const pairingFixtureIds =
    draw.kind === 'Pairing' ? resolvePairingFixtureIds(draw, rounds) : null;
  const pairingMapBlocked =
    draw.kind === 'Pairing' && canApply && pairingFixtureIds === null;

  return (
    <article className="structure-draw-detail">
      <header className="stack stack--tight">
        <h3 className="structure-draw-detail__title">
          {t('title', { kind: drawResolutionKindLabel(draw.kind) })}
        </h3>
        <p className="badge-row">
          <DrawStatusBadge status={draw.status} />
          <DrawResolutionBadge state={draw.resolutionState} />
          {ui.isApplied ? (
            <StatusBadge tone="ok">{t('applied')}</StatusBadge>
          ) : null}
        </p>
      </header>

      <p className="structure-draw-detail__message" role="status">
        {t(ui.messageKey)}
      </p>

      {ui.showResults && draw.kind === 'Pairing' && draw.pairings.length > 0 ? (
        <div className="stack stack--tight">
          <h4 className="structure-draw-detail__section">{t('result')}</h4>
          <ul className="draw-pairing-list">
            {draw.pairings.map((pairing) => (
              <li
                key={`${pairing.entryAId}-${pairing.entryBId}`}
                className="draw-pairing"
              >
                <span className="draw-pairing__side">
                  {pairing.entryADisplayName?.trim() ||
                    t('unknownEntry', { ns: 'common' })}
                </span>
                <span className="draw-pairing__vs">{t('vs')}</span>
                <span className="draw-pairing__side">
                  {pairing.entryBDisplayName?.trim() ||
                    t('unknownEntry', { ns: 'common' })}
                </span>
              </li>
            ))}
          </ul>
        </div>
      ) : null}

      {ui.showResults &&
      draw.kind === 'Slot' &&
      draw.slotPlacements.length > 0 ? (
        <div className="stack stack--tight">
          <h4 className="structure-draw-detail__section">{t('placements')}</h4>
          <ul className="draw-placement-list">
            {draw.slotPlacements.map((placement) => (
              <li
                key={`${placement.slotKey}-${placement.entryId}`}
                className="draw-placement"
              >
                <code>{placement.slotKey}</code>
                <span className="draw-placement__arrow" aria-hidden="true">
                  →
                </span>
                <span className="draw-placement__entry">
                  {placement.displayName?.trim() ||
                    t('unknownEntry', { ns: 'common' })}
                </span>
              </li>
            ))}
          </ul>
        </div>
      ) : null}

      {ui.showResults &&
      draw.kind === 'Group' &&
      draw.resolutionState === 'Resolved' ? (
        <p className="hint" role="status">
          {t('groupPlacementsHint')}
        </p>
      ) : null}

      {pairingMapBlocked ? (
        <p className="ds-notice ds-notice--warning" role="status">
          {t('pairingMapBlocked')}
        </p>
      ) : null}

      {(canPublish || canApply || canCancel) && (
        <div className="button-row" aria-busy={busy}>
          {canPublish ? (
            <button
              type="button"
              className="ds-btn ds-btn--primary"
              disabled={busy}
              onClick={onPublish}
            >
              {publishPending ? (
                <PendingLabel>{t('publishing')}</PendingLabel>
              ) : (
                t('publish')
              )}
            </button>
          ) : null}
          {canApply && !pairingMapBlocked ? (
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
              className="ds-btn ds-btn--ghost"
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
      )}
    </article>
  );
}

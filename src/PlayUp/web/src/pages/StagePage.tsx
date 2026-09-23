import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useParams } from 'react-router-dom';
import {
  applyDraw,
  fetchCompetitionDetail,
  fetchStageOverview,
  materializeCupFromOccupiedSlots,
  prepareStage,
  publishDraw,
  startStage,
} from '../api';
import { ConfirmDialog } from '../design-system/components/ConfirmDialog';
import { queryKeys } from '../queryKeys';
import {
  DrawResolutionBadge,
  DrawStatusBadge,
  EmptyState,
  ErrorState,
  LoadingState,
  MutationError,
  PageHeader,
  PendingLabel,
  StageStatusBadge,
  StatusBadge,
} from '../ui';
import { drawResolutionKindLabel } from '../i18n/enumLabels';
import {
  type StageDraw,
  type StageOverview,
  type StageRound,
  type StageSlot,
} from '../types';
import { getDrawUiProjection, resolvePairingFixtureIds } from './drawUi';
import './StagePage.css';

export function StagePage() {
  const { stageId = '' } = useParams();
  const { t } = useTranslation('stage');

  // SERVER STATE: StageOverview lives in TanStack Query — one cache entry for the stage.
  // Draw UI below only reads this result; it never copies draws into useState.
  const stageQuery = useQuery({
    queryKey: queryKeys.stages.detail(stageId),
    queryFn: () => fetchStageOverview(stageId),
    enabled: stageId.length > 0,
  });

  // Same query key as GET /competitions/{id} (MatchHub / shell context).
  const competitionId = stageQuery.data?.competitionId;
  const competitionQuery = useQuery({
    queryKey: queryKeys.competitions.detail(competitionId ?? ''),
    queryFn: () => fetchCompetitionDetail(competitionId!),
    enabled: Boolean(competitionId),
  });

  return (
    <main id="main" className="page">
      <PageHeader
        eyebrow={t('eyebrow')}
        title={stageQuery.data?.name ?? t('titleFallback')}
        back={
          competitionId
            ? {
                to: `/competitions/${competitionId}/structure`,
                label: competitionQuery.data?.name
                  ? t('backNamed', { name: competitionQuery.data.name })
                  : t('back'),
              }
            : undefined
        }
        badges={
          stageQuery.data && (
            <StageStatusBadge status={stageQuery.data.status} />
          )
        }
      />

      {stageQuery.isPending && <LoadingState />}
      {stageQuery.isError && <ErrorState error={stageQuery.error} />}
      {stageQuery.data && <StageOverviewView data={stageQuery.data} />}
    </main>
  );
}

function StageOverviewView({ data }: { data: StageOverview }) {
  const { t } = useTranslation('stage');
  const queryClient = useQueryClient();
  const fixtureCount = data.rounds.reduce(
    (sum, round) => sum + round.fixtures.length,
    0,
  );

  // UX gate only: Domain still rejects Prepare / Start when status is wrong.
  const canPrepare = data.status === 'Draft';
  const canStart = data.status === 'Ready';

  // useMutation = write on user intent. Server state stays in the stage query.
  const prepareMutation = useMutation({
    mutationFn: () => prepareStage(data.id),
    onSuccess: async () => {
      // Invalidate → active observers refetch → badge shows Ready from GET.
      await queryClient.invalidateQueries({
        queryKey: queryKeys.stages.detail(data.id),
      });
      // CompetitionDetail.stages[].status would stay Draft for staleTime (30s)
      // after Back → Competition; Prepare changes that field, so invalidate it.
      await queryClient.invalidateQueries({
        queryKey: queryKeys.competitions.detail(data.competitionId),
      });
    },
  });

  const startMutation = useMutation({
    mutationFn: () => startStage(data.id),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: queryKeys.stages.detail(data.id),
      });
      // Same field as Prepare: CompetitionPage shows stage status from overview.
      await queryClient.invalidateQueries({
        queryKey: queryKeys.competitions.detail(data.competitionId),
      });
    },
  });

  const stageActionBusy = prepareMutation.isPending || startMutation.isPending;
  const stageActionError = prepareMutation.error ?? startMutation.error;
  const showStageActions =
    canPrepare || canStart || prepareMutation.isError || startMutation.isError;

  return (
    <div className="section-stack">
      <section className="ds-panel" aria-labelledby="stage-heading">
        <div className="ds-panel-head">
          <h2 className="ds-panel-head__title" id="stage-heading">
            {t('operations.heading')}
          </h2>
          <span className="id-chip">{data.id}</span>
        </div>

        <div className="button-row">
          {showStageActions && (
            <span className="button-row" aria-busy={stageActionBusy}>
              {canPrepare && (
                <button
                  type="button"
                  className="ds-btn ds-btn--primary"
                  disabled={stageActionBusy}
                  onClick={() => prepareMutation.mutate()}
                >
                  {prepareMutation.isPending ? (
                    <PendingLabel>{t('operations.preparing')}</PendingLabel>
                  ) : (
                    t('operations.prepare')
                  )}
                </button>
              )}
              {canStart && (
                <button
                  type="button"
                  className="ds-btn ds-btn--primary"
                  disabled={stageActionBusy}
                  onClick={() => startMutation.mutate()}
                >
                  {startMutation.isPending ? (
                    <PendingLabel>{t('operations.starting')}</PendingLabel>
                  ) : (
                    t('operations.start')
                  )}
                </button>
              )}
            </span>
          )}
          <Link
            className="ds-btn ds-btn--secondary"
            to={`/stages/${data.id}/matches`}
          >
            {t('operations.viewMatches')}
          </Link>
        </div>

        {stageActionError && <MutationError error={stageActionError} />}
      </section>

      <CupConfrontationsPanel data={data} />

      <section className="ds-panel" aria-labelledby="rounds-heading">
        <div className="ds-panel-head">
          <h2 className="ds-panel-head__title" id="rounds-heading">
            {t('rounds.heading', { count: data.rounds.length })}
          </h2>
          <span className="ds-eyebrow">
            {t('rounds.subtitle', { count: fixtureCount })}
          </span>
        </div>
        {data.rounds.length === 0 ? (
          <EmptyState title={t('rounds.emptyTitle')}>
            {t('rounds.emptyBody')}
          </EmptyState>
        ) : (
          <ul className="plain-list">
            {data.rounds.map((round) => (
              <li key={round.id}>
                <strong>{round.name}</strong>
                <span className="muted">
                  {' '}
                  ·{' '}
                  {t('rounds.fixtureCount', {
                    count: round.fixtures.length,
                  })}
                </span>
                {round.fixtures.length > 0 && (
                  <ul className="nested-list">
                    {round.fixtures.map((fixture) => (
                      <li key={fixture.id} className="mono">
                        {fixture.slotAKey ?? '—'} vs {fixture.slotBKey ?? '—'}
                        {fixture.attachments.length > 0 && (
                          <span className="muted">
                            {' '}
                            (
                            {fixture.attachments
                              .map((a) =>
                                t('rounds.leg', { index: a.legIndex }),
                              )
                              .join(', ')}
                            )
                          </span>
                        )}
                      </li>
                    ))}
                  </ul>
                )}
              </li>
            ))}
          </ul>
        )}
      </section>

      <section className="ds-panel" aria-labelledby="slots-heading">
        <h2 className="ds-panel-head__title" id="slots-heading">
          {t('slots.heading', { count: data.slots.length })}
        </h2>
        {data.slots.length === 0 ? (
          <EmptyState title={t('slots.emptyTitle')}>
            {t('slots.emptyBody')}
          </EmptyState>
        ) : (
          <ul className="plain-list">
            {data.slots.map((slot) => (
              <li key={slot.slotKey}>
                <code>{slot.slotKey}</code>
                {slot.displayName ? (
                  <> — {slot.displayName}</>
                ) : (
                  <span className="muted"> — {t('slots.empty')}</span>
                )}
              </li>
            ))}
          </ul>
        )}
      </section>

      <DrawSection
        stageId={data.id}
        draws={data.draws}
        slots={data.slots}
        rounds={data.rounds}
      />
    </div>
  );
}

type CupSlotPairDraft = { slotAKey: string; slotBKey: string };

/**
 * Explicit SlotA↔SlotB pairing for materialize-from-slots (D2 / Slice 4).
 * No naming heuristic — organizer chooses pairs among occupied slots not yet
 * covered by a complete Fixture (same rule as Overview readiness).
 */
function CupConfrontationsPanel({ data }: { data: StageOverview }) {
  const { t } = useTranslation('stage');
  const queryClient = useQueryClient();
  const [pairs, setPairs] = useState<CupSlotPairDraft[]>([]);
  const [slotA, setSlotA] = useState('');
  const [slotB, setSlotB] = useState('');

  const occupied = data.slots.filter((slot) => slot.entryId != null);
  const pairable = occupied.filter((slot) => !slot.coveredByCompleteFixture);
  const coveredCount = occupied.filter(
    (slot) => slot.coveredByCompleteFixture,
  ).length;
  const usedKeys = new Set(
    pairs.flatMap((pair) => [pair.slotAKey, pair.slotBKey]),
  );
  const availableForSelect = pairable.filter(
    (slot) => !usedKeys.has(slot.slotKey),
  );

  const canShow =
    (data.status === 'Draft' || data.status === 'Ready') &&
    data.rounds.length > 0 &&
    occupied.length >= 2;

  const materializeMutation = useMutation({
    mutationFn: () =>
      materializeCupFromOccupiedSlots(
        data.id,
        pairs.map((pair) => ({
          slotAKey: pair.slotAKey,
          slotBKey: pair.slotBKey,
        })),
      ),
    onSuccess: async () => {
      setPairs([]);
      setSlotA('');
      setSlotB('');
      await queryClient.invalidateQueries({
        queryKey: queryKeys.stages.detail(data.id),
      });
      await queryClient.invalidateQueries({
        queryKey: queryKeys.competitions.overview(data.competitionId),
      });
      await queryClient.invalidateQueries({
        queryKey: queryKeys.competitions.detail(data.competitionId),
      });
    },
  });

  if (!canShow) {
    return null;
  }

  const canAdd =
    slotA.length > 0 &&
    slotB.length > 0 &&
    slotA !== slotB &&
    !usedKeys.has(slotA) &&
    !usedKeys.has(slotB);

  const pairingExhausted = pairable.length < 2 && pairs.length === 0;

  return (
    <section className="ds-panel" aria-labelledby="confrontations-heading">
      <h2 className="ds-panel-head__title" id="confrontations-heading">
        {t('confrontations.heading')}
      </h2>
      <p className="muted">{t('confrontations.intro')}</p>
      <p className="muted">
        {t('confrontations.occupiedHint', { count: occupied.length })}
      </p>
      {coveredCount > 0 && (
        <p className="muted">
          {t('confrontations.coveredHint', { count: coveredCount })}
        </p>
      )}

      {pairingExhausted ? (
        <p className="ds-notice" role="status">
          {t('confrontations.allCovered')}
        </p>
      ) : (
        <>
          {pairs.length > 0 && (
            <ul
              className="plain-list"
              aria-label={t('confrontations.pairsHeading')}
            >
              {pairs.map((pair) => (
                <li
                  key={`${pair.slotAKey}:${pair.slotBKey}`}
                  className="button-row"
                >
                  <span>
                    {t('confrontations.pairLabel', {
                      slotA: pair.slotAKey,
                      slotB: pair.slotBKey,
                    })}
                  </span>
                  <button
                    type="button"
                    className="ds-btn ds-btn--secondary"
                    onClick={() =>
                      setPairs((current) =>
                        current.filter(
                          (item) =>
                            item.slotAKey !== pair.slotAKey ||
                            item.slotBKey !== pair.slotBKey,
                        ),
                      )
                    }
                  >
                    {t('confrontations.removePair')}
                  </button>
                </li>
              ))}
            </ul>
          )}
          {pairs.length === 0 && (
            <p className="muted">{t('confrontations.pairsEmpty')}</p>
          )}

          {availableForSelect.length >= 2 && (
            <div className="button-row">
              <label>
                {t('confrontations.slotA')}{' '}
                <select
                  value={slotA}
                  onChange={(event) => setSlotA(event.target.value)}
                >
                  <option value="">
                    {t('confrontations.selectPlaceholder')}
                  </option>
                  {availableForSelect
                    .filter((slot) => slot.slotKey !== slotB)
                    .map((slot) => (
                      <option key={slot.slotKey} value={slot.slotKey}>
                        {slot.slotKey}
                        {slot.displayName ? ` — ${slot.displayName}` : ''}
                      </option>
                    ))}
                </select>
              </label>
              <label>
                {t('confrontations.slotB')}{' '}
                <select
                  value={slotB}
                  onChange={(event) => setSlotB(event.target.value)}
                >
                  <option value="">
                    {t('confrontations.selectPlaceholder')}
                  </option>
                  {availableForSelect
                    .filter((slot) => slot.slotKey !== slotA)
                    .map((slot) => (
                      <option key={slot.slotKey} value={slot.slotKey}>
                        {slot.slotKey}
                        {slot.displayName ? ` — ${slot.displayName}` : ''}
                      </option>
                    ))}
                </select>
              </label>
              <button
                type="button"
                className="ds-btn ds-btn--secondary"
                disabled={!canAdd}
                onClick={() => {
                  setPairs((current) => [
                    ...current,
                    { slotAKey: slotA, slotBKey: slotB },
                  ]);
                  setSlotA('');
                  setSlotB('');
                }}
              >
                {t('confrontations.addPair')}
              </button>
            </div>
          )}

          <div className="button-row">
            <button
              type="button"
              className="ds-btn ds-btn--primary"
              disabled={pairs.length === 0 || materializeMutation.isPending}
              onClick={() => materializeMutation.mutate()}
            >
              {materializeMutation.isPending ? (
                <PendingLabel>{t('confrontations.submitting')}</PendingLabel>
              ) : (
                t('confrontations.submit')
              )}
            </button>
          </div>
        </>
      )}

      {materializeMutation.isSuccess && (
        <p className="ds-notice" role="status">
          {materializeMutation.data.alreadyComplete
            ? t('confrontations.successAlreadyComplete')
            : t('confrontations.successCreated', {
                count: materializeMutation.data.createdCount,
              })}
        </p>
      )}
      {materializeMutation.isError && (
        <MutationError error={materializeMutation.error} />
      )}
    </section>
  );
}

/**
 * Composition: a named section keeps StageOverviewView readable.
 * Same page module — not a features/draws layer.
 */
function DrawSection({
  stageId,
  draws,
  slots,
  rounds,
}: {
  stageId: string;
  draws: StageDraw[];
  slots: StageSlot[];
  rounds: StageRound[];
}) {
  const { t } = useTranslation('stage');
  return (
    <section className="ds-panel" aria-labelledby="draws-heading">
      <h2 id="draws-heading" className="ds-panel-head__title">
        {t('draws.heading', { count: draws.length })}
      </h2>
      {draws.length === 0 ? (
        <EmptyState title={t('draws.emptyTitle')}>
          {t('draws.emptyBody')}
        </EmptyState>
      ) : (
        <ul className="draw-list">
          {draws.map((draw) => (
            <li key={draw.id}>
              <DrawCard
                stageId={stageId}
                draw={draw}
                slots={slots}
                rounds={rounds}
              />
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}

function DrawCard({
  stageId,
  draw,
  slots,
  rounds,
}: {
  stageId: string;
  draw: StageDraw;
  slots: StageSlot[];
  rounds: StageRound[];
}) {
  const { t } = useTranslation('draw');
  // DERIVED UI: computed each render from props (server state), never useState.
  const ui = getDrawUiProjection(draw, slots, rounds);

  return (
    <article className="draw-card">
      <header className="stack stack--tight">
        <h3 className="draw-card__title">
          {t('title', { kind: drawResolutionKindLabel(draw.kind) })}
        </h3>
        <p className="badge-row">
          <DrawStatusBadge status={draw.status} />
          <DrawResolutionBadge state={draw.resolutionState} />
          {ui.isApplied && <StatusBadge tone="ok">{t('applied')}</StatusBadge>}
        </p>
      </header>

      <p className="draw-card__message" role="status">
        {t(ui.messageKey)}
      </p>

      {ui.showResults &&
        draw.kind === 'Pairing' &&
        draw.pairings.length > 0 && (
          <div className="stack stack--tight">
            <h4 className="draw-card__results-title">{t('result')}</h4>
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
        )}

      {ui.showResults &&
        draw.kind === 'Slot' &&
        draw.slotPlacements.length > 0 && (
          <div className="stack stack--tight">
            <h4 className="draw-card__results-title">{t('placements')}</h4>
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
        )}

      {ui.showResults &&
        draw.kind === 'Group' &&
        draw.resolutionState === 'Resolved' && (
          <p className="hint" role="status">
            {t('groupPlacementsHint')}
          </p>
        )}

      <DrawActions
        stageId={stageId}
        draw={draw}
        rounds={rounds}
        isApplied={ui.isApplied}
      />
    </article>
  );
}

/**
 * useMutation = “run this write when the user asks”, not “keep this data fresh”.
 * Client state here is only the confirmation gate — not a copy of the Draw.
 */
function DrawActions({
  stageId,
  draw,
  rounds,
  isApplied,
}: {
  stageId: string;
  draw: StageDraw;
  rounds: StageRound[];
  isApplied: boolean;
}) {
  const { t } = useTranslation('draw');
  const { t: tCommon } = useTranslation('common');
  const queryClient = useQueryClient();
  const [applyConfirmOpen, setApplyConfirmOpen] = useState(false);

  const canPublish =
    draw.status === 'Draft' && draw.resolutionState === 'Resolved';
  const canApply =
    draw.status === 'Published' &&
    draw.resolutionState === 'Resolved' &&
    !isApplied &&
    (draw.kind === 'Slot' || draw.kind === 'Group' || draw.kind === 'Pairing');

  const pairingFixtureIds =
    draw.kind === 'Pairing' ? resolvePairingFixtureIds(draw, rounds) : null;
  const pairingMapBlocked =
    draw.kind === 'Pairing' && canApply && pairingFixtureIds === null;

  const publishMutation = useMutation({
    mutationFn: () => publishDraw(stageId, draw.id),
    onSuccess: async () => {
      // invalidateQueries marks cache stale → active queries refetch.
      // Prefer this over refetchQueries: only mounted observers refetch;
      // inactive keys refresh when next used.
      await queryClient.invalidateQueries({
        queryKey: queryKeys.stages.detail(stageId),
      });
    },
  });

  const applyMutation = useMutation({
    mutationFn: () => {
      if (draw.kind === 'Slot' || draw.kind === 'Group') {
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
    onSuccess: async () => {
      setApplyConfirmOpen(false);
      await queryClient.invalidateQueries({
        queryKey: queryKeys.stages.detail(stageId),
      });
      if (draw.kind === 'Pairing') {
        await queryClient.invalidateQueries({
          queryKey: queryKeys.matches.byStage(stageId),
        });
      }
    },
  });

  const busy = publishMutation.isPending || applyMutation.isPending;
  const mutationError = publishMutation.error ?? applyMutation.error;

  function handlePublish() {
    publishMutation.mutate();
  }

  function handleApply() {
    setApplyConfirmOpen(true);
  }

  if (!canPublish && !canApply && !pairingMapBlocked && !mutationError) {
    return null;
  }

  return (
    <>
      <div className="button-row" aria-busy={busy}>
        {canPublish && (
          <button
            type="button"
            className="ds-btn ds-btn--primary"
            disabled={busy}
            onClick={handlePublish}
          >
            {publishMutation.isPending ? (
              <PendingLabel>{t('publishing')}</PendingLabel>
            ) : (
              t('publish')
            )}
          </button>
        )}

        {canApply && draw.kind === 'Slot' && (
          <button
            type="button"
            className="ds-btn ds-btn--primary"
            disabled={busy || applyConfirmOpen}
            onClick={handleApply}
          >
            {applyMutation.isPending ? (
              <PendingLabel>{t('applying')}</PendingLabel>
            ) : (
              t('apply')
            )}
          </button>
        )}

        {canApply && draw.kind === 'Pairing' && pairingFixtureIds !== null && (
          <button
            type="button"
            className="ds-btn ds-btn--primary"
            disabled={busy || applyConfirmOpen}
            onClick={handleApply}
          >
            {applyMutation.isPending ? (
              <PendingLabel>{t('applying')}</PendingLabel>
            ) : (
              t('apply')
            )}
          </button>
        )}

        {pairingMapBlocked && (
          <p className="ds-notice ds-notice--warning" role="status">
            {t('pairingMapBlocked')}
          </p>
        )}

        {mutationError && <MutationError error={mutationError} />}
      </div>
      <ConfirmDialog
        open={applyConfirmOpen}
        title={t('confirmApplyTitle')}
        message={t('confirmApply')}
        confirmLabel={t('apply')}
        cancelLabel={tCommon('cancel')}
        closeLabel={tCommon('close')}
        confirmDisabled={applyMutation.isPending}
        confirmPending={applyMutation.isPending}
        confirmPendingLabel={t('applying')}
        onCancel={() => {
          if (applyMutation.isPending) {
            return;
          }
          setApplyConfirmOpen(false);
        }}
        onConfirm={() => {
          if (applyMutation.isPending) {
            return;
          }
          applyMutation.mutate();
        }}
      />
    </>
  );
}

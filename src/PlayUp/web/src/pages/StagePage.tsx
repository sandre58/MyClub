import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useParams } from 'react-router-dom';
import {
  applyDraw,
  fetchCompetitionDetail,
  fetchStageOverview,
  fetchStageSchematic,
  materializeCupFromOccupiedSlots,
  prepareStage,
  publishAndApplyDraw,
  startStage,
} from '../api';
import { ConfirmDialog } from '../design-system/components/ConfirmDialog';
import { CheckIcon } from '../design-system/icons/contentIcons';
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
  type SchematicConnection,
  type StageBracketPair,
  type StageDraw,
  type StageOverview,
  type StageRound,
  type StageSlot,
} from '../types';
import {
  getDrawUiProjection,
  groupPlacementRows,
  projectSlotDrawResult,
} from './drawUi';
import './StagePage.css';
import './structure.css';

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
                    <>
                      <CheckIcon size="sm" />
                      {t('operations.prepare')}
                    </>
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
                    <>
                      <CheckIcon size="sm" />
                      {t('operations.start')}
                    </>
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
        bracketPairs={data.bracketPairs ?? []}
      />
    </div>
  );
}

/**
 * Materialize Cup confrontations from Domain BracketPairs (pairKey).
 * Lists schematic connections with pairKey; eligibility = both slots occupied
 * and no fixture bound yet (fixtureId null / matchNumber 0).
 */
function CupConfrontationsPanel({ data }: { data: StageOverview }) {
  const { t } = useTranslation('stage');
  const queryClient = useQueryClient();
  const [selectedPairKeys, setSelectedPairKeys] = useState<string[]>([]);

  const occupied = data.slots.filter((slot) => slot.entryId != null);
  const slotByKey = new Map(data.slots.map((slot) => [slot.slotKey, slot]));

  const canShow =
    (data.status === 'Draft' || data.status === 'Ready') &&
    data.rounds.length > 0 &&
    occupied.length >= 2;

  const schematicQuery = useQuery({
    queryKey: queryKeys.stages.schematic(data.id),
    queryFn: () => fetchStageSchematic(data.id),
    enabled: canShow,
  });

  const bracketPairs = (schematicQuery.data?.connections ?? []).filter(
    (conn): conn is SchematicConnection & { pairKey: string } =>
      Boolean(conn.pairKey),
  );

  const rows = bracketPairs.map((conn) => {
    const slotA = conn.slotAKey ? slotByKey.get(conn.slotAKey) : undefined;
    const slotB = conn.slotBKey ? slotByKey.get(conn.slotBKey) : undefined;
    const bothOccupied =
      slotA?.entryId != null &&
      slotB?.entryId != null &&
      slotA.entryId !== slotB.entryId;
    const materialized = Boolean(conn.fixtureId) || conn.matchNumber > 0;
    const eligible = bothOccupied && !materialized;
    return {
      pairKey: conn.pairKey,
      slotAKey: conn.slotAKey ?? '—',
      slotBKey: conn.slotBKey ?? '—',
      eligible,
      materialized,
    };
  });

  const eligibleKeys = rows.filter((row) => row.eligible).map((row) => row.pairKey);
  const eligibleCount = eligibleKeys.length;
  const materializedCount = rows.filter((row) => row.materialized).length;
  const selectionValid = selectedPairKeys.every((key) =>
    eligibleKeys.includes(key),
  );
  const keysToSubmit =
    selectedPairKeys.length > 0 && selectionValid
      ? selectedPairKeys
      : eligibleKeys;

  const materializeMutation = useMutation({
    mutationFn: () =>
      materializeCupFromOccupiedSlots(data.id, keysToSubmit),
    onSuccess: async () => {
      setSelectedPairKeys([]);
      await queryClient.invalidateQueries({
        queryKey: queryKeys.stages.detail(data.id),
      });
      await queryClient.invalidateQueries({
        queryKey: queryKeys.stages.schematic(data.id),
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

  const togglePair = (pairKey: string) => {
    setSelectedPairKeys((current) =>
      current.includes(pairKey)
        ? current.filter((key) => key !== pairKey)
        : [...current, pairKey],
    );
  };

  const pairingExhausted =
    rows.length > 0 && eligibleCount === 0 && materializedCount === rows.length;

  return (
    <section className="ds-panel" aria-labelledby="confrontations-heading">
      <h2 className="ds-panel-head__title" id="confrontations-heading">
        {t('confrontations.heading')}
      </h2>
      <p className="muted">{t('confrontations.intro')}</p>
      <p className="muted">
        {t('confrontations.occupiedHint', { count: occupied.length })}
      </p>
      {materializedCount > 0 && (
        <p className="muted">
          {t('confrontations.coveredHint', { count: materializedCount })}
        </p>
      )}

      {schematicQuery.isLoading && (
        <p className="muted">{t('confrontations.loadingPairs')}</p>
      )}
      {schematicQuery.isError && (
        <MutationError error={schematicQuery.error} />
      )}

      {schematicQuery.isSuccess && rows.length === 0 && (
        <p className="muted">{t('confrontations.noBracketPairs')}</p>
      )}

      {pairingExhausted ? (
        <p className="ds-notice" role="status">
          {t('confrontations.allCovered')}
        </p>
      ) : rows.length > 0 ? (
        <>
          <ul
            className="plain-list"
            aria-label={t('confrontations.pairsHeading')}
          >
            {rows.map((row) => (
              <li key={row.pairKey} className="button-row">
                {row.eligible ? (
                  <label>
                    <input
                      type="checkbox"
                      checked={selectedPairKeys.includes(row.pairKey)}
                      onChange={() => togglePair(row.pairKey)}
                    />{' '}
                    <code>{row.pairKey}</code>
                    {' — '}
                    {t('confrontations.pairLabel', {
                      slotA: row.slotAKey,
                      slotB: row.slotBKey,
                    })}
                  </label>
                ) : (
                  <span className="muted">
                    <code>{row.pairKey}</code>
                    {' — '}
                    {t('confrontations.pairLabel', {
                      slotA: row.slotAKey,
                      slotB: row.slotBKey,
                    })}
                    {row.materialized
                      ? ` — ${t('confrontations.pairMaterialized')}`
                      : ` — ${t('confrontations.pairWaitingSlots')}`}
                  </span>
                )}
              </li>
            ))}
          </ul>

          {eligibleCount > 0 && (
            <div className="button-row">
              <button
                type="button"
                className="ds-btn ds-btn--primary"
                disabled={materializeMutation.isPending}
                onClick={() => materializeMutation.mutate()}
              >
                {materializeMutation.isPending ? (
                  <PendingLabel>{t('confrontations.submitting')}</PendingLabel>
                ) : (
                  <>
                    <CheckIcon size="sm" />
                    {selectedPairKeys.length > 0 && selectionValid
                      ? t('confrontations.submitSelected', {
                          count: selectedPairKeys.length,
                        })
                      : t('confrontations.submitAll', {
                          count: eligibleCount,
                        })}
                  </>
                )}
              </button>
            </div>
          )}
        </>
      ) : null}

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
  bracketPairs,
}: {
  stageId: string;
  draws: StageDraw[];
  slots: StageSlot[];
  rounds: StageRound[];
  bracketPairs: StageBracketPair[];
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
                bracketPairs={bracketPairs}
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
  bracketPairs,
}: {
  stageId: string;
  draw: StageDraw;
  slots: StageSlot[];
  rounds: StageRound[];
  bracketPairs: StageBracketPair[];
}) {
  const { t } = useTranslation('draw');
  const { t: tStructure } = useTranslation('structure');
  // DERIVED UI: computed each render from props (server state), never useState.
  const ui = getDrawUiProjection(draw, slots, rounds);
  const slotResult =
    draw.kind === 'Slot' && draw.slotPlacements.length > 0
      ? projectSlotDrawResult(draw.slotPlacements, bracketPairs, '?')
      : { confrontations: [], unpaired: [] };

  return (
    <article className="draw-card">
      <header className="stack stack--tight">
        <h3 className="draw-card__title">
          {t('title', { kind: drawResolutionKindLabel(draw.kind) })}
        </h3>
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
      </header>

      <p className="draw-card__message" role="status">
        {t(ui.messageKey)}
      </p>

      {ui.showResults &&
        draw.kind === 'Slot' &&
        (slotResult.confrontations.length > 0 ||
          slotResult.unpaired.length > 0) && (
          <div className="stack stack--tight">
            <h4 className="draw-card__results-title">{t('result')}</h4>
            {slotResult.confrontations.length > 0 ? (
              <ul className="draw-pairing-list">
                {slotResult.confrontations.map((row) => (
                  <li key={row.key} className="draw-pairing">
                    <span>
                      <code>{row.sideA.slotKey}</code> {row.sideA.displayName}
                    </span>
                    <span className="draw-pairing__vs">{t('vs')}</span>
                    <span>
                      <code>{row.sideB.slotKey}</code> {row.sideB.displayName}
                    </span>
                  </li>
                ))}
              </ul>
            ) : null}
            {slotResult.unpaired.length > 0 ? (
              <ul className="draw-placement-list">
                {slotResult.unpaired.map((side) => (
                  <li key={side.slotKey} className="draw-placement">
                    <code>{side.slotKey}</code>
                    <span className="draw-placement__arrow" aria-hidden="true">
                      →
                    </span>
                    <span className="draw-placement__entry">
                      {side.displayName}
                    </span>
                  </li>
                ))}
              </ul>
            ) : null}
          </div>
        )}

      {ui.showResults &&
        draw.kind === 'Group' &&
        (draw.groupPlacements?.length ?? 0) > 0 && (
          <div className="stack stack--tight">
            <h4 className="draw-card__results-title">{t('result')}</h4>
            <ul className="draw-group-list">
              {groupPlacementRows(
                draw.groupPlacements!,
                t('unknownEntry', { ns: 'common' }),
                t('unknownGroup'),
              ).map((row) => (
                <li key={row.groupId} className="draw-group-row">
                  <span className="draw-group-row__label">
                    {tStructure('place.group', { name: row.groupLabel })}
                  </span>
                  <span className="draw-group-row__entries">
                    {row.entries.map((e) => e.displayName).join(' · ')}
                  </span>
                </li>
              ))}
            </ul>
          </div>
        )}

      <DrawActions
        stageId={stageId}
        draw={draw}
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
  isApplied,
}: {
  stageId: string;
  draw: StageDraw;
  isApplied: boolean;
}) {
  const { t } = useTranslation('draw');
  const { t: tCommon } = useTranslation('common');
  const queryClient = useQueryClient();
  const [applyConfirmOpen, setApplyConfirmOpen] = useState(false);

  const canPublishAndApply =
    draw.status === 'Draft' &&
    draw.resolutionState === 'Resolved' &&
    (draw.kind === 'Slot' || draw.kind === 'Group');
  const canApply =
    draw.status === 'Published' &&
    draw.resolutionState === 'Resolved' &&
    !isApplied &&
    (draw.kind === 'Slot' || draw.kind === 'Group');

  const publishAndApplyMutation = useMutation({
    mutationFn: () =>
      publishAndApplyDraw(stageId, draw.id),
    onSettled: async () => {
      await queryClient.invalidateQueries({
        queryKey: queryKeys.stages.detail(stageId),
      });
    },
  });

  const applyMutation = useMutation({
    mutationFn: () => applyDraw(stageId, draw.id),
    onSuccess: async () => {
      setApplyConfirmOpen(false);
      await queryClient.invalidateQueries({
        queryKey: queryKeys.stages.detail(stageId),
      });
    },
  });

  const busy = publishAndApplyMutation.isPending || applyMutation.isPending;
  const mutationError =
    publishAndApplyMutation.error ?? applyMutation.error;

  function handlePublishAndApply() {
    publishAndApplyMutation.mutate();
  }

  function handleApply() {
    setApplyConfirmOpen(true);
  }

  if (!canPublishAndApply && !canApply && !mutationError) {
    return null;
  }

  return (
    <>
      <div className="button-row" aria-busy={busy}>
        {canPublishAndApply && (
          <button
            type="button"
            className="ds-btn ds-btn--primary"
            disabled={busy}
            onClick={handlePublishAndApply}
          >
            {publishAndApplyMutation.isPending ? (
              <PendingLabel>{t('publishingAndApplying')}</PendingLabel>
            ) : (
              <>
                <CheckIcon size="sm" />
                {t('publishAndApply')}
              </>
            )}
          </button>
        )}

        {canApply && (
          <button
            type="button"
            className="ds-btn ds-btn--primary"
            disabled={busy || applyConfirmOpen}
            onClick={handleApply}
          >
            {applyMutation.isPending ? (
              <PendingLabel>{t('applying')}</PendingLabel>
            ) : (
              <>
                <CheckIcon size="sm" />
                {t('apply')}
              </>
            )}
          </button>
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
          if (applyMutation.isPending) {
            return;
          }
          applyMutation.mutate();
        }}
      />
    </>
  );
}

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link, useParams } from 'react-router-dom'
import {
  applyDraw,
  fetchCompetitionOverview,
  fetchStageOverview,
  prepareStage,
  publishDraw,
  startStage,
} from '../api'
import { queryKeys } from '../queryKeys'
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
} from '../ui'
import { drawResolutionKindLabel } from '../i18n/enumLabels'
import {
  type StageDraw,
  type StageOverview,
  type StageRound,
  type StageSlot,
} from '../types'
import { getDrawUiProjection, resolvePairingFixtureIds } from './drawUi'

export function StagePage() {
  const { stageId = '' } = useParams()

  // SERVER STATE: StageOverview lives in TanStack Query — one cache entry for the stage.
  // Draw UI below only reads this result; it never copies draws into useState.
  const stageQuery = useQuery({
    queryKey: queryKeys.stages.detail(stageId),
    queryFn: () => fetchStageOverview(stageId),
    enabled: stageId.length > 0,
  })

  // Same query key as CompetitionPage → cache reuse when navigating Competition → Stage.
  const competitionId = stageQuery.data?.competitionId
  const competitionQuery = useQuery({
    queryKey: queryKeys.competitions.detail(competitionId ?? ''),
    queryFn: () => fetchCompetitionOverview(competitionId!),
    enabled: Boolean(competitionId),
  })

  return (
    <main id="main" className="page">
      <PageHeader
        eyebrow="Stage"
        title={stageQuery.data?.name ?? 'Stage'}
        back={
          competitionId
            ? {
                to: `/competitions/${competitionId}/overview`,
                label: competitionQuery.data?.name
                  ? `Back to ${competitionQuery.data.name}`
                  : 'Back to competition',
              }
            : undefined
        }
        badges={
          stageQuery.data && <StageStatusBadge status={stageQuery.data.status} />
        }
      />

      {stageQuery.isPending && <LoadingState />}
      {stageQuery.isError && <ErrorState error={stageQuery.error} />}
      {stageQuery.data && <StageOverviewView data={stageQuery.data} />}
    </main>
  )
}

function StageOverviewView({ data }: { data: StageOverview }) {
  const queryClient = useQueryClient()
  const fixtureCount = data.rounds.reduce(
    (sum, round) => sum + round.fixtures.length,
    0,
  )

  // UX gate only: Domain still rejects Prepare / Start when status is wrong.
  const canPrepare = data.status === 'Draft'
  const canStart = data.status === 'Ready'

  // useMutation = write on user intent. Server state stays in the stage query.
  const prepareMutation = useMutation({
    mutationFn: () => prepareStage(data.id),
    onSuccess: async () => {
      // Invalidate → active observers refetch → badge shows Ready from GET.
      await queryClient.invalidateQueries({
        queryKey: queryKeys.stages.detail(data.id),
      })
      // CompetitionOverview.stages[].status would stay Draft for staleTime (30s)
      // after Back → Competition; Prepare changes that field, so invalidate it.
      await queryClient.invalidateQueries({
        queryKey: queryKeys.competitions.detail(data.competitionId),
      })
    },
  })

  const startMutation = useMutation({
    mutationFn: () => startStage(data.id),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: queryKeys.stages.detail(data.id),
      })
      // Same field as Prepare: CompetitionPage shows stage status from overview.
      await queryClient.invalidateQueries({
        queryKey: queryKeys.competitions.detail(data.competitionId),
      })
    },
  })

  const stageActionBusy = prepareMutation.isPending || startMutation.isPending
  const stageActionError = prepareMutation.error ?? startMutation.error
  const showStageActions =
    canPrepare || canStart || prepareMutation.isError || startMutation.isError

  return (
    <div className="section-stack">
      <section className="card" aria-labelledby="stage-heading">
        <div className="card__head">
          <h2 className="card__title" id="stage-heading">
            Stage operations
          </h2>
          <span className="id-chip">{data.id}</span>
        </div>

        <div className="button-row">
          {showStageActions && (
            <span className="button-row" aria-busy={stageActionBusy}>
              {canPrepare && (
                <button
                  type="button"
                  className="btn btn--primary"
                  disabled={stageActionBusy}
                  onClick={() => prepareMutation.mutate()}
                >
                  {prepareMutation.isPending ? (
                    <PendingLabel>Preparing stage…</PendingLabel>
                  ) : (
                    'Prepare stage'
                  )}
                </button>
              )}
              {canStart && (
                <button
                  type="button"
                  className="btn btn--primary"
                  disabled={stageActionBusy}
                  onClick={() => startMutation.mutate()}
                >
                  {startMutation.isPending ? (
                    <PendingLabel>Starting stage…</PendingLabel>
                  ) : (
                    'Start stage'
                  )}
                </button>
              )}
            </span>
          )}
          <Link className="btn" to={`/stages/${data.id}/matches`}>
            View matches
          </Link>
        </div>

        {stageActionError && <MutationError error={stageActionError} />}
      </section>

      <section className="card" aria-labelledby="rounds-heading">
        <div className="card__head">
          <h2 className="card__title" id="rounds-heading">
            Rounds ({data.rounds.length})
          </h2>
          <p className="card__subtitle">
            {fixtureCount} fixture{fixtureCount === 1 ? '' : 's'} total · match
            list is a separate read
          </p>
        </div>
        {data.rounds.length === 0 ? (
          <EmptyState title="No rounds defined">
            Preparing the stage creates its rounds and fixtures.
          </EmptyState>
        ) : (
          <ul className="plain-list">
            {data.rounds.map((round) => (
              <li key={round.id}>
                <strong>{round.name}</strong>
                <span className="muted">
                  {' '}
                  · {round.fixtures.length} fixture
                  {round.fixtures.length === 1 ? '' : 's'}
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
                              .map((a) => `leg ${a.legIndex}`)
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

      <section className="card" aria-labelledby="slots-heading">
        <h2 className="card__title" id="slots-heading">
          Slots ({data.slots.length})
        </h2>
        {data.slots.length === 0 ? (
          <EmptyState title="No slots on this stage">
            Slots appear once the structure defines placement positions.
          </EmptyState>
        ) : (
          <ul className="plain-list">
            {data.slots.map((slot) => (
              <li key={slot.slotKey}>
                <code>{slot.slotKey}</code>
                {slot.displayName ? (
                  <> — {slot.displayName}</>
                ) : (
                  <span className="muted"> — empty</span>
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
  )
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
  stageId: string
  draws: StageDraw[]
  slots: StageSlot[]
  rounds: StageRound[]
}) {
  return (
    <section className="card" aria-labelledby="draws-heading">
      <h2 id="draws-heading" className="card__title">
        Draws ({draws.length})
      </h2>
      {draws.length === 0 ? (
        <EmptyState title="No draws yet">
          A draw appears once the stage structure requires one.
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
  )
}

function DrawCard({
  stageId,
  draw,
  slots,
  rounds,
}: {
  stageId: string
  draw: StageDraw
  slots: StageSlot[]
  rounds: StageRound[]
}) {
  // DERIVED UI: computed each render from props (server state), never useState.
  const ui = getDrawUiProjection(draw, slots, rounds)

  return (
    <article className="draw-card">
      <header className="stack stack--tight">
        <h3 className="draw-card__title">
          {drawResolutionKindLabel(draw.kind)} draw
        </h3>
        <p className="badge-row">
          <DrawStatusBadge status={draw.status} />
          <DrawResolutionBadge state={draw.resolutionState} />
          {ui.isApplied && <StatusBadge tone="ok">Applied</StatusBadge>}
        </p>
      </header>

      <p className="draw-card__message" role="status">
        {ui.message}
      </p>

      {ui.showResults && draw.kind === 'Pairing' && draw.pairings.length > 0 && (
        <div className="stack stack--tight">
          <h4 className="draw-card__results-title">Result</h4>
          <ul className="draw-pairing-list">
            {draw.pairings.map((pairing) => (
              <li
                key={`${pairing.entryAId}-${pairing.entryBId}`}
                className="draw-pairing"
              >
                <span className="draw-pairing__side">
                  {pairing.entryADisplayName?.trim() || 'Unknown entry'}
                </span>
                <span className="draw-pairing__vs">vs</span>
                <span className="draw-pairing__side">
                  {pairing.entryBDisplayName?.trim() || 'Unknown entry'}
                </span>
              </li>
            ))}
          </ul>
        </div>
      )}

      {ui.showResults && draw.kind === 'Slot' && draw.slotPlacements.length > 0 && (
        <div className="stack stack--tight">
          <h4 className="draw-card__results-title">Placements</h4>
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
                  {placement.displayName?.trim() || 'Unknown entry'}
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
          Group placements are not shown in this overview yet.
        </p>
      )}

      <DrawActions
        stageId={stageId}
        draw={draw}
        rounds={rounds}
        isApplied={ui.isApplied}
      />
    </article>
  )
}

/**
 * useMutation = “run this write when the user asks”, not “keep this data fresh”.
 * Client state here is only the confirmation gate (window.confirm) — not a copy of the Draw.
 */
function DrawActions({
  stageId,
  draw,
  rounds,
  isApplied,
}: {
  stageId: string
  draw: StageDraw
  rounds: StageRound[]
  isApplied: boolean
}) {
  const queryClient = useQueryClient()

  const canPublish =
    draw.status === 'Draft' && draw.resolutionState === 'Resolved'
  const canApply =
    draw.status === 'Published' &&
    draw.resolutionState === 'Resolved' &&
    !isApplied &&
    (draw.kind === 'Slot' || draw.kind === 'Pairing')

  const pairingFixtureIds =
    draw.kind === 'Pairing' ? resolvePairingFixtureIds(draw, rounds) : null
  const pairingMapBlocked =
    draw.kind === 'Pairing' && canApply && pairingFixtureIds === null

  const publishMutation = useMutation({
    mutationFn: () => publishDraw(stageId, draw.id),
    onSuccess: async () => {
      // invalidateQueries marks cache stale → active queries refetch.
      // Prefer this over refetchQueries: only mounted observers refetch;
      // inactive keys refresh when next used.
      await queryClient.invalidateQueries({
        queryKey: queryKeys.stages.detail(stageId),
      })
    },
  })

  const applyMutation = useMutation({
    mutationFn: () => {
      if (draw.kind === 'Slot') {
        return applyDraw(stageId, draw.id, { fixtureIds: [] })
      }

      if (draw.kind === 'Pairing') {
        const fixtureIds = resolvePairingFixtureIds(draw, rounds)
        if (fixtureIds === null) {
          throw new Error(
            'Cannot apply pairing: fixture count must match pairing count for a 1:1 map.',
          )
        }
        return applyDraw(stageId, draw.id, { fixtureIds })
      }

      throw new Error('Apply is not available for this draw kind.')
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: queryKeys.stages.detail(stageId),
      })
      if (draw.kind === 'Pairing') {
        await queryClient.invalidateQueries({
          queryKey: queryKeys.matches.byStage(stageId),
        })
      }
    },
  })

  const busy = publishMutation.isPending || applyMutation.isPending
  const mutationError = publishMutation.error ?? applyMutation.error

  function handlePublish() {
    publishMutation.mutate()
  }

  function handleApply() {
    // CLIENT STATE: confirmation is local UI intent, not server state.
    // window.confirm is acceptable for this phase — replace with a small
    // accessible dialog later if organizers need richer UX.
    const confirmed = window.confirm(
      'Apply this draw? This will update the stage and may create matches.',
    )
    if (!confirmed) {
      return
    }
    applyMutation.mutate()
  }

  if (!canPublish && !canApply && !pairingMapBlocked && !mutationError) {
    return null
  }

  return (
    <div className="button-row" aria-busy={busy}>
      {canPublish && (
        <button
          type="button"
          className="btn btn--primary btn--sm"
          disabled={busy}
          onClick={handlePublish}
        >
          {publishMutation.isPending ? (
            <PendingLabel>Publishing draw…</PendingLabel>
          ) : (
            'Publish draw'
          )}
        </button>
      )}

      {canApply && draw.kind === 'Slot' && (
        <button
          type="button"
          className="btn btn--primary btn--sm"
          disabled={busy}
          onClick={handleApply}
        >
          {applyMutation.isPending ? (
            <PendingLabel>Applying draw…</PendingLabel>
          ) : (
            'Apply draw'
          )}
        </button>
      )}

      {canApply && draw.kind === 'Pairing' && pairingFixtureIds !== null && (
        <button
          type="button"
          className="btn btn--primary btn--sm"
          disabled={busy}
          onClick={handleApply}
        >
          {applyMutation.isPending ? (
            <PendingLabel>Applying draw…</PendingLabel>
          ) : (
            'Apply draw'
          )}
        </button>
      )}

      {pairingMapBlocked && (
        <p className="notice notice--warning" role="status">
          Apply is unavailable: the number of pairings must equal the number of
          stage fixtures for a 1:1 mapping.
        </p>
      )}

      {mutationError && <MutationError error={mutationError} />}
    </div>
  )
}

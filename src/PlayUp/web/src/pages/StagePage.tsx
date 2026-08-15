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
import {
  BackLink,
  EmptyState,
  ErrorState,
  LoadingState,
  formatError,
} from '../queryUi'
import {
  drawResolutionKindLabel,
  drawResolutionStateLabel,
  drawStatusLabel,
  stageStatusLabel,
  type StageDraw,
  type StageOverview,
  type StageRound,
  type StageSlot,
} from '../types'
import {
  getDrawUiProjection,
  resolvePairingFixtureIds,
} from './drawUi'

export function StagePage() {
  const { stageId = '' } = useParams()

  // SERVER STATE: StageOverview lives in TanStack Query — one cache entry for the stage.
  // Draw UI below only reads this result; it never copies draws into useState.
  const stageQuery = useQuery({
    queryKey: ['stages', stageId],
    queryFn: () => fetchStageOverview(stageId),
    enabled: stageId.length > 0,
  })

  // Same query key as CompetitionPage → cache reuse when navigating Competition → Stage.
  const competitionId = stageQuery.data?.competitionId
  const competitionQuery = useQuery({
    queryKey: ['competitions', competitionId ?? ''],
    queryFn: () => fetchCompetitionOverview(competitionId!),
    enabled: Boolean(competitionId),
  })

  return (
    <main id="main" className="page">
      <header className="page__header">
        <p className="eyebrow">Stage</p>
        <h1>{stageQuery.data?.name ?? 'Stage'}</h1>
        {competitionId && (
          <BackLink to={`/competitions/${competitionId}`}>
            ←{' '}
            {competitionQuery.data?.name
              ? `Back to ${competitionQuery.data.name}`
              : 'Back to competition'}
          </BackLink>
        )}
      </header>

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
  const canPrepare = data.status === 0
  const canStart = data.status === 1

  // useMutation = write on user intent. Server state stays in the stage query.
  const prepareMutation = useMutation({
    mutationFn: () => prepareStage(data.id),
    onSuccess: async () => {
      // Invalidate → active observers refetch → badge shows Ready from GET.
      await queryClient.invalidateQueries({ queryKey: ['stages', data.id] })
      // CompetitionOverview.stages[].status would stay Draft for staleTime (30s)
      // after Back → Competition; Prepare changes that field, so invalidate it.
      await queryClient.invalidateQueries({
        queryKey: ['competitions', data.competitionId],
      })
    },
  })

  const startMutation = useMutation({
    mutationFn: () => startStage(data.id),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['stages', data.id] })
      // Same field as Prepare: CompetitionPage shows stage status from overview.
      await queryClient.invalidateQueries({
        queryKey: ['competitions', data.competitionId],
      })
    },
  })

  const stageActionBusy = prepareMutation.isPending || startMutation.isPending
  const stageActionError = prepareMutation.error ?? startMutation.error
  const showStageActions =
    canPrepare || canStart || prepareMutation.isError || startMutation.isError

  return (
    <article className="panel">
      <header className="panel__header">
        <p className="status-line">
          <span className="status-badge status-badge--neutral">
            <span className="status-badge__dot" aria-hidden="true" />
            {stageStatusLabel[data.status]}
          </span>
        </p>
        <p className="mono muted">{data.id}</p>
        <p>
          <Link className="action-link" to={`/stages/${data.id}/matches`}>
            View matches →
          </Link>
        </p>
        {showStageActions && (
          <div className="stage-actions" aria-busy={stageActionBusy}>
            {canPrepare && (
              <button
                type="button"
                className="btn"
                disabled={stageActionBusy}
                onClick={() => prepareMutation.mutate()}
              >
                {prepareMutation.isPending
                  ? 'Preparing stage…'
                  : 'Prepare stage'}
              </button>
            )}
            {canStart && (
              <button
                type="button"
                className="btn"
                disabled={stageActionBusy}
                onClick={() => startMutation.mutate()}
              >
                {startMutation.isPending ? 'Starting stage…' : 'Start stage'}
              </button>
            )}
            {stageActionError && (
              <p className="error" role="alert">
                {formatError(stageActionError)}
              </p>
            )}
          </div>
        )}
      </header>

      <section>
        <h2 className="section-title">Rounds ({data.rounds.length})</h2>
        {data.rounds.length === 0 ? (
          <EmptyState>No rounds defined.</EmptyState>
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
        <p className="hint">
          {fixtureCount} fixture{fixtureCount === 1 ? '' : 's'} total · match
          list is a separate read.
        </p>
      </section>

      <section>
        <h2 className="section-title">Slots ({data.slots.length})</h2>
        {data.slots.length === 0 ? (
          <EmptyState>No slots on this stage.</EmptyState>
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
    </article>
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
    <section aria-labelledby="draws-heading">
      <h2 id="draws-heading" className="section-title">
        Draws ({draws.length})
      </h2>
      {draws.length === 0 ? (
        <EmptyState>No draws yet.</EmptyState>
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
      <header className="draw-card__header">
        <h3 className="draw-card__title">
          {drawResolutionKindLabel[draw.kind]} draw
        </h3>
        <p className="draw-card__badges">
          <span className={`status-badge status-badge--${ui.statusTone}`}>
            <span className="status-badge__dot" aria-hidden="true" />
            {drawStatusLabel[draw.status]}
          </span>
          <span className="status-badge status-badge--neutral">
            <span className="status-badge__dot" aria-hidden="true" />
            {drawResolutionStateLabel[draw.resolutionState]}
          </span>
          {ui.isApplied && (
            <span className="status-badge status-badge--live">
              <span className="status-badge__dot" aria-hidden="true" />
              Applied
            </span>
          )}
        </p>
      </header>

      <p className="draw-card__message" role="status">
        {ui.message}
      </p>

      {ui.showResults && draw.kind === 2 && draw.pairings.length > 0 && (
        <div className="draw-card__results">
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

      {ui.showResults && draw.kind === 0 && draw.slotPlacements.length > 0 && (
        <div className="draw-card__results">
          <h4 className="draw-card__results-title">Placements</h4>
          <ul className="draw-placement-list">
            {draw.slotPlacements.map((placement) => (
              <li
                key={`${placement.slotKey}-${placement.entryId}`}
                className="draw-placement"
              >
                <code className="draw-placement__slot">{placement.slotKey}</code>
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

      {ui.showResults && draw.kind === 1 && draw.resolutionState === 1 && (
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

  const canPublish = draw.status === 0 && draw.resolutionState === 1
  const canApply =
    draw.status === 1 &&
    draw.resolutionState === 1 &&
    !isApplied &&
    (draw.kind === 0 || draw.kind === 2)

  const pairingFixtureIds =
    draw.kind === 2 ? resolvePairingFixtureIds(draw, rounds) : null
  const pairingMapBlocked = draw.kind === 2 && canApply && pairingFixtureIds === null

  const publishMutation = useMutation({
    mutationFn: () => publishDraw(stageId, draw.id),
    onSuccess: async () => {
      // invalidateQueries marks cache stale → active queries refetch.
      // Prefer this over refetchQueries: only mounted observers refetch;
      // inactive keys refresh when next used.
      await queryClient.invalidateQueries({ queryKey: ['stages', stageId] })
    },
  })

  const applyMutation = useMutation({
    mutationFn: () => {
      if (draw.kind === 0) {
        return applyDraw(stageId, draw.id, { fixtureIds: [] })
      }

      if (draw.kind === 2) {
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
      await queryClient.invalidateQueries({ queryKey: ['stages', stageId] })
      if (draw.kind === 2) {
        await queryClient.invalidateQueries({
          queryKey: ['matches', 'by-stage', stageId],
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
    <div className="draw-actions" aria-busy={busy}>
      {canPublish && (
        <button
          type="button"
          className="btn"
          disabled={busy}
          onClick={handlePublish}
        >
          {publishMutation.isPending ? 'Publishing draw…' : 'Publish draw'}
        </button>
      )}

      {canApply && draw.kind === 0 && (
        <button
          type="button"
          className="btn"
          disabled={busy}
          onClick={handleApply}
        >
          {applyMutation.isPending ? 'Applying draw…' : 'Apply draw'}
        </button>
      )}

      {canApply && draw.kind === 2 && pairingFixtureIds !== null && (
        <button
          type="button"
          className="btn"
          disabled={busy}
          onClick={handleApply}
        >
          {applyMutation.isPending ? 'Applying draw…' : 'Apply draw'}
        </button>
      )}

      {pairingMapBlocked && (
        <p className="hint" role="status">
          Apply is unavailable: the number of pairings must equal the number of
          stage fixtures for a 1:1 mapping.
        </p>
      )}

      {mutationError && (
        <p className="error" role="alert">
          {formatError(mutationError)}
        </p>
      )}
    </div>
  )
}

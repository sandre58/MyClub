import { useQuery } from '@tanstack/react-query'
import { Link, useParams } from 'react-router-dom'
import { fetchCompetitionOverview, fetchStageOverview } from '../api'
import { BackLink, EmptyState, ErrorState, LoadingState } from '../queryUi'
import {
  drawResolutionKindLabel,
  drawResolutionStateLabel,
  drawStatusLabel,
  stageStatusLabel,
  type StageDraw,
  type StageOverview,
  type StageSlot,
} from '../types'
import { getDrawUiProjection } from './drawUi'

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
  const fixtureCount = data.rounds.reduce(
    (sum, round) => sum + round.fixtures.length,
    0,
  )

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

      <DrawSection draws={data.draws} slots={data.slots} />
    </article>
  )
}

/**
 * Composition: a named section keeps StageOverviewView readable.
 * Same page module — not a features/draws layer.
 */
function DrawSection({
  draws,
  slots,
}: {
  draws: StageDraw[]
  slots: StageSlot[]
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
              <DrawCard draw={draw} slots={slots} />
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}

function DrawCard({ draw, slots }: { draw: StageDraw; slots: StageSlot[] }) {
  // DERIVED UI: computed each render from props (server state), never useState.
  const ui = getDrawUiProjection(draw, slots)

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

      <p className="draw-card__message">{ui.message}</p>

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
                <span className="draw-pairing__vs" aria-hidden="true">
                  vs
                </span>
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
    </article>
  )
}

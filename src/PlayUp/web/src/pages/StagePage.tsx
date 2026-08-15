import { useQuery } from '@tanstack/react-query'
import { Link, useParams } from 'react-router-dom'
import { fetchCompetitionOverview, fetchStageOverview } from '../api'
import { BackLink, EmptyState, ErrorState, LoadingState } from '../queryUi'
import {
  drawResolutionKindLabel,
  drawResolutionStateLabel,
  drawStatusLabel,
  stageStatusLabel,
  type StageOverview,
} from '../types'

export function StagePage() {
  const { stageId = '' } = useParams()

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

      <section>
        <h2 className="section-title">Draws ({data.draws.length})</h2>
        {data.draws.length === 0 ? (
          <EmptyState>No draws yet.</EmptyState>
        ) : (
          <ul className="plain-list">
            {data.draws.map((draw) => (
              <li key={draw.id}>
                <strong>{drawResolutionKindLabel[draw.kind]}</strong>{' '}
                <span className="muted">
                  ({drawStatusLabel[draw.status]} ·{' '}
                  {drawResolutionStateLabel[draw.resolutionState]})
                </span>
                {draw.pairings.length > 0 && (
                  <ul className="nested-list">
                    {draw.pairings.map((pairing) => (
                      <li
                        key={`${pairing.entryAId}-${pairing.entryBId}`}
                      >
                        {pairing.entryADisplayName ?? 'Entry A'} vs{' '}
                        {pairing.entryBDisplayName ?? 'Entry B'}
                      </li>
                    ))}
                  </ul>
                )}
                {draw.slotPlacements.length > 0 && (
                  <ul className="nested-list">
                    {draw.slotPlacements.map((placement) => (
                      <li key={`${placement.slotKey}-${placement.entryId}`}>
                        <code>{placement.slotKey}</code> →{' '}
                        {placement.displayName ?? placement.entryId}
                      </li>
                    ))}
                  </ul>
                )}
              </li>
            ))}
          </ul>
        )}
      </section>
    </article>
  )
}

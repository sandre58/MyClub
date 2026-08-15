import { useQuery } from '@tanstack/react-query'
import { Link, useParams } from 'react-router-dom'
import { fetchCompetitionOverview } from '../api'
import { EmptyState, ErrorState, LoadingState } from '../queryUi'
import {
  competitionStatusLabel,
  entryStatusLabel,
  stageStatusLabel,
  type CompetitionOverview,
} from '../types'

/**
 * Route param :competitionId comes from /competitions/:competitionId.
 * useParams reads it; the page does not put business data in the URL.
 */
export function CompetitionPage() {
  const { competitionId = '' } = useParams()

  const query = useQuery({
    queryKey: ['competitions', competitionId],
    queryFn: () => fetchCompetitionOverview(competitionId),
    enabled: competitionId.length > 0,
  })

  return (
    <main id="main" className="page">
      <header className="page__header">
        <p className="eyebrow">Competition</p>
        <h1>{query.data?.name ?? 'Competition'}</h1>
      </header>

      {query.isPending && <LoadingState />}
      {query.isError && <ErrorState error={query.error} />}
      {query.data && <CompetitionOverviewView data={query.data} />}
    </main>
  )
}

function CompetitionOverviewView({ data }: { data: CompetitionOverview }) {
  return (
    <article className="panel">
      <header className="panel__header">
        <p className="status-line">
          <span className="status-badge status-badge--neutral">
            <span className="status-badge__dot" aria-hidden="true" />
            {competitionStatusLabel[data.status]}
          </span>
        </p>
        <p className="mono muted">{data.id}</p>
      </header>

      <section>
        <h2 className="section-title">Entries</h2>
        {data.entries.length === 0 ? (
          <EmptyState>No entries yet.</EmptyState>
        ) : (
          <ul className="plain-list">
            {data.entries.map((entry) => (
              <li key={entry.entryId}>
                {entry.displayName}{' '}
                <span className="muted">
                  ({entryStatusLabel[entry.status]})
                </span>
              </li>
            ))}
          </ul>
        )}
      </section>

      <section>
        <h2 className="section-title">Stages</h2>
        {data.stages.length === 0 ? (
          <EmptyState>No stages in this competition.</EmptyState>
        ) : (
          <ul className="entity-list">
            {data.stages.map((stage) => (
              <li key={stage.stageId} className="entity-list__item">
                <Link
                  className="entity-list__link"
                  to={`/stages/${stage.stageId}`}
                >
                  <span className="entity-list__title">{stage.name}</span>
                  <span className="muted">
                    {stageStatusLabel[stage.status]}
                  </span>
                </Link>
              </li>
            ))}
          </ul>
        )}
      </section>
    </article>
  )
}

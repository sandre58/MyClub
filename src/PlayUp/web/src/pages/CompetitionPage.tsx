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
    <main className="page">
      <header className="page__header">
        <p className="eyebrow">Competition</p>
        <h1>Overview</h1>
        <p className="lede">
          Stages below are real <code>Link</code>s — client navigation, no full
          reload.
        </p>
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
        <h2>{data.name}</h2>
        <p>
          Status: <strong>{competitionStatusLabel[data.status]}</strong>
        </p>
        <p className="mono">{data.id}</p>
      </header>

      <section>
        <h3>Entries</h3>
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
        <h3>Stages</h3>
        {data.stages.length === 0 ? (
          <EmptyState>No stages in this competition.</EmptyState>
        ) : (
          <ul className="link-list">
            {data.stages.map((stage) => (
              <li key={stage.stageId}>
                <Link to={`/stages/${stage.stageId}`}>{stage.name}</Link>{' '}
                <span className="muted">
                  ({stageStatusLabel[stage.status]})
                </span>
              </li>
            ))}
          </ul>
        )}
      </section>
    </article>
  )
}

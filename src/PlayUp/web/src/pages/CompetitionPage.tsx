import { useQuery } from '@tanstack/react-query'
import { Link, useParams } from 'react-router-dom'
import { fetchCompetitionOverview } from '../api'
import {
  CompetitionStatusBadge,
  EmptyState,
  EntryStatusBadge,
  ErrorState,
  LoadingState,
  PageHeader,
  StageStatusBadge,
} from '../ui'
import { type CompetitionOverview } from '../types'

/**
 * Competition Overview — GET /competitions/{id}.
 * Route: /competitions/:competitionId/overview (workspace is the parent hub).
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
      <PageHeader
        eyebrow="Competition"
        title="Stages & entries"
        back={
          competitionId
            ? {
                to: `/competitions/${competitionId}`,
                label: 'Back to workspace',
              }
            : undefined
        }
        badges={
          query.data && <CompetitionStatusBadge status={query.data.status} />
        }
      />

      {query.isPending && <LoadingState />}
      {query.isError && <ErrorState error={query.error} />}
      {query.data && <CompetitionOverviewView data={query.data} />}
    </main>
  )
}

function CompetitionOverviewView({ data }: { data: CompetitionOverview }) {
  return (
    <div className="section-stack">
      <section className="card" aria-labelledby="stages-heading">
        <div className="card__head">
          <h2 className="card__title" id="stages-heading">
            Stages
          </h2>
          <p className="card__subtitle">
            {data.stages.length} stage{data.stages.length === 1 ? '' : 's'}
          </p>
        </div>
        {data.stages.length === 0 ? (
          <EmptyState title="No stages in this competition">
            Configure the structure from the Organisation hub to create one.
          </EmptyState>
        ) : (
          <ul className="row-list">
            {data.stages.map((stage) => (
              <li key={stage.stageId}>
                <Link className="row" to={`/stages/${stage.stageId}`}>
                  <span className="row__main">
                    <span className="row__title">{stage.name}</span>
                  </span>
                  <span className="row__aside">
                    <StageStatusBadge status={stage.status} />
                    <span className="row__chevron" aria-hidden="true">
                      →
                    </span>
                  </span>
                </Link>
              </li>
            ))}
          </ul>
        )}
      </section>

      <section className="card" aria-labelledby="entries-heading">
        <div className="card__head">
          <h2 className="card__title" id="entries-heading">
            Entries
          </h2>
          <p className="card__subtitle">
            {data.entries.length} entr{data.entries.length === 1 ? 'y' : 'ies'}
          </p>
        </div>
        {data.entries.length === 0 ? (
          <EmptyState title="No entries yet">
            Add participants from the Organisation hub.
          </EmptyState>
        ) : (
          <ul className="row-list">
            {data.entries.map((entry) => (
              <li key={entry.entryId} className="entry">
                <span className="entry__name">{entry.displayName}</span>
                <EntryStatusBadge status={entry.status} />
              </li>
            ))}
          </ul>
        )}
      </section>

      <p className="caption">
        Competition <span className="id-chip">{data.id}</span>
      </p>
    </div>
  )
}

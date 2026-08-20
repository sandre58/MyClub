import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { fetchCompetitions } from '../api'
import { queryKeys } from '../queryKeys'
import {
  CompetitionStatusBadge,
  EmptyState,
  ErrorState,
  LoadingState,
  PageHeader,
} from '../ui'
import { type CompetitionListItem } from '../types'

/**
 * Organizer Competition List — GET /competitions.
 * Selecting a row opens that competition’s workspace.
 */
export function CompetitionsPage() {
  const query = useQuery({
    queryKey: queryKeys.competitions.all,
    queryFn: fetchCompetitions,
  })

  return (
    <main id="main" className="page">
      <PageHeader
        eyebrow="Competitions"
        title="Competition list"
        back={{ to: '/', label: 'Back to home' }}
        lede={
          query.data
            ? `${query.data.length} competition${query.data.length === 1 ? '' : 's'} on this Host.`
            : undefined
        }
      />

      {query.isPending && <LoadingState />}
      {query.isError && <ErrorState error={query.error} />}
      {query.data && <CompetitionList items={query.data} />}
    </main>
  )
}

function CompetitionList({ items }: { items: CompetitionListItem[] }) {
  if (items.length === 0) {
    return (
      <EmptyState title="No competitions yet">
        Create a competition on the Host, then refresh this list.
      </EmptyState>
    )
  }

  return (
    <ul className="row-list">
      {items.map((item) => (
        <li key={item.id}>
          <Link className="row" to={`/competitions/${item.id}`}>
            <span className="row__main">
              <span className="row__title">{item.name}</span>
            </span>
            <span className="row__aside">
              <CompetitionStatusBadge status={item.status} />
              <span className="row__chevron" aria-hidden="true">
                →
              </span>
            </span>
          </Link>
        </li>
      ))}
    </ul>
  )
}

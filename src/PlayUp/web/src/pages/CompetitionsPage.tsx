import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { fetchCompetitions } from '../api'
import { BackLink, EmptyState, ErrorState, LoadingState } from '../queryUi'
import {
  competitionStatusLabel,
  type CompetitionListItem,
} from '../types'

/**
 * Organizer Competition List — GET /competitions.
 * Selecting a row opens that competition’s workspace.
 */
export function CompetitionsPage() {
  const query = useQuery({
    queryKey: ['competitions'],
    queryFn: fetchCompetitions,
  })

  return (
    <main id="main" className="page">
      <header className="page__header">
        <p className="eyebrow">Competitions</p>
        <h1>Competition list</h1>
        <BackLink to="/">← Back to home</BackLink>
      </header>

      {query.isPending && <LoadingState />}
      {query.isError && <ErrorState error={query.error} />}
      {query.data && <CompetitionList items={query.data} />}
    </main>
  )
}

function CompetitionList({ items }: { items: CompetitionListItem[] }) {
  if (items.length === 0) {
    return (
      <EmptyState>
        No competitions yet. Create one on the Host, then refresh this list.
      </EmptyState>
    )
  }

  return (
    <ul className="entity-list">
      {items.map((item) => (
        <li key={item.id} className="entity-list__item">
          <Link
            className="entity-list__link"
            to={`/competitions/${item.id}`}
          >
            <span className="entity-list__title">{item.name}</span>
            <span className="muted">
              {competitionStatusLabel[item.status]}
            </span>
          </Link>
        </li>
      ))}
    </ul>
  )
}

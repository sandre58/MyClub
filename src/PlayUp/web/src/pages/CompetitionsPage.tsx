import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
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
  const { t } = useTranslation('competitions')
  const query = useQuery({
    queryKey: queryKeys.competitions.all,
    queryFn: fetchCompetitions,
  })

  return (
    <main id="main" className="page">
      <PageHeader
        eyebrow={t('eyebrow')}
        title={t('title')}
        back={{ to: '/', label: t('back') }}
        lede={
          query.data
            ? t('lede', { count: query.data.length })
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
  const { t } = useTranslation('competitions')

  if (items.length === 0) {
    return (
      <EmptyState title={t('emptyTitle')}>{t('emptyBody')}</EmptyState>
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

import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'
import { fetchCompetitionOverview } from '../api'
import { queryKeys } from '../queryKeys'
import {
  CompetitionStatusBadge,
  EmptyState,
  EntryStatusBadge,
  ErrorState,
  LoadingState,
  PageHeader,
  StageStatusBadge,
} from '../ui'
import { PanelHead } from '../design-system/components/PanelHead'
import { type CompetitionOverview } from '../types'

/**
 * Competition Overview — GET /competitions/{id}.
 * Route: /competitions/:competitionId/overview (workspace is the parent hub).
 */
export function CompetitionPage() {
  const { competitionId = '' } = useParams()
  const { t } = useTranslation('competitions')

  const query = useQuery({
    queryKey: queryKeys.competitions.detail(competitionId),
    queryFn: () => fetchCompetitionOverview(competitionId),
    enabled: competitionId.length > 0,
  })

  return (
    <main id="main" className="page">
      <PageHeader
        eyebrow={t('overview.eyebrow')}
        title={t('overview.title')}
        back={
          competitionId
            ? {
                to: `/competitions/${competitionId}`,
                label: t('overview.back'),
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
  const { t } = useTranslation('competitions')

  return (
    <div className="section-stack">
      <section className="ds-panel" aria-labelledby="stages-heading">
        <PanelHead
          id="stages-heading"
          title={t('overview.stagesHeading')}
          aside={t('overview.stagesSubtitle', { count: data.stages.length })}
        />
        {data.stages.length === 0 ? (
          <EmptyState title={t('overview.stagesEmptyTitle')}>
            {t('overview.stagesEmptyBody')}
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

      <section className="ds-panel" aria-labelledby="entries-heading">
        <PanelHead
          id="entries-heading"
          title={t('overview.entriesHeading')}
          aside={t('overview.entriesSubtitle', { count: data.entries.length })}
        />
        {data.entries.length === 0 ? (
          <EmptyState title={t('overview.entriesEmptyTitle')}>
            {t('overview.entriesEmptyBody')}
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
        {t('overview.caption')} <span className="id-chip">{data.id}</span>
      </p>
    </div>
  )
}

import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'
import { fetchCompetitionWorkspace } from '../api'
import { actionLabel } from '../i18n/actionLabels'
import { completionModeLabel } from '../i18n/enumLabels'
import { queryKeys } from '../queryKeys'
import {
  CompetitionStatusBadge,
  ErrorState,
  LoadingState,
  PageHeader,
} from '../ui'
import { type WorkspaceSummary } from '../types'

/**
 * Competition Workspace — GET /competitions/{id}/workspace.
 * Landing hub after selecting a competition from the list: state first,
 * then where to go, then what is blocking.
 */
export function CompetitionWorkspacePage() {
  const { competitionId = '' } = useParams()
  const { t } = useTranslation('workspace')

  const query = useQuery({
    queryKey: queryKeys.competitions.workspace(competitionId),
    queryFn: () => fetchCompetitionWorkspace(competitionId),
    enabled: competitionId.length > 0,
  })

  return (
    <main id="main" className="page">
      <PageHeader
        eyebrow={t('eyebrow')}
        title={t('title')}
        back={{ to: '/competitions', label: t('back') }}
        badges={
          query.data && <CompetitionStatusBadge status={query.data.status} />
        }
      />

      {query.isPending && <LoadingState />}
      {query.isError && <ErrorState error={query.error} />}
      {query.data && <WorkspaceSummaryView data={query.data} />}
    </main>
  )
}

function WorkspaceSummaryView({ data }: { data: WorkspaceSummary }) {
  const { t } = useTranslation(['workspace', 'enums'])
  const hasAttention = data.attentionCount > 0
  const blockers = data.completionBlockers ?? []

  const nextStepLabel = data.nextActionCode
    ? actionLabel(data.nextActionCode)
    : t('nextStep.none')

  return (
    <div className="section-stack">
      <section className="card" aria-labelledby="workspace-state">
        <div className="card__head">
          <h2 className="card__title" id="workspace-state">
            {t('stateHeading')}
          </h2>
          <span className="id-chip">{data.id}</span>
        </div>

        <div className="stat-grid">
          <div className={`stat${hasAttention ? ' stat--attention' : ''}`}>
            <p className="stat__label">{t('attention.label')}</p>
            <p className="stat__value">
              {hasAttention ? (
                <Link to={`/competitions/${data.id}/matches`}>
                  {data.attentionCount}
                </Link>
              ) : (
                <span className="muted">{data.attentionCount}</span>
              )}
            </p>
            <p className="stat__hint">
              {hasAttention ? t('attention.hintOpen') : t('attention.hintClear')}
            </p>
          </div>

          <div className="stat">
            <p className="stat__label">{t('nextStep.label')}</p>
            <p className="stat__value stat__value--text">{nextStepLabel}</p>
            {data.nextActionCode && (
              <p className="stat__hint">
                <span className="mono">{data.nextActionCode}</span>
              </p>
            )}
          </div>

          <div className={`stat${data.canCompleteNormally ? ' stat--ok' : ''}`}>
            <p className="stat__label">{t('completion.label')}</p>
            <p className="stat__value stat__value--text">
              {data.completionMode
                ? completionModeLabel(data.completionMode)
                : data.canCompleteNormally
                  ? t('completion.ready')
                  : t('completion.notReady')}
            </p>
            <p className="stat__hint">
              {blockers.length > 0
                ? t('completion.blocker', { count: blockers.length })
                : t('completion.noBlockers')}
            </p>
          </div>
        </div>

        {blockers.length > 0 && (
          <div className="stack stack--tight">
            <h3 className="stat__label">{t('completion.blockersHeading')}</h3>
            <ul className="check-list">
              {blockers.map((code) => (
                <li key={code} className="check check--no">
                  <span className="check__mark" aria-hidden="true">
                    !
                  </span>
                  <span>
                    {t(`completionBlocker.${code}`, {
                      ns: 'enums',
                      defaultValue: code,
                    })}{' '}
                    <span className="mono">({code})</span>
                  </span>
                </li>
              ))}
            </ul>
          </div>
        )}
      </section>

      <section className="section-stack" aria-labelledby="workspace-continue">
        <h2 className="card__title" id="workspace-continue">
          {t('continue')}
        </h2>
        <div className="card-grid">
          <Link
            className="nav-card"
            to={`/competitions/${data.id}/organisation`}
          >
            <span className="nav-card__title">
              {t('nav.organisation.title')}
              <span className="row__chevron" aria-hidden="true">
                →
              </span>
            </span>
            <span className="nav-card__desc">{t('nav.organisation.desc')}</span>
          </Link>
          <Link className="nav-card" to={`/competitions/${data.id}/matches`}>
            <span className="nav-card__title">
              {t('nav.matches.title')}
              <span className="row__chevron" aria-hidden="true">
                →
              </span>
            </span>
            <span className="nav-card__desc">{t('nav.matches.desc')}</span>
          </Link>
          <Link className="nav-card" to={`/competitions/${data.id}/overview`}>
            <span className="nav-card__title">
              {t('nav.overview.title')}
              <span className="row__chevron" aria-hidden="true">
                →
              </span>
            </span>
            <span className="nav-card__desc">{t('nav.overview.desc')}</span>
          </Link>
        </div>
      </section>
    </div>
  )
}

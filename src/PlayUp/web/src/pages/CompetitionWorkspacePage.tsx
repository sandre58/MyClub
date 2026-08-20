import { useQuery } from '@tanstack/react-query'
import { Link, useParams } from 'react-router-dom'
import { fetchCompetitionWorkspace } from '../api'
import { queryKeys } from '../queryKeys'
import {
  CompetitionStatusBadge,
  ErrorState,
  LoadingState,
  PageHeader,
} from '../ui'
import { completionModeLabel } from '../i18n/enumLabels'
import { type WorkspaceSummary } from '../types'

/**
 * Competition Workspace — GET /competitions/{id}/workspace.
 * Landing hub after selecting a competition from the list: state first,
 * then where to go, then what is blocking.
 */
export function CompetitionWorkspacePage() {
  const { competitionId = '' } = useParams()

  const query = useQuery({
    queryKey: queryKeys.competitions.workspace(competitionId),
    queryFn: () => fetchCompetitionWorkspace(competitionId),
    enabled: competitionId.length > 0,
  })

  return (
    <main id="main" className="page">
      <PageHeader
        eyebrow="Workspace"
        title="Workspace"
        back={{ to: '/competitions', label: 'Back to competitions' }}
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
  const hasAttention = data.attentionCount > 0
  const blockers = data.completionBlockers ?? []

  return (
    <div className="section-stack">
      <section className="card" aria-labelledby="workspace-state">
        <div className="card__head">
          <h2 className="card__title" id="workspace-state">
            Where this competition stands
          </h2>
          <span className="id-chip">{data.id}</span>
        </div>

        <div className="stat-grid">
          <div className={`stat${hasAttention ? ' stat--attention' : ''}`}>
            <p className="stat__label">Needs attention</p>
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
              {hasAttention
                ? 'Open the hub to resolve them'
                : 'Nothing to resolve right now'}
            </p>
          </div>

          <div className="stat">
            <p className="stat__label">Next step</p>
            <p className="stat__value stat__value--text">
              {data.nextActionLabel ?? 'No suggested step'}
            </p>
            {data.nextActionCode && (
              <p className="stat__hint">
                <span className="mono">{data.nextActionCode}</span>
              </p>
            )}
          </div>

          <div className={`stat${data.canCompleteNormally ? ' stat--ok' : ''}`}>
            <p className="stat__label">Completion</p>
            <p className="stat__value stat__value--text">
              {data.completionMode
                ? completionModeLabel(data.completionMode)
                : data.canCompleteNormally
                  ? 'Ready to complete'
                  : 'Not completable yet'}
            </p>
            <p className="stat__hint">
              {blockers.length > 0
                ? `${blockers.length} blocker${blockers.length === 1 ? '' : 's'}`
                : 'No completion blocker'}
            </p>
          </div>
        </div>

        {blockers.length > 0 && (
          <div className="stack stack--tight">
            <h3 className="stat__label">Completion blockers</h3>
            <ul className="check-list">
              {blockers.map((code) => (
                <li key={code} className="check check--no">
                  <span className="check__mark" aria-hidden="true">
                    !
                  </span>
                  <span className="mono">{code}</span>
                </li>
              ))}
            </ul>
          </div>
        )}
      </section>

      <section className="section-stack" aria-labelledby="workspace-continue">
        <h2 className="card__title" id="workspace-continue">
          Continue
        </h2>
        <div className="card-grid">
          <Link
            className="nav-card"
            to={`/competitions/${data.id}/organisation`}
          >
            <span className="nav-card__title">
              Organisation
              <span className="row__chevron" aria-hidden="true">
                →
              </span>
            </span>
            <span className="nav-card__desc">
              Participants, regulation and structure of the competition.
            </span>
          </Link>
          <Link className="nav-card" to={`/competitions/${data.id}/matches`}>
            <span className="nav-card__title">
              Match hub
              <span className="row__chevron" aria-hidden="true">
                →
              </span>
            </span>
            <span className="nav-card__desc">
              Attention items and every match of the competition.
            </span>
          </Link>
          <Link className="nav-card" to={`/competitions/${data.id}/overview`}>
            <span className="nav-card__title">
              Stages &amp; entries
              <span className="row__chevron" aria-hidden="true">
                →
              </span>
            </span>
            <span className="nav-card__desc">
              Read-only overview of stages and registered entries.
            </span>
          </Link>
        </div>
      </section>
    </div>
  )
}

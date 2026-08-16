import { useQuery } from '@tanstack/react-query'
import { Link, useParams } from 'react-router-dom'
import { fetchCompetitionWorkspace } from '../api'
import { BackLink, ErrorState, LoadingState } from '../queryUi'
import {
  competitionStatusLabel,
  completionModeLabel,
  type WorkspaceSummary,
} from '../types'

/**
 * Competition Workspace — GET /competitions/{id}/workspace.
 * Landing hub after selecting a competition from the list.
 */
export function CompetitionWorkspacePage() {
  const { competitionId = '' } = useParams()

  const query = useQuery({
    queryKey: ['competitions', competitionId, 'workspace'],
    queryFn: () => fetchCompetitionWorkspace(competitionId),
    enabled: competitionId.length > 0,
  })

  return (
    <main id="main" className="page">
      <header className="page__header">
        <p className="eyebrow">Workspace</p>
        <h1>{query.data?.name ?? 'Competition'}</h1>
        <BackLink to="/competitions">← Back to competitions</BackLink>
      </header>

      {query.isPending && <LoadingState />}
      {query.isError && <ErrorState error={query.error} />}
      {query.data && <WorkspaceSummaryView data={query.data} />}
    </main>
  )
}

function WorkspaceSummaryView({ data }: { data: WorkspaceSummary }) {
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
        <h2 className="section-title">Summary</h2>
        <ul className="plain-list">
          <li>
            Attention items:{' '}
            <span className="muted">{data.attentionCount}</span>
          </li>
          {data.nextActionLabel && (
            <li>
              Next step:{' '}
              <span className="muted">{data.nextActionLabel}</span>
              {data.nextActionCode && (
                <span className="mono muted"> ({data.nextActionCode})</span>
              )}
            </li>
          )}
          {data.completionMode && (
            <li>
              Completion mode:{' '}
              <span className="muted">
                {completionModeLabel[data.completionMode]}
              </span>
            </li>
          )}
          {data.canCompleteNormally && (
            <li>
              <span className="muted">Ready for normal completion</span>
            </li>
          )}
        </ul>
        {data.completionBlockers && data.completionBlockers.length > 0 && (
          <>
            <h3 className="section-title">Completion blockers</h3>
            <ul className="plain-list">
              {data.completionBlockers.map((code) => (
                <li key={code} className="mono muted">
                  {code}
                </li>
              ))}
            </ul>
          </>
        )}
      </section>

      <section>
        <h2 className="section-title">Continue</h2>
        <ul className="entity-list">
          <li className="entity-list__item">
            <Link
              className="entity-list__link"
              to={`/competitions/${data.id}/organisation`}
            >
              <span className="entity-list__title">Organisation</span>
              <span className="muted">
                Participants, regulation, structure
              </span>
            </Link>
          </li>
          <li className="entity-list__item">
            <Link
              className="entity-list__link"
              to={`/competitions/${data.id}/overview`}
            >
              <span className="entity-list__title">
                Stages &amp; entries
              </span>
              <span className="muted">Competition overview</span>
            </Link>
          </li>
        </ul>
      </section>
    </article>
  )
}

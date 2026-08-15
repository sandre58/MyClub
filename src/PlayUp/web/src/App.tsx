import { useQuery } from '@tanstack/react-query'
import { Navigate, Route, Routes, useParams } from 'react-router-dom'
import { ApiError, fetchCompetitionOverview } from './api'
import {
  competitionStatusLabel,
  entryStatusLabel,
  stageStatusLabel,
  type CompetitionOverview,
} from './types'

const seedCompetitionId = import.meta.env.VITE_SEED_COMPETITION_ID as
  | string
  | undefined

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<HomePage />} />
      <Route path="/competitions/:competitionId" element={<CompetitionPage />} />
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  )
}

function HomePage() {
  if (seedCompetitionId) {
    return <Navigate to={`/competitions/${seedCompetitionId}`} replace />
  }

  return (
    <main className="page">
      <header className="page__header">
        <p className="eyebrow">Play’up · Organizer</p>
        <h1>Choose a competition</h1>
        <p className="lede">
          Set <code>VITE_SEED_COMPETITION_ID</code> in <code>.env.local</code>,
          or open <code>/competitions/&lt;guid&gt;</code> directly.
        </p>
      </header>
    </main>
  )
}

function CompetitionPage() {
  const { competitionId = '' } = useParams()

  const query = useQuery({
    queryKey: ['competitions', competitionId],
    queryFn: () => fetchCompetitionOverview(competitionId),
    enabled: competitionId.length > 0,
  })

  return (
    <main className="page">
      <header className="page__header">
        <p className="eyebrow">Play’up · Organizer</p>
        <h1>Competition overview</h1>
        <p className="lede">
          Browser → Router → TanStack Query → fetch → Vite proxy → Host API
        </p>
      </header>

      {query.isPending && (
        <p className="hint" role="status">
          Loading…
        </p>
      )}

      {query.isError && (
        <p className="error" role="alert">
          {formatError(query.error)}
        </p>
      )}

      {query.data && <CompetitionOverviewView data={query.data} />}
    </main>
  )
}

function CompetitionOverviewView({ data }: { data: CompetitionOverview }) {
  return (
    <article className="overview">
      <header>
        <h2>{data.name}</h2>
        <p>
          Status: <strong>{competitionStatusLabel[data.status]}</strong>
        </p>
        <p className="mono">{data.id}</p>
      </header>

      <section>
        <h3>Entries</h3>
        <ul>
          {data.entries.map((entry) => (
            <li key={entry.entryId}>
              {entry.displayName}{' '}
              <span className="muted">({entryStatusLabel[entry.status]})</span>
            </li>
          ))}
        </ul>
      </section>

      <section>
        <h3>Stages</h3>
        <ul>
          {data.stages.map((stage) => (
            <li key={stage.stageId}>
              {stage.name}{' '}
              <span className="muted">({stageStatusLabel[stage.status]})</span>
            </li>
          ))}
        </ul>
      </section>
    </article>
  )
}

function NotFoundPage() {
  return (
    <main className="page">
      <h1>Page not found</h1>
      <p className="hint">Unknown route.</p>
    </main>
  )
}

function formatError(error: unknown): string {
  if (error instanceof ApiError) {
    return `${error.message} (${error.status})`
  }

  if (error instanceof Error) {
    return error.message
  }

  return 'Unknown error'
}

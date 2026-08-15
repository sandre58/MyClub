import { Navigate } from 'react-router-dom'

const seedCompetitionId = import.meta.env.VITE_SEED_COMPETITION_ID as
  | string
  | undefined

export function HomePage() {
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
      <p className="hint">
        Deep links also work for <code>/stages/:id</code>,{' '}
        <code>/stages/:id/matches</code>, and <code>/matches/:id</code>.
      </p>
    </main>
  )
}

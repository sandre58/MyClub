import { Link } from 'react-router-dom'

const seedCompetitionId = import.meta.env.VITE_SEED_COMPETITION_ID as
  | string
  | undefined

/**
 * Product entry — Accueil.
 * Primary path is Competition List; seed remains an optional local shortcut.
 */
export function HomePage() {
  return (
    <main id="main" className="page">
      <header className="page__header">
        <p className="eyebrow">Play’up · Organizer</p>
        <h1>Welcome</h1>
        <p className="lede">
          Open your competitions, pick one, and continue from its workspace.
        </p>
      </header>

      <ul className="entity-list">
        <li className="entity-list__item">
          <Link className="entity-list__link" to="/competitions">
            <span className="entity-list__title">Competition list</span>
            <span className="muted">Browse competitions on this Host</span>
          </Link>
        </li>
        {seedCompetitionId && (
          <li className="entity-list__item">
            <Link
              className="entity-list__link"
              to={`/competitions/${seedCompetitionId}`}
            >
              <span className="entity-list__title">Seed competition</span>
              <span className="muted">Dev shortcut from .env.local</span>
            </Link>
          </li>
        )}
      </ul>

      <p className="hint">
        Deep links also work for <code>/stages/:id</code>,{' '}
        <code>/stages/:id/matches</code>, and <code>/matches/:id</code>.
      </p>
    </main>
  )
}

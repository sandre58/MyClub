import { Link } from 'react-router-dom'
import { PageHeader } from '../ui'

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
      <PageHeader
        eyebrow="Play’up · Organizer"
        title="Welcome"
        lede="Open a competition to prepare its organisation, follow the matches that need you, and record results."
      />

      <div className="card-grid">
        <Link className="nav-card" to="/competitions">
          <span className="nav-card__title">
            Competition list
            <span className="row__chevron" aria-hidden="true">
              →
            </span>
          </span>
          <span className="nav-card__desc">
            Browse every competition on this Host and open its workspace.
          </span>
        </Link>
        {seedCompetitionId && (
          <Link className="nav-card" to={`/competitions/${seedCompetitionId}`}>
            <span className="nav-card__title">
              Seed competition
              <span className="row__chevron" aria-hidden="true">
                →
              </span>
            </span>
            <span className="nav-card__desc">
              Development shortcut configured in <code>.env.local</code>.
            </span>
          </Link>
        )}
      </div>

      <p className="caption">
        Deep links also work for <code>/stages/:id</code>,{' '}
        <code>/stages/:id/matches</code>, and <code>/matches/:id</code>.
      </p>
    </main>
  )
}

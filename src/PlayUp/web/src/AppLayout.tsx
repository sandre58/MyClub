import { Link, Outlet } from 'react-router-dom'

/**
 * Shared shell for organizer screens.
 *
 * Outlet = “render the matched child route here”.
 * Without it, nested routes would have nowhere to appear.
 */
export function AppLayout() {
  return (
    <div className="app">
      <a className="skip-link" href="#main">
        Skip to content
      </a>
      <header className="app-header">
        <nav className="app-nav" aria-label="Primary">
          <Link to="/" className="app-nav__brand">
            Play’up
          </Link>
          <Link to="/competitions" className="app-nav__meta">
            Competitions
          </Link>
        </nav>
      </header>
      <Outlet />
    </div>
  )
}

import { Link, Outlet } from 'react-router-dom'

/**
 * Shared shell for organizer screens.
 *
 * Outlet = “render the matched child route here”.
 * Without it, nested routes would have nowhere to appear.
 * Client navigation (Link) updates the URL and swaps the Outlet content
 * without a full browser document reload.
 */
export function AppLayout() {
  return (
    <div className="app">
      <header className="app-header">
        <nav className="app-nav" aria-label="Primary">
          <Link to="/" className="app-nav__brand">
            Play’up
          </Link>
          <span className="app-nav__meta">Organizer</span>
        </nav>
      </header>
      <Outlet />
    </div>
  )
}

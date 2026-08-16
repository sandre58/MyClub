import { Link, NavLink, Outlet } from 'react-router-dom'

/**
 * Shared shell for organizer screens.
 *
 * Outlet = “render the matched child route here”.
 * Without it, nested routes would have nowhere to appear.
 *
 * Top bar rather than a sidebar: the app has two global destinations,
 * everything else is competition-scoped and navigated from inside a
 * competition (see the context nav on competition pages).
 */
export function AppLayout() {
  return (
    <div className="app">
      <a className="skip-link" href="#main">
        Skip to content
      </a>
      <header className="app-header">
        <div className="app-header__inner">
          <Link to="/" className="brand">
            <span className="brand__mark" aria-hidden="true">
              P
            </span>
            Play’up
          </Link>
          <nav className="app-nav" aria-label="Primary">
            <NavLink to="/" end className="app-nav__link">
              Home
            </NavLink>
            <NavLink to="/competitions" className="app-nav__link">
              Competitions
            </NavLink>
          </nav>
        </div>
      </header>
      <Outlet />
    </div>
  )
}

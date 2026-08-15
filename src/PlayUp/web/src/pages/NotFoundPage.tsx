import { Link } from 'react-router-dom'

export function NotFoundPage() {
  return (
    <main id="main" className="page">
      <header className="page__header">
        <p className="eyebrow">Error</p>
        <h1>Page not found</h1>
        <p className="lede">This route does not exist in the organizer app.</p>
      </header>
      <p>
        <Link className="action-link" to="/">
          ← Back to home
        </Link>
      </p>
    </main>
  )
}

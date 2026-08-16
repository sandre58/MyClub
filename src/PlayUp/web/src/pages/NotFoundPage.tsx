import { Link } from 'react-router-dom'
import { PageHeader } from '../ui'

export function NotFoundPage() {
  return (
    <main id="main" className="page page--narrow">
      <PageHeader
        eyebrow="Error 404"
        title="Page not found"
        lede="This route does not exist in the organizer app."
      />
      <p>
        <Link className="btn btn--primary" to="/">
          Back to home
        </Link>
      </p>
    </main>
  )
}

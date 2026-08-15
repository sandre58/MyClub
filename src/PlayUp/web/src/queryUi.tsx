import { Link } from 'react-router-dom'
import type { ReactNode } from 'react'
import { ApiError } from './api'

/** Shared loading / error / empty chrome used by every read page. */
export function LoadingState({ label = 'Loading…' }: { label?: string }) {
  return (
    <p className="hint" role="status">
      {label}
    </p>
  )
}

export function ErrorState({ error }: { error: unknown }) {
  const notFound = error instanceof ApiError && error.status === 404

  return (
    <p className="error" role="alert">
      {notFound ? 'Not found. Check the id in the URL.' : formatError(error)}
    </p>
  )
}

export function EmptyState({ children }: { children: ReactNode }) {
  return <p className="hint">{children}</p>
}

export function BackLink({ to, children }: { to: string; children: ReactNode }) {
  return (
    <p className="back">
      <Link to={to}>{children}</Link>
    </p>
  )
}

export function formatError(error: unknown): string {
  if (error instanceof ApiError) {
    return `${error.message} (${error.status})`
  }

  if (error instanceof Error) {
    return error.message
  }

  return 'Unknown error'
}

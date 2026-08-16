import { Link } from 'react-router-dom'
import type { ReactNode } from 'react'
import { ApiError } from './api'
import type { MatchStatus } from './types'
import { matchStatusLabel } from './types'

/** Shared loading / error / empty chrome used by every read page. */
export function LoadingState({ label = 'Loading…' }: { label?: string }) {
  return (
    <p className="hint" role="status" aria-live="polite">
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
  return (
    <p className="hint" role="status">
      {children}
    </p>
  )
}

export function BackLink({ to, children }: { to: string; children: ReactNode }) {
  return (
    <p className="back">
      <Link to={to}>{children}</Link>
    </p>
  )
}

/** Compact status chip shared by match list and match detail. */
export function MatchStatusBadge({ status }: { status: MatchStatus }) {
  return (
    <span
      className={`status-badge status-badge--${matchStatusTone(status)}`}
    >
      <span className="status-badge__dot" aria-hidden="true" />
      {matchStatusLabel[status]}
    </span>
  )
}

export function matchStatusTone(
  status: MatchStatus,
): 'scheduled' | 'live' | 'finished' | 'other' {
  switch (status) {
    case 'Scheduled':
      return 'scheduled'
    case 'Live':
      return 'live'
    case 'Finished':
      return 'finished'
    default:
      return 'other'
  }
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

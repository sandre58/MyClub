import { Link, NavLink } from 'react-router-dom'
import type { ReactNode } from 'react'
import { ApiError } from './api'
import type {
  CompetitionStatus,
  DrawResolutionState,
  DrawStatus,
  EntryStatus,
  MatchStatus,
  StageStatus,
} from './types'
import {
  competitionStatusLabel,
  drawResolutionStateLabel,
  drawStatusLabel,
  entryStatusLabel,
  matchStatusLabel,
  stageStatusLabel,
} from './types'

/**
 * Shared page chrome: header, navigation, status badges and the
 * loading / error / empty / pending states every read page needs.
 *
 * Everything here is reused by at least two screens — a class alone is
 * not a reason to add a component.
 */

/** Visual meaning of a state, shared by every status family. */
export type StatusTone =
  | 'neutral'
  | 'info'
  | 'ok'
  | 'live'
  | 'done'
  | 'warn'
  | 'danger'

export function PageHeader({
  eyebrow,
  title,
  back,
  badges,
  lede,
  actions,
}: {
  eyebrow: string
  title: string
  back?: { to: string; label: string }
  badges?: ReactNode
  lede?: ReactNode
  actions?: ReactNode
}) {
  return (
    <header className="page-header">
      {back && (
        <p className="page-header__top">
          <BackLink to={back.to}>{back.label}</BackLink>
        </p>
      )}
      <p className="eyebrow">{eyebrow}</p>
      <div className="page-header__title-row">
        <h1 className="page-title">{title}</h1>
        {badges}
        {actions && <div className="row__aside">{actions}</div>}
      </div>
      {lede && <p className="lede">{lede}</p>}
    </header>
  )
}

export function BackLink({
  to,
  children,
}: {
  to: string
  children: ReactNode
}) {
  return (
    <Link className="back-link" to={to}>
      <span aria-hidden="true">←</span>
      {children}
    </Link>
  )
}

/**
 * Lateral navigation inside one competition.
 * Rendered on the sub-screens; the workspace itself is the hub they return to.
 */
export function CompetitionNav({
  competitionId,
  current,
}: {
  competitionId: string
  current: 'organisation' | 'matches' | 'overview'
}) {
  const items = [
    { key: 'workspace', label: 'Workspace', to: `/competitions/${competitionId}` },
    {
      key: 'organisation',
      label: 'Organisation',
      to: `/competitions/${competitionId}/organisation`,
    },
    {
      key: 'matches',
      label: 'Match hub',
      to: `/competitions/${competitionId}/matches`,
    },
    {
      key: 'overview',
      label: 'Stages & entries',
      to: `/competitions/${competitionId}/overview`,
    },
  ]

  return (
    <nav className="context-nav" aria-label="Competition sections">
      {items.map((item) =>
        item.key === current ? (
          <span
            key={item.key}
            className="context-nav__item context-nav__item--current"
            aria-current="page"
          >
            {item.label}
          </span>
        ) : (
          <NavLink key={item.key} to={item.to} className="context-nav__item" end>
            {item.label}
          </NavLink>
        ),
      )}
    </nav>
  )
}

export function StatusBadge({
  tone,
  children,
}: {
  tone: StatusTone
  children: ReactNode
}) {
  return (
    <span className={`status-badge status-badge--${tone}`}>
      <span className="status-badge__dot" aria-hidden="true" />
      {children}
    </span>
  )
}

export function CompetitionStatusBadge({
  status,
}: {
  status: CompetitionStatus
}) {
  return (
    <StatusBadge tone={competitionStatusTone[status]}>
      {competitionStatusLabel[status]}
    </StatusBadge>
  )
}

export function StageStatusBadge({ status }: { status: StageStatus }) {
  return (
    <StatusBadge tone={stageStatusTone[status]}>
      {stageStatusLabel[status]}
    </StatusBadge>
  )
}

export function MatchStatusBadge({ status }: { status: MatchStatus }) {
  return (
    <StatusBadge tone={matchStatusTone[status]}>
      {matchStatusLabel[status]}
    </StatusBadge>
  )
}

export function EntryStatusBadge({ status }: { status: EntryStatus }) {
  return (
    <StatusBadge tone={entryStatusTone[status]}>
      {entryStatusLabel[status]}
    </StatusBadge>
  )
}

export function DrawStatusBadge({ status }: { status: DrawStatus }) {
  return (
    <StatusBadge tone={drawStatusTone[status]}>
      {drawStatusLabel[status]}
    </StatusBadge>
  )
}

export function DrawResolutionBadge({
  state,
}: {
  state: DrawResolutionState
}) {
  return (
    <StatusBadge tone={drawResolutionTone[state]}>
      {drawResolutionStateLabel[state]}
    </StatusBadge>
  )
}

/** One colour vocabulary for every status family across the app. */
const competitionStatusTone: Record<CompetitionStatus, StatusTone> = {
  Draft: 'neutral',
  Ready: 'info',
  Running: 'live',
  Suspended: 'warn',
  Completed: 'done',
  Archived: 'neutral',
}

const stageStatusTone: Record<StageStatus, StatusTone> = {
  Draft: 'neutral',
  Ready: 'info',
  Running: 'live',
  Suspended: 'warn',
  Completed: 'done',
}

const matchStatusTone: Record<MatchStatus, StatusTone> = {
  Scheduled: 'neutral',
  Live: 'live',
  Finished: 'done',
  Postponed: 'warn',
  Cancelled: 'danger',
}

const entryStatusTone: Record<EntryStatus, StatusTone> = {
  Active: 'ok',
  Qualified: 'info',
  Eliminated: 'done',
  Withdrawn: 'warn',
  Excluded: 'danger',
}

const drawStatusTone: Record<DrawStatus, StatusTone> = {
  Draft: 'neutral',
  Published: 'info',
  Cancelled: 'danger',
}

const drawResolutionTone: Record<DrawResolutionState, StatusTone> = {
  NotResolved: 'neutral',
  Resolved: 'ok',
  NoSolution: 'danger',
}

export function LoadingState({ label = 'Loading…' }: { label?: string }) {
  return (
    <p className="loading-state" role="status" aria-live="polite">
      <span className="spinner" aria-hidden="true" />
      {label}
    </p>
  )
}

export function ErrorState({ error }: { error: unknown }) {
  const notFound = error instanceof ApiError && error.status === 404

  return (
    <p className="notice notice--danger" role="alert">
      {notFound ? 'Not found. Check the id in the URL.' : formatError(error)}
    </p>
  )
}

/** Inline failure of a write, next to the action that failed. */
export function MutationError({ error }: { error: unknown }) {
  return (
    <p className="notice notice--danger" role="alert">
      {formatError(error)}
    </p>
  )
}

/**
 * Empty lists are product states, not blanks: say what is missing and,
 * when the Host really exposes one, what the organizer can do next.
 */
export function EmptyState({
  title,
  children,
  action,
}: {
  title?: string
  children: ReactNode
  action?: ReactNode
}) {
  return (
    <div className="empty-state">
      {title && <p className="empty-state__title">{title}</p>}
      <p className="empty-state__body">{children}</p>
      {action}
    </div>
  )
}

/** Spinner + label inside a button while its mutation runs. */
export function PendingLabel({ children }: { children: ReactNode }) {
  return (
    <>
      <span className="spinner" aria-hidden="true" />
      {children}
    </>
  )
}

function formatError(error: unknown): string {
  if (error instanceof ApiError) {
    return `${error.message} (${error.status})`
  }

  if (error instanceof Error) {
    return error.message
  }

  return 'Unknown error'
}

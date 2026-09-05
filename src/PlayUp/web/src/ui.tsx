import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import type { ReactNode } from 'react'
import { ApiError } from './api'
import { apiErrorLabel } from './i18n/apiErrorLabel'
import { Status, statusToneFromLegacy } from './design-system/components/Status'
import {
  WaitMark,
  type WaitSize,
} from './design-system/components/WaitMark'
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
} from './i18n/enumLabels'

/**
 * Shared page primitives: header, status badges and the loading / error /
 * empty / pending states every read page needs.
 *
 * PageHeader expresses page title and local context only — global navigation
 * and competition context live in the Shell (14.6).
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
    <header className="ds-admin-page-head">
      {back && (
        <p className="ds-admin-page-head__top">
          <BackLink to={back.to}>{back.label}</BackLink>
        </p>
      )}
      <p className="ds-eyebrow">{eyebrow}</p>
      <div className="ds-admin-page-head__title-row">
        <h1 className="ds-admin-page-head__title">{title}</h1>
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
    <Link className="ds-back-link" to={to}>
      <span aria-hidden="true">←</span>
      {children}
    </Link>
  )
}

export function StatusBadge({
  tone,
  children,
  variant = 'soft',
  shape = 'rounded',
  density = 'context',
}: {
  tone: StatusTone
  children: ReactNode
  variant?: 'soft' | 'outline'
  shape?: 'rounded' | 'pill'
  density?: 'context' | 'compact'
}) {
  return (
    <Status
      density={density}
      tone={statusToneFromLegacy(tone)}
      variant={variant}
      shape={shape}
    >
      {children}
    </Status>
  )
}

export function CompetitionStatusBadge({
  status,
  density = 'context',
}: {
  status: CompetitionStatus
  density?: 'context' | 'compact'
}) {
  return (
    <StatusBadge density={density} tone={competitionStatusTone[status]}>
      {competitionStatusLabel(status)}
    </StatusBadge>
  )
}

export function StageStatusBadge({ status }: { status: StageStatus }) {
  return (
    <StatusBadge tone={stageStatusTone[status]}>
      {stageStatusLabel(status)}
    </StatusBadge>
  )
}

export function MatchStatusBadge({ status }: { status: MatchStatus }) {
  return (
    <StatusBadge tone={matchStatusTone[status]}>
      {matchStatusLabel(status)}
    </StatusBadge>
  )
}

export function EntryStatusBadge({ status }: { status: EntryStatus }) {
  return (
    <StatusBadge tone={entryStatusTone[status]}>
      {entryStatusLabel(status)}
    </StatusBadge>
  )
}

export function DrawStatusBadge({ status }: { status: DrawStatus }) {
  return (
    <StatusBadge tone={drawStatusTone[status]}>
      {drawStatusLabel(status)}
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
      {drawResolutionStateLabel(state)}
    </StatusBadge>
  )
}

/** One colour vocabulary for every status family across the app. */
const competitionStatusTone: Record<CompetitionStatus, StatusTone> = {
  Draft: 'info',
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
  Withdrawn: 'warn',
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

export function LoadingState({
  label,
  size = 'page',
}: {
  label?: string
  size?: WaitSize
}) {
  const { t } = useTranslation('common')
  const text = label ?? t('loading')

  return <WaitMark size={size}>{text}</WaitMark>
}

export function ErrorState({ error }: { error: unknown }) {
  const { t } = useTranslation('common')
  const notFound = error instanceof ApiError && error.status === 404

  return (
    <p className="ds-notice ds-notice--danger" role="alert">
      {notFound ? t('notFound') : formatError(error, t)}
    </p>
  )
}

/** Inline failure of a write, next to the action that failed. */
export function MutationError({ error }: { error: unknown }) {
  const { t } = useTranslation('common')

  return (
    <p className="ds-notice ds-notice--danger" role="alert">
      {formatError(error, t)}
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
    <div className="ds-empty">
      {title && <p className="ds-empty__title">{title}</p>}
      <p className="ds-empty__body">{children}</p>
      {action}
    </div>
  )
}

/** Spinner + label inside a button while its mutation runs. */
export function PendingLabel({ children }: { children: ReactNode }) {
  return (
    <>
      <span className="ds-spinner" aria-hidden="true" />
      {children}
    </>
  )
}

function formatError(
  error: unknown,
  t: (key: string, options?: Record<string, unknown>) => string,
): string {
  if (error instanceof ApiError) {
    const localized = apiErrorLabel(error)
    return t('errorWithStatus', {
      message: localized ?? error.detail ?? error.message,
      status: error.status,
    })
  }

  if (error instanceof Error) {
    return error.message
  }

  return t('unknownError')
}

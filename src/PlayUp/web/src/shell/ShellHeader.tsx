import type { SVGProps } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { fetchNeedsAttention } from '../api'
import { useShellCompetitionContext } from './useShellCompetitionContext'

type ShellHeaderProps = {
  /** Reserved for 14.6.4 — opens the attention drawer when implemented. */
  onAttentionClick?: () => void
}

/**
 * Shell header (14.6.3) — global context, attention trigger, shell actions.
 * Business navigation stays in the Sidebar; no drawer in this phase.
 */
export function ShellHeader({ onAttentionClick }: ShellHeaderProps) {
  const { competitionId, competitionName, state } = useShellCompetitionContext()

  const attentionQuery = useQuery({
    queryKey: ['competitions', competitionId ?? '', 'attention'],
    queryFn: () => fetchNeedsAttention(competitionId!),
    enabled: Boolean(competitionId),
  })

  const attentionCount =
    attentionQuery.data?.count ?? attentionQuery.data?.items.length ?? 0

  return (
    <header className="shell-header">
      <div className="shell-header__identity">
        <span className="shell-header__wordmark">PLAY&apos;UP</span>
        <span className="shell-header__separator" aria-hidden="true">
          ·
        </span>
        <ShellHeaderCompetitionContext
          competitionId={competitionId}
          competitionName={competitionName}
          state={state}
        />
      </div>

      <div className="shell-header__actions">
        <AttentionTrigger
          count={attentionCount}
          onClick={onAttentionClick}
        />
        <button
          type="button"
          className="ds-btn ds-btn--ghost ds-icon-button shell-header__icon-action"
          aria-label="Paramètres"
          disabled
        >
          <SettingsIcon aria-hidden="true" />
        </button>
        <button
          type="button"
          className="ds-btn ds-btn--ghost shell-header__user-action"
          aria-label="Utilisateur"
          disabled
        >
          <UserIcon className="shell-header__user-icon" aria-hidden="true" />
          <span className="shell-header__user-label">Utilisateur</span>
        </button>
      </div>
    </header>
  )
}

function ShellHeaderCompetitionContext({
  competitionId,
  competitionName,
  state,
}: {
  competitionId?: string
  competitionName?: string
  state: ReturnType<typeof useShellCompetitionContext>['state']
}) {
  if (state === 'loading') {
    return (
      <div className="shell-header__context" aria-label="Competition context">
        <span className="shell-header__context-loading">…</span>
      </div>
    )
  }

  if (state === 'selected' && competitionId) {
    return (
      <div className="shell-header__context" aria-label="Competition context">
        <span className="shell-header__competition-name">
          {competitionName ?? 'Compétition'}
        </span>
        <Link
          className="ds-btn ds-btn--ghost shell-header__change"
          to="/competitions"
        >
          Changer
        </Link>
      </div>
    )
  }

  if (state === 'empty') {
    return (
      <div className="shell-header__context" aria-label="Competition context">
        <span className="shell-header__context-message">
          Aucune compétition — créez-en une sur le Host
        </span>
        <Link
          className="ds-btn ds-btn--ghost shell-header__change"
          to="/competitions"
        >
          Liste
        </Link>
      </div>
    )
  }

  return (
    <div className="shell-header__context" aria-label="Competition context">
      <span className="shell-header__context-message">
        Choisir une compétition
      </span>
      <Link
        className="ds-btn ds-btn--ghost shell-header__change"
        to="/competitions"
      >
        Changer
      </Link>
    </div>
  )
}

function AttentionTrigger({
  count,
  onClick,
}: {
  count: number
  onClick?: () => void
}) {
  const hasAttention = count > 0
  const accessibleLabel =
    count === 0
      ? 'À traiter, aucun élément'
      : count === 1
        ? 'À traiter, 1 élément'
        : `À traiter, ${count} éléments`

  return (
    <button
      type="button"
      className={`shell-header__attention ds-btn ds-btn--ghost${
        hasAttention ? ' shell-header__attention--active' : ''
      }`}
      aria-label={accessibleLabel}
      onClick={onClick}
    >
      <span
        className={`ds-state${
          hasAttention ? ' ds-state--attention' : ' ds-state--neutral'
        }`}
      >
        <span className="ds-state__figure">{count}</span>
        <AttentionIcon className="ds-state__icon" aria-hidden="true" />
        <span className="ds-state__label">À traiter</span>
      </span>
    </button>
  )
}

function AttentionIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 20 20" {...props}>
      <path
        d="M10 3.5 17.5 16.5H2.5L10 3.5Z"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.75"
        strokeLinejoin="round"
      />
      <path
        d="M10 8.5v4"
        stroke="currentColor"
        strokeWidth="1.75"
        strokeLinecap="round"
      />
      <circle cx="10" cy="14.25" r="0.8" fill="currentColor" />
    </svg>
  )
}

function SettingsIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 20 20" {...props}>
      <circle
        cx="10"
        cy="10"
        r="2.25"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.75"
      />
      <path
        d="M10 2.5v2M10 15.5v2M2.5 10h2M15.5 10h2M4.6 4.6l1.4 1.4M14 14l1.4 1.4M4.6 15.4l1.4-1.4M14 6l1.4-1.4"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.75"
        strokeLinecap="round"
      />
    </svg>
  )
}

function UserIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 20 20" {...props}>
      <circle
        cx="10"
        cy="7"
        r="2.75"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.75"
      />
      <path
        d="M4.5 16.5c.75-2.75 2.75-4 5.5-4s4.75 1.25 5.5 4"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.75"
        strokeLinecap="round"
      />
    </svg>
  )
}

import type { RefObject, SVGProps } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { fetchNeedsAttention } from '../api'
import { queryKeys } from '../queryKeys'
import { useShellCompetitionContext } from './useShellCompetitionContext'

type ShellHeaderProps = {
  attentionDrawerId?: string
  attentionDrawerOpen?: boolean
  attentionTriggerRef?: RefObject<HTMLButtonElement | null>
  onAttentionClick?: () => void
}

/**
 * Shell header (14.6.3+) — global context, attention trigger, shell actions.
 * Opens the attention drawer via onAttentionClick; business nav stays in Sidebar.
 */
export function ShellHeader({
  attentionDrawerId,
  attentionDrawerOpen = false,
  attentionTriggerRef,
  onAttentionClick,
}: ShellHeaderProps) {
  const { t } = useTranslation('shell')
  const { competitionId, competitionName, state } = useShellCompetitionContext()

  const attentionQuery = useQuery({
    queryKey: queryKeys.competitions.attention(competitionId ?? ''),
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
          buttonRef={attentionTriggerRef}
          count={attentionCount}
          drawerId={attentionDrawerId}
          drawerOpen={attentionDrawerOpen}
          onClick={onAttentionClick}
        />
        <button
          type="button"
          className="ds-btn ds-btn--ghost ds-icon-button shell-header__icon-action"
          aria-label={t('actions.settingsAria')}
          aria-disabled="true"
          disabled
        >
          <SettingsIcon aria-hidden="true" />
        </button>
        <button
          type="button"
          className="ds-btn ds-btn--ghost shell-header__user-action"
          aria-label={t('actions.userAria')}
          aria-disabled="true"
          disabled
        >
          <UserIcon className="shell-header__user-icon" aria-hidden="true" />
          <span className="shell-header__user-label">{t('actions.user')}</span>
        </button>
      </div>
    </header>
  )
}

function ShellHeaderCompetitionContext({
  competitionName,
  state,
}: {
  competitionId?: string
  competitionName?: string
  state: ReturnType<typeof useShellCompetitionContext>['state']
}) {
  const { t } = useTranslation('shell')

  if (state === 'loading') {
    return (
      <div
        className="shell-header__context"
        aria-busy="true"
        aria-label={t('competition.contextLabel')}
      >
        <span className="shell-header__context-loading">
          {t('competition.loading')}
        </span>
      </div>
    )
  }

  if (state === 'selected' && competitionName) {
    return (
      <div className="shell-header__context" aria-label={t('competition.contextLabel')}>
        <span className="shell-header__competition-name">{competitionName}</span>
        <Link
          className="ds-btn ds-btn--ghost shell-header__change"
          to="/competitions"
        >
          {t('competition.change')}
        </Link>
      </div>
    )
  }

  if (state === 'unavailable') {
    return (
      <div className="shell-header__context" aria-label={t('competition.contextLabel')}>
        <span className="shell-header__context-message">
          {t('competition.unavailable')}
        </span>
        <Link
          className="ds-btn ds-btn--ghost shell-header__change"
          to="/competitions"
        >
          {t('competition.change')}
        </Link>
      </div>
    )
  }

  if (state === 'empty') {
    return (
      <div className="shell-header__context" aria-label={t('competition.contextLabel')}>
        <span className="shell-header__context-message">
          {t('competition.none')}
        </span>
        <Link
          className="ds-btn ds-btn--ghost shell-header__change"
          to="/competitions"
        >
          {t('competition.list')}
        </Link>
      </div>
    )
  }

  return (
    <div className="shell-header__context" aria-label={t('competition.contextLabel')}>
      <span className="shell-header__context-message">
        {t('competition.choose')}
      </span>
      <Link
        className="ds-btn ds-btn--ghost shell-header__change"
        to="/competitions"
      >
        {t('competition.change')}
      </Link>
    </div>
  )
}

function AttentionTrigger({
  count,
  drawerId,
  drawerOpen,
  onClick,
  buttonRef,
}: {
  count: number
  drawerId?: string
  drawerOpen: boolean
  onClick?: () => void
  buttonRef?: RefObject<HTMLButtonElement | null>
}) {
  const { t } = useTranslation('shell')
  const hasAttention = count > 0
  const accessibleLabel = t('attention.trigger', { count })

  return (
    <button
      ref={buttonRef}
      type="button"
      className={`shell-header__attention ds-btn ds-btn--ghost${
        hasAttention ? ' shell-header__attention--active' : ''
      }`}
      aria-expanded={drawerOpen}
      aria-haspopup="dialog"
      aria-controls={drawerId}
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
        <span className="ds-state__label">{t('attention.label')}</span>
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

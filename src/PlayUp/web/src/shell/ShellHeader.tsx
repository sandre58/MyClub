import type { RefObject } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { fetchCompetitionCockpit } from '../api'
import { TeamCrest } from '../design-system/TeamCrest'
import { AttentionBellIcon } from '../design-system/icons/shellIcons'
import { queryKeys } from '../queryKeys'
import type { CompetitionStatus } from '../types'
import { CompetitionStatusBadge } from '../ui'
import { formatCompetitionPeriod } from './competitionPeriod'
import { useShellCompetitionContext } from './useShellCompetitionContext'

type ShellHeaderProps = {
  attentionDrawerId?: string
  attentionDrawerOpen?: boolean
  attentionTriggerRef?: RefObject<HTMLButtonElement | null>
  onAttentionClick?: () => void
}

/**
 * Shell header (A) — competition context + À traiter on navy chrome.
 * Change-competition lives on the rail lockup (Accueil), not here.
 */
export function ShellHeader({
  attentionDrawerId,
  attentionDrawerOpen = false,
  attentionTriggerRef,
  onAttentionClick,
}: ShellHeaderProps) {
  const { competitionId, competitionName, logoMediaId, state } =
    useShellCompetitionContext()

  const cockpitQuery = useQuery({
    queryKey: queryKeys.competitions.cockpit(competitionId ?? ''),
    queryFn: () => fetchCompetitionCockpit(competitionId!),
    enabled: Boolean(competitionId),
  })

  const attentionCount = cockpitQuery.data?.attentionSummary.count ?? 0
  const competitionStatus = cockpitQuery.data?.status
  const periodLabel = formatCompetitionPeriod(
    cockpitQuery.data?.period?.start,
    cockpitQuery.data?.period?.end,
  )

  return (
    <header className="shell-header ds-shell-header">
      <ShellHeaderCompetitionContext
        competitionName={competitionName}
        logoMediaId={logoMediaId}
        competitionStatus={competitionStatus}
        periodLabel={periodLabel}
        state={state}
      />

      <span className="ds-shell-header__spacer" aria-hidden="true" />

      <div className="shell-header__actions">
        <AttentionTrigger
          buttonRef={attentionTriggerRef}
          count={attentionCount}
          drawerId={attentionDrawerId}
          drawerOpen={attentionDrawerOpen}
          onClick={onAttentionClick}
        />
      </div>
    </header>
  )
}

function ShellHeaderCompetitionContext({
  competitionName,
  logoMediaId,
  competitionStatus,
  periodLabel,
  state,
}: {
  competitionName?: string
  logoMediaId?: string | null
  competitionStatus?: CompetitionStatus
  periodLabel?: string | null
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
        <span className="ds-shell-header__crest shell-header__crest" aria-hidden="true">
          <TeamCrest
            name={competitionName}
            logoMediaId={logoMediaId}
            size="lg"
          />
        </span>

        <div className="ds-shell-header__identity">
          <span className="ds-shell-header__name">{competitionName}</span>

          {(competitionStatus || periodLabel) && (
            <div className="ds-shell-header__meta">
              {competitionStatus && (
                <CompetitionStatusBadge
                  status={competitionStatus}
                  density="compact"
                />
              )}
              {competitionStatus && periodLabel && (
                <span className="shell-header__meta-separator" aria-hidden="true">
                  ·
                </span>
              )}
              {periodLabel && (
                <span className="shell-header__period ds-meta">{periodLabel}</span>
              )}
            </div>
          )}
        </div>
      </div>
    )
  }

  if (state === 'unavailable') {
    return (
      <div className="shell-header__context" aria-label={t('competition.contextLabel')}>
        <span className="shell-header__context-message">
          {t('competition.unavailable')}
        </span>
      </div>
    )
  }

  if (state === 'empty') {
    return (
      <div className="shell-header__context" aria-label={t('competition.contextLabel')}>
        <span className="shell-header__context-message">
          {t('competition.none')}
        </span>
      </div>
    )
  }

  return (
    <div className="shell-header__context" aria-label={t('competition.contextLabel')}>
      <span className="shell-header__context-message">
        {t('competition.choose')}
      </span>
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
  const tooltipLabel = t('attention.label')

  return (
    <button
      ref={buttonRef}
      type="button"
      className="ds-shell-header__bell ds-btn ds-btn--ghost ds-icon-button shell-header__attention shell-header__icon-control"
      aria-expanded={hasAttention ? drawerOpen : undefined}
      aria-haspopup={hasAttention ? 'dialog' : undefined}
      aria-controls={hasAttention ? drawerId : undefined}
      aria-label={accessibleLabel}
      title={tooltipLabel}
      disabled={!hasAttention}
      onClick={hasAttention ? onClick : undefined}
    >
      <AttentionBellIcon
        size="lg"
        className="shell-header__attention-icon"
        aria-hidden="true"
      />
      {hasAttention && (
        <span className="ds-shell-header__badge ds-num" aria-hidden="true">
          {count}
        </span>
      )}
    </button>
  )
}


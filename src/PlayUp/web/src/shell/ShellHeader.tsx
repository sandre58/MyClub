import type { RefObject } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { fetchNeedsAttention } from '../api';
import { TeamCrest } from '../design-system/TeamCrest';
import {
  AttentionBellIcon,
  CloseIcon,
  SidebarExpandIcon,
} from '../design-system/icons/shellIcons';
import { queryKeys } from '../queryKeys';
import type { CompetitionStatus } from '../types';
import { CompetitionStatusBadge } from '../ui';
import { toIntlLocale } from '../i18n/intlLocale';
import { formatCompetitionPeriod } from './competitionPeriod';
import { PreferencesMenu } from './PreferencesMenu';
import { useShellCompetitionContext } from './useShellCompetitionContext';
import type { ShellViewport } from './useShellViewport';
import { Tooltip } from '../design-system/components/Tooltip';

type ShellHeaderProps = {
  viewport?: ShellViewport;
  phoneNavOpen?: boolean;
  navMenuTriggerRef?: RefObject<HTMLButtonElement | null>;
  onTogglePhoneNav?: () => void;
  attentionDrawerId?: string;
  attentionDrawerOpen?: boolean;
  attentionTriggerRef?: RefObject<HTMLButtonElement | null>;
  onAttentionClick?: () => void;
};

/**
 * Shell header (A) — competition context + À traiter on navy chrome.
 * Accueil / change-competition is the rail lockup. On phone it lives at
 * the top of the overlay (above Pilotage), not in this bar.
 */
export function ShellHeader({
  viewport = 'desktop',
  phoneNavOpen = false,
  navMenuTriggerRef,
  onTogglePhoneNav,
  attentionDrawerId,
  attentionDrawerOpen = false,
  attentionTriggerRef,
  onAttentionClick,
}: ShellHeaderProps) {
  const { t, i18n } = useTranslation('shell');
  const {
    competitionId,
    competitionName,
    logoMediaId,
    status,
    scheduledStart,
    scheduledEnd,
    state,
  } = useShellCompetitionContext();

  const attentionQuery = useQuery({
    queryKey: queryKeys.competitions.attention(competitionId ?? ''),
    queryFn: () => fetchNeedsAttention(competitionId!),
    enabled: Boolean(competitionId),
  });

  const attentionCount =
    attentionQuery.data?.count ?? attentionQuery.data?.items.length ?? 0;
  const competitionStatus = status;
  const periodLabel = formatCompetitionPeriod(
    scheduledStart,
    scheduledEnd,
    toIntlLocale(i18n.language),
  );

  return (
    <header className="shell-header ds-shell-header">
      {viewport === 'phone' ? (
        <button
          ref={navMenuTriggerRef}
          type="button"
          className="ds-shell-header__nav-toggle ds-btn ds-btn--ghost ds-icon-button shell-header__icon-control"
          aria-expanded={phoneNavOpen}
          aria-controls="shell-sidebar-nav"
          aria-label={
            phoneNavOpen ? t('sidebar.menuClose') : t('sidebar.menuOpen')
          }
          onClick={onTogglePhoneNav}
        >
          {phoneNavOpen ? (
            <CloseIcon size="sm" aria-hidden="true" />
          ) : (
            <SidebarExpandIcon size="sm" aria-hidden="true" />
          )}
        </button>
      ) : null}

      <ShellHeaderCompetitionContext
        competitionName={competitionName}
        logoMediaId={logoMediaId}
        competitionStatus={competitionStatus}
        periodLabel={periodLabel}
        state={state}
        crestSize={viewport === 'phone' ? 'md' : 'lg'}
      />

      <span className="ds-shell-header__spacer" aria-hidden="true" />

      <div className="shell-header__actions">
        <PreferencesMenu />
        <AttentionTrigger
          buttonRef={attentionTriggerRef}
          count={attentionCount}
          drawerId={attentionDrawerId}
          drawerOpen={attentionDrawerOpen}
          onClick={onAttentionClick}
        />
      </div>
    </header>
  );
}

function ShellHeaderCompetitionContext({
  competitionName,
  logoMediaId,
  competitionStatus,
  periodLabel,
  state,
  crestSize,
}: {
  competitionName?: string;
  logoMediaId?: string | null;
  competitionStatus?: CompetitionStatus;
  periodLabel?: string | null;
  state: ReturnType<typeof useShellCompetitionContext>['state'];
  crestSize: 'md' | 'lg';
}) {
  const { t } = useTranslation('shell');

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
    );
  }

  if (state === 'selected' && competitionName) {
    return (
      <div
        className="shell-header__context"
        aria-label={t('competition.contextLabel')}
      >
        <span
          className="ds-shell-header__crest shell-header__crest"
          aria-hidden="true"
        >
          <TeamCrest
            name={competitionName}
            logoMediaId={logoMediaId}
            size={crestSize}
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
                <span
                  className="shell-header__meta-separator"
                  aria-hidden="true"
                >
                  ·
                </span>
              )}
              {periodLabel && (
                <span className="shell-header__period ds-meta">
                  {periodLabel}
                </span>
              )}
            </div>
          )}
        </div>
      </div>
    );
  }

  if (state === 'unavailable') {
    return (
      <div
        className="shell-header__context"
        aria-label={t('competition.contextLabel')}
      >
        <span className="shell-header__context-message">
          {t('competition.unavailable')}
        </span>
      </div>
    );
  }

  if (state === 'empty') {
    return (
      <div
        className="shell-header__context"
        aria-label={t('competition.contextLabel')}
      >
        <span className="shell-header__context-message">
          {t('competition.none')}
        </span>
      </div>
    );
  }

  return (
    <div
      className="shell-header__context"
      aria-label={t('competition.contextLabel')}
    >
      <span className="shell-header__context-message">
        {t('competition.choose')}
      </span>
    </div>
  );
}

function AttentionTrigger({
  count,
  drawerId,
  drawerOpen,
  onClick,
  buttonRef,
}: {
  count: number;
  drawerId?: string;
  drawerOpen: boolean;
  onClick?: () => void;
  buttonRef?: RefObject<HTMLButtonElement | null>;
}) {
  const { t } = useTranslation('shell');
  const hasAttention = count > 0;
  const accessibleLabel = t('attention.trigger', { count });
  const tooltipLabel = t('attention.label');

  return (
    <Tooltip content={tooltipLabel}>
      <button
        ref={buttonRef}
        type="button"
        className="ds-shell-header__bell ds-btn ds-btn--ghost ds-icon-button shell-header__attention shell-header__icon-control"
        aria-expanded={hasAttention ? drawerOpen : undefined}
        aria-haspopup={hasAttention ? 'dialog' : undefined}
        aria-controls={hasAttention ? drawerId : undefined}
        aria-label={accessibleLabel}
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
    </Tooltip>
  );
}

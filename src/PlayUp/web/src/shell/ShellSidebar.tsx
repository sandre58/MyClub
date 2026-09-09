import type { ComponentType, SVGProps } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useLocation, useParams } from 'react-router-dom';
import {
  ClassementsNavIcon,
  MatchesNavIcon,
  OrganisationNavIcon,
  OverviewNavIcon,
  RegulationNavIcon,
  SidebarCollapseIcon,
  SidebarExpandIcon,
  TeamsNavIcon,
  VenuesNavIcon,
} from '../design-system/icons/shellIcons';
import { PlayUpLockupMark } from '../design-system/PlayUpLockupMark';
import { PlayUpWordmark } from '../design-system/PlayUpWordmark';
import {
  resolveActiveDestination,
  shellDestinationHrefs,
  shellNavGroups,
  type ShellNavItemKey,
} from './shellDestinations';
import { useShellCompetitionContext } from './useShellCompetitionContext';

type ShellSidebarProps = {
  collapsed: boolean;
  hideCollapse?: boolean;
  inert?: boolean;
  onToggleCollapse: () => void;
};

const navIcons: Record<
  ShellNavItemKey,
  ComponentType<SVGProps<SVGSVGElement>>
> = {
  overview: OverviewNavIcon,
  organisation: OrganisationNavIcon,
  matches: MatchesNavIcon,
  classements: ClassementsNavIcon,
  teams: TeamsNavIcon,
  venues: VenuesNavIcon,
  regulation: RegulationNavIcon,
};

/**
 * Structural sidebar (Shell A). Lockup → Accueil. Collapse lives in the rail.
 */
export function ShellSidebar({
  collapsed,
  hideCollapse = false,
  inert = false,
  onToggleCollapse,
}: ShellSidebarProps) {
  const { t } = useTranslation('shell');
  const { stageId, matchId } = useParams();
  const { competitionId } = useShellCompetitionContext();
  const location = useLocation();
  const activeKey = resolveActiveDestination(location.pathname);
  const hrefs = shellDestinationHrefs({ competitionId, stageId, matchId });

  return (
    <aside
      className="shell-sidebar ds-shell-rail"
      data-collapsed={collapsed ? 'true' : 'false'}
      aria-label={t('sidebar.label')}
      aria-hidden={inert || undefined}
      inert={inert || undefined}
    >
      <Link
        className="ds-shell-rail__brand"
        to="/"
        aria-label={t('sidebar.home')}
      >
        <PlayUpLockupMark />
        <span className="ds-shell-rail__wordmark">
          <PlayUpWordmark surface="chrome" />
        </span>
      </Link>

      <nav
        id="shell-sidebar-nav"
        className="ds-shell-rail__nav"
        aria-label={t('sidebar.primary')}
        tabIndex={-1}
      >
        {shellNavGroups.map((group) => (
          <div key={group.id} className="ds-shell-rail__group">
            <p className="ds-shell-rail__group-label">
              {t(`groups.${group.id}`)}
            </p>
            {group.items.map((item) => {
              const Icon = navIcons[item.key];
              const label = t(`navigation.${item.key}`);
              const isActive =
                item.hrefKey !== undefined && item.hrefKey === activeKey;

              if (item.hrefKey) {
                return (
                  <Link
                    key={item.key}
                    to={hrefs[item.hrefKey]}
                    className="ds-shell-rail__link"
                    data-active={isActive ? 'true' : 'false'}
                    aria-current={isActive ? 'page' : undefined}
                    title={collapsed ? label : undefined}
                  >
                    <Icon className="ds-shell-rail__icon" />
                    <span className="ds-shell-rail__label">{label}</span>
                  </Link>
                );
              }

              return (
                <button
                  key={item.key}
                  type="button"
                  className="ds-shell-rail__link"
                  disabled
                  aria-label={t('sidebar.comingSoon', { label })}
                  title={t('sidebar.comingSoon', { label })}
                >
                  <Icon className="ds-shell-rail__icon" />
                  <span className="ds-shell-rail__label">{label}</span>
                </button>
              );
            })}
          </div>
        ))}
      </nav>

      {hideCollapse ? null : (
        <div className="ds-shell-rail__footer">
          <button
            type="button"
            className="ds-shell-rail__collapse"
            aria-expanded={!collapsed}
            aria-controls="shell-sidebar-nav"
            aria-label={collapsed ? t('sidebar.expand') : t('sidebar.collapse')}
            title={collapsed ? t('sidebar.expand') : t('sidebar.collapse')}
            onClick={onToggleCollapse}
          >
            {collapsed ? (
              <SidebarExpandIcon size="sm" />
            ) : (
              <SidebarCollapseIcon size="sm" />
            )}
          </button>
        </div>
      )}
    </aside>
  );
}

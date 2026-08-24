import type { ComponentType, SVGProps } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useLocation, useParams } from 'react-router-dom'
import {
  ClassementsNavIcon,
  MatchesNavIcon,
  OrganisationNavIcon,
  OverviewNavIcon,
  SettingsNavIcon,
  SidebarCollapseIcon,
  SidebarExpandIcon,
} from '../design-system/icons/shellIcons'
import {
  resolveActiveDestination,
  shellDestinationHrefs,
  type ShellDestinationKey,
} from './shellDestinations'
import { useShellCompetitionContext } from './useShellCompetitionContext'

type ShellSidebarProps = {
  collapsed: boolean
  onToggleCollapse: () => void
}

type NavDestination = {
  key: ShellDestinationKey
  label: string
  to: string
  icon: ComponentType<SVGProps<SVGSVGElement>>
}

const destinationDefinitions: Array<
  Omit<NavDestination, 'to' | 'label'> & { key: ShellDestinationKey }
> = [
  { key: 'cockpit', icon: OverviewNavIcon },
  { key: 'organisation', icon: OrganisationNavIcon },
  { key: 'matches', icon: MatchesNavIcon },
  { key: 'classements', icon: ClassementsNavIcon },
]

/**
 * Structural sidebar (14.6.2+). Resolves competition context for safe hrefs.
 */
export function ShellSidebar({
  collapsed,
  onToggleCollapse,
}: ShellSidebarProps) {
  const { t } = useTranslation('shell')
  const { stageId, matchId } = useParams()
  const { competitionId } = useShellCompetitionContext()
  const location = useLocation()
  const activeKey = resolveActiveDestination(location.pathname)
  const hrefs = shellDestinationHrefs({ competitionId, stageId, matchId })
  const destinations = destinationDefinitions.map((item) => ({
    ...item,
    label: t(`navigation.${item.key}`),
    to: hrefs[item.key],
  }))
  const cockpitHref = hrefs.cockpit

  return (
    <aside
      className="shell-sidebar"
      data-collapsed={collapsed ? 'true' : 'false'}
      aria-label={t('sidebar.label')}
    >
      <div className="shell-sidebar__brand-row">
        <Link
          className="shell-sidebar__brand"
          to={cockpitHref}
          aria-label={t('sidebar.home')}
        >
          <span className="shell-sidebar__brand-mark" aria-hidden="true">
            P
          </span>
          <span
            className={`shell-sidebar__brand-text${collapsed ? ' ds-visually-hidden' : ''}`}
          >
            Play&apos;up
          </span>
        </Link>

        <button
          type="button"
          className="shell-sidebar__edge-toggle"
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

      <nav
        id="shell-sidebar-nav"
        className="shell-sidebar__nav"
        aria-label={t('sidebar.primary')}
      >
        {destinations.map((item) => {
          const isActive = item.key === activeKey
          const Icon = item.icon

          return (
            <Link
              key={item.key}
              to={item.to}
              className="shell-sidebar__link"
              data-active={isActive ? 'true' : 'false'}
              aria-current={isActive ? 'page' : undefined}
              title={collapsed ? item.label : undefined}
            >
              <span className="shell-sidebar__link-indicator" aria-hidden="true" />
              <Icon className="shell-sidebar__icon" />
              <span
                className={`shell-sidebar__label${collapsed ? ' ds-visually-hidden' : ''}`}
              >
                {item.label}
              </span>
            </Link>
          )
        })}
      </nav>

      <div className="shell-sidebar__footer">
        <button
          type="button"
          className="shell-sidebar__footer-control"
          aria-label={t('actions.settingsAria')}
          title={t('actions.settings')}
          aria-disabled="true"
          disabled
        >
          <SettingsNavIcon className="shell-sidebar__icon" />
          <span
            className={`shell-sidebar__footer-control-label${collapsed ? ' ds-visually-hidden' : ''}`}
          >
            {t('actions.settings')}
          </span>
        </button>
      </div>
    </aside>
  )
}

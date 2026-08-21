import type { ReactElement, SVGProps } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useLocation, useParams } from 'react-router-dom'
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
  icon: (props: SVGProps<SVGSVGElement>) => ReactElement
}

const destinationDefinitions: Array<
  Omit<NavDestination, 'to' | 'label'> & { key: ShellDestinationKey }
> = [
  { key: 'cockpit', icon: HomeIcon },
  { key: 'organisation', icon: OrganisationIcon },
  { key: 'matches', icon: MatchesIcon },
  { key: 'classements', icon: ClassementsIcon },
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
          <span className="shell-sidebar__brand-text">Play&apos;up</span>
        </Link>
      </div>

      <nav
        id="shell-sidebar-nav"
        className="shell-sidebar__nav"
        aria-label={t('sidebar.primary')}
      >
        {destinations.map((item) => {
          const isActive = item.key === activeKey

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
              <item.icon className="shell-sidebar__icon" aria-hidden="true" />
              <span className="shell-sidebar__label">{item.label}</span>
            </Link>
          )
        })}
      </nav>

      <div className="shell-sidebar__footer">
        <button
          type="button"
          className="ds-btn ds-btn--ghost ds-icon-button shell-sidebar__toggle"
          aria-expanded={!collapsed}
          aria-controls="shell-sidebar-nav"
          aria-label={collapsed ? t('sidebar.expand') : t('sidebar.collapse')}
          onClick={onToggleCollapse}
        >
          {collapsed ? (
            <ExpandIcon aria-hidden="true" />
          ) : (
            <CollapseIcon aria-hidden="true" />
          )}
          <span className="shell-sidebar__toggle-label">
            {collapsed ? t('sidebar.expand') : t('sidebar.collapse')}
          </span>
        </button>
      </div>
    </aside>
  )
}

function HomeIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 20 20" {...props}>
      <path
        d="M3.5 8.5 10 3.5l6.5 5v8h-4.5V11H8v5.5H3.5z"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.75"
        strokeLinejoin="round"
      />
    </svg>
  )
}

function OrganisationIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 20 20" {...props}>
      <circle
        cx="6"
        cy="6"
        r="2.25"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.75"
      />
      <circle
        cx="14"
        cy="6"
        r="2.25"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.75"
      />
      <path
        d="M3.5 15c.6-2 2.1-3 4.5-3s3.9 1 4.5 3M9.5 15c.5-1.7 1.8-2.5 4-2.5 1.5 0 2.7.5 3 2.5"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.75"
        strokeLinecap="round"
      />
    </svg>
  )
}

function MatchesIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 20 20" {...props}>
      <path
        d="M4.5 5.5h11M4.5 10h11M4.5 14.5h11"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.75"
        strokeLinecap="round"
      />
      <circle cx="7" cy="5.5" r="1" fill="currentColor" />
      <circle cx="11" cy="10" r="1" fill="currentColor" />
      <circle cx="14" cy="14.5" r="1" fill="currentColor" />
    </svg>
  )
}

function ClassementsIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 20 20" {...props}>
      <path
        d="M4.5 15.5V4.5h11v11z"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.75"
        strokeLinejoin="round"
      />
      <path
        d="M7 12.5 9 10.5l1.75 1.5 2.25-3"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.75"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  )
}

function CollapseIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 20 20" {...props}>
      <path
        d="M13 5 8 10l5 5"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.75"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  )
}

function ExpandIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 20 20" {...props}>
      <path
        d="M7 5 12 10 7 15"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.75"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  )
}

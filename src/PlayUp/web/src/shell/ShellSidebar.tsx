import { NavLink, useParams } from 'react-router-dom'

type ShellSidebarProps = {
  collapsed: boolean
  onToggleCollapse: () => void
}

type NavDestination = {
  key: string
  label: string
  to: string | null
  end?: boolean
}

/**
 * Structural sidebar (14.6.1). Icons, active styling, and IA details come later.
 */
export function ShellSidebar({
  collapsed,
  onToggleCollapse,
}: ShellSidebarProps) {
  const { competitionId } = useParams()
  const destinations = shellDestinations(competitionId)

  return (
    <aside
      className="shell-sidebar"
      data-collapsed={collapsed ? 'true' : 'false'}
      aria-label="Workspace navigation"
    >
      <div className="shell-sidebar__toolbar">
        <button
          type="button"
          className="ds-btn ds-btn--ghost shell-sidebar__toggle"
          aria-expanded={!collapsed}
          aria-controls="shell-sidebar-nav"
          onClick={onToggleCollapse}
        >
          {collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
        </button>
      </div>

      <nav
        id="shell-sidebar-nav"
        className="shell-sidebar__nav"
        aria-label="Primary"
      >
        {destinations.map((item) => {
          const shortLabel = collapsed ? item.label.charAt(0) : item.label

          if (!item.to) {
            return (
              <span
                key={item.key}
                className="shell-sidebar__item"
                aria-disabled="true"
                title={item.label}
              >
                {shortLabel}
              </span>
            )
          }

          return (
            <NavLink
              key={item.key}
              to={item.to}
              end={item.end}
              className="shell-sidebar__link"
              title={item.label}
            >
              {shortLabel}
            </NavLink>
          )
        })}
      </nav>
    </aside>
  )
}

function shellDestinations(competitionId: string | undefined): NavDestination[] {
  if (!competitionId) {
    return [
      { key: 'cockpit', label: 'Cockpit', to: '/competitions', end: true },
      { key: 'organisation', label: 'Organisation', to: null },
      { key: 'matches', label: 'Matchs', to: null },
      { key: 'consultation', label: 'Consultation', to: null },
    ]
  }

  const base = `/competitions/${competitionId}`

  return [
    { key: 'cockpit', label: 'Cockpit', to: base, end: true },
    { key: 'organisation', label: 'Organisation', to: `${base}/organisation` },
    { key: 'matches', label: 'Matchs', to: `${base}/matches` },
    { key: 'consultation', label: 'Consultation', to: `${base}/overview` },
  ]
}

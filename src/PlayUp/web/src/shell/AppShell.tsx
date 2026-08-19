import { useState } from 'react'
import { Outlet } from 'react-router-dom'
import '../design-system/fonts'
import '../design-system/index.css'
import './shell.css'
import { ShellHeader } from './ShellHeader'
import { ShellMain } from './ShellMain'
import { ShellSidebar } from './ShellSidebar'

/**
 * Product shell (14.6.1) — global framing only. Business pages render via Outlet.
 */
export function AppShell() {
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false)

  return (
    <div
      className="ds-root shell"
      data-font="plex"
      data-palette="slate"
      data-density="standard"
    >
      <a className="shell-skip" href="#main">
        Skip to content
      </a>

      <div className="shell__frame">
        <ShellSidebar
          collapsed={sidebarCollapsed}
          onToggleCollapse={() => setSidebarCollapsed((value) => !value)}
        />

        <div className="shell__column">
          <ShellHeader />
          <ShellMain>
            <Outlet />
          </ShellMain>
        </div>
      </div>
    </div>
  )
}

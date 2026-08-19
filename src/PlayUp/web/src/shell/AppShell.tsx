import { useEffect, useRef, useState } from 'react'
import { Outlet } from 'react-router-dom'
import '../design-system/fonts'
import '../design-system/index.css'
import './shell.css'
import { AttentionDrawer } from './AttentionDrawer'
import { ShellHeader } from './ShellHeader'
import { ShellMain } from './ShellMain'
import { ShellSidebar } from './ShellSidebar'
import { SHELL_ATTENTION_DRAWER_PANEL_ID } from './shellIds'

const sidebarCollapsedStorageKey = 'playup:shell:sidebar-collapsed'

/**
 * Product shell (14.6.1) — global framing only. Business pages render via Outlet.
 */
export function AppShell() {
  const attentionTriggerRef = useRef<HTMLButtonElement>(null)
  const [attentionDrawerOpen, setAttentionDrawerOpen] = useState(false)
  const [sidebarCollapsed, setSidebarCollapsed] = useState(() => {
    if (typeof window === 'undefined') {
      return false
    }

    return window.localStorage.getItem(sidebarCollapsedStorageKey) === 'true'
  })

  useEffect(() => {
    window.localStorage.setItem(
      sidebarCollapsedStorageKey,
      String(sidebarCollapsed),
    )
  }, [sidebarCollapsed])

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

      <div className="shell__frame" inert={attentionDrawerOpen || undefined}>
        <ShellSidebar
          collapsed={sidebarCollapsed}
          onToggleCollapse={() => setSidebarCollapsed((value) => !value)}
        />

        <div className="shell__column">
          <ShellHeader
            attentionDrawerId={SHELL_ATTENTION_DRAWER_PANEL_ID}
            attentionDrawerOpen={attentionDrawerOpen}
            attentionTriggerRef={attentionTriggerRef}
            onAttentionClick={() => setAttentionDrawerOpen(true)}
          />
          <ShellMain>
            <Outlet />
          </ShellMain>
        </div>
      </div>

      <AttentionDrawer
        open={attentionDrawerOpen}
        panelId={SHELL_ATTENTION_DRAWER_PANEL_ID}
        onClose={() => setAttentionDrawerOpen(false)}
        returnFocusRef={attentionTriggerRef}
      />
    </div>
  )
}

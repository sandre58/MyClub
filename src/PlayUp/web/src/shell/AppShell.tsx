import { useEffect, useLayoutEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Outlet, useLocation } from 'react-router-dom'
import '../design-system/fonts'
import '../design-system/index.css'
import { useDismissLayer } from '../design-system/useDismissLayer'
import './shell.css'
import { AttentionDrawer } from './AttentionDrawer'
import { PageErrorBoundary } from './PageErrorBoundary'
import { ShellHeader } from './ShellHeader'
import { ShellMain } from './ShellMain'
import { ShellSidebar } from './ShellSidebar'
import { SHELL_ATTENTION_DRAWER_PANEL_ID } from './shellIds'
import { useShellViewport } from './useShellViewport'

const sidebarCollapsedStorageKey = 'playup:shell:sidebar-collapsed'

/**
 * Product shell (14.6.1) — global framing only. Business pages render via Outlet.
 */
export function AppShell() {
  const { t } = useTranslation('common')
  const location = useLocation()
  const viewport = useShellViewport()
  const attentionTriggerRef = useRef<HTMLButtonElement>(null)
  const navMenuTriggerRef = useRef<HTMLButtonElement>(null)
  const shellRef = useRef<HTMLDivElement>(null)
  const [attentionDrawerOpen, setAttentionDrawerOpen] = useState(false)
  const [phoneNavOpen, setPhoneNavOpen] = useState(false)
  const [motionViewport, setMotionViewport] = useState(viewport)
  const [chromeReady, setChromeReady] = useState(viewport === 'desktop')
  const [tabletExpanded, setTabletExpanded] = useState(false)
  const [desktopCollapsed, setDesktopCollapsed] = useState(() => {
    if (typeof window === 'undefined') {
      return false
    }

    return window.localStorage.getItem(sidebarCollapsedStorageKey) === 'true'
  })

  useEffect(() => {
    window.localStorage.setItem(
      sidebarCollapsedStorageKey,
      String(desktopCollapsed),
    )
  }, [desktopCollapsed])

  useEffect(() => {
    if (viewport !== 'tablet') {
      setTabletExpanded(false)
    }

    if (viewport !== 'phone') {
      setPhoneNavOpen(false)
    }

    if (viewport === 'desktop') {
      setMotionViewport('desktop')
      setChromeReady(true)
      return
    }

    setChromeReady(false)
    setMotionViewport(viewport)
    let cancelled = false
    requestAnimationFrame(() => {
      requestAnimationFrame(() => {
        if (!cancelled) {
          setChromeReady(true)
        }
      })
    })
    return () => {
      cancelled = true
    }
  }, [viewport])

  useEffect(() => {
    setPhoneNavOpen(false)
  }, [location.pathname])

  useEffect(() => {
    if (viewport !== 'phone' || !phoneNavOpen) {
      return
    }

    document.getElementById('shell-sidebar-nav')?.focus()
  }, [viewport, phoneNavOpen])

  useDismissLayer(viewport === 'phone' && phoneNavOpen, () => {
    setPhoneNavOpen(false)
    navMenuTriggerRef.current?.focus()
  })

  const navReady = viewport === motionViewport && chromeReady

  const collapsed =
    viewport === 'phone'
      ? false
      : viewport === 'tablet'
        ? !tabletExpanded
        : desktopCollapsed

  useLayoutEffect(() => {
    const root = shellRef.current
    if (!root) {
      return
    }

    const header = root.querySelector('.ds-shell-header')
    const rail = root.querySelector('.ds-shell-rail')
    if (!(header instanceof HTMLElement) || typeof ResizeObserver === 'undefined') {
      return
    }

    const sync = () => {
      const headerHeight = `${header.offsetHeight}px`
      root.style.setProperty('--shell-chrome-end', headerHeight)
      if (viewport === 'phone') {
        root.style.setProperty('--shell-phone-chrome-end', headerHeight)
        root.style.setProperty('--shell-rail-end', '0px')
        return
      }

      root.style.removeProperty('--shell-phone-chrome-end')
      const railWidth = rail instanceof HTMLElement ? rail.offsetWidth : 0
      root.style.setProperty('--shell-rail-end', `${railWidth}px`)
    }

    sync()
    const observer = new ResizeObserver(sync)
    observer.observe(header)
    if (rail instanceof HTMLElement) {
      observer.observe(rail)
    }
    return () => {
      observer.disconnect()
      root.style.removeProperty('--shell-chrome-end')
      root.style.removeProperty('--shell-rail-end')
      root.style.removeProperty('--shell-phone-chrome-end')
    }
  }, [viewport, collapsed])

  const onToggleCollapse = () => {
    if (viewport === 'tablet') {
      setTabletExpanded((value) => !value)
      return
    }

    setDesktopCollapsed((value) => !value)
  }

  const openAttention = () => {
    setPhoneNavOpen(false)
    setAttentionDrawerOpen(true)
  }

  const togglePhoneNav = () => {
    setAttentionDrawerOpen(false)
    setPhoneNavOpen((value) => !value)
  }

  return (
    <div
      ref={shellRef}
      className="ds-root shell"
      data-font="plex"
      data-palette="slate"
      data-density="standard"
      data-shell-vp={viewport}
      data-nav-open={phoneNavOpen ? 'true' : 'false'}
      data-nav-ready={navReady ? 'true' : 'false'}
    >
      <a className="shell-skip" href="#main">
        {t('skipToContent')}
      </a>

      <div className="shell__frame" inert={attentionDrawerOpen || undefined}>
        <ShellSidebar
          collapsed={collapsed}
          hideCollapse={viewport === 'phone'}
          inert={viewport === 'phone' && !phoneNavOpen}
          onToggleCollapse={onToggleCollapse}
        />

        <div className="shell__column">
          <ShellHeader
            viewport={viewport}
            phoneNavOpen={phoneNavOpen}
            navMenuTriggerRef={navMenuTriggerRef}
            onTogglePhoneNav={togglePhoneNav}
            attentionDrawerId={SHELL_ATTENTION_DRAWER_PANEL_ID}
            attentionDrawerOpen={attentionDrawerOpen}
            attentionTriggerRef={attentionTriggerRef}
            onAttentionClick={openAttention}
          />
          {viewport === 'phone' ? (
            <button
              type="button"
              className="shell-nav-backdrop"
              tabIndex={-1}
              aria-hidden="true"
              hidden={!phoneNavOpen}
              onClick={() => {
                setPhoneNavOpen(false)
                navMenuTriggerRef.current?.focus()
              }}
            />
          ) : null}
          <div className="shell__workspace" inert={phoneNavOpen || undefined}>
            <ShellMain>
              <PageErrorBoundary>
                <Outlet />
              </PageErrorBoundary>
            </ShellMain>
          </div>
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

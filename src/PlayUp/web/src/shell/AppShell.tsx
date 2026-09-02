import { useEffect, useLayoutEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Outlet, useLocation } from 'react-router-dom'
import '../design-system/fonts'
import '../design-system/index.css'
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

  useLayoutEffect(() => {
    const root = shellRef.current
    if (!root || viewport !== 'phone') {
      root?.style.removeProperty('--shell-phone-chrome-end')
      return
    }

    const header = root.querySelector('.ds-shell-header')
    if (!(header instanceof HTMLElement) || typeof ResizeObserver === 'undefined') {
      return
    }

    const sync = () => {
      root.style.setProperty('--shell-phone-chrome-end', `${header.offsetHeight}px`)
    }

    sync()
    const observer = new ResizeObserver(sync)
    observer.observe(header)
    return () => {
      observer.disconnect()
      root.style.removeProperty('--shell-phone-chrome-end')
    }
  }, [viewport])

  useEffect(() => {
    setPhoneNavOpen(false)
  }, [location.pathname])

  useEffect(() => {
    if (viewport !== 'phone' || !phoneNavOpen) {
      return
    }

    const nav = document.getElementById('shell-sidebar-nav')
    nav?.focus()

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key !== 'Escape') {
        return
      }

      event.preventDefault()
      setPhoneNavOpen(false)
      navMenuTriggerRef.current?.focus()
    }

    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [viewport, phoneNavOpen])

  const navReady = viewport === motionViewport && chromeReady

  const collapsed =
    viewport === 'phone'
      ? false
      : viewport === 'tablet'
        ? !tabletExpanded
        : desktopCollapsed

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

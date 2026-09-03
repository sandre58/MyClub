import { useQuery } from '@tanstack/react-query'
import { useEffect, useId, useRef, useState, type RefObject } from 'react'
import { useTranslation } from 'react-i18next'
import { fetchCompetitionOverview } from '../api'
import { CloseIcon } from '../design-system/icons/shellIcons'
import { queryKeys } from '../queryKeys'
import type { OverviewSituation } from '../types'
import {
  AttentionSituationRow,
  partitionAttentionItems,
} from './AttentionSituationRow'
import { useShellCompetitionContext } from './useShellCompetitionContext'
import { SHELL_MOTION_EXIT_MS } from './shellMotion'

type AttentionDrawerProps = {
  open: boolean
  panelId: string
  onClose: () => void
  returnFocusRef: RefObject<HTMLButtonElement | null>
}

/**
 * Temporary triage surface (14.6.4) — not navigation, not a generic drawer primitive.
 * Phase 16.2: consumes Overview attentionSummary (same métier source as the Overview page).
 */
export function AttentionDrawer({
  open,
  panelId,
  onClose,
  returnFocusRef,
}: AttentionDrawerProps) {
  const { t } = useTranslation(['shell', 'common'])
  const titleId = useId()
  const panelRef = useRef<HTMLDivElement>(null)
  const closeButtonRef = useRef<HTMLButtonElement>(null)
  const hadOpenedRef = useRef(false)
  const [mounted, setMounted] = useState(open)
  const [visible, setVisible] = useState(open)

  const { competitionId, state: contextState } = useShellCompetitionContext()

  const overviewQuery = useQuery({
    queryKey: queryKeys.competitions.overview(competitionId ?? ''),
    queryFn: () => fetchCompetitionOverview(competitionId!),
    enabled: (open || mounted) && Boolean(competitionId),
  })

  const items = overviewQuery.data?.attentionSummary.items ?? []
  const count = overviewQuery.data?.attentionSummary.count ?? items.length
  const titleLabel = t('shell:attention.label')
  const showCount = !overviewQuery.isPending && count > 0

  useEffect(() => {
    if (open) {
      setMounted(true)
      const frame = requestAnimationFrame(() => {
        requestAnimationFrame(() => setVisible(true))
      })
      return () => cancelAnimationFrame(frame)
    }

    setVisible(false)
    const timeout = window.setTimeout(() => setMounted(false), SHELL_MOTION_EXIT_MS)
    return () => window.clearTimeout(timeout)
  }, [open])

  useEffect(() => {
    if (!open || !visible) {
      return
    }

    const shell = panelRef.current?.closest('.shell')
    if (shell instanceof HTMLElement) {
      shell.scrollLeft = 0
      shell.scrollTop = 0
    }

    closeButtonRef.current?.focus({ preventScroll: true })
  }, [open, visible])

  useEffect(() => {
    if (open) {
      hadOpenedRef.current = true
    }
  }, [open])

  useEffect(() => {
    if (hadOpenedRef.current && !mounted) {
      hadOpenedRef.current = false
      returnFocusRef.current?.focus({ preventScroll: true })
    }
  }, [mounted, returnFocusRef])

  useEffect(() => {
    if (!open) {
      return
    }

    function onKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        event.preventDefault()
        onClose()
      }
    }

    document.addEventListener('keydown', onKeyDown)
    return () => document.removeEventListener('keydown', onKeyDown)
  }, [open, onClose])

  useFocusTrap(panelRef, open && visible)

  if (!mounted) {
    return null
  }

  return (
    <div
      className="shell-attention-drawer"
      data-open={visible ? 'true' : 'false'}
    >
      <button
        type="button"
        className="shell-attention-drawer__backdrop"
        aria-label={t('shell:attention.closeDrawer')}
        onClick={onClose}
        tabIndex={-1}
      />

      <div
        ref={panelRef}
        id={panelId}
        className="shell-attention-drawer__panel ds-overlay"
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
      >
        <header className="shell-attention-drawer__head">
          <h2 id={titleId} className="shell-attention-drawer__lockup">
            {showCount ? (
              <span className="shell-attention-drawer__count">{count}</span>
            ) : null}
            <span className="shell-attention-drawer__copy">
              <span className="shell-attention-drawer__title">{titleLabel}</span>
              <span className="shell-attention-drawer__lede">
                {t('shell:attention.lede')}
              </span>
            </span>
          </h2>
          <button
            ref={closeButtonRef}
            type="button"
            className="ds-btn ds-btn--ghost ds-icon-button shell-attention-drawer__close"
            aria-label={t('common:close')}
            title={t('common:close')}
            onClick={onClose}
          >
            <CloseIcon size="md" aria-hidden="true" />
          </button>
        </header>

        <div className="shell-attention-drawer__body">
          <AttentionDrawerContent
            contextState={contextState}
            competitionId={competitionId}
            pending={Boolean(competitionId) && overviewQuery.isPending}
            error={overviewQuery.error}
            items={items}
            onNavigate={onClose}
          />
        </div>
      </div>
    </div>
  )
}

function AttentionDrawerContent({
  contextState,
  competitionId,
  pending,
  error,
  items,
  onNavigate,
}: {
  contextState: ReturnType<typeof useShellCompetitionContext>['state']
  competitionId?: string
  pending: boolean
  error: unknown
  items: OverviewSituation[]
  onNavigate: () => void
}) {
  const { t } = useTranslation('shell')

  if (!competitionId) {
    if (contextState === 'loading') {
      return (
        <p className="shell-attention-drawer__message" aria-busy="true">
          {t('competition.loading')}
        </p>
      )
    }

    if (contextState === 'empty') {
      return (
        <p className="shell-attention-drawer__message">
          {t('attention.emptyHost')}
        </p>
      )
    }

    return (
      <p className="shell-attention-drawer__message">
        {t('attention.noContext')}
      </p>
    )
  }

  if (contextState === 'loading' || pending) {
    return (
      <p className="shell-attention-drawer__message" aria-busy="true">
        {t('competition.loading')}
      </p>
    )
  }

  if (error) {
    return (
      <p className="shell-attention-drawer__message" role="alert">
        {t('attention.loadError')}
      </p>
    )
  }

  if (items.length === 0) {
    return (
      <div className="shell-attention-drawer__empty">
        <p className="shell-attention-drawer__empty-title">
          {t('attention.emptyTitle')}
        </p>
        <p className="shell-attention-drawer__hint ds-meta">
          {t('attention.emptyHint')}
        </p>
      </div>
    )
  }

  const { blocking, attention } = partitionAttentionItems(items)

  return (
    <div className="shell-attention-drawer__groups">
      {blocking.length > 0 ? (
        <AttentionDrawerGroup
          label={t('attention.groupBlocking')}
          items={blocking}
          competitionId={competitionId}
          onNavigate={onNavigate}
        />
      ) : null}
      {attention.length > 0 ? (
        <AttentionDrawerGroup
          label={t('attention.groupAttention')}
          items={attention}
          competitionId={competitionId}
          onNavigate={onNavigate}
        />
      ) : null}
    </div>
  )
}

function AttentionDrawerGroup({
  label,
  items,
  competitionId,
  onNavigate,
}: {
  label: string
  items: OverviewSituation[]
  competitionId: string
  onNavigate: () => void
}) {
  return (
    <section className="shell-attention-drawer__group">
      <h3 className="shell-attention-drawer__group-label">{label}</h3>
      <ul className="shell-attention-drawer__list">
        {items.map((item) => (
          <AttentionSituationRow
            key={`${item.source}:${item.targetType}:${item.targetId}:${item.matchId}`}
            item={item}
            competitionId={competitionId}
            onNavigate={onNavigate}
          />
        ))}
      </ul>
    </section>
  )
}

function useFocusTrap(
  containerRef: RefObject<HTMLElement | null>,
  active: boolean,
) {
  useEffect(() => {
    if (!active || !containerRef.current) {
      return
    }

    const container = containerRef.current

    function getFocusableElements() {
      return Array.from(
        container.querySelectorAll<HTMLElement>(
          'a[href], button:not([disabled]), textarea, input, select, [tabindex]:not([tabindex="-1"])',
        ),
      )
    }

    function onKeyDown(event: KeyboardEvent) {
      if (event.key !== 'Tab') {
        return
      }

      const focusable = getFocusableElements()
      if (focusable.length === 0) {
        return
      }

      const first = focusable[0]
      const last = focusable[focusable.length - 1]
      const activeElement = document.activeElement

      if (event.shiftKey && activeElement === first) {
        event.preventDefault()
        last.focus({ preventScroll: true })
      } else if (!event.shiftKey && activeElement === last) {
        event.preventDefault()
        first.focus({ preventScroll: true })
      }
    }

    container.addEventListener('keydown', onKeyDown)
    return () => container.removeEventListener('keydown', onKeyDown)
  }, [active, containerRef])
}

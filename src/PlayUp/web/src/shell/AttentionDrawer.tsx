import { useQueries, useQuery } from '@tanstack/react-query'
import { useEffect, useId, useRef, type RefObject, type SVGProps } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import {
  fetchCompetitionOverview,
  fetchMatchesByStage,
  fetchNeedsAttention,
} from '../api'
import { queryKeys } from '../queryKeys'
import type { NeedsAttentionItem } from '../types'
import {
  attentionItemContextLabel,
  attentionItemHref,
  attentionSeverityStateClass,
  type AttentionMatchRow,
} from './attentionItemHref'
import { useShellCompetitionContext } from './useShellCompetitionContext'

type AttentionDrawerProps = {
  open: boolean
  panelId: string
  onClose: () => void
  returnFocusRef: RefObject<HTMLButtonElement | null>
}

/**
 * Temporary triage surface (14.6.4) — not navigation, not a generic drawer primitive.
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
  const wasOpenRef = useRef(false)

  const { competitionId, competitionName, state: contextState } =
    useShellCompetitionContext()

  const attentionQuery = useQuery({
    queryKey: queryKeys.competitions.attention(competitionId ?? ''),
    queryFn: () => fetchNeedsAttention(competitionId!),
    enabled: open && Boolean(competitionId),
  })

  const items = attentionQuery.data?.items ?? []
  const hasFixtureItems = items.some((item) => item.targetType === 'Fixture')

  const overviewQuery = useQuery({
    queryKey: queryKeys.competitions.detail(competitionId ?? ''),
    queryFn: () => fetchCompetitionOverview(competitionId!),
    enabled: open && Boolean(competitionId) && hasFixtureItems,
  })

  const stages = overviewQuery.data?.stages ?? []

  const matchQueries = useQueries({
    queries: stages.map((stage) => ({
      queryKey: queryKeys.matches.byStage(stage.stageId),
      queryFn: () => fetchMatchesByStage(stage.stageId),
      enabled: open && overviewQuery.isSuccess && stages.length > 0,
    })),
  })

  const matchRows: AttentionMatchRow[] = []
  stages.forEach((stage, index) => {
    const matches = matchQueries[index]?.data
    if (!matches) {
      return
    }
    for (const match of matches) {
      matchRows.push({ match, stageName: stage.name })
    }
  })

  const pending =
    Boolean(competitionId) &&
    (attentionQuery.isPending ||
      (hasFixtureItems &&
        (overviewQuery.isPending ||
          matchQueries.some((query) => query.isPending))))

  useEffect(() => {
    if (!open) {
      return
    }

    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    closeButtonRef.current?.focus()

    return () => {
      document.body.style.overflow = previousOverflow
    }
  }, [open])

  useEffect(() => {
    if (wasOpenRef.current && !open) {
      returnFocusRef.current?.focus()
    }
    wasOpenRef.current = open
  }, [open, returnFocusRef])

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

  useFocusTrap(panelRef, open)

  if (!open) {
    return null
  }

  return (
    <div className="shell-attention-drawer" data-open="true">
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
          <div className="shell-attention-drawer__title-group">
            <h2 id={titleId} className="shell-attention-drawer__title">
              {t('shell:attention.label')}
            </h2>
            {competitionName ? (
              <p className="shell-attention-drawer__subtitle ds-meta">
                {competitionName}
              </p>
            ) : contextState === 'loading' && competitionId ? (
              <p
                className="shell-attention-drawer__subtitle shell-attention-drawer__subtitle--loading ds-meta"
                aria-busy="true"
              >
                {t('shell:competition.loading')}
              </p>
            ) : null}
          </div>
          <button
            ref={closeButtonRef}
            type="button"
            className="ds-btn ds-btn--ghost ds-icon-button shell-attention-drawer__close"
            aria-label={t('common:close')}
            onClick={onClose}
          >
            <CloseIcon aria-hidden="true" />
          </button>
        </header>

        <div className="shell-attention-drawer__body">
          <AttentionDrawerContent
            contextState={contextState}
            competitionId={competitionId}
            pending={pending}
            error={attentionQuery.error}
            items={items}
            matchRows={matchRows}
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
  matchRows,
  onNavigate,
}: {
  contextState: ReturnType<typeof useShellCompetitionContext>['state']
  competitionId?: string
  pending: boolean
  error: unknown
  items: NeedsAttentionItem[]
  matchRows: AttentionMatchRow[]
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
        <div
          className="ds-state ds-state--neutral"
          aria-label={t('attention.emptyAria')}
        >
          <span className="ds-state__figure">0</span>
          <span className="ds-state__label">{t('attention.emptyTitle')}</span>
        </div>
        <p className="shell-attention-drawer__hint ds-meta">
          {t('attention.emptyHint')}
        </p>
      </div>
    )
  }

  return (
    <ul className="shell-attention-drawer__list">
      {items.map((item) => (
        <AttentionDrawerItem
          key={`${item.source}:${item.targetType}:${item.targetId}:${item.reason}`}
          item={item}
          matchRows={matchRows}
          competitionId={competitionId}
          onNavigate={onNavigate}
        />
      ))}
    </ul>
  )
}

function AttentionDrawerItem({
  item,
  matchRows,
  competitionId,
  onNavigate,
}: {
  item: NeedsAttentionItem
  matchRows: AttentionMatchRow[]
  competitionId: string
  onNavigate: () => void
}) {
  const { t } = useTranslation('shell')
  const href =
    attentionItemHref(item, matchRows) ??
    (item.targetType === 'Fixture'
      ? `/competitions/${competitionId}/matches`
      : null)

  const severityClass = attentionSeverityStateClass(item.severity)
  const contextLabel = attentionItemContextLabel(item)

  const content = (
    <>
      <div className="shell-attention-drawer__item-main">
        <div className={`ds-state ${severityClass}`}>
          <AttentionMarkIcon className="ds-state__icon" aria-hidden="true" />
          <span className="ds-state__label">{item.reason}</span>
        </div>
        <p className="shell-attention-drawer__item-context ds-meta">
          {contextLabel}
        </p>
      </div>
      {href && (
        <span className="shell-attention-drawer__item-action ds-meta">
          {t('attention.open')}
        </span>
      )}
    </>
  )

  return (
    <li className="shell-attention-drawer__item">
      {href ? (
        <Link
          className="shell-attention-drawer__item-link"
          to={href}
          onClick={onNavigate}
        >
          {content}
        </Link>
      ) : (
        <div className="shell-attention-drawer__item-static">{content}</div>
      )}
    </li>
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
        last.focus()
      } else if (!event.shiftKey && activeElement === last) {
        event.preventDefault()
        first.focus()
      }
    }

    container.addEventListener('keydown', onKeyDown)
    return () => container.removeEventListener('keydown', onKeyDown)
  }, [active, containerRef])
}

function CloseIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 20 20" {...props}>
      <path
        d="m5 5 10 10M15 5 5 15"
        fill="none"
        stroke="currentColor"
        strokeLinecap="round"
        strokeWidth="1.75"
      />
    </svg>
  )
}

function AttentionMarkIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 20 20" {...props}>
      <path
        d="M10 3.5 17.5 16.5H2.5L10 3.5Z"
        fill="none"
        stroke="currentColor"
        strokeLinejoin="round"
        strokeWidth="1.75"
      />
      <path
        d="M10 8.5v4"
        stroke="currentColor"
        strokeLinecap="round"
        strokeWidth="1.75"
      />
      <circle cx="10" cy="14.25" fill="currentColor" r="0.8" />
    </svg>
  )
}

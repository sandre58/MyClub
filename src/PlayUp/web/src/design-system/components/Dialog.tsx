import {
  useEffect,
  useId,
  useRef,
  useState,
  type ReactNode,
  type RefObject,
} from 'react'
import { CloseIcon } from '../icons/shellIcons'
import { DS_MOTION_EXIT_MS } from '../motion'
import { getFocusableElements, useFocusTrap } from '../useFocusTrap'

export type DialogSize = 'sm' | 'md'

export type DialogProps = {
  open: boolean
  onClose: () => void
  title: string
  children: ReactNode
  /** Right-aligned action row. Close lives in the header only. */
  footer?: ReactNode
  /** Blocks Escape, backdrop, and the header close control. */
  closeDisabled?: boolean
  /**
   * When false, Tab is not trapped (e.g. parent dialog while a ConfirmDialog
   * is stacked on top). Default true.
   */
  trapFocus?: boolean
  /** Element to restore focus on close. Defaults to the opener at mount. */
  returnFocusRef?: RefObject<HTMLElement | null>
  size?: DialogSize
  /** Accessible name for the icon close control. */
  closeLabel?: string
}

/**
 * Centered overlay chrome — title, body, optional footer.
 * Not a window manager; AttentionDrawer stays separate (Shell triage).
 */
export function Dialog({
  open,
  onClose,
  title,
  children,
  footer,
  closeDisabled = false,
  trapFocus = true,
  returnFocusRef,
  size = 'sm',
  closeLabel = 'Fermer',
}: DialogProps) {
  const titleId = useId()
  const panelRef = useRef<HTMLDivElement>(null)
  const bodyRef = useRef<HTMLDivElement>(null)
  const closeButtonRef = useRef<HTMLButtonElement>(null)
  const hadOpenedRef = useRef(false)
  const fallbackReturnRef = useRef<HTMLElement | null>(null)
  const [mounted, setMounted] = useState(open)
  const [visible, setVisible] = useState(open)

  useEffect(() => {
    if (open) {
      setMounted(true)
      const frame = requestAnimationFrame(() => {
        requestAnimationFrame(() => setVisible(true))
      })
      return () => cancelAnimationFrame(frame)
    }

    setVisible(false)
    const timeout = window.setTimeout(() => setMounted(false), DS_MOTION_EXIT_MS)
    return () => window.clearTimeout(timeout)
  }, [open])

  useEffect(() => {
    if (open) {
      hadOpenedRef.current = true
      const active = document.activeElement
      if (active instanceof HTMLElement) {
        fallbackReturnRef.current = active
      }
    }
  }, [open])

  useEffect(() => {
    if (hadOpenedRef.current && !mounted) {
      hadOpenedRef.current = false
      const target = returnFocusRef?.current ?? fallbackReturnRef.current
      target?.focus({ preventScroll: true })
      fallbackReturnRef.current = null
    }
  }, [mounted, returnFocusRef])

  useEffect(() => {
    if (!open || !visible) {
      return
    }

    const body = bodyRef.current
    const bodyFocusable = body ? getFocusableElements(body) : []
    const initial = bodyFocusable[0] ?? closeButtonRef.current
    initial?.focus({ preventScroll: true })
  }, [open, visible])

  useEffect(() => {
    if (!open) {
      return
    }

    function onKeyDown(event: KeyboardEvent) {
      if (event.key !== 'Escape' || closeDisabled) {
        return
      }
      event.preventDefault()
      onClose()
    }

    document.addEventListener('keydown', onKeyDown)
    return () => document.removeEventListener('keydown', onKeyDown)
  }, [open, onClose, closeDisabled])

  useEffect(() => {
    if (!mounted || !panelRef.current) {
      return
    }

    const panel = panelRef.current
    const inerted: HTMLElement[] = []
    let current: HTMLElement | null = panel

    while (current && current !== document.body) {
      const parent: HTMLElement | null = current.parentElement
      if (!parent) {
        break
      }
      for (const sibling of Array.from(parent.children)) {
        if (sibling !== current && sibling instanceof HTMLElement) {
          if (!sibling.inert) {
            sibling.inert = true
            inerted.push(sibling)
          }
        }
      }
      current = parent
    }

    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'

    return () => {
      for (const element of inerted) {
        element.inert = false
      }
      document.body.style.overflow = previousOverflow
    }
  }, [mounted])

  useFocusTrap(panelRef, open && visible && trapFocus)

  if (!mounted) {
    return null
  }

  return (
    <div className="ds-dialog" data-open={visible ? 'true' : 'false'} data-size={size}>
      <button
        type="button"
        className="ds-dialog__backdrop"
        aria-hidden="true"
        tabIndex={-1}
        disabled={closeDisabled}
        onClick={() => {
          if (!closeDisabled) {
            onClose()
          }
        }}
      />
      <div
        ref={panelRef}
        className="ds-dialog__panel ds-overlay"
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
      >
        <header className="ds-dialog__header">
          <h3 id={titleId} className="ds-dialog__title">
            {title}
          </h3>
          <button
            ref={closeButtonRef}
            type="button"
            className="ds-btn ds-btn--ghost ds-icon-button"
            aria-label={closeLabel}
            title={closeLabel}
            disabled={closeDisabled}
            onClick={onClose}
          >
            <CloseIcon size="md" aria-hidden="true" />
          </button>
        </header>
        <div ref={bodyRef} className="ds-dialog__body">
          {children}
        </div>
        {footer != null ? (
          <div className="ds-dialog__footer">{footer}</div>
        ) : null}
      </div>
    </div>
  )
}

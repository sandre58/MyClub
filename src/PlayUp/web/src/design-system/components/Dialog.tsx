import {
  useEffect,
  useId,
  useLayoutEffect,
  useRef,
  type ReactNode,
  type RefObject,
} from 'react';
import { CloseIcon } from '../icons/shellIcons';
import { DS_MOTION_EXIT_MS } from '../motion';
import { usePresence } from '../usePresence';
import { useDismissLayer } from '../useDismissLayer';
import { getFocusableElements, useFocusTrap } from '../useFocusTrap';
import { Tooltip } from './Tooltip';

export type DialogSize = 'sm' | 'md' | 'lg';

export type DialogProps = {
  open: boolean;
  onClose: () => void;
  title: string;
  /** Optional subtitle under the title (e.g. regulation editor scope). */
  description?: string;
  children: ReactNode;
  /**
   * Non-blocking window status / notice — left of actions, fills remaining
   * width. Prefer this over body notices so the scroll region does not shift.
   */
  footerStatus?: ReactNode;
  /** Action controls — right-aligned (full row under status on narrow). */
  footer?: ReactNode;
  /** Blocks Escape, backdrop, and the header close control. */
  closeDisabled?: boolean;
  /**
   * When false, Tab is not trapped (e.g. parent dialog while a ConfirmDialog
   * is stacked on top). Default true.
   */
  trapFocus?: boolean;
  /** Element to restore focus on close. Defaults to the opener at mount. */
  returnFocusRef?: RefObject<HTMLElement | null>;
  size?: DialogSize;
  /** Accessible name for the icon close control. */
  closeLabel?: string;
  /**
   * Where to place focus when the dialog opens (and when `focusKey` changes).
   * `body` prefers the first body control. `footer` uses the footer rule only
   * (primary, then a non-destructive footer action, then the header X).
   */
  initialFocus?: 'body' | 'footer';
  /**
   * Re-applies `initialFocus` while the dialog stays open. Pass a value that
   * changes with the product state (for example the current draw status).
   */
  focusKey?: string;
};

function pickFooterInitialFocus(
  footer: HTMLElement | null,
  closeButton: HTMLButtonElement | null,
): HTMLElement | null {
  const primary = footer?.querySelector<HTMLElement>(
    '.ds-btn--primary:not(:disabled)',
  );
  if (primary) {
    return primary;
  }
  const neutral = footer
    ? [...footer.querySelectorAll<HTMLButtonElement>('button')].find(
        (button) =>
          !button.disabled &&
          !button.classList.contains('ds-btn--primary') &&
          !button.classList.contains('ds-btn--destructive'),
      )
    : undefined;
  if (neutral) {
    return neutral;
  }
  if (closeButton && !closeButton.disabled) {
    return closeButton;
  }
  return null;
}

/**
 * Centered overlay chrome — title, body, optional footer.
 * Footer = optional status (left, fluid width) + actions (right).
 * Not a window manager; AttentionDrawer stays separate (Shell triage).
 */
export function Dialog({
  open,
  onClose,
  title,
  description,
  children,
  footerStatus,
  footer,
  closeDisabled = false,
  trapFocus = true,
  returnFocusRef,
  size = 'sm',
  closeLabel = 'Fermer',
  initialFocus = 'body',
  focusKey,
}: DialogProps) {
  const titleId = useId();
  const descriptionId = useId();
  const panelRef = useRef<HTMLDivElement>(null);
  const bodyRef = useRef<HTMLDivElement>(null);
  const footerRef = useRef<HTMLDivElement>(null);
  const closeButtonRef = useRef<HTMLButtonElement>(null);
  const hadOpenedRef = useRef(false);
  const fallbackReturnRef = useRef<HTMLElement | null>(null);
  const { present: mounted, state: presenceState } = usePresence(
    open,
    DS_MOTION_EXIT_MS,
  );
  const visible = presenceState === 'open';

  useEffect(() => {
    if (open) {
      hadOpenedRef.current = true;
      const active = document.activeElement;
      if (active instanceof HTMLElement) {
        fallbackReturnRef.current = active;
      }
    }
  }, [open]);

  // Return focus in the commit that removes the dialog, before paint.
  useLayoutEffect(() => {
    if (hadOpenedRef.current && !mounted) {
      hadOpenedRef.current = false;
      const target = returnFocusRef?.current ?? fallbackReturnRef.current;
      target?.focus({ preventScroll: true });
      fallbackReturnRef.current = null;
    }
  }, [mounted, returnFocusRef]);

  useEffect(() => {
    if (!open || !visible) {
      return;
    }

    const body = bodyRef.current;
    const bodyFocusable = body ? getFocusableElements(body) : [];
    const footerInitial = pickFooterInitialFocus(
      footerRef.current,
      closeButtonRef.current,
    );
    // Body first by default. Footer mode never lands on a destructive action
    // or on a body control: primary, then a neutral footer action, then X.
    const initial =
      initialFocus === 'footer'
        ? footerInitial
        : (bodyFocusable[0] ?? footerInitial);
    initial?.focus({ preventScroll: true });
  }, [open, visible, initialFocus, focusKey]);

  // Stay on the dismiss stack while open so Escape is consumed even when
  // closeDisabled (nested popovers still dismiss first via LIFO).
  useDismissLayer(open, () => {
    if (!closeDisabled) {
      onClose();
    }
  });

  // Release the scroll lock in the commit that removes the dialog, before paint.
  useLayoutEffect(() => {
    if (!mounted || !panelRef.current) {
      return;
    }

    const panel = panelRef.current;
    const inerted: HTMLElement[] = [];
    let current: HTMLElement | null = panel;

    while (current && current !== document.body) {
      const parent: HTMLElement | null = current.parentElement;
      if (!parent) {
        break;
      }
      for (const sibling of Array.from(parent.children)) {
        if (sibling !== current && sibling instanceof HTMLElement) {
          if (!sibling.inert) {
            sibling.inert = true;
            inerted.push(sibling);
          }
        }
      }
      current = parent;
    }

    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';

    return () => {
      for (const element of inerted) {
        element.inert = false;
      }
      document.body.style.overflow = previousOverflow;
    };
  }, [mounted]);

  useFocusTrap(panelRef, open && visible && trapFocus);

  if (!mounted) {
    return null;
  }

  const showFooter = footer != null || footerStatus != null;

  return (
    <div
      className="ds-dialog"
      data-open={visible ? 'true' : 'false'}
      data-size={size}
    >
      <button
        type="button"
        className="ds-dialog__backdrop"
        aria-hidden="true"
        tabIndex={-1}
        disabled={closeDisabled}
        onClick={() => {
          if (!closeDisabled) {
            onClose();
          }
        }}
      />
      <div
        ref={panelRef}
        className="ds-dialog__panel ds-overlay"
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        aria-describedby={description ? descriptionId : undefined}
      >
        <header className="ds-dialog__header">
          <div className="ds-dialog__heading">
            <h3 id={titleId} className="ds-dialog__title">
              {title}
            </h3>
            {description ? (
              <p id={descriptionId} className="ds-dialog__description">
                {description}
              </p>
            ) : null}
          </div>
          <Tooltip content={closeLabel}>
            <button
              ref={closeButtonRef}
              type="button"
              className="ds-btn ds-btn--ghost ds-icon-button"
              aria-label={closeLabel}
              disabled={closeDisabled}
              onClick={onClose}
            >
              <CloseIcon size="md" aria-hidden="true" />
            </button>
          </Tooltip>
        </header>
        <div ref={bodyRef} className="ds-dialog__body">
          {children}
        </div>
        {showFooter ? (
          <div ref={footerRef} className="ds-dialog__footer">
            {footerStatus != null ? (
              <div className="ds-dialog__footer-status">{footerStatus}</div>
            ) : null}
            {footer != null ? (
              <div className="ds-dialog__footer-actions">{footer}</div>
            ) : null}
          </div>
        ) : null}
      </div>
    </div>
  );
}

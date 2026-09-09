import {
  useEffect,
  useId,
  useRef,
  useState,
  type ReactNode,
  type RefObject,
} from 'react';
import { CloseIcon } from '../icons/shellIcons';
import { DS_MOTION_EXIT_MS } from '../motion';
import { useDismissLayer } from '../useDismissLayer';
import { getFocusableElements, useFocusTrap } from '../useFocusTrap';

export type DialogSize = 'sm' | 'md' | 'lg';

export type DialogProps = {
  open: boolean;
  onClose: () => void;
  title: string;
  /** Optional subtitle under the title (e.g. regulation editor scope). */
  description?: string;
  children: ReactNode;
  /** Right-aligned action row. Close lives in the header only. */
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
};

function pickFooterInitialFocus(footer: HTMLElement): HTMLElement | null {
  const preferred = footer.querySelector<HTMLElement>(
    '.ds-btn--primary:not(:disabled), .ds-btn--destructive:not(:disabled)',
  );
  if (preferred) {
    return preferred;
  }
  const focusable = getFocusableElements(footer);
  return focusable[focusable.length - 1] ?? null;
}

/**
 * Centered overlay chrome — title, body, optional footer.
 * Not a window manager; AttentionDrawer stays separate (Shell triage).
 */
export function Dialog({
  open,
  onClose,
  title,
  description,
  children,
  footer,
  closeDisabled = false,
  trapFocus = true,
  returnFocusRef,
  size = 'sm',
  closeLabel = 'Fermer',
}: DialogProps) {
  const titleId = useId();
  const descriptionId = useId();
  const panelRef = useRef<HTMLDivElement>(null);
  const bodyRef = useRef<HTMLDivElement>(null);
  const footerRef = useRef<HTMLDivElement>(null);
  const closeButtonRef = useRef<HTMLButtonElement>(null);
  const hadOpenedRef = useRef(false);
  const fallbackReturnRef = useRef<HTMLElement | null>(null);
  const [mounted, setMounted] = useState(open);
  const [visible, setVisible] = useState(open);

  useEffect(() => {
    if (open) {
      setMounted(true);
      const frame = requestAnimationFrame(() => {
        requestAnimationFrame(() => setVisible(true));
      });
      return () => cancelAnimationFrame(frame);
    }

    setVisible(false);
    const timeout = window.setTimeout(
      () => setMounted(false),
      DS_MOTION_EXIT_MS,
    );
    return () => window.clearTimeout(timeout);
  }, [open]);

  useEffect(() => {
    if (open) {
      hadOpenedRef.current = true;
      const active = document.activeElement;
      if (active instanceof HTMLElement) {
        fallbackReturnRef.current = active;
      }
    }
  }, [open]);

  useEffect(() => {
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
    const footerInitial = footerRef.current
      ? pickFooterInitialFocus(footerRef.current)
      : null;
    // Prefer a body field; otherwise the primary footer action (Enter confirms).
    // Never default to the header close control when an action footer exists.
    const initial = bodyFocusable[0] ?? footerInitial ?? closeButtonRef.current;
    initial?.focus({ preventScroll: true });
  }, [open, visible]);

  // Stay on the dismiss stack while open so Escape is consumed even when
  // closeDisabled (nested popovers still dismiss first via LIFO).
  useDismissLayer(open, () => {
    if (!closeDisabled) {
      onClose();
    }
  });

  useEffect(() => {
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
          <div ref={footerRef} className="ds-dialog__footer">
            {footer}
          </div>
        ) : null}
      </div>
    </div>
  );
}

import { useEffect, useRef, useState, useSyncExternalStore } from 'react';
import { ToastToneIcon } from '../icons/toastIcons';
import { CloseIcon } from '../icons/shellIcons';
import { DS_MOTION_EXIT_MS } from '../motion';
import {
  dismissToast,
  getToastsServerSnapshot,
  getToastsSnapshot,
  subscribeToasts,
  TOAST_DURATION_MS,
  type ToastItem,
} from '../toastStore';

export type ToasterProps = {
  /** Accessible name for the dismiss control. */
  closeLabel?: string;
};

/**
 * Canvas-local toast host — mount inside a `position: relative` workspace
 * (shell-main / Design Lab board). Bottom-end stack; does not steal focus.
 * Soft fill (~16% tone), no border; tone icon + 2px progress (pauses on hover; shrinks left).
 */
export function Toaster({ closeLabel = 'Fermer' }: ToasterProps) {
  const toasts = useSyncExternalStore(
    subscribeToasts,
    getToastsSnapshot,
    getToastsServerSnapshot,
  );

  if (toasts.length === 0) {
    return null;
  }

  return (
    <div className="ds-toaster" aria-label="Notifications">
      {toasts.map((toast) => (
        <ToastView key={toast.id} toast={toast} closeLabel={closeLabel} />
      ))}
    </div>
  );
}

function ToastView({
  toast,
  closeLabel,
}: {
  toast: ToastItem;
  closeLabel: string;
}) {
  const [open, setOpen] = useState(false);
  const [leaving, setLeaving] = useState(false);
  const [paused, setPaused] = useState(false);
  const rootRef = useRef<HTMLDivElement>(null);
  const remainingRef = useRef(TOAST_DURATION_MS[toast.tone]);
  const startedAtRef = useRef(0);
  const timerRef = useRef<number | null>(null);
  const leavingRef = useRef(false);

  useEffect(() => {
    function clearTimer() {
      if (timerRef.current != null) {
        window.clearTimeout(timerRef.current);
        timerRef.current = null;
      }
    }

    function beginLeave() {
      if (leavingRef.current) {
        return;
      }
      leavingRef.current = true;
      clearTimer();
      setPaused(false);
      setLeaving(true);
      setOpen(false);
    }

    function scheduleDismiss(delay: number) {
      clearTimer();
      if (delay <= 0) {
        beginLeave();
        return;
      }
      startedAtRef.current = Date.now();
      timerRef.current = window.setTimeout(() => {
        timerRef.current = null;
        beginLeave();
      }, delay);
    }

    const frame = requestAnimationFrame(() => {
      requestAnimationFrame(() => setOpen(true));
    });
    scheduleDismiss(remainingRef.current);

    const node = rootRef.current;

    function onEnter() {
      setPaused(true);
      if (timerRef.current != null) {
        const elapsed = Date.now() - startedAtRef.current;
        remainingRef.current = Math.max(0, remainingRef.current - elapsed);
        clearTimer();
      }
    }

    function onLeaveHover() {
      setPaused(false);
      if (!leavingRef.current) {
        scheduleDismiss(remainingRef.current);
      }
    }

    node?.addEventListener('mouseenter', onEnter);
    node?.addEventListener('mouseleave', onLeaveHover);

    return () => {
      cancelAnimationFrame(frame);
      clearTimer();
      node?.removeEventListener('mouseenter', onEnter);
      node?.removeEventListener('mouseleave', onLeaveHover);
    };
  }, [toast.id]);

  useEffect(() => {
    if (!leaving) {
      return;
    }
    const timeout = window.setTimeout(() => {
      dismissToast(toast.id);
    }, DS_MOTION_EXIT_MS);
    return () => window.clearTimeout(timeout);
  }, [leaving, toast.id]);

  return (
    <div
      ref={rootRef}
      className="ds-toast"
      data-tone={toast.tone}
      data-open={open ? 'true' : 'false'}
      data-paused={paused ? 'true' : 'false'}
      role={toast.tone === 'error' ? 'alert' : 'status'}
      aria-live={toast.tone === 'error' ? 'assertive' : 'polite'}
    >
      <div className="ds-toast__body">
        <span className="ds-toast__icon" aria-hidden="true">
          <ToastToneIcon tone={toast.tone} size="md" />
        </span>
        <p className="ds-toast__message">{toast.message}</p>
        <button
          type="button"
          className="ds-btn ds-btn--ghost ds-icon-button ds-toast__close"
          aria-label={closeLabel}
          title={closeLabel}
          onClick={() => {
            if (leavingRef.current) {
              return;
            }
            leavingRef.current = true;
            if (timerRef.current != null) {
              window.clearTimeout(timerRef.current);
              timerRef.current = null;
            }
            setPaused(false);
            setLeaving(true);
            setOpen(false);
          }}
        >
          <CloseIcon size="sm" aria-hidden="true" />
        </button>
      </div>
      <div className="ds-toast__progress" aria-hidden="true" />
    </div>
  );
}

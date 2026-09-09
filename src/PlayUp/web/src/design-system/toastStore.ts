/**
 * Module store for ephemeral toasts — no global UI store library.
 * Toaster subscribes via useSyncExternalStore; callers use `notify`.
 */

export type ToastTone = 'success' | 'error' | 'info' | 'attention';

/**
 * Reserved for a future 0–1 event action (Réessayer, etc.).
 * Accepted by the API in V1 but not rendered — see Notion Toast decision.
 */
export type ToastAction = {
  label: string;
  onAction: () => void;
};

export type NotifyOptions = {
  action?: ToastAction;
};

export type ToastItem = {
  id: string;
  message: string;
  tone: ToastTone;
  /** Reserved — not shown in V1. */
  action?: ToastAction;
};

export const TOAST_MAX_VISIBLE = 3;

export const TOAST_DURATION_MS: Record<ToastTone, number> = {
  success: 4000,
  info: 4000,
  attention: 6000,
  /** Sticky-ish: long auto-dismiss; dismiss manuel reste prioritaire. */
  error: 10_000,
};

let toasts: ToastItem[] = [];
let seq = 0;
const listeners = new Set<() => void>();

function emit() {
  for (const listener of listeners) {
    listener();
  }
}

export function subscribeToasts(listener: () => void): () => void {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}

export function getToastsSnapshot(): ToastItem[] {
  return toasts;
}

export function getToastsServerSnapshot(): ToastItem[] {
  return [];
}

function pushToast(
  tone: ToastTone,
  message: string,
  options?: NotifyOptions,
): string {
  const trimmed = message.trim();
  if (!trimmed) {
    return '';
  }

  seq += 1;
  const id = `toast-${seq}`;
  const next: ToastItem = {
    id,
    message: trimmed,
    tone,
    action: options?.action,
  };

  const stacked = [...toasts, next];
  toasts =
    stacked.length > TOAST_MAX_VISIBLE
      ? stacked.slice(stacked.length - TOAST_MAX_VISIBLE)
      : stacked;
  emit();
  return id;
}

export function dismissToast(id: string): void {
  const next = toasts.filter((toast) => toast.id !== id);
  if (next.length === toasts.length) {
    return;
  }
  toasts = next;
  emit();
}

/** Test / lab helper — clears the queue. */
export function clearToasts(): void {
  if (toasts.length === 0) {
    return;
  }
  toasts = [];
  emit();
}

export const notify = {
  success(message: string, options?: NotifyOptions): string {
    return pushToast('success', message, options);
  },
  error(message: string, options?: NotifyOptions): string {
    return pushToast('error', message, options);
  },
  info(message: string, options?: NotifyOptions): string {
    return pushToast('info', message, options);
  },
  attention(message: string, options?: NotifyOptions): string {
    return pushToast('attention', message, options);
  },
};

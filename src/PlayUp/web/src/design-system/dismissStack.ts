/**
 * LIFO stack for Escape-dismissible layers (Dialog, popovers, page gestures).
 *
 * One document keydown listener. The topmost enabled layer consumes Escape;
 * lower layers do not see the same keypress.
 *
 * Component-local keys (arrows, Enter on Select, etc.) stay on the component —
 * this stack is only for dismiss coordination.
 */

export type DismissLayer = {
  id: string;
  onDismiss: () => void;
  /**
   * When false, skipped when resolving Escape (next enabled layer below wins).
   * Prefer leaving enabled and no-opping `onDismiss` when Escape must be
   * consumed without side effects (e.g. Dialog `closeDisabled`).
   */
  enabled?: boolean;
};

type StackEntry = {
  id: string;
  onDismiss: () => void;
  enabled: boolean;
};

const stack: StackEntry[] = [];
let listening = false;
let seq = 0;

function onDocumentKeyDown(event: KeyboardEvent) {
  if (event.key !== 'Escape') {
    return;
  }

  for (let index = stack.length - 1; index >= 0; index -= 1) {
    const layer = stack[index];
    if (!layer.enabled) {
      continue;
    }
    event.preventDefault();
    layer.onDismiss();
    return;
  }
}

function ensureListener() {
  if (listening) {
    return;
  }
  document.addEventListener('keydown', onDocumentKeyDown);
  listening = true;
}

function teardownListenerIfEmpty() {
  if (stack.length > 0 || !listening) {
    return;
  }
  document.removeEventListener('keydown', onDocumentKeyDown);
  listening = false;
}

export type DismissLayerHandle = {
  /** Update without reordering the stack (avoids LIFO corruption on re-render). */
  setEnabled: (enabled: boolean) => void;
  unregister: () => void;
};

/**
 * Push a dismiss layer onto the LIFO stack.
 * Keep `onDismiss` stable (ref wrapper); toggle participation via `setEnabled`.
 */
export function pushDismissLayer(
  onDismiss: () => void,
  options?: { enabled?: boolean },
): DismissLayerHandle {
  const id = `dismiss-${++seq}`;
  const entry: StackEntry = {
    id,
    onDismiss,
    enabled: options?.enabled !== false,
  };
  stack.push(entry);
  ensureListener();

  return {
    setEnabled(enabled: boolean) {
      entry.enabled = enabled;
    },
    unregister() {
      const index = stack.findIndex((candidate) => candidate.id === id);
      if (index >= 0) {
        stack.splice(index, 1);
      }
      teardownListenerIfEmpty();
    },
  };
}

/** @internal — tests only */
export function __resetDismissStackForTests() {
  stack.length = 0;
  if (listening) {
    document.removeEventListener('keydown', onDocumentKeyDown);
    listening = false;
  }
}

/** @internal — tests only */
export function __dismissStackDepthForTests() {
  return stack.length;
}

/** @internal — tests only */
export function __dismissStackEnabledDepthForTests() {
  return stack.filter((entry) => entry.enabled).length;
}

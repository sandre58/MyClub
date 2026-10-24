import { useState } from 'react';

type OpenEdgeHandlers = {
  onOpen?: () => void;
  onClose?: () => void;
};

/** Run handlers when `open` crosses true/false (during render). */
export function useOpenEdge(open: boolean, handlers: OpenEdgeHandlers): void {
  const [wasOpen, setWasOpen] = useState(open);
  if (open === wasOpen) {
    return;
  }
  setWasOpen(open);
  if (open) {
    handlers.onOpen?.();
  } else {
    handlers.onClose?.();
  }
}

/**
 * When `active` is true and `key` changes, run `sync` during render.
 * Clears the remembered key when `active` is false.
 */
export function useSyncWhen(
  active: boolean,
  key: string,
  sync: () => void,
): void {
  const [lastKey, setLastKey] = useState<string | null>(null);

  if (!active) {
    if (lastKey !== null) {
      setLastKey(null);
    }
    return;
  }

  if (key === lastKey) {
    return;
  }

  setLastKey(key);
  sync();
}

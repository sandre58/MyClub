import { useEffect } from 'react';

export type ResettableMutation = {
  reset: () => void;
};

/**
 * Clears TanStack mutation error/success state when an overlay opens so a failed
 * submit does not reappear after the user closes and reopens the dialog.
 */
export function useClearMutationsOnOpen(
  open: boolean,
  ...mutations: ResettableMutation[]
): void {
  useEffect(() => {
    if (!open) {
      return;
    }
    for (const mutation of mutations) {
      mutation.reset();
    }
    // Open edge only — mutation identities are stable; including them retriggers every render.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open]);
}

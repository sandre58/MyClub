import { useOpenEdge } from './useRenderSync';

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
  useOpenEdge(open, {
    onOpen: () => {
      for (const mutation of mutations) {
        mutation.reset();
      }
    },
  });
}

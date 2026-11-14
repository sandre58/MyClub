import { useEffect, useRef } from 'react';

const SLOT_FOCUS_MS = 2_500;

/**
 * Scrolls to a schematic place, highlights it briefly, and focuses its freshness action.
 */
export function useStructureSlotFocus(
  slotKey: string | null | undefined,
  schematicReady: boolean,
  onConsumed?: () => void,
): void {
  const consumedRef = useRef(false);

  useEffect(() => {
    consumedRef.current = false;
  }, [slotKey]);

  useEffect(() => {
    if (!slotKey || !schematicReady || consumedRef.current) {
      return;
    }

    let slot: HTMLElement | null = null;
    let timer: number | undefined;
    const frame = requestAnimationFrame(() => {
      slot = document.querySelector<HTMLElement>(
        `[data-slot-key="${CSS.escape(slotKey)}"]`,
      );
      if (!slot) {
        return;
      }

      consumedRef.current = true;
      slot.scrollIntoView({ behavior: 'smooth', block: 'center' });
      slot.classList.add('schematic-slot--focus');

      const action = slot.querySelector<HTMLElement>(
        '.structure-freshness-badge__action:not(:disabled)',
      );
      action?.focus({ preventScroll: true });

      timer = window.setTimeout(() => {
        slot?.classList.remove('schematic-slot--focus');
        onConsumed?.();
      }, SLOT_FOCUS_MS);
    });

    return () => {
      cancelAnimationFrame(frame);
      if (timer !== undefined) {
        window.clearTimeout(timer);
      }
      slot?.classList.remove('schematic-slot--focus');
    };
  }, [slotKey, schematicReady, onConsumed]);
}

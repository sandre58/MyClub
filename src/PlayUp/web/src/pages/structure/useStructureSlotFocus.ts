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
  const onConsumedRef = useRef(onConsumed);
  useEffect(() => {
    onConsumedRef.current = onConsumed;
  });

  const visualSlotRef = useRef<string | null>(null);
  const consumedOnceRef = useRef(false);

  useEffect(() => {
    if (slotKey) {
      consumedOnceRef.current = false;
      if (slotKey !== visualSlotRef.current) {
        visualSlotRef.current = null;
      }
    }
  }, [slotKey]);

  useEffect(() => {
    const highlightKey = slotKey ?? visualSlotRef.current;
    if (!highlightKey || !schematicReady) {
      return;
    }

    let slot: HTMLElement | null = null;
    let timer: number | undefined;
    const frame = requestAnimationFrame(() => {
      slot = document.querySelector<HTMLElement>(
        `[data-slot-key="${CSS.escape(highlightKey)}"]`,
      );
      if (!slot) {
        return;
      }

      if (slotKey && !consumedOnceRef.current) {
        consumedOnceRef.current = true;
        visualSlotRef.current = slotKey;
        onConsumedRef.current?.();
      }

      slot.scrollIntoView({ behavior: 'smooth', block: 'center' });
      slot.classList.add('schematic-slot--focus');

      const action = slot.querySelector<HTMLElement>(
        '.structure-freshness-badge__action:not(:disabled)',
      );
      action?.focus({ preventScroll: true });

      if (timer !== undefined) {
        window.clearTimeout(timer);
      }
      timer = window.setTimeout(() => {
        slot?.classList.remove('schematic-slot--focus');
        if (visualSlotRef.current === highlightKey) {
          visualSlotRef.current = null;
        }
      }, SLOT_FOCUS_MS);
    });

    return () => {
      cancelAnimationFrame(frame);
      if (timer !== undefined) {
        window.clearTimeout(timer);
      }
      if (slotKey && slotKey !== visualSlotRef.current) {
        slot?.classList.remove('schematic-slot--focus');
      }
    };
  }, [slotKey, schematicReady]);
}

import { useEffect, useState } from 'react';

export type PresenceState = 'open' | 'closed';

/**
 * Keep a surface mounted through its exit transition.
 * `present` stays true until `closed` has had time to animate out.
 */
export function usePresence(
  open: boolean,
  exitMs = 200,
): { present: boolean; state: PresenceState } {
  const [present, setPresent] = useState(open);
  const [state, setState] = useState<PresenceState>(open ? 'open' : 'closed');

  useEffect(() => {
    if (open) {
      setPresent(true);
      const frame = window.requestAnimationFrame(() => {
        window.requestAnimationFrame(() => {
          setState('open');
        });
      });
      return () => window.cancelAnimationFrame(frame);
    }

    setState('closed');
    return undefined;
  }, [open]);

  useEffect(() => {
    if (open || !present) {
      return;
    }

    const timeout = window.setTimeout(() => {
      setPresent(false);
    }, exitMs);

    return () => window.clearTimeout(timeout);
  }, [open, present, exitMs]);

  return { present, state };
}

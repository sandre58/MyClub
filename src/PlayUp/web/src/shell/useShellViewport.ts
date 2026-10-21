import { useEffect, useState } from 'react';
import {
  SHELL_PHONE_QUERY,
  SHELL_TABLET_QUERY,
} from '../layout/viewportBreakpoints';

export type ShellViewport = 'phone' | 'tablet' | 'desktop';

export { SHELL_PHONE_QUERY, SHELL_TABLET_QUERY };

export function readShellViewport(
  matchMedia: (query: string) => { matches: boolean } = window.matchMedia,
): ShellViewport {
  if (matchMedia(SHELL_PHONE_QUERY).matches) {
    return 'phone';
  }

  if (matchMedia(SHELL_TABLET_QUERY).matches) {
    return 'tablet';
  }

  return 'desktop';
}

/**
 * Shell chrome viewport. Phone = overlay rail; tablet = icon rail;
 * desktop = user expand/collapse (localStorage).
 */
export function useShellViewport(): ShellViewport {
  const [viewport, setViewport] = useState<ShellViewport>(() => {
    if (
      typeof window === 'undefined' ||
      typeof window.matchMedia !== 'function'
    ) {
      return 'desktop';
    }

    return readShellViewport();
  });

  useEffect(() => {
    if (typeof window.matchMedia !== 'function') {
      return;
    }

    const phone = window.matchMedia(SHELL_PHONE_QUERY);
    const tablet = window.matchMedia(SHELL_TABLET_QUERY);
    const update = () => setViewport(readShellViewport());

    phone.addEventListener('change', update);
    tablet.addEventListener('change', update);
    update();

    return () => {
      phone.removeEventListener('change', update);
      tablet.removeEventListener('change', update);
    };
  }, []);

  return viewport;
}

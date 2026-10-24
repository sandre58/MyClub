import { useEffect, useState } from 'react';
import type { ShellViewport } from './useShellViewport';

export function useShellChromeMotion(viewport: ShellViewport): {
  motionViewport: ShellViewport;
  chromeReady: boolean;
} {
  const [motionViewport, setMotionViewport] = useState<ShellViewport>(viewport);
  const [chromeReady, setChromeReady] = useState(viewport === 'desktop');
  const [prevViewport, setPrevViewport] = useState(viewport);

  if (viewport !== prevViewport) {
    setPrevViewport(viewport);
    if (viewport === 'desktop') {
      setMotionViewport('desktop');
      setChromeReady(true);
    } else {
      setChromeReady(false);
      setMotionViewport(viewport);
    }
  }

  useEffect(() => {
    if (viewport === 'desktop') {
      return;
    }

    let cancelled = false;
    const frame = window.requestAnimationFrame(() => {
      window.requestAnimationFrame(() => {
        if (!cancelled) {
          setChromeReady(true);
        }
      });
    });
    return () => {
      cancelled = true;
      window.cancelAnimationFrame(frame);
    };
  }, [viewport]);

  return { motionViewport, chromeReady };
}

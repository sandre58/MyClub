import { useLayoutEffect } from 'react';
import { reapplyThemeToDom } from './reapplyThemeToDom';

/**
 * Re-stamps `data-theme` on every commit. React strips attributes it does not
 * own from rendered nodes — applyTheme must stay the sole authority without JSX.
 */
export function useThemeRoot(): void {
  useLayoutEffect(() => {
    reapplyThemeToDom();
  });
}

import type { ResolvedTheme } from './config';

const ROOT_SELECTOR = '.ds-root';

/** Sole DOM authority for theme attributes — do not set data-theme elsewhere. */
export function applyTheme(resolved: ResolvedTheme): void {
  if (typeof document === 'undefined') {
    return;
  }

  document.documentElement.style.colorScheme = resolved;
  document.documentElement.dataset.theme = resolved;

  for (const root of document.querySelectorAll(ROOT_SELECTOR)) {
    root.setAttribute('data-theme', resolved);
  }
}

type SystemThemeListener = () => void;

let systemListener: SystemThemeListener | null = null;
let mediaQuery: MediaQueryList | null = null;

function onSystemThemeChange(): void {
  systemListener?.();
}

/**
 * Subscribe to OS theme changes. Call with null to unsubscribe.
 * Only active while user preference is `system` (orchestrated by setThemePreference).
 */
export function subscribeToSystemTheme(
  listener: SystemThemeListener | null,
): void {
  if (typeof window === 'undefined') {
    return;
  }

  if (mediaQuery) {
    mediaQuery.removeEventListener('change', onSystemThemeChange);
    mediaQuery = null;
  }

  systemListener = listener;

  if (!listener) {
    return;
  }

  mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
  mediaQuery.addEventListener('change', onSystemThemeChange);
}

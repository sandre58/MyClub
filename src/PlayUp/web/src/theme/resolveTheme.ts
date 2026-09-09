import type { ResolvedTheme, ThemePreference } from './config';

function systemPrefersDark(): boolean {
  if (typeof window === 'undefined') {
    return false;
  }

  return window.matchMedia('(prefers-color-scheme: dark)').matches;
}

/** Maps a user preference to the resolved light/dark theme for DOM application. */
export function resolveTheme(preference: ThemePreference): ResolvedTheme {
  if (preference === 'light') {
    return 'light';
  }

  if (preference === 'dark') {
    return 'dark';
  }

  return systemPrefersDark() ? 'dark' : 'light';
}

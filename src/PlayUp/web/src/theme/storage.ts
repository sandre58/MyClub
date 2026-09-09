import {
  DEFAULT_THEME_PREFERENCE,
  THEME_STORAGE_KEY,
  isThemePreference,
  type ThemePreference,
} from './config';

function readStoredTheme(): ThemePreference | null {
  if (typeof window === 'undefined') {
    return null;
  }

  try {
    const stored = window.localStorage.getItem(THEME_STORAGE_KEY);
    if (stored && isThemePreference(stored)) {
      return stored;
    }
  } catch {
    // private mode / blocked storage — fall through
  }

  return null;
}

export function getStoredTheme(): ThemePreference | null {
  return readStoredTheme();
}

export function setStoredTheme(preference: ThemePreference): void {
  if (typeof window === 'undefined') {
    return;
  }

  try {
    window.localStorage.setItem(THEME_STORAGE_KEY, preference);
  } catch {
    // ignore quota / private mode
  }
}

/** Stored preference or product default (`system`). */
export function resolvePreference(): ThemePreference {
  return readStoredTheme() ?? DEFAULT_THEME_PREFERENCE;
}

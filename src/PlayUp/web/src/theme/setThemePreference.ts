import { applyTheme, subscribeToSystemTheme } from './applyTheme';
import type { ThemePreference } from './config';
import { resolveTheme } from './resolveTheme';
import { resolvePreference, setStoredTheme } from './storage';

let currentPreference: ThemePreference = resolvePreference();

function syncDomFromPreference(preference: ThemePreference): void {
  applyTheme(resolveTheme(preference));
}

function syncSystemSubscription(preference: ThemePreference): void {
  if (preference === 'system') {
    subscribeToSystemTheme(() => {
      syncDomFromPreference('system');
    });
    return;
  }

  subscribeToSystemTheme(null);
}

/** Boot: apply stored/default preference and wire system listener if needed. */
export function initTheme(): void {
  currentPreference = resolvePreference();
  syncDomFromPreference(currentPreference);
  syncSystemSubscription(currentPreference);
}

/** User-facing entry: persist preference, apply DOM, manage OS listener. */
export function setThemePreference(preference: ThemePreference): void {
  currentPreference = preference;
  setStoredTheme(preference);
  syncDomFromPreference(preference);
  syncSystemSubscription(preference);
}

/** Read current preference for UI binding (not resolved theme). */
export function getThemePreference(): ThemePreference {
  return currentPreference;
}

/** Test helper — reset in-memory preference and unsubscribe OS listener. */
export function resetThemeState(preference: ThemePreference = resolvePreference()): void {
  currentPreference = preference;
  subscribeToSystemTheme(null);
}

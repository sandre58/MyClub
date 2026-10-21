import { applyTheme } from './applyTheme';
import { resolveTheme } from './resolveTheme';
import { getThemePreference } from './setThemePreference';

/** Re-scan `.ds-root` nodes after mount — uses applyTheme as sole DOM authority. */
export function reapplyThemeToDom(): void {
  applyTheme(resolveTheme(getThemePreference()));
}

/** Product default theme preference when no stored value exists. */
export const DEFAULT_THEME_PREFERENCE = 'system';

export const THEME_STORAGE_KEY = 'playup:theme';

export const THEME_PREFERENCES = ['system', 'light', 'dark'] as const;

export type ThemePreference = (typeof THEME_PREFERENCES)[number];

export const RESOLVED_THEMES = ['light', 'dark'] as const;

export type ResolvedTheme = (typeof RESOLVED_THEMES)[number];

export function isThemePreference(value: string): value is ThemePreference {
  return (THEME_PREFERENCES as readonly string[]).includes(value);
}

export function isResolvedTheme(value: string): value is ResolvedTheme {
  return (RESOLVED_THEMES as readonly string[]).includes(value);
}

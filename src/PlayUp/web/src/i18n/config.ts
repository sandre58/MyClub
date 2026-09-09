/** Product default locale — used when no stored preference and no en-* browser language. */
export const DEFAULT_LOCALE = 'fr';

export const FALLBACK_LOCALE = 'fr';

export const SUPPORTED_LOCALES = ['fr', 'en'] as const;

export type SupportedLocale = (typeof SUPPORTED_LOCALES)[number];

export const LOCALE_STORAGE_KEY = 'playup:locale';

/**
 * Registered namespaces. Prefer extending these over inventing ad-hoc strings.
 */
export const I18N_NAMESPACES = [
  'common',
  'shell',
  'enums',
  'actions',
  'overview',
  'matches',
  'draw',
  'organisation',
  'teams',
  'regulation',
  'stage',
  'home',
  'competitions',
  'classements',
  'errors',
] as const;

export type I18nNamespace = (typeof I18N_NAMESPACES)[number];

export function isSupportedLocale(value: string): value is SupportedLocale {
  return (SUPPORTED_LOCALES as readonly string[]).includes(value);
}

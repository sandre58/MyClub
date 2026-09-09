/** Product default locale — explicit, not browser-detected (14.8.2). */
export const DEFAULT_LOCALE = 'fr';

export const FALLBACK_LOCALE = 'fr';

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

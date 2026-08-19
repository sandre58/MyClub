/** Product default locale — explicit, not browser-detected (14.8.2). */
export const DEFAULT_LOCALE = 'fr'

export const FALLBACK_LOCALE = 'fr'

/**
 * Initial namespaces. Add `navigation`, `competition`, `organisation`, `matches`,
 * `enums` later without changing the init shape.
 */
export const I18N_NAMESPACES = ['common', 'shell'] as const

export type I18nNamespace = (typeof I18N_NAMESPACES)[number]

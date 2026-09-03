/**
 * Play'Up viewport breakpoints — shared media-query constants.
 *
 * SoT narrative: Notion « Shell responsive » + page Équipes (responsive).
 * Cursor rule: `.cursor/rules/design/04-layout.mdc`
 *
 * Three intentional tiers (do not collapse into one value):
 * - Shell chrome (48 / 64 rem) — rail overlay vs icon-rail vs expanded
 * - Page grids (52 rem) — operational two-column surfaces
 * - DS composition (56 rem) — asymmetric panel pairs
 */

/** Shell phone — matches `useShellViewport` / Accueil compact chrome. */
export const VIEWPORT_PHONE_MAX = '47.999rem'
export const SHELL_PHONE_QUERY = `(max-width: ${VIEWPORT_PHONE_MAX})` as const

/** Shell tablet — icon rail in-flow, header full. */
export const SHELL_TABLET_QUERY =
  '(min-width: 48rem) and (max-width: 63.999rem)' as const

/** Page grids — Overview, Organisation, Matchs, Classements, Équipes split. */
export const PAGE_GRID_MIN = '52rem'
export const PAGE_GRID_NARROW_QUERY = '(max-width: 51.999rem)' as const

/** DS asymmetric pairs — `panels.css`, layout rule 56rem. */
export const DS_COMPOSITION_NARROW_QUERY = '(max-width: 56rem)' as const

/** Équipes list/detail — same threshold as page grid narrow. */
export const TEAMS_NARROW_QUERY = PAGE_GRID_NARROW_QUERY

export function matchViewport(query: string): boolean {
  if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') {
    return false
  }
  return window.matchMedia(query).matches
}

export function isTeamsNarrowViewport(): boolean {
  return matchViewport(TEAMS_NARROW_QUERY)
}

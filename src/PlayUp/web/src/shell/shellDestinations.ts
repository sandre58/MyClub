export type ShellDestinationKey =
  | 'cockpit'
  | 'organisation'
  | 'matches'
  | 'classements'

export type ShellNavItemKey =
  | ShellDestinationKey
  | 'teams'
  | 'venues'
  | 'regulation'

export type ShellNavGroupId = 'pilotage' | 'competition' | 'referentiel'

export type ShellDestinationHrefs = Record<ShellDestinationKey, string>

export type ShellNavItemSpec = {
  key: ShellNavItemKey
  hrefKey?: ShellDestinationKey
}

export type ShellNavGroupSpec = {
  id: ShellNavGroupId
  items: readonly ShellNavItemSpec[]
}

/** Visual SoT (Shell A). Référentiel items have no product route yet. */
export const shellNavGroups: readonly ShellNavGroupSpec[] = [
  { id: 'pilotage', items: [{ key: 'cockpit', hrefKey: 'cockpit' }] },
  {
    id: 'competition',
    items: [
      { key: 'organisation', hrefKey: 'organisation' },
      { key: 'matches', hrefKey: 'matches' },
      { key: 'classements', hrefKey: 'classements' },
    ],
  },
  {
    id: 'referentiel',
    items: [{ key: 'teams' }, { key: 'venues' }, { key: 'regulation' }],
  },
]

/**
 * Structural sidebar hrefs. Uses resolved competition context when available.
 * Without a competition, competition-scoped links fall back to the list — never to invalid ids.
 */
export function shellDestinationHrefs({
  competitionId,
  stageId,
  matchId,
}: {
  competitionId?: string
  stageId?: string
  matchId?: string
}): ShellDestinationHrefs {
  const competitionListHref = '/'

  return {
    cockpit: competitionId ? `/competitions/${competitionId}` : '/',
    organisation: competitionId
      ? `/competitions/${competitionId}/organisation`
      : competitionListHref,
    matches: competitionId
      ? `/competitions/${competitionId}/matches`
      : stageId
        ? `/stages/${stageId}/matches`
        : matchId
          ? `/matches/${matchId}`
          : competitionListHref,
    classements: competitionId
      ? `/competitions/${competitionId}/classements`
      : competitionListHref,
  }
}

export function resolveActiveDestination(pathname: string): ShellDestinationKey | null {
  if (
    pathname === '/' ||
    pathname === '/competitions' ||
    /^\/competitions\/[^/]+$/.test(pathname)
  ) {
    return 'cockpit'
  }

  if (/^\/competitions\/[^/]+\/organisation(?:\/|$)/.test(pathname)) {
    return 'organisation'
  }

  if (
    /^\/competitions\/[^/]+\/matches$/.test(pathname) ||
    /^\/stages\/[^/]+\/matches$/.test(pathname) ||
    /^\/matches\/[^/]+$/.test(pathname) ||
    /^\/stages\/[^/]+$/.test(pathname)
  ) {
    return 'matches'
  }

  if (/^\/competitions\/[^/]+\/classements$/.test(pathname)) {
    return 'classements'
  }

  return null
}

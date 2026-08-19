export type ShellDestinationKey =
  | 'cockpit'
  | 'organisation'
  | 'matches'
  | 'consultation'

export type ShellDestinationHrefs = Record<ShellDestinationKey, string>

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
  const competitionListHref = '/competitions'

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
    consultation: competitionId
      ? `/competitions/${competitionId}/overview`
      : stageId
        ? `/stages/${stageId}`
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

  if (/^\/competitions\/[^/]+\/organisation$/.test(pathname)) {
    return 'organisation'
  }

  if (
    /^\/competitions\/[^/]+\/matches$/.test(pathname) ||
    /^\/stages\/[^/]+\/matches$/.test(pathname) ||
    /^\/matches\/[^/]+$/.test(pathname)
  ) {
    return 'matches'
  }

  if (
    /^\/competitions\/[^/]+\/overview$/.test(pathname) ||
    /^\/stages\/[^/]+$/.test(pathname)
  ) {
    return 'consultation'
  }

  return null
}

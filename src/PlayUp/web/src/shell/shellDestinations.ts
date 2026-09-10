export type ShellDestinationKey =
  | 'overview'
  | 'structure'
  | 'matches'
  | 'classements'
  | 'teams'
  | 'regulation';

export type ShellNavItemKey = ShellDestinationKey | 'venues';

export type ShellNavGroupId = 'pilotage' | 'competition' | 'referentiel';

export type ShellDestinationHrefs = Record<ShellDestinationKey, string>;

export type ShellNavItemSpec = {
  key: ShellNavItemKey;
  hrefKey?: ShellDestinationKey;
};

export type ShellNavGroupSpec = {
  id: ShellNavGroupId;
  items: readonly ShellNavItemSpec[];
};

/** Visual SoT (Shell A). Stades has no product route yet. */
export const shellNavGroups: readonly ShellNavGroupSpec[] = [
  { id: 'pilotage', items: [{ key: 'overview', hrefKey: 'overview' }] },
  {
    id: 'competition',
    items: [
      { key: 'structure', hrefKey: 'structure' },
      { key: 'matches', hrefKey: 'matches' },
      { key: 'classements', hrefKey: 'classements' },
    ],
  },
  {
    id: 'referentiel',
    items: [
      { key: 'teams', hrefKey: 'teams' },
      { key: 'venues' },
      { key: 'regulation', hrefKey: 'regulation' },
    ],
  },
];

/**
 * Structural sidebar hrefs. Uses resolved competition context when available.
 * Without a competition, competition-scoped links fall back to the list — never to invalid ids.
 */
export function shellDestinationHrefs({
  competitionId,
  stageId,
  matchId,
}: {
  competitionId?: string;
  stageId?: string;
  matchId?: string;
}): ShellDestinationHrefs {
  const competitionListHref = '/';

  return {
    overview: competitionId ? `/competitions/${competitionId}` : '/',
    structure: competitionId
      ? `/competitions/${competitionId}/structure`
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
    teams: competitionId
      ? `/competitions/${competitionId}/teams`
      : competitionListHref,
    regulation: competitionId
      ? `/competitions/${competitionId}/regulation`
      : competitionListHref,
  };
}

export function resolveActiveDestination(
  pathname: string,
): ShellDestinationKey | null {
  if (
    pathname === '/' ||
    pathname === '/competitions' ||
    /^\/competitions\/[^/]+$/.test(pathname)
  ) {
    return 'overview';
  }

  if (/^\/competitions\/[^/]+\/structure$/.test(pathname)) {
    return 'structure';
  }

  if (/^\/competitions\/[^/]+\/teams(?:\/[^/]+)?$/.test(pathname)) {
    return 'teams';
  }

  if (/^\/competitions\/[^/]+\/regulation$/.test(pathname)) {
    return 'regulation';
  }

  if (
    /^\/competitions\/[^/]+\/matches$/.test(pathname) ||
    /^\/stages\/[^/]+\/matches$/.test(pathname) ||
    /^\/matches\/[^/]+$/.test(pathname) ||
    /^\/stages\/[^/]+$/.test(pathname)
  ) {
    return 'matches';
  }

  if (/^\/competitions\/[^/]+\/classements$/.test(pathname)) {
    return 'classements';
  }

  return null;
}

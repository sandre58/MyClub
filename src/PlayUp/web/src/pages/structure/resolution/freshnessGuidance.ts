import type { StructureResolutionOccupant } from '../../../types';
import { structureDeepLink } from '../structureNavigation';

export type FreshnessAction =
  | { kind: 'replace' }
  | {
      kind: 'navigate';
      href: string;
      labelKey: string;
      labelParams?: Record<string, string>;
    };

function stageHref(
  competitionId: string | null | undefined,
  stageId: string | null | undefined,
  section?: 'classement',
): string | null {
  const stage = stageId?.trim();
  const competition = competitionId?.trim();
  if (!stage || !competition) {
    return null;
  }
  return structureDeepLink({
    competitionId: competition,
    stageId: stage,
    section,
  });
}

function matchHref(matchId: string | null | undefined): string | null {
  const id = matchId?.trim();
  return id ? `/matches/${id}` : null;
}

function navigate(
  href: string,
  labelKey: string,
  labelParams?: Record<string, string>,
): FreshnessAction {
  return labelParams
    ? { kind: 'navigate', href, labelKey, labelParams }
    : { kind: 'navigate', href, labelKey };
}

/**
 * The single corrective action for a freshness badge.
 * `canReplace` is true when the surface can open the cascade preview.
 * Navigation is a route; replace is not.
 */
export function freshnessAction(
  occupant: StructureResolutionOccupant,
  competitionId: string | null | undefined,
  options: { canReplace: boolean },
): FreshnessAction | null {
  if (occupant.state === 'Perime') {
    if (occupant.stalenessCause === 'AmontPerime') {
      const href = stageHref(competitionId, occupant.upstreamStageId);
      return href ? navigate(href, 'resolution.freshness.upstreamLink') : null;
    }
    if (
      options.canReplace &&
      occupant.cascadeReplaceable &&
      (occupant.stalenessCause === 'ResultatAmont' ||
        occupant.stalenessCause === 'RegleQualification')
    ) {
      return { kind: 'replace' };
    }
    return null;
  }

  if (occupant.state !== 'Indetermine' || !occupant.indeterminacyCause) {
    return null;
  }

  const cause = occupant.indeterminacyCause;
  const upstream = stageHref(
    competitionId,
    occupant.upstreamStageId,
    cause === 'SelectionVide' ? 'classement' : undefined,
  );
  const match = matchHref(occupant.actionMatchId);

  if (cause === 'MatchNonJoue' || cause === 'Egalite') {
    const href = match ?? upstream;
    if (!href) {
      return null;
    }
    const matchLabel = occupant.actionMatchLabel?.trim();
    if (cause === 'MatchNonJoue' && matchLabel) {
      return navigate(
        href,
        'resolution.freshness.indeterminacy.MatchNonJoue.actionNamed',
        { match: matchLabel },
      );
    }
    return navigate(href, `resolution.freshness.indeterminacy.${cause}.action`);
  }

  if (
    cause === 'SelectionVide' ||
    cause === 'SourceAbsente' ||
    cause === 'Autre'
  ) {
    if (!upstream) {
      return null;
    }
    return navigate(
      upstream,
      `resolution.freshness.indeterminacy.${cause}.action`,
    );
  }

  return null;
}

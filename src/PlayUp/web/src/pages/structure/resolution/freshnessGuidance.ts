import type { StructureResolutionOccupant } from '../../../types';
import { structureDeepLink } from '../structureNavigation';

export type FreshnessAction =
  | { kind: 'replace' }
  | { kind: 'release' }
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

function regulationHref(
  competitionId: string | null | undefined,
): string | null {
  const competition = competitionId?.trim();
  return competition ? `/competitions/${competition}/regulation` : null;
}

export type IndeterminacyExplanation = {
  team: string;
  detailKey: string;
  detailParams?: Record<string, string>;
};

/** Team and cause clause for the unconfirmed tooltip sentence. */
export function indeterminacyExplanation(
  occupant: StructureResolutionOccupant,
  entryName: string | null | undefined,
): IndeterminacyExplanation | null {
  const team = entryName?.trim();
  if (
    !team ||
    occupant.state !== 'Indetermine' ||
    !occupant.indeterminacyCause
  ) {
    return null;
  }

  const cause = occupant.indeterminacyCause;
  const match = occupant.actionMatchLabel?.trim();
  if (cause === 'MatchNonJoue' && match) {
    return {
      team,
      detailKey: 'resolution.freshness.indeterminacy.MatchNonJoue.clauseNamed',
      detailParams: { match },
    };
  }

  return {
    team,
    detailKey: `resolution.freshness.indeterminacy.${cause}.clause`,
  };
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
 * `canRelease` is true when the surface can open the release preview.
 * Navigation is a route; replace and release are not.
 */
export function freshnessAction(
  occupant: StructureResolutionOccupant,
  competitionId: string | null | undefined,
  options: { canReplace: boolean; canRelease?: boolean },
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

  if (options.canRelease && occupant.releasable) {
    return { kind: 'release' };
  }

  const cause = occupant.indeterminacyCause;
  const upstream = stageHref(competitionId, occupant.upstreamStageId);
  const match = matchHref(occupant.actionMatchId);

  if (cause === 'MatchNonJoue') {
    const href = match ?? upstream;
    if (!href) {
      return null;
    }
    const matchLabel = occupant.actionMatchLabel?.trim();
    if (matchLabel) {
      return navigate(
        href,
        'resolution.freshness.indeterminacy.MatchNonJoue.actionNamed',
        { match: matchLabel },
      );
    }
    return navigate(
      href,
      'resolution.freshness.indeterminacy.MatchNonJoue.action',
    );
  }

  if (cause === 'Egalite') {
    const href = match ?? upstream;
    return href
      ? navigate(href, 'resolution.freshness.indeterminacy.Egalite.action')
      : null;
  }

  if (cause === 'EgaliteClassement') {
    const href = stageHref(
      competitionId,
      occupant.upstreamStageId,
      'classement',
    );
    return href
      ? navigate(
          href,
          'resolution.freshness.indeterminacy.EgaliteClassement.action',
        )
      : null;
  }

  if (cause === 'ConfigurationInvalide') {
    const href = regulationHref(competitionId);
    return href
      ? navigate(
          href,
          'resolution.freshness.indeterminacy.ConfigurationInvalide.action',
        )
      : null;
  }

  return null;
}

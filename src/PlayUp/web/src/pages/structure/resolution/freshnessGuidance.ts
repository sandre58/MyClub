import type { StructureResolutionOccupant } from '../../../types';
import { structureDeepLink } from '../structureNavigation';

export type FreshnessNavigateIcon = 'upstream' | 'open';

export type FreshnessAction =
  | { kind: 'replace' }
  | { kind: 'release' }
  | {
      kind: 'navigate';
      href: string;
      labelKey: string;
      icon: FreshnessNavigateIcon;
      labelParams?: Record<string, string>;
    };

function stageHref(
  competitionId: string | null | undefined,
  stageId: string | null | undefined,
): string | null {
  const stage = stageId?.trim();
  const competition = competitionId?.trim();
  if (!stage || !competition) {
    return null;
  }
  return structureDeepLink({
    competitionId: competition,
    stageId: stage,
  });
}

function standingsHref(
  competitionId: string | null | undefined,
  stageId: string | null | undefined,
): string | null {
  const stage = stageId?.trim();
  const competition = competitionId?.trim();
  if (!stage || !competition) {
    return null;
  }
  return `/competitions/${competition}/classements?stage=${encodeURIComponent(stage)}`;
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
  icon: FreshnessNavigateIcon,
  labelParams?: Record<string, string>,
): FreshnessAction {
  return labelParams
    ? { kind: 'navigate', href, labelKey, icon, labelParams }
    : { kind: 'navigate', href, labelKey, icon };
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
      return href
        ? navigate(href, 'resolution.freshness.upstreamLink', 'upstream')
        : null;
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

  // Q10-A: one dialog. The badge offers release; "Play the match" stays the link inside it.
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
    if (match) {
      if (matchLabel) {
        return navigate(
          href,
          'resolution.freshness.indeterminacy.MatchNonJoue.actionNamed',
          'open',
          { match: matchLabel },
        );
      }
      return navigate(
        href,
        'resolution.freshness.indeterminacy.MatchNonJoue.action',
        'open',
      );
    }
    return navigate(href, 'resolution.freshness.upstreamPhaseLink', 'upstream');
  }

  if (cause === 'Egalite') {
    const href = match ?? upstream;
    if (!href) {
      return null;
    }
    if (match) {
      return navigate(
        href,
        'resolution.freshness.indeterminacy.Egalite.action',
        'open',
      );
    }
    return navigate(href, 'resolution.freshness.upstreamPhaseLink', 'upstream');
  }

  if (cause === 'EgaliteClassement') {
    const href = standingsHref(competitionId, occupant.upstreamStageId);
    return href
      ? navigate(
          href,
          'resolution.freshness.indeterminacy.EgaliteClassement.action',
          'open',
        )
      : null;
  }

  if (cause === 'ConfigurationInvalide') {
    const upstreamHref = stageHref(competitionId, occupant.upstreamStageId);
    if (upstreamHref) {
      return navigate(
        upstreamHref,
        'resolution.freshness.upstreamPhaseLink',
        'upstream',
      );
    }
    const href = regulationHref(competitionId);
    return href
      ? navigate(
          href,
          'resolution.freshness.indeterminacy.ConfigurationInvalide.action',
          'open',
        )
      : null;
  }

  return null;
}

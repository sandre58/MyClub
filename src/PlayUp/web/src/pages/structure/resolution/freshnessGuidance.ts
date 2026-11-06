import type { StructureResolutionOccupant } from '../../../types';
import { structureDeepLink } from '../structureNavigation';

export type FreshnessPanel =
  | {
      kind: 'upstream';
      href: string | null;
    }
  | {
      kind: 'indeterminate';
      cause: NonNullable<StructureResolutionOccupant['indeterminacyCause']>;
      href: string | null;
      matchLabel: string | null;
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

/**
 * Panel for an upstream-stale badge or an indeterminate badge.
 * Replayable qualification and progression staleness other than AmontPerime
 * stays on the replace control.
 */
export function freshnessPanel(
  occupant: StructureResolutionOccupant,
  competitionId: string | null | undefined,
): FreshnessPanel | null {
  if (
    occupant.state === 'Perime' &&
    occupant.stalenessCause === 'AmontPerime'
  ) {
    return {
      kind: 'upstream',
      href: stageHref(competitionId, occupant.upstreamStageId),
    };
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

  let href: string | null = null;
  if (cause === 'MatchNonJoue' || cause === 'Egalite') {
    href = match ?? upstream;
  } else if (cause === 'SourceAbsente') {
    href = upstream;
  } else if (cause === 'SelectionVide' || cause === 'Autre') {
    href = upstream;
  }

  return {
    kind: 'indeterminate',
    cause,
    href,
    matchLabel: occupant.actionMatchLabel?.trim() || null,
  };
}

import type { ReactNode } from 'react';

import { StructureResolutionFreshnessBadge } from '../resolution/StructureResolutionFreshnessBadge';
import type { StructurePhaseTeamLineProps } from './structurePhaseTeamLineTypes';

export function structurePhaseTeamFreshnessAction(
  props: Pick<
    StructurePhaseTeamLineProps,
    | 'displayName'
    | 'resolutionOccupant'
    | 'multiplePlacements'
    | 'entryName'
    | 'onReplace'
    | 'onRelease'
    | 'pending'
  >,
): ReactNode {
  const {
    displayName,
    resolutionOccupant,
    multiplePlacements = false,
    entryName,
    onReplace,
    onRelease,
    pending = false,
  } = props;
  if (!resolutionOccupant || pending) {
    return null;
  }
  return (
    <StructureResolutionFreshnessBadge
      occupant={resolutionOccupant}
      multiplePlacements={multiplePlacements}
      entryName={entryName ?? displayName}
      onReplace={onReplace}
      onRelease={onRelease}
      actionOnly
    />
  );
}

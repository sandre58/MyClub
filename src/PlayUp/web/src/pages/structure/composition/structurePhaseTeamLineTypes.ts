import type { StructureResolutionOccupant } from '../../../types';

export type StructurePhaseTeamLineProps = {
  displayName: string;
  logoMediaId?: string | null;
  primaryColor?: string | null;
  withdrawn?: boolean;
  ineligible?: boolean;
  pending?: boolean;
  pendingLabel?: string;
  placed?: boolean;
  placedPlaceLabel?: string | null;
  /** Overrides accessible hint short place when placement projection differs from visible label. */
  placedHintShortPlace?: string | null;
  provenanceMeta?: string | null;
  provenanceTitle?: string | null;
  resolutionOccupant?: StructureResolutionOccupant | null;
  multiplePlacements?: boolean;
  entryName?: string | null;
  onReplace?: (occupant: StructureResolutionOccupant) => void;
  onRelease?: (occupant: StructureResolutionOccupant) => void;
  /** Qualifiers panel — compact hint row under the rule label (option B). */
  layout?: 'default' | 'qualifierHint';
  /** Qualifier rule row — action rendered in the parent grid (option A). */
  omitFreshnessAction?: boolean;
};

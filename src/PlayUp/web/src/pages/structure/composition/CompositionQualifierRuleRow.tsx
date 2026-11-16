import { useTranslation } from 'react-i18next';

import type {
  StructureEntry,
  StructureInboundFeedRule,
  StageSchematic,
} from '../../../types';
import type { CompositionQualifierRule } from './structureCompositionQualifierFeeds';
import type { InboundFeedSlotView } from './structureCompositionInboundHub';
import { CompositionQualifierRuleLabel } from './CompositionQualifierRuleLabel';
import { StructurePhaseTeamLine } from './StructurePhaseTeamLine';
import {
  qualifierSeatPlaceLabel,
  qualifierSeatPlacedTooltip,
} from './compositionQualifierSeatPlaceLabel';
import {
  entryPlaceAccessibilityLabel,
  type EntryPlaceProjection,
} from '../draw/structureEntryPlacementProjection';
import type { StructureResolutionOccupant } from '../../../types';

export function CompositionQualifierRuleRow({
  rule,
  seat,
  entry,
  inboundRule,
  schematic,
  entryPlaceProjections,
  listedResolution,
  onReplace,
  onRelease,
}: {
  rule: CompositionQualifierRule;
  seat: InboundFeedSlotView;
  entry: StructureEntry | undefined;
  inboundRule: StructureInboundFeedRule | undefined;
  schematic: StageSchematic | null | undefined;
  entryPlaceProjections: ReadonlyMap<string, EntryPlaceProjection>;
  listedResolution: {
    occupant: StructureResolutionOccupant | null;
    multiplePlacements: boolean;
  } | null;
  onReplace?: (occupant: StructureResolutionOccupant) => void;
  onRelease?: (occupant: StructureResolutionOccupant) => void;
}) {
  const { t } = useTranslation('structure');
  const placedInPhase = seat.placed;
  const placeLabel = qualifierSeatPlaceLabel({
    seat,
    inboundRule,
    schematic,
    entryPlaceProjections,
  });
  const entryProjection =
    entry != null ? entryPlaceProjections.get(entry.entryId) : undefined;
  const placedTooltip =
    placedInPhase && entryProjection
      ? (entryPlaceAccessibilityLabel(entryProjection, t) ??
        qualifierSeatPlacedTooltip(placedInPhase, placeLabel, t))
      : qualifierSeatPlacedTooltip(placedInPhase, placeLabel, t);

  const teamName =
    seat.teamName ?? entry?.displayName ?? t('manualPlacement.unknownTeam');

  return (
    <>
      <div className="structure-composition-qualifiers__rule-primary">
        <CompositionQualifierRuleLabel rule={rule} />
      </div>
      {seat.resolved ? (
        <StructurePhaseTeamLine
          layout="qualifierHint"
          displayName={teamName}
          logoMediaId={entry?.logoMediaId}
          primaryColor={entry?.primaryColor}
          withdrawn={entry?.status === 'Withdrawn'}
          ineligible={entry != null && entry.status !== 'Active'}
          placed={placedInPhase}
          placedPlaceLabel={placeLabel}
          placedTooltip={placedTooltip}
          resolutionOccupant={listedResolution?.occupant ?? null}
          multiplePlacements={listedResolution?.multiplePlacements ?? false}
          entryName={teamName}
          onReplace={onReplace}
          onRelease={onRelease}
        />
      ) : (
        <StructurePhaseTeamLine
          layout="qualifierHint"
          displayName=""
          pending
          pendingLabel={t('composition.qualifiers.unresolved')}
          placed={placedInPhase}
          placedPlaceLabel={placeLabel}
          placedTooltip={placedTooltip}
        />
      )}
    </>
  );
}

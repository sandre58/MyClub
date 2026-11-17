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
import { structurePhaseTeamFreshnessAction } from './structurePhaseTeamFreshnessAction';
import {
  qualifierSeatPlaceLabel,
  qualifierSeatPlacedTooltipModel,
} from './compositionQualifierSeatPlaceLabel';
import {
  buildPlacedTooltipModel,
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
  destinationPhaseName,
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
  destinationPhaseName?: string | null;
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
  const placedTooltipModel =
    placedInPhase && entryProjection
      ? buildPlacedTooltipModel(entryProjection, destinationPhaseName, t)
      : qualifierSeatPlacedTooltipModel(
          placedInPhase,
          placeLabel,
          destinationPhaseName,
          t,
        );

  const teamName =
    seat.teamName ?? entry?.displayName ?? t('manualPlacement.unknownTeam');

  const teamLineProps = seat.resolved
    ? {
        layout: 'qualifierHint' as const,
        displayName: teamName,
        logoMediaId: entry?.logoMediaId,
        primaryColor: entry?.primaryColor,
        withdrawn: entry?.status === 'Withdrawn',
        ineligible: entry != null && entry.status !== 'Active',
        placed: placedInPhase,
        placedPlaceLabel: placeLabel,
        placedTooltipModel,
        resolutionOccupant: listedResolution?.occupant ?? null,
        multiplePlacements: listedResolution?.multiplePlacements ?? false,
        entryName: teamName,
        onReplace,
        onRelease,
        omitFreshnessAction: true,
      }
    : {
        layout: 'qualifierHint' as const,
        displayName: '',
        pending: true,
        pendingLabel: t('composition.qualifiers.unresolved'),
        placed: placedInPhase,
        placedPlaceLabel: placeLabel,
        placedTooltipModel,
        omitFreshnessAction: true,
      };

  const freshnessAction = structurePhaseTeamFreshnessAction(teamLineProps);

  const ruleStack = (
    <div className="structure-composition-qualifiers__rule-stack">
      <div className="structure-composition-qualifiers__rule-primary">
        <CompositionQualifierRuleLabel rule={rule} />
      </div>
      <StructurePhaseTeamLine {...teamLineProps} />
    </div>
  );

  if (freshnessAction == null) {
    return ruleStack;
  }

  return (
    <div className="structure-composition-qualifiers__rule-grid">
      {ruleStack}
      <div className="structure-entries__action">{freshnessAction}</div>
    </div>
  );
}

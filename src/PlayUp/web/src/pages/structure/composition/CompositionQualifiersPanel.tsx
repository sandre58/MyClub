import type { ReactNode } from 'react';
import { useMemo } from 'react';
import type {
  StageSchematic,
  StructureEntry,
  StructureFormatKind,
  StructureInboundComposition,
  StructureResolutionOccupant,
} from '../../../types';
import type { CompositionQualifierFeedGroup } from './structureCompositionQualifierFeeds';
import { CompositionQualifierRuleRow } from './CompositionQualifierRuleRow';
import { FeedGroupPhaseHead } from './FeedGroupPhaseHead';
import { inboundSeatsForFeedRule } from './structureCompositionInboundHub';
import { showPerEntryPlacement } from '../draw/structureEntryPlacementProjection';
import type { EntryPlaceProjection } from '../draw/structureEntryPlacementProjection';
import { useStructureCascadeReplace } from '../resolution/structureCascadeReplaceContext';
import { useStructureResolutionRelease } from '../resolution/structureResolutionReleaseContext';
import {
  buildStructureResolutionOccupantIndex,
  lookupListedResolutionOccupant,
} from '../resolution/structureResolutionOccupants';

export function CompositionQualifiersPanel({
  groups,
  inboundComposition,
  entries,
  formatKind,
  entryPlaceProjections,
  schematic,
  resolutionOccupants,
  onViewPhase,
  renderGroupAction,
}: {
  groups: CompositionQualifierFeedGroup[];
  inboundComposition: StructureInboundComposition | null | undefined;
  entries: readonly StructureEntry[];
  formatKind: StructureFormatKind | null | undefined;
  entryPlaceProjections: ReadonlyMap<string, EntryPlaceProjection>;
  schematic: StageSchematic | null | undefined;
  resolutionOccupants?: StructureResolutionOccupant[] | null;
  onViewPhase?: (peerId: string) => void;
  renderGroupAction?: (group: CompositionQualifierFeedGroup) => ReactNode;
}) {
  const cascade = useStructureCascadeReplace();
  const release = useStructureResolutionRelease();
  const resolutionIndex = useMemo(
    () => buildStructureResolutionOccupantIndex(resolutionOccupants),
    [resolutionOccupants],
  );
  const entryById = new Map(entries.map((entry) => [entry.entryId, entry]));
  const perEntryPlacement = showPerEntryPlacement(formatKind);

  return (
    <div className="structure-composition-qualifiers">
      <ul className="structure-flux-groups">
        {groups.map((group) => (
          <li key={group.peerId} className="structure-flux-group">
            <FeedGroupPhaseHead
              group={group}
              onViewPhase={onViewPhase}
              renderGroupAction={renderGroupAction}
            />
            <ul className="structure-feed-group__rules">
              {group.rules.flatMap((rule) => {
                const inboundRule = inboundComposition?.rules.find(
                  (row) => row.feedKey === rule.key,
                );
                const seats =
                  rule.hubSeats ??
                  inboundSeatsForFeedRule(
                    inboundComposition,
                    {
                      uiFeedKey: rule.key,
                      sourceStageId: group.peerId,
                      qualPathOrder: rule.qualPathOrder,
                      progressionPairKey: rule.progressionPairKey,
                      progressionOutcome: rule.progressionOutcome,
                    },
                    rule.volume,
                  );
                return seats.map((seat, seatIndex) => {
                  const entry =
                    seat.entryId != null
                      ? entryById.get(seat.entryId)
                      : undefined;
                  const showUnplacedOpacity =
                    perEntryPlacement &&
                    seat.resolved &&
                    seat.entryId != null &&
                    !seat.placed &&
                    entry?.status === 'Active';
                  const listedResolution =
                    seat.entryId != null
                      ? lookupListedResolutionOccupant(
                          resolutionIndex,
                          seat.entryId,
                        )
                      : null;

                  return (
                    <li
                      key={`${rule.key}-${seatIndex}`}
                      className={[
                        'structure-feed-group__rule-line',
                        'structure-composition-qualifiers__rule-line--after',
                        showUnplacedOpacity
                          ? 'structure-composition-qualifiers__rule-line--unplaced'
                          : null,
                      ]
                        .filter(Boolean)
                        .join(' ')}
                    >
                      <CompositionQualifierRuleRow
                        rule={rule}
                        seat={seat}
                        entry={entry}
                        inboundRule={inboundRule}
                        schematic={schematic}
                        entryPlaceProjections={entryPlaceProjections}
                        listedResolution={listedResolution}
                        onReplace={cascade?.requestReplace}
                        onRelease={release?.requestRelease}
                      />
                    </li>
                  );
                });
              })}
            </ul>
          </li>
        ))}
      </ul>
    </div>
  );
}

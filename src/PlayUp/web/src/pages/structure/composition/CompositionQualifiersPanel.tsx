import type { ReactNode } from 'react';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import {
  EntryCell,
  EntryCellPlacedMarker,
} from '../../../design-system/components/EntryCell';
import type {
  StructureEntry,
  StructureFormatKind,
  StructureInboundComposition,
  StructureResolutionOccupant,
} from '../../../types';
import type { CompositionQualifierFeedGroup } from './structureCompositionQualifierFeeds';
import { CompositionQualifierRuleLabel } from './CompositionQualifierRuleLabel';
import { FeedGroupPhaseHead } from './FeedGroupPhaseHead';
import { inboundSeatsForFeedRule } from './structureCompositionInboundHub';
import {
  primaryPlaceLabel,
  showPerEntryPlacement,
} from '../draw/structureEntryPlacementProjection';
import type { EntryPlaceProjection } from '../draw/structureEntryPlacementProjection';
import { StructureResolutionFreshnessBadge } from '../resolution/StructureResolutionFreshnessBadge';
import {
  buildStructureResolutionOccupantIndex,
  lookupPopulationResolutionOccupant,
} from '../resolution/structureResolutionOccupants';

export function CompositionQualifiersPanel({
  groups,
  inboundComposition,
  entries,
  formatKind,
  entryPlaceProjections,
  resolutionOccupants,
  onViewPhase,
  renderGroupAction,
}: {
  groups: CompositionQualifierFeedGroup[];
  inboundComposition: StructureInboundComposition | null | undefined;
  entries: readonly StructureEntry[];
  formatKind: StructureFormatKind | null | undefined;
  entryPlaceProjections: ReadonlyMap<string, EntryPlaceProjection>;
  resolutionOccupants?: StructureResolutionOccupant[] | null;
  onViewPhase?: (peerId: string) => void;
  renderGroupAction?: (group: CompositionQualifierFeedGroup) => ReactNode;
}) {
  const { t } = useTranslation('structure');
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
                  const placedInPhase = seat.placed;
                  const showUnplacedOpacity =
                    perEntryPlacement &&
                    seat.resolved &&
                    seat.entryId != null &&
                    !placedInPhase &&
                    entry?.status === 'Active';
                  const placeLabel =
                    seat.entryId != null
                      ? primaryPlaceLabel(
                          entryPlaceProjections.get(seat.entryId),
                        )
                      : null;
                  const placedTooltip =
                    placedInPhase && placeLabel
                      ? t('entries.entryPlacedTooltip', { place: placeLabel })
                      : placedInPhase
                        ? t('entries.entryPlacedStatus')
                        : null;

                  return (
                    <li
                      key={`${rule.key}-${seatIndex}`}
                      className={[
                        'structure-feed-group__rule-line',
                        showUnplacedOpacity
                          ? 'structure-composition-qualifiers__rule-line--unplaced'
                          : null,
                      ]
                        .filter(Boolean)
                        .join(' ')}
                    >
                      <div className="structure-composition-qualifiers__rule-feed">
                        <CompositionQualifierRuleLabel rule={rule} />
                      </div>
                      <div className="structure-composition-qualifiers__rule-outcome">
                        {seat.resolved ? (
                          <EntryCell
                            variant="chip"
                            displayName={
                              seat.teamName ??
                              entry?.displayName ??
                              t('manualPlacement.unknownTeam')
                            }
                            logoMediaId={entry?.logoMediaId}
                            primaryColor={entry?.primaryColor}
                          />
                        ) : (
                          <EntryCell
                            variant="chip"
                            displayName=""
                            pending
                            pendingLabel={t(
                              'composition.qualifiers.unresolved',
                            )}
                          />
                        )}
                        {placedInPhase ? (
                          <EntryCellPlacedMarker
                            tooltip={
                              placedTooltip ?? t('entries.entryPlacedStatus')
                            }
                          />
                        ) : (
                          <span
                            className="structure-composition-qualifiers__slot-placed-slot"
                            aria-hidden="true"
                          />
                        )}
                        {seat.entryId ? (
                          <StructureResolutionFreshnessBadge
                            occupant={lookupPopulationResolutionOccupant(
                              resolutionIndex,
                              seat.entryId,
                            )}
                          />
                        ) : null}
                      </div>
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

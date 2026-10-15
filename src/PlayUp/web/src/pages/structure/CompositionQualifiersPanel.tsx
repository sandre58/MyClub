import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Chip } from '../../design-system/components/Chip';
import { Tooltip } from '../../design-system/components/Tooltip';
import { LocateIcon, HourglassIcon } from '../../design-system/icons/contentIcons';
import { TeamCrest } from '../../design-system/TeamCrest';
import type {
  StructureEntry,
  StructureFormatKind,
  StructureInboundComposition,
} from '../../types';
import type { FeedGroup } from './structurePhaseFeeds';
import { CompositionQualifierRuleLabel } from './CompositionQualifierRuleLabel';
import { FeedGroupPhaseHead } from './FeedGroupPhaseHead';
import { inboundSeatsForFeedKey } from './structureCompositionInboundHub';
import {
  primaryPlaceLabel,
  showPerEntryPlacement,
} from './structureEntryPlacementProjection';
import type { EntryPlaceProjection } from './structureEntryPlacementProjection';

export function CompositionQualifiersPanel({
  groups,
  inboundComposition,
  entries,
  formatKind,
  schematicReady,
  entryPlaceProjections,
  onViewPhase,
  renderGroupAction,
}: {
  groups: FeedGroup[];
  inboundComposition: StructureInboundComposition | null | undefined;
  entries: readonly StructureEntry[];
  formatKind: StructureFormatKind | null | undefined;
  schematicReady: boolean;
  entryPlaceProjections: ReadonlyMap<string, EntryPlaceProjection>;
  onViewPhase?: (peerId: string) => void;
  renderGroupAction?: (group: FeedGroup) => ReactNode;
}) {
  const { t } = useTranslation('structure');
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
                const seats = inboundSeatsForFeedKey(
                  inboundComposition,
                  rule.key,
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
                    schematicReady &&
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
                          <Chip
                            tone="neutral"
                            className="structure-composition-qualifiers__status-chip structure-composition-qualifiers__status-chip--resolved"
                          >
                            <TeamCrest
                              name={
                                seat.teamName ??
                                entry?.displayName ??
                                t('manualPlacement.unknownTeam')
                              }
                              logoMediaId={entry?.logoMediaId}
                              primaryColor={entry?.primaryColor}
                              size="sm"
                              className="structure-composition-qualifiers__status-crest"
                            />
                            <span className="structure-composition-qualifiers__status-text">
                              {seat.teamName ??
                                entry?.displayName ??
                                t('manualPlacement.unknownTeam')}
                            </span>
                          </Chip>
                        ) : (
                          <Chip
                            tone="neutral"
                            className="structure-composition-qualifiers__status-chip structure-composition-qualifiers__status-chip--pending"
                          >
                            <HourglassIcon size="sm" aria-hidden="true" />
                            <span className="structure-composition-qualifiers__status-text">
                              {t('composition.qualifiers.unresolved')}
                            </span>
                          </Chip>
                        )}
                        <span
                          className="structure-composition-qualifiers__slot-placed-slot"
                          aria-hidden={!placedInPhase}
                        >
                          {placedInPhase ? (
                            <Tooltip
                              content={
                                placedTooltip ?? t('entries.entryPlacedStatus')
                              }
                            >
                              <span className="structure-composition-qualifiers__slot-placed">
                                <LocateIcon size="sm" aria-hidden="true" />
                              </span>
                            </Tooltip>
                          ) : null}
                        </span>
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

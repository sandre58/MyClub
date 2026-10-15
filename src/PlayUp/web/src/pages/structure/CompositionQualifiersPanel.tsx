import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Chip } from '../../design-system/components/Chip';
import { Tooltip } from '../../design-system/components/Tooltip';
import {
  LocateIcon,
  HourglassIcon,
  ViewIcon,
} from '../../design-system/icons/contentIcons';
import { TeamCrest } from '../../design-system/TeamCrest';
import type {
  StructureEntry,
  StructureFormatKind,
  StructureInboundComposition,
} from '../../types';
import type { FeedGroup } from './structurePhaseFeeds';
import { CompositionQualifierRuleLabel } from './CompositionQualifierRuleLabel';
import { inboundSeatsForFeedKey } from './structureCompositionInboundHub';
import {
  primaryPlaceLabel,
  showPerEntryPlacement,
} from './structureEntryPlacementProjection';
import type { EntryPlaceProjection } from './structureEntryPlacementProjection';

const compactIcon =
  'ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact';

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
            <div className="structure-composition-qualifiers__phase-head">
              <h4 className="structure-composition-qualifiers__phase-title">
                {group.peerName}
              </h4>
              <div className="structure-composition-qualifiers__phase-trail">
                {renderGroupAction ? (
                  <div className="structure-composition-qualifiers__phase-actions">
                    {renderGroupAction(group)}
                  </div>
                ) : null}
                {onViewPhase ? (
                  <Tooltip content={t('composition.qualifiers.viewPhase')}>
                    <a
                      href="#"
                      className={compactIcon}
                      aria-label={t('composition.qualifiers.viewPhase')}
                      onClick={(event) => {
                        event.preventDefault();
                        onViewPhase(group.peerId);
                      }}
                    >
                      <ViewIcon size="sm" />
                    </a>
                  </Tooltip>
                ) : null}
              </div>
            </div>
            <ul className="structure-composition-qualifiers__rules">
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
                        'structure-composition-qualifiers__rule-line',
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

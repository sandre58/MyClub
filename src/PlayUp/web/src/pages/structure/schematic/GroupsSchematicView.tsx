import { Fragment, type ReactNode } from 'react';
import { AlreadyPlacedStatus } from '../../../design-system/components/Status';
import { TeamCrest } from '../../../design-system/TeamCrest';
import '../phase-schematic.css';
import {
  groupsSchematicEntryAt,
  type GroupsSchematicEntry,
  type GroupsSchematicGroup,
} from './groupsSchematic';

function placedAria(entry: GroupsSchematicEntry, labels: PlacedLabels): string {
  const already = `${entry.displayName}, ${labels.alreadyPlaced}`;
  return entry.origin === 'Manual'
    ? `${already}, ${labels.originManualHint}`
    : already;
}

/**
 * Groups × teams schematic. Empty seats are dashed ghosts.
 * The phase sheet passes `renderSeat` so feed labels stay on the source cases.
 */
type PlacedLabels = {
  alreadyPlaced: string;
  originManualHint: string;
};

export function GroupsSchematicView({
  groups,
  ariaLabel,
  ghostLabel,
  renderSeat,
  seatSummary,
  placedLabels,
}: {
  groups: readonly GroupsSchematicGroup[];
  ariaLabel?: string;
  ghostLabel?: string;
  renderSeat?: (
    group: GroupsSchematicGroup,
    seat: number,
  ) => ReactNode | undefined;
  /** Phase sheet pool counter. Omitted by the draw dialog. */
  seatSummary?: (group: GroupsSchematicGroup) => string | null;
  /** Cup pin copy. Omitted by the phase sheet, which does not carry draw origin. */
  placedLabels?: PlacedLabels;
}) {
  return (
    <div
      className="regulation-schematic regulation-schematic--groups"
      role="group"
      {...(ariaLabel ? { 'aria-label': ariaLabel } : {})}
    >
      <div className="regulation-schematic__cards">
        {groups.map((group, index) => {
          const seatCount = Math.max(group.capacity, group.entries.length);
          const summary = seatSummary?.(group);
          return (
            <div
              key={group.id || `group-${index}`}
              className={`regulation-schematic__card regulation-schematic__card--${index % 4}`}
            >
              <div className="regulation-schematic__card-head">
                <span className="regulation-schematic__card-label">
                  {group.name}
                </span>
                {summary ? (
                  <span className="regulation-schematic__card-feed">
                    {summary}
                  </span>
                ) : null}
              </div>
              <div className="regulation-schematic__card-slots">
                {Array.from({ length: seatCount }, (_, seat) => {
                  const custom = renderSeat?.(group, seat);
                  if (custom != null) {
                    return (
                      <Fragment key={`${group.id}-seat-${seat}`}>
                        {custom}
                      </Fragment>
                    );
                  }
                  const entry = groupsSchematicEntryAt(group, seat);
                  if (!entry) {
                    return (
                      <span
                        key={`${group.id}-ghost-${seat}`}
                        className="schematic-slot schematic-slot--full schematic-slot--empty"
                      >
                        {ghostLabel ? (
                          <span className="schematic-slot__primary">
                            {ghostLabel}
                          </span>
                        ) : null}
                      </span>
                    );
                  }
                  const pinned =
                    placedLabels != null &&
                    entry.origin != null &&
                    entry.origin !== 'Draw';
                  return (
                    <span
                      key={`${group.id}-${entry.id}-${seat}`}
                      className="schematic-slot schematic-slot--full"
                      {...(pinned
                        ? { 'aria-label': placedAria(entry, placedLabels) }
                        : {})}
                    >
                      <span className="schematic-slot__body">
                        <TeamCrest
                          name={entry.displayName}
                          logoMediaId={entry.logoMediaId}
                          primaryColor={entry.primaryColor}
                          size="sm"
                          className="schematic-slot__crest"
                        />
                        <span className="schematic-slot__copy">
                          <span className="schematic-slot__name-row">
                            <span
                              className={
                                pinned
                                  ? 'schematic-slot__primary structure-draw-slot__name--placed'
                                  : 'schematic-slot__primary'
                              }
                            >
                              {entry.displayName}
                            </span>
                            {pinned ? (
                              <AlreadyPlacedStatus
                                label={placedLabels.alreadyPlaced}
                              />
                            ) : null}
                          </span>
                        </span>
                      </span>
                    </span>
                  );
                })}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}

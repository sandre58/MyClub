import { Fragment, type ReactNode } from 'react';
import { TeamCrest } from '../../../design-system/TeamCrest';
import '../phase-schematic.css';
import {
  groupsSchematicEntryAt,
  type GroupsSchematicGroup,
} from './groupsSchematic';

/**
 * Groups × teams schematic. Empty seats are dashed ghosts.
 * The phase sheet passes `renderSeat` so feed labels stay on the source cases.
 */
export function GroupsSchematicView({
  groups,
  ariaLabel,
  ghostLabel,
  renderSeat,
}: {
  groups: readonly GroupsSchematicGroup[];
  ariaLabel: string;
  ghostLabel?: string;
  renderSeat?: (
    group: GroupsSchematicGroup,
    seat: number,
  ) => ReactNode | undefined;
}) {
  return (
    <div
      className="regulation-schematic regulation-schematic--groups"
      aria-label={ariaLabel}
    >
      <div className="regulation-schematic__cards">
        {groups.map((group, index) => {
          const seatCount = Math.max(group.capacity, group.entries.length);
          return (
            <div
              key={group.id || `group-${index}`}
              className={`regulation-schematic__card regulation-schematic__card--${index % 4}`}
            >
              <span className="regulation-schematic__card-label">
                {group.name}
              </span>
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
                  return (
                    <span
                      key={`${group.id}-${entry.id}-${seat}`}
                      className="schematic-slot schematic-slot--full"
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
                          <span className="schematic-slot__primary">
                            {entry.displayName}
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

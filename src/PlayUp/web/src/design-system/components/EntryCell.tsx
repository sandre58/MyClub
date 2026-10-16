import type { ReactNode } from 'react';

import { Chip } from './Chip';
import { Tooltip } from './Tooltip';
import { HourglassIcon, LocateIcon } from '../icons/contentIcons';
import { TeamCrest } from '../TeamCrest';

export type EntryCellVariant = 'list-row' | 'chip';

export type EntryCellProps = {
  displayName: string;
  logoMediaId?: string | null;
  primaryColor?: string | null;
  variant?: EntryCellVariant;
  ineligible?: boolean;
  ineligibleLabel?: string;
  unplaced?: boolean;
  placed?: boolean;
  placedTooltip?: string | null;
  pending?: boolean;
  pendingLabel?: string;
  ariaLabel?: string;
  chipClassName?: string;
  crestClassName?: string;
  nameClassName?: string;
  trailing?: ReactNode;
};

export function EntryCellPlacedMarker({
  tooltip,
  className = 'structure-composition-qualifiers__slot-placed-slot',
}: {
  tooltip: string;
  className?: string;
}) {
  return (
    <span className={className} aria-hidden={false}>
      <Tooltip content={tooltip}>
        <span className="structure-composition-qualifiers__slot-placed">
          <LocateIcon size="sm" aria-hidden="true" />
        </span>
      </Tooltip>
    </span>
  );
}

export function EntryCell({
  displayName,
  logoMediaId,
  primaryColor,
  variant = 'list-row',
  ineligible = false,
  ineligibleLabel,
  placed = false,
  placedTooltip,
  pending = false,
  pendingLabel,
  ariaLabel,
  chipClassName,
  crestClassName,
  nameClassName = 'structure-entries__team-name',
  trailing,
}: EntryCellProps) {
  const placedSlot =
    trailing ??
    (placed ? (
      <Tooltip content={placedTooltip ?? displayName}>
        <span className="structure-entries__team-placed">
          <LocateIcon size="sm" aria-hidden="true" />
        </span>
      </Tooltip>
    ) : null);

  if (variant === 'chip') {
    if (pending) {
      return (
        <Chip
          tone="neutral"
          className={[
            'structure-composition-qualifiers__status-chip',
            'structure-composition-qualifiers__status-chip--pending',
            chipClassName,
          ]
            .filter(Boolean)
            .join(' ')}
        >
          <HourglassIcon size="sm" aria-hidden="true" />
          <span className="structure-composition-qualifiers__status-text">
            {pendingLabel}
          </span>
        </Chip>
      );
    }
    return (
      <Chip
        tone="neutral"
        className={[
          'structure-composition-qualifiers__status-chip',
          'structure-composition-qualifiers__status-chip--resolved',
          chipClassName,
        ]
          .filter(Boolean)
          .join(' ')}
      >
        <TeamCrest
          name={displayName}
          logoMediaId={logoMediaId}
          primaryColor={primaryColor}
          size="sm"
          className={[
            'structure-composition-qualifiers__status-crest',
            crestClassName,
          ]
            .filter(Boolean)
            .join(' ')}
        />
        <span className="structure-composition-qualifiers__status-text">
          {displayName}
        </span>
      </Chip>
    );
  }

  if (pending) {
    return (
      <>
        <span className="structure-entries__team-pending-icon" aria-hidden="true">
          <HourglassIcon size="sm" />
        </span>
        <span
          className={[
            nameClassName,
            'structure-entries__team-name--pending',
          ]
            .filter(Boolean)
            .join(' ')}
        >
          {pendingLabel}
        </span>
        <span
          className="structure-entries__team-placed-slot"
          aria-hidden={!trailing}
        >
          {trailing ?? null}
        </span>
      </>
    );
  }

  return (
    <>
      <TeamCrest
        name={displayName}
        logoMediaId={logoMediaId}
        primaryColor={primaryColor}
        size="sm"
      />
      <span className={nameClassName}>{displayName}</span>
      {ineligible && ineligibleLabel ? (
        <span className="structure-entries__team-warn">{ineligibleLabel}</span>
      ) : null}
      <span
        className="structure-entries__team-placed-slot"
        aria-hidden={!placed && !trailing}
        aria-label={ariaLabel}
      >
        {placedSlot}
      </span>
    </>
  );
}

export function entryCellListItemClassName(options: {
  ineligible?: boolean;
  unplaced?: boolean;
}): string {
  return [
    'structure-entries__team',
    options.ineligible ? 'structure-entries__team--ineligible' : null,
    options.unplaced ? 'structure-entries__team--unplaced' : null,
  ]
    .filter(Boolean)
    .join(' ');
}

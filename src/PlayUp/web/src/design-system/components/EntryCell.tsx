import type { ReactNode } from 'react';

import { Chip } from './Chip';
import { Tooltip } from './Tooltip';
import { HourglassIcon, LocateIcon, WithdrawIcon } from '../icons/contentIcons';
import { TeamCrest } from '../TeamCrest';

export type EntryCellVariant = 'list-row' | 'chip';

export type EntryCellProps = {
  displayName: string;
  logoMediaId?: string | null;
  primaryColor?: string | null;
  variant?: EntryCellVariant;
  ineligible?: boolean;
  /** @deprecated Prefer `ineligibleTooltip` (icon on row end). */
  ineligibleLabel?: string;
  /** Withdrawal / inactive — icon + tooltip in the trailing slot (not inline warning copy). */
  ineligibleTooltip?: string;
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

export function EntryCellWithdrawnMarker({
  tooltip,
  className = 'structure-entries__team-withdrawn',
}: {
  tooltip: string;
  className?: string;
}) {
  return (
    <Tooltip content={tooltip}>
      <span className={className} role="img" aria-label={tooltip}>
        <WithdrawIcon size="sm" aria-hidden="true" />
      </span>
    </Tooltip>
  );
}

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
  ineligibleTooltip,
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
  const ineligibleSlot =
    ineligible && ineligibleTooltip ? (
      <EntryCellWithdrawnMarker tooltip={ineligibleTooltip} />
    ) : null;

  const placedSlot =
    trailing ??
    ineligibleSlot ??
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
        <span
          className="structure-entries__team-pending-icon"
          aria-hidden="true"
        >
          <HourglassIcon size="sm" />
        </span>
        <span
          className={[nameClassName, 'structure-entries__team-name--pending']
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
      {ineligible && ineligibleLabel && !ineligibleTooltip ? (
        <span className="structure-entries__team-warn">{ineligibleLabel}</span>
      ) : null}
      <span
        className="structure-entries__team-placed-slot"
        aria-hidden={!placed && !trailing && !ineligibleSlot}
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

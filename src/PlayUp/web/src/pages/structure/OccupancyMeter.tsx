// -----------------------------------------------------------------------
// Shared occupancy chrome — X vs Places N (exact | short | over).
// Content (labels, draft Z, gap copy) stays with consumers.
// -----------------------------------------------------------------------

import type { ReactNode } from 'react';

export type OccupancyMeterState = 'exact' | 'short' | 'over';

export function occupancyMeterState(
  current: number,
  capacity: number,
): OccupancyMeterState {
  if (current === capacity) return 'exact';
  if (current < capacity) return 'short';
  return 'over';
}

export function occupancyMeterRatio(
  current: number,
  capacity: number,
): number {
  if (capacity > 0) return Math.min(1, Math.max(0, current / capacity));
  return current > 0 ? 1 : 0;
}

type OccupancyMeterProps = {
  current: number;
  capacity: number;
  /** Defaults from current vs capacity. */
  state?: OccupancyMeterState;
  /** Normal progress uses brand fill even when below capacity (not an alert). */
  fillVariant?: 'default' | 'brand' | 'attention';
  header?: ReactNode;
  footer?: ReactNode;
  ariaLabel: string;
  className?: string;
};

/** Shared track + tone chrome for Places N occupancy. */
export function OccupancyMeter({
  current,
  capacity,
  state: stateProp,
  fillVariant = 'default',
  header,
  footer,
  ariaLabel,
  className,
}: OccupancyMeterProps) {
  const state = stateProp ?? occupancyMeterState(current, capacity);
  const ratio = occupancyMeterRatio(current, capacity);

  return (
    <div
      className={[
        'structure-occupancy-meter',
        `structure-occupancy-meter--${state}`,
        fillVariant === 'brand'
          ? 'structure-occupancy-meter--brand-fill'
          : fillVariant === 'attention'
            ? 'structure-occupancy-meter--attention-fill'
            : null,
        className,
      ]
        .filter(Boolean)
        .join(' ')}
      role="img"
      aria-label={ariaLabel}
    >
      {header}
      <span className="structure-occupancy-meter__track" aria-hidden="true">
        <span
          className="structure-occupancy-meter__fill"
          style={{ width: `${ratio * 100}%` }}
        />
      </span>
      {footer}
    </div>
  );
}

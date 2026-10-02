// -----------------------------------------------------------------------
// Destination occupancy meter — Qual / Prog authoring tiles (shared chrome).
// -----------------------------------------------------------------------

import {
  OccupancyMeter,
  occupancyMeterState,
} from './OccupancyMeter';

/** Expected destination occupancy (X) vs Places N (Y); +Z = current intent only. */
export function DestinationDraftMeter({
  count,
  capacity,
  draft,
  label,
  draftLabel,
  ariaLabel,
}: {
  count: number;
  capacity: number;
  draft: number;
  label: string;
  draftLabel: string;
  ariaLabel: string;
}) {
  return (
    <OccupancyMeter
      current={count}
      capacity={capacity}
      state={occupancyMeterState(count, capacity)}
      ariaLabel={ariaLabel}
      className="structure-occupancy-meter--compact"
      header={
        <span className="structure-occupancy-meter__label" aria-hidden="true">
          {label}
        </span>
      }
      footer={
        <span
          className={[
            'structure-occupancy-meter__draft',
            draft > 0 ? 'structure-occupancy-meter__draft--active' : null,
          ]
            .filter(Boolean)
            .join(' ')}
          aria-hidden="true"
        >
          {draftLabel}
        </span>
      }
    />
  );
}

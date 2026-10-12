import type { ReactNode } from 'react';
import { OccupancyMeter, occupancyMeterState } from './OccupancyMeter';
import type { DrawCtaPoolTone } from './structureDrawCtaPool';

function OperationalGaugeReadiness({
  poolTone,
  children,
}: {
  poolTone: DrawCtaPoolTone;
  children: ReactNode;
}) {
  return (
    <span className="structure-operational-gauge__readiness" data-pool-tone={poolTone}>
      {children}
    </span>
  );
}

export function StructureOperationalRatioGauge({
  current,
  capacity,
  caption,
  ariaLabel,
  poolTone = 'neutral',
  unknownCapacity,
  dense = false,
  layout = 'inline',
  fillVariant = 'default',
}: {
  current: number;
  capacity: number | null;
  caption: string;
  ariaLabel: string;
  poolTone?: DrawCtaPoolTone;
  unknownCapacity?: boolean;
  dense?: boolean;
  /** `trackOnly` — progress track only; ratio and copy live outside (operational strip). */
  layout?: 'inline' | 'trackOnly';
  fillVariant?: 'default' | 'brand' | 'attention';
}) {
  const gaugeClass = [
    'structure-operational-gauge',
    'structure-occupancy-meter--compact',
    dense ? 'structure-operational-gauge--dense' : null,
  ]
    .filter(Boolean)
    .join(' ');
  if (unknownCapacity || capacity == null || capacity <= 0) {
    return (
      <div
        className="structure-operational-gauge structure-operational-gauge--unknown"
        aria-label={ariaLabel}
      >
        <OperationalGaugeReadiness poolTone="neutral">
          <span className="structure-operational-gauge__ratio">
            <span className="structure-operational-gauge__ratio-filled">{current}</span>
          </span>
          <span className="structure-operational-gauge__caption">{caption}</span>
        </OperationalGaugeReadiness>
      </div>
    );
  }

  const state = occupancyMeterState(current, capacity);

  const meter = (
    <OccupancyMeter
      current={current}
      capacity={capacity}
      state={state}
      fillVariant={poolTone === 'danger' ? 'default' : fillVariant}
      ariaLabel={ariaLabel}
      className={layout === 'trackOnly' ? 'structure-operational-gauge__meter' : gaugeClass}
      header={
        layout === 'trackOnly' ? undefined : (
          <OperationalGaugeReadiness poolTone={poolTone}>
            <span className="structure-operational-gauge__ratio">
              <span className="structure-operational-gauge__ratio-filled">{current}</span>
              <span className="structure-operational-gauge__ratio-sep">/</span>
              <span className="structure-operational-gauge__ratio-cap">{capacity}</span>
            </span>
            <span className="structure-operational-gauge__caption">{caption}</span>
          </OperationalGaugeReadiness>
        )
      }
    />
  );

  if (layout === 'trackOnly') {
    return (
      <div className={gaugeClass} data-pool-tone={poolTone}>
        {meter}
      </div>
    );
  }

  return meter;
}

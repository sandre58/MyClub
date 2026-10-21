import type { ReactNode } from 'react';
import { PanelHead } from './PanelHead';
import { CycleLine } from './CycleLine';

const DEFAULT_CYCLE_STEPS = [
  'Préparation',
  'Calendrier',
  'En cours',
  'Terminée',
];

export function OverviewReading({
  title,
  reading,
  label,
  cycle,
  id,
}: {
  title: string;
  reading: ReactNode;
  label?: ReactNode;
  cycle?: { current: string; done?: string[]; steps?: string[] };
  id?: string;
}) {
  return (
    <section className="ds-panel ds-overview-situation" aria-labelledby={id}>
      <PanelHead id={id} title={title} />
      <div className="ds-overview-situation__reading">{reading}</div>
      {label != null ? (
        typeof label === 'string' ? (
          <p className="ds-overview-situation__label">{label}</p>
        ) : (
          label
        )
      ) : null}
      {cycle ? (
        <CycleLine
          steps={cycle.steps ?? DEFAULT_CYCLE_STEPS}
          current={cycle.current}
          done={cycle.done}
        />
      ) : null}
    </section>
  );
}

export function OverviewReadingNum({
  children,
  suffix,
}: {
  children: ReactNode;
  suffix?: ReactNode;
}) {
  return (
    <span className="ds-overview-situation__num ds-num">
      {children}
      {suffix != null ? <small>{suffix}</small> : null}
    </span>
  );
}

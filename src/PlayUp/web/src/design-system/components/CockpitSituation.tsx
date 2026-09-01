import type { ReactNode } from 'react'
import { PanelHead } from './PanelHead'
import { CycleLine } from './CycleLine'

const DEFAULT_CYCLE_STEPS = ['Préparation', 'Calendrier', 'En cours', 'Terminée']

export function CockpitSituation({
  title,
  reading,
  label,
  cycle,
  id,
}: {
  title: string
  reading: ReactNode
  label?: ReactNode
  cycle?: { current: string; done?: string[]; steps?: string[] }
  id?: string
}) {
  return (
    <section className="ds-panel ds-cockpit-situation" aria-labelledby={id}>
      <PanelHead id={id} title={title} />
      <div className="ds-cockpit-situation__reading">{reading}</div>
      {label != null ? (
        typeof label === 'string' ? (
          <p className="ds-cockpit-situation__label">{label}</p>
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
  )
}

export function CockpitSituationNum({
  children,
  suffix,
}: {
  children: ReactNode
  suffix?: ReactNode
}) {
  return (
    <span className="ds-cockpit-situation__num ds-num">
      {children}
      {suffix != null ? <small>{suffix}</small> : null}
    </span>
  )
}

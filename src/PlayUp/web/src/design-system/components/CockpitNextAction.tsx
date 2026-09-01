import type { ReactNode } from 'react'
import { PanelHead } from './PanelHead'

/**
 * Prochaine action — title/why + primary CTA.
 * Lab: `eyebrow` inside body. Product: `heading` via PanelHead (tests).
 */
export function CockpitNextAction({
  eyebrow,
  title,
  why,
  action,
  heading,
  headingId,
  icon,
  className,
}: {
  eyebrow?: string
  title: string
  why?: string
  action?: ReactNode
  heading?: string
  headingId?: string
  icon?: ReactNode
  className?: string
}) {
  const classes = ['ds-panel', 'ds-cockpit-next-action', className]
    .filter(Boolean)
    .join(' ')

  return (
    <section className={classes} aria-labelledby={headingId}>
      {heading ? <PanelHead id={headingId} title={heading} icon={icon} /> : null}
      <div className="ds-cockpit-next-action__body">
        {!heading && eyebrow ? <span className="ds-eyebrow">{eyebrow}</span> : null}
        <span className="ds-cockpit-next-action__title">{title}</span>
        {why ? <span className="ds-cockpit-next-action__why">{why}</span> : null}
      </div>
      {action}
    </section>
  )
}

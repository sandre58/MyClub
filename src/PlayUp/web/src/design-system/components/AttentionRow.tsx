import type { ReactNode } from 'react'

export function AttentionRow({
  count,
  icon,
  title,
  detail,
  action,
  tone = 'attention',
}: {
  count: ReactNode
  icon: ReactNode
  title: string
  detail?: string
  action?: ReactNode
  tone?: 'attention' | 'info'
}) {
  return (
    <div className="ds-cockpit-attention__row" data-tone={tone}>
      <span className="ds-cockpit-attention__count ds-num">{count}</span>
      <span className="ds-cockpit-attention__icon" aria-hidden="true">
        {icon}
      </span>
      <span className="ds-cockpit-attention__text">
        <span className="ds-cockpit-attention__title">{title}</span>
        {detail ? (
          <span className="ds-cockpit-attention__detail">{detail}</span>
        ) : null}
      </span>
      {action}
    </div>
  )
}

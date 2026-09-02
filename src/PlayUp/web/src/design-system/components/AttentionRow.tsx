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
    <div className="ds-overview-attention__row" data-tone={tone}>
      <span className="ds-overview-attention__count ds-num">{count}</span>
      <span className="ds-overview-attention__icon" aria-hidden="true">
        {icon}
      </span>
      <span className="ds-overview-attention__text">
        <span className="ds-overview-attention__title">{title}</span>
        {detail ? (
          <span className="ds-overview-attention__detail">{detail}</span>
        ) : null}
      </span>
      {action}
    </div>
  )
}

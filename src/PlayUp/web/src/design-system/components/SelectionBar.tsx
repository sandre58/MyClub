import type { ReactNode } from 'react'

/**
 * Multi-select chrome — count + icon toolbar.
 * Host owns geometry (ops tail / slot); this paints the bar.
 */
export function SelectionBar({
  countLabel,
  children,
  className,
}: {
  countLabel: ReactNode
  children: ReactNode
  className?: string
}) {
  const classes = ['ds-selection-bar', className].filter(Boolean).join(' ')

  return (
    <div className={classes} role="status">
      <p className="ds-selection-bar__count">{countLabel}</p>
      <div className="ds-icon-toolbar">{children}</div>
    </div>
  )
}

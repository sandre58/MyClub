import type { ReactNode } from 'react'

export type StatusDensity = 'context' | 'dense'
export type StatusVariant = 'soft' | 'outline'
export type StatusShape = 'rounded' | 'pill'
export type StatusTone =
  | 'neutral'
  | 'info'
  | 'success'
  | 'live'
  | 'done'
  | 'attention'
  | 'error'

/**
 * Status primitive — presentation only. Read supplies labels and meaning.
 */
export function Status({
  density,
  tone = 'info',
  variant = 'soft',
  shape = 'rounded',
  children,
  className,
}: {
  density: StatusDensity
  tone?: StatusTone
  variant?: StatusVariant
  shape?: StatusShape
  children: ReactNode
  className?: string
}) {
  const classes = [
    'ds-status',
    `ds-status--${density}`,
    density === 'context' && `ds-status--${variant}`,
    density === 'context' && `ds-status--tone-${tone}`,
    density === 'context' && `ds-status--${shape}`,
    className,
  ]
    .filter(Boolean)
    .join(' ')

  return <span className={classes}>{children}</span>
}

/** Maps legacy StatusBadge tone names to StatusTone. */
export function statusToneFromLegacy(
  tone: 'neutral' | 'info' | 'ok' | 'live' | 'done' | 'warn' | 'danger',
): StatusTone {
  switch (tone) {
    case 'ok':
      return 'success'
    case 'warn':
      return 'attention'
    case 'danger':
      return 'error'
    default:
      return tone
  }
}

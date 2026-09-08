import type { ReactNode } from 'react'

export type ChipTone = 'neutral' | 'soft' | 'accent' | 'win' | 'draw' | 'loss'

/**
 * Lightweight fact chip — rule tokens, meta labels. Not a lifecycle Status.
 */
export function Chip({
  tone = 'neutral',
  title,
  children,
  className,
}: {
  tone?: ChipTone
  title?: string
  children: ReactNode
  className?: string
}) {
  const classes = ['ds-chip', `ds-chip--${tone}`, className].filter(Boolean).join(' ')
  return (
    <span className={classes} title={title}>
      {children}
    </span>
  )
}

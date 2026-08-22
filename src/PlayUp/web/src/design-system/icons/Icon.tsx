import type { ReactNode, SVGProps } from 'react'

export type IconSize = 'sm' | 'md' | 'lg'

const sizeClass: Record<IconSize, string> = {
  sm: 'ds-icon ds-icon--sm',
  md: 'ds-icon ds-icon--md',
  lg: 'ds-icon ds-icon--lg',
}

/**
 * Stroke icon wrapper — 24×24 grid, Lucide-compatible geometry.
 * Colour = currentColor on the parent only (monocolor chrome).
 */
export function Icon({
  size = 'md',
  className,
  children,
  ...props
}: SVGProps<SVGSVGElement> & {
  size?: IconSize
  children: ReactNode
}) {
  const classes = [sizeClass[size], className].filter(Boolean).join(' ')

  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={2}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden={props['aria-hidden'] ?? true}
      {...props}
      className={classes}
    >
      {children}
    </svg>
  )
}

import type { LucideIcon as LucideGlyph } from 'lucide-react'
import type { ReactNode, SVGProps } from 'react'

export type IconSize = 'sm' | 'md' | 'lg'

const sizeClass: Record<IconSize, string> = {
  sm: 'ds-icon ds-icon--sm',
  md: 'ds-icon ds-icon--md',
  lg: 'ds-icon ds-icon--lg',
}

const sizePixels: Record<IconSize, number> = {
  sm: 16,
  md: 20,
  lg: 24,
}

export type AppIconProps = SVGProps<SVGSVGElement> & {
  size?: IconSize
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
}: AppIconProps & {
  size?: IconSize
  children: ReactNode
}) {
  const classes = [sizeClass[size], className].filter(Boolean).join(' ')

  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={props.strokeWidth ?? 'var(--icon-stroke-width, 1.8)'}
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

export function LucideIcon({
  icon: Glyph,
  size = 'md',
  className,
  strokeWidth,
  ...props
}: AppIconProps & {
  icon: LucideGlyph
}) {
  const classes = [sizeClass[size], className].filter(Boolean).join(' ')

  return (
    <Glyph
      {...props}
      size={sizePixels[size]}
      strokeWidth={strokeWidth ?? 'var(--icon-stroke-width, 1.8)'}
      aria-hidden={props['aria-hidden'] ?? true}
      className={classes}
    />
  )
}

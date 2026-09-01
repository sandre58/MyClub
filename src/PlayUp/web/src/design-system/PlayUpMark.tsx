import { useId } from 'react'

export type PlayUpMarkVariant = 'gradient' | 'brand' | 'ink' | 'on-chrome'

export type PlayUpMarkProps = {
  size?: number
  variant?: PlayUpMarkVariant
  /** System lean angle in degrees. Default 12 — arbitrated 2026-09-01. */
  leanDeg?: 9 | 12
  /** Accessible name. Omit for decorative use next to a visible "Play'Up". */
  title?: string
  className?: string
}

/**
 * Play'Up monogram: a heavy italic P cut by one ascending arrow.
 *
 * A single reserve does three jobs: it is the arrow, it stands in for the
 * counter, and where it runs out at the bottom left it cuts the foot loose as
 * its own mass. Adding a round counter on top of it is what breaks the mark —
 * the bowl is not thick enough to carry both.
 *
 * The gradient is the brand's one sanctioned exception to the no-gradient
 * rule and never leaves this mark. Below 24px use a flat variant.
 */
export function PlayUpMark({
  size = 24,
  variant = 'brand',
  leanDeg = 12,
  title,
  className,
}: PlayUpMarkProps) {
  const uid = useId()
  const reserveId = `playup-reserve-${uid}`
  const gradientId = `playup-gradient-${uid}`
  const italic = `translate(10 0) skewX(-${leanDeg})`

  return (
    <svg
      className={className}
      width={size}
      height={size}
      viewBox="0 0 100 100"
      fill="none"
      role={title ? 'img' : undefined}
      aria-hidden={title ? undefined : true}
      focusable="false"
    >
      {title ? <title>{title}</title> : null}
      <defs>
        {variant === 'gradient' ? (
          <linearGradient id={gradientId} x1="0.1" y1="1" x2="0.9" y2="0">
            <stop offset="0%" stopColor="#154a8f" />
            <stop offset="100%" stopColor="#2f86e0" />
          </linearGradient>
        ) : null}
        <mask
          id={reserveId}
          maskUnits="userSpaceOnUse"
          maskContentUnits="userSpaceOnUse"
          x="0"
          y="0"
          width="100"
          height="100"
        >
          <rect x="0" y="0" width="100" height="100" fill="#fff" />
          <g transform={italic}>
            <path
              d={ARROW}
              transform={ARROW_PLACEMENT}
              fill="#000"
              stroke="#000"
              strokeWidth={reserveGrowth(size)}
              strokeLinejoin="miter"
            />
          </g>
        </mask>
      </defs>
      <g transform={italic}>
        <path
          d={BODY}
          fill={markPaint(variant, gradientId)}
          mask={`url(#${reserveId})`}
        />
      </g>
    </svg>
  )
}

/** Heavy P: round bowl, thick stem, foot sheared on the diagonal. */
const BODY = 'M12 84 V16 A8 8 0 0 1 20 8 H36 A34 34 0 1 1 44 70 V92 H22 Z'

/** Tail running out of the foot, head standing in for the counter. */
const ARROW = 'M0 -10 H48 V-20 L74 0 L48 20 V10 H0 Z'
const ARROW_PLACEMENT = 'translate(20 86) rotate(-51.6)'

function markPaint(variant: PlayUpMarkVariant, gradientId: string): string {
  switch (variant) {
    case 'gradient':
      return `url(#${gradientId})`
    case 'ink':
      return 'var(--color-ink)'
    case 'on-chrome':
      return 'var(--color-on-chrome)'
    default:
      return 'var(--color-brand)'
  }
}

/**
 * The reserve has to be grown at small sizes or the arrow welds itself shut
 * and the mark collapses into a solid blob.
 */
function reserveGrowth(size: number): number {
  if (size <= 18) return 6
  if (size <= 24) return 4
  if (size <= 36) return 2
  return 0
}

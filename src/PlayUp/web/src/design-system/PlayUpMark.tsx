import { useId } from 'react'

export type PlayUpMarkVariant = 'gradient' | 'brand' | 'ink' | 'on-chrome'

export type PlayUpMarkProps = {
  size?: number
  variant?: PlayUpMarkVariant
  /** Accessible name. Omit for decorative use next to a visible "Play’Up". */
  title?: string
  className?: string
}

/**
 * Play’Up monogram: a P whose counter is an ascending arrow.
 *
 * The gradient is the brand's one sanctioned exception to the no-gradient
 * rule and never leaves this mark. Below 24px use a flat variant — the two
 * stops are too close together to survive that few pixels.
 */
export function PlayUpMark({
  size = 24,
  variant = 'brand',
  title,
  className = '',
}: PlayUpMarkProps) {
  const uid = useId()
  const maskId = `playup-mark-mask-${uid}`
  const gradientId = `playup-mark-gradient-${uid}`

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
          <linearGradient id={gradientId} x1="0.05" y1="1" x2="0.95" y2="0">
            <stop offset="0%" stopColor="#154a8f" />
            <stop offset="100%" stopColor="#2f86e0" />
          </linearGradient>
        ) : null}
        <mask
          id={maskId}
          maskUnits="userSpaceOnUse"
          maskContentUnits="userSpaceOnUse"
          x="0"
          y="0"
          width="100"
          height="100"
        >
          <rect x="0" y="0" width="100" height="100" fill="#fff" />
          <g
            transform={GLYPH_TRANSFORM}
            stroke="#000"
            strokeWidth={arrowReserve(size)}
            strokeLinecap="round"
            strokeLinejoin="round"
            fill="none"
          >
            <path d={ARROW_SHAFT} />
            <path d={ARROW_HEAD} />
          </g>
        </mask>
      </defs>
      <g mask={`url(#${maskId})`}>
        <g transform={GLYPH_TRANSFORM}>
          <path d={BODY} fill={markPaint(variant, gradientId)} />
        </g>
      </g>
    </svg>
  )
}

/** 9° from vertical — the single angle the whole identity is built on. */
const GLYPH_TRANSFORM = 'translate(8 0) skewX(-9)'

const BODY = 'M20 94 V6 H55 A31 31 0 0 1 55 68 H43 V94 Z'

/**
 * The arrow is the counter. A round counter plus an arrow does not fit: the
 * bowl is only 15 units thick between the two, so the arrow either severs the
 * stem or falls into the hole. Making the reserve itself the arrow keeps the
 * letter readable and the gesture legible.
 */
const ARROW_SHAFT = 'M52 55 L74 27'
const ARROW_HEAD = 'M56 27 L74 27 L74 45'

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
 * Optical correction: the smaller the mark, the wider the reserve has to be
 * to survive rasterization, otherwise the arrow closes up into a solid bowl.
 */
function arrowReserve(size: number): number {
  if (size <= 18) return 12
  if (size <= 24) return 10.5
  if (size <= 36) return 9
  return 8
}

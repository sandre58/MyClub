import { useId } from 'react'
import {
  MONOGRAM,
  buildCartouche,
  buildPBody,
  buildReserve,
  fillForVariant,
  monogramInkForVariant,
  opticalTierForSize,
  type MarkVariant,
} from './brand/monogram-geometry'

export type PlayUpMarkVariant = MarkVariant | 'on-chrome'

export type PlayUpMarkProps = {
  size?: number
  variant?: PlayUpMarkVariant
  /** App-mark lockup: rounded square cartouche behind an inset monogram. */
  cartouche?: boolean
  /** Accessible name. Omit for decorative use next to a visible "Play'Up". */
  title?: string
  className?: string
}

/**
 * Play'Up monogram — consumes brand/monogram-geometry (SoT), never redraws.
 * Gradient ≥ 24px, flat below. Reserve thickens automatically at small sizes.
 */
export function PlayUpMark({
  size = 24,
  variant = 'brand',
  cartouche = false,
  title,
  className,
}: PlayUpMarkProps) {
  const uid = useId()
  const maskId = `playup-reserve-${uid}`
  const gradientId = `playup-gradient-${uid}`

  const resolved: MarkVariant = variant === 'on-chrome' ? 'white' : variant
  const paintVariant: MarkVariant =
    resolved === 'gradient' && size < 24 ? 'brand' : resolved

  const tier = opticalTierForSize(size)
  const inset = cartouche ? MONOGRAM.cartoucheInset : undefined

  const cartoucheFill = !cartouche
    ? null
    : paintVariant === 'gradient'
      ? `url(#${gradientId})`
      : paintVariant === 'brand'
        ? 'var(--color-brand, #1c67c4)'
        : MONOGRAM.gradientDeep

  const letterFill = cartouche
    ? monogramInkForVariant(paintVariant)
    : fillForVariant(paintVariant, gradientId)

  return (
    <svg
      className={className}
      width={size}
      height={size}
      viewBox={`0 0 ${MONOGRAM.viewBox} ${MONOGRAM.viewBox}`}
      fill="none"
      role={title ? 'img' : undefined}
      aria-hidden={title ? undefined : true}
      focusable="false"
    >
      {title ? <title>{title}</title> : null}
      <defs>
        {paintVariant === 'gradient' ? (
          <linearGradient id={gradientId} x1="0.12" y1="1" x2="0.88" y2="0">
            <stop offset="0%" stopColor={MONOGRAM.gradientDeep} />
            <stop offset="100%" stopColor={MONOGRAM.gradientBright} />
          </linearGradient>
        ) : null}
        <mask id={maskId} maskUnits="userSpaceOnUse" x="0" y="0" width="100" height="100">
          <rect width="100" height="100" fill="#fff" />
          <g transform={inset}>
            {buildReserve(tier).map((el, i) =>
              el.kind === 'fill' ? (
                <path
                  key={i}
                  d={el.d}
                  fill="#000"
                  stroke="#000"
                  strokeWidth={1.5}
                  strokeLinejoin="round"
                />
              ) : (
                <path
                  key={i}
                  d={el.d}
                  fill="none"
                  stroke="#000"
                  strokeWidth={el.width}
                  strokeLinecap="round"
                />
              ),
            )}
          </g>
        </mask>
      </defs>
      {cartoucheFill ? <path d={buildCartouche()} fill={cartoucheFill} /> : null}
      <path
        d={buildPBody()}
        transform={inset}
        fill={letterFill}
        mask={`url(#${maskId})`}
      />
    </svg>
  )
}

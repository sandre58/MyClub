/**
 * Play'Up monogram — master vector geometry (SoT).
 *
 * Construction (decision 2026-09-01, refined 2026-09-01 evening):
 *   - Solid round P: circular bowl tangent to a 12°-leaned stem, rounded foot.
 *     The letter fully encloses the reserve — the silhouette is never broken.
 *   - The counter is an ascending arrow, slightly curved, whose tail forks:
 *     one branch is the arrow's own tail, the second bends into a U in the
 *     bottom of the P — the letter literally contains "Up".
 *   - 12° baked into coordinates — no CSS skewX anywhere.
 *
 * Pipeline: brand decision → this module → SVG assets → PlayUpMark → Design Lab.
 */

export const MONOGRAM = {
  viewBox: 100,
  /** Rounded square — ~22 % corner radius, distinct from heraldic shield. */
  cartoucheRx: 22,
  leanDeg: 12,
  gradientDeep: '#154a8f',
  gradientBright: '#2f86e0',
  /** Letter inset when drawn inside the cartouche. */
  cartoucheInset: 'translate(15 13) scale(0.72)',
} as const

type Vec = { x: number; y: number }

const DEG = Math.PI / 180
const LEAN = MONOGRAM.leanDeg * DEG

/** Stem axis, pointing up. */
const U: Vec = { x: Math.sin(LEAN), y: -Math.cos(LEAN) }
/** Perpendicular, pointing right. */
const PV: Vec = { x: Math.cos(LEAN), y: Math.sin(LEAN) }

const add = (a: Vec, b: Vec): Vec => ({ x: a.x + b.x, y: a.y + b.y })
const mul = (a: Vec, k: number): Vec => ({ x: a.x * k, y: a.y * k })
const fmt = (v: Vec) => `${round(v.x)} ${round(v.y)}`
const round = (n: number) => Math.round(n * 100) / 100

/* ------------------------------------------------------------------ */
/* Letter — solid round P                                              */
/* ------------------------------------------------------------------ */

const STEM_W = 17
const STEM_HALF = STEM_W / 2
const BOWL_R = 32

/** Foot centre (bottom of stem, before the rounded cap). */
const FOOT_C: Vec = { x: 24.5, y: 90 }
const BL = add(FOOT_C, mul(PV, -STEM_HALF))
const BR = add(FOOT_C, mul(PV, STEM_HALF))

/** Tangency parameter along the left edge — sets the bowl height. */
const T_PARAM = 56
/** Tangency point: bowl circle is tangent to the stem's left edge. */
const TAN = add(BL, mul(U, T_PARAM))
/** Bowl centre. */
const BOWL_C = add(TAN, mul(PV, BOWL_R))

/** Intersection of the bowl circle with the stem's right edge (lower hit). */
function rightEdgeJunction(): Vec {
  const d = { x: BR.x - BOWL_C.x, y: BR.y - BOWL_C.y }
  const b = d.x * U.x + d.y * U.y
  const c = d.x * d.x + d.y * d.y - BOWL_R * BOWL_R
  const t = -b - Math.sqrt(b * b - c)
  return add(BR, mul(U, t))
}

/** Heavy round P — stem, tangent bowl, rounded foot cap. */
export function buildPBody(): string {
  const j = rightEdgeJunction()
  return [
    `M ${fmt(BL)}`,
    `L ${fmt(TAN)}`,
    `A ${BOWL_R} ${BOWL_R} 0 1 1 ${fmt(j)}`,
    `L ${fmt(BR)}`,
    `A ${STEM_HALF} ${STEM_HALF} 0 0 1 ${fmt(BL)}`,
    'Z',
  ].join(' ')
}

/* ------------------------------------------------------------------ */
/* Reserve — curved arrow forking into a U, fully inside the letter    */
/* ------------------------------------------------------------------ */

export type OpticalTier = 'master' | 'small' | 'tiny'

const OPTICAL: Record<
  OpticalTier,
  { strokeW: number; headHalf: number; headDepth: number }
> = {
  master: { strokeW: 7.2, headHalf: 10.5, headDepth: 14 },
  small: { strokeW: 8.2, headHalf: 11.5, headDepth: 15 },
  tiny: { strokeW: 9.6, headHalf: 13, headDepth: 16.5 },
}

/** Map rendered px → optical tier for reserve thickening. */
export function opticalTierForSize(px: number): OpticalTier {
  if (px <= 16) return 'tiny'
  if (px <= 20) return 'small'
  return 'master'
}

/** Head axis elevation (from horizontal). */
const HEAD_ANGLE = 42 * DEG
const HEAD_DIR: Vec = { x: Math.cos(HEAD_ANGLE), y: -Math.sin(HEAD_ANGLE) }
const HEAD_PERP: Vec = { x: -HEAD_DIR.y, y: HEAD_DIR.x }

/** Arrow tip — inside the bowl, ~10 units from the edge. */
const TIP = add(BOWL_C, mul(HEAD_DIR, 22))

/** Fork point — where the tail and the U separate, above the junction. */
const FORK: Vec = { x: 40, y: 54 }

export type ReserveElement =
  | { kind: 'fill'; d: string }
  | { kind: 'stroke'; d: string; width: number }

/**
 * Reserve elements, drawn in the mask (or as debug overlay):
 *   - solid triangular head;
 *   - slightly curved shaft from fork to head base;
 *   - tail branch (round cap);
 *   - U branch: bends down and back up in the bottom of the P.
 */
export function buildReserve(tier: OpticalTier = 'master'): ReserveElement[] {
  const { strokeW, headHalf, headDepth } = OPTICAL[tier]

  const base = add(TIP, mul(HEAD_DIR, -headDepth))
  const b1 = add(base, mul(HEAD_PERP, headHalf))
  const b2 = add(base, mul(HEAD_PERP, -headHalf))
  const head = `M ${fmt(TIP)} L ${fmt(b1)} L ${fmt(b2)} Z`

  // Slightly curved shaft — control point pulled toward lower right.
  const mid = mul(add(FORK, base), 0.5)
  const ctrl = add(mid, mul(HEAD_PERP, 3.5))
  const shaft = `M ${fmt(FORK)} Q ${fmt(ctrl)} ${fmt(base)}`

  // Tail (branch 1) — continues the curve down into the stem, round cap.
  const tail = `M ${fmt(FORK)} Q 33.5 61.5 29.5 71`

  // U (branch 2) — bends down and back up in the bottom of the bowl.
  const u = `M ${fmt(FORK)} Q 40.5 62 48.5 62.5 Q 57.8 63 58.8 48.5`

  return [
    { kind: 'fill', d: head },
    { kind: 'stroke', d: shaft, width: strokeW },
    { kind: 'stroke', d: tail, width: strokeW },
    { kind: 'stroke', d: u, width: strokeW },
  ]
}

/* ------------------------------------------------------------------ */
/* Cartouche / variants                                                */
/* ------------------------------------------------------------------ */

/** Cartouche rounded-square path (full viewBox). */
export function buildCartouche(): string {
  const { viewBox: m, cartoucheRx: r } = MONOGRAM
  return `M ${r} 0 H ${m - r} Q ${m} 0 ${m} ${r} V ${m - r} Q ${m} ${m} ${m - r} ${m} H ${r} Q 0 ${m} 0 ${m - r} V ${r} Q 0 0 ${r} 0 Z`
}

export type MarkVariant = 'gradient' | 'brand' | 'ink' | 'white'

export function fillForVariant(variant: MarkVariant, gradientId: string): string {
  switch (variant) {
    case 'gradient':
      return `url(#${gradientId})`
    case 'ink':
      return 'var(--color-ink, #14171c)'
    case 'white':
      return '#ffffff'
    default:
      return 'var(--color-brand, #1c67c4)'
  }
}

/** Monogram ink when drawn on a cartouche. */
export function monogramInkForVariant(variant: MarkVariant): string {
  if (variant === 'white') return MONOGRAM.gradientDeep
  return '#ffffff'
}

/* ------------------------------------------------------------------ */
/* Standalone asset serialization                                      */
/* ------------------------------------------------------------------ */

function svgDoc(body: string): string {
  const { viewBox } = MONOGRAM
  return `<?xml version="1.0" encoding="UTF-8"?>
<!-- Generated from design-system/brand/monogram-geometry.ts - do not edit by hand. -->
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${viewBox} ${viewBox}" fill="none">
${body}
</svg>`
}

/** Reserve serialized for a mask (black on white) or overlay (any color). */
export function serializeReserve(
  tier: OpticalTier,
  color: string,
  transform?: string,
): string {
  const t = transform ? ` transform="${transform}"` : ''
  return buildReserve(tier)
    .map((el) =>
      el.kind === 'fill'
        ? `<path d="${el.d}"${t} fill="${color}" stroke="${color}" stroke-width="1.5" stroke-linejoin="round"/>`
        : `<path d="${el.d}"${t} fill="none" stroke="${color}" stroke-width="${el.width}" stroke-linecap="round"/>`,
    )
    .join('\n      ')
}

function maskDef(id: string, tier: OpticalTier, transform?: string): string {
  return `  <mask id="${id}">
      <rect width="100" height="100" fill="#fff"/>
      ${serializeReserve(tier, '#000', transform)}
    </mask>`
}

/** Master brand asset — gradient monogram, no cartouche. */
export function serializeMasterSvg(): string {
  const { gradientDeep, gradientBright } = MONOGRAM
  return svgDoc(`  <defs>
    <linearGradient id="playup-brand-gradient" x1="0.12" y1="1" x2="0.88" y2="0">
      <stop offset="0%" stop-color="${gradientDeep}"/>
      <stop offset="100%" stop-color="${gradientBright}"/>
    </linearGradient>
  ${maskDef('playup-arrow-reserve', 'master')}
  </defs>
  <path d="${buildPBody()}" fill="url(#playup-brand-gradient)" mask="url(#playup-arrow-reserve)"/>`)
}

/** App mark / favicon — flat brand cartouche, white monogram, tiny tier. */
export function serializeFaviconSvg(): string {
  const inset = MONOGRAM.cartoucheInset
  return svgDoc(`  <defs>
  ${maskDef('playup-favicon-reserve', 'tiny', inset)}
  </defs>
  <path d="${buildCartouche()}" fill="#1c67c4"/>
  <path d="${buildPBody()}" transform="${inset}" fill="#fff" mask="url(#playup-favicon-reserve)"/>`)
}

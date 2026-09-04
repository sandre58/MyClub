/** Color math for ColorPicker — opaque #RRGGBB only (no alpha). */

export type Hsv = { h: number; s: number; v: number }
export type Rgb = { r: number; g: number; b: number }

export type ColorFormat = 'hex' | 'rgb' | 'hsb'

export const HEX6 = /^#[0-9A-Fa-f]{6}$/

const DEFAULT_HSV: Hsv = { h: 210, s: 0.65, v: 0.62 }

export function defaultHsv(): Hsv {
  return { ...DEFAULT_HSV }
}

export function clamp01(n: number): number {
  return Math.min(1, Math.max(0, n))
}

export function clampHue(n: number): number {
  if (!Number.isFinite(n)) {
    return 0
  }
  const wrapped = n % 360
  return wrapped < 0 ? wrapped + 360 : wrapped
}

export function clampByte(n: number): number {
  if (!Number.isFinite(n)) {
    return 0
  }
  return Math.min(255, Math.max(0, Math.round(n)))
}

export function rgbToHex(rgb: Rgb): string {
  return `#${clampByte(rgb.r).toString(16).padStart(2, '0').toUpperCase()}${clampByte(rgb.g).toString(16).padStart(2, '0').toUpperCase()}${clampByte(rgb.b).toString(16).padStart(2, '0').toUpperCase()}`
}

export function hexToRgb(hex: string): Rgb | null {
  if (!HEX6.test(hex)) {
    return null
  }
  return {
    r: Number.parseInt(hex.slice(1, 3), 16),
    g: Number.parseInt(hex.slice(3, 5), 16),
    b: Number.parseInt(hex.slice(5, 7), 16),
  }
}

export function hsvToRgb(hsv: Hsv): Rgb {
  const h = clampHue(hsv.h)
  const s = clamp01(hsv.s)
  const v = clamp01(hsv.v)

  const c = v * s
  const x = c * (1 - Math.abs(((h / 60) % 2) - 1))
  const m = v - c

  let r = 0
  let g = 0
  let b = 0

  if (h < 60) {
    r = c
    g = x
  } else if (h < 120) {
    r = x
    g = c
  } else if (h < 180) {
    g = c
    b = x
  } else if (h < 240) {
    g = x
    b = c
  } else if (h < 300) {
    r = x
    b = c
  } else {
    r = c
    b = x
  }

  return {
    r: clampByte((r + m) * 255),
    g: clampByte((g + m) * 255),
    b: clampByte((b + m) * 255),
  }
}

export function rgbToHsv(rgb: Rgb, fallbackHue = DEFAULT_HSV.h): Hsv {
  const r = clampByte(rgb.r) / 255
  const g = clampByte(rgb.g) / 255
  const b = clampByte(rgb.b) / 255

  const max = Math.max(r, g, b)
  const min = Math.min(r, g, b)
  const d = max - min

  let h = fallbackHue
  if (d > 1e-6) {
    if (max === r) {
      h = 60 * (((g - b) / d) % 6)
    } else if (max === g) {
      h = 60 * ((b - r) / d + 2)
    } else {
      h = 60 * ((r - g) / d + 4)
    }
  }

  return {
    h: clampHue(h),
    s: max <= 1e-6 ? 0 : d / max,
    v: max,
  }
}

export function hsvToHex(hsv: Hsv): string {
  return rgbToHex(hsvToRgb(hsv))
}

/**
 * Parse #RRGGBB → HSV. When chroma is near 0, keep `fallbackHue`
 * so the hue slider does not jump on greys.
 */
export function hexToHsv(hex: string, fallbackHue = DEFAULT_HSV.h): Hsv | null {
  const rgb = hexToRgb(hex)
  if (!rgb) {
    return null
  }
  return rgbToHsv(rgb, fallbackHue)
}

/** Pure hue at full S/V — used for the S/V panel base fill. */
export function hueCss(h: number): string {
  return hsvToHex({ h: clampHue(h), s: 1, v: 1 })
}

/** Normalize EyeDropper / pasted hex to #RRGGBB uppercase when possible. */
export function normalizeHex(raw: string): string | null {
  const trimmed = raw.trim()
  if (HEX6.test(trimmed)) {
    return trimmed.toUpperCase()
  }
  const short = /^#([0-9A-Fa-f]{3})$/.exec(trimmed)
  if (short) {
    const [r, g, b] = short[1]
    return `#${r}${r}${g}${g}${b}${b}`.toUpperCase()
  }
  return null
}

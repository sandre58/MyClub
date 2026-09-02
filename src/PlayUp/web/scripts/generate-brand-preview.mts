/**
 * Scratch visual-iteration page (not product, not committed intent).
 * Run: npx tsx scripts/generate-brand-preview.mts → public/brand/preview.html
 */
import { writeFileSync } from 'node:fs'
import {
  MONOGRAM,
  buildCartouche,
  buildPBody,
  opticalTierForSize,
  serializeReserve,
} from '../src/design-system/brand/monogram-geometry.ts'

function mark(size: number, fill: string, cartouche?: { bg: string; ink: string }) {
  const tier = opticalTierForSize(size)
  const id = `m${Math.random().toString(36).slice(2, 8)}`
  const inset = cartouche ? MONOGRAM.cartoucheInset : undefined
  const insetAttr = inset ? ` transform="${inset}"` : ''
  const grad =
    fill === 'gradient'
      ? `<linearGradient id="g${id}" x1="0.12" y1="1" x2="0.88" y2="0"><stop offset="0%" stop-color="${MONOGRAM.gradientDeep}"/><stop offset="100%" stop-color="${MONOGRAM.gradientBright}"/></linearGradient>`
      : ''
  const paint = fill === 'gradient' ? `url(#g${id})` : fill
  return `<svg width="${size}" height="${size}" viewBox="0 0 100 100" fill="none">
  <defs>${grad}<mask id="${id}"><rect width="100" height="100" fill="#fff"/>${serializeReserve(tier, '#000', inset)}</mask></defs>
  ${cartouche ? `<path d="${buildCartouche()}" fill="${cartouche.bg}"/>` : ''}
  <path d="${buildPBody()}"${insetAttr} fill="${cartouche ? cartouche.ink : paint}" mask="url(#${id})"/>
</svg>`
}

const sizes = [16, 20, 24, 32, 48, 96, 160]

function debugView(size: number) {
  return `<svg width="${size}" height="${size}" viewBox="0 0 100 100" fill="none">
  <rect width="100" height="100" fill="#fff"/>
  <path d="${buildPBody()}" fill="#9db2c8"/>
  ${serializeReserve('master', 'rgba(200,30,30,.75)')}
</svg>`
}

const html = `<!doctype html><meta charset="utf-8"><title>brand preview</title>
<style>
body{margin:0;font:13px system-ui;display:flex;flex-direction:column}
section{display:flex;align-items:flex-end;gap:28px;padding:24px 32px}
.dark{background:#0f1728;color:#e8ecf5}.light{background:#f6f7f9;color:#14171c}
figure{margin:0;display:flex;flex-direction:column;align-items:center;gap:6px}
figcaption{font-size:11px;opacity:.7}
h2{margin:16px 32px 0;font-size:13px;text-transform:uppercase;letter-spacing:.08em}
.light h2{color:#5a6270}
</style>
<div class="light">
<h2>Debug — lettre + réserve (master)</h2>
<section><figure>${debugView(320)}<figcaption>320px overlay</figcaption></figure><figure>${mark(320, 'gradient')}<figcaption>320px final</figcaption></figure></section>
</div>
<div class="dark">
<h2>Dégradé sur chrome — sans cartouche</h2>
<section>${sizes.map((s) => `<figure>${mark(s, 'gradient')}<figcaption>${s}px</figcaption></figure>`).join('')}</section>
<h2>Blanc sur chrome</h2>
<section>${[16, 24, 48, 96].map((s) => `<figure>${mark(s, '#ffffff')}<figcaption>${s}px</figcaption></figure>`).join('')}</section>
</div>
<div class="light">
<h2>Dégradé sur clair</h2>
<section>${[16, 24, 48, 96, 160].map((s) => `<figure>${mark(s, 'gradient')}<figcaption>${s}px</figcaption></figure>`).join('')}</section>
<h2>Encre sur clair</h2>
<section>${[16, 24, 48, 96].map((s) => `<figure>${mark(s, '#14171c')}<figcaption>${s}px</figcaption></figure>`).join('')}</section>
<h2>App mark — cartouche brand flat</h2>
<section>${[16, 24, 32, 48, 96].map((s) => `<figure>${mark(s, '', { bg: '#1c67c4', ink: '#fff' })}<figcaption>${s}px</figcaption></figure>`).join('')}</section>
</div>`

writeFileSync('public/brand/preview.html', html)
console.log('preview written')

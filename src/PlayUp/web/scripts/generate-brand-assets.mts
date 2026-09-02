/**
 * Regenerate brand SVG assets from the geometry SoT.
 * Run: npx tsx scripts/generate-brand-assets.mts
 */
import { mkdirSync, writeFileSync } from 'node:fs'
import {
  serializeFaviconSvg,
  serializeMasterSvg,
} from '../src/design-system/brand/monogram-geometry.ts'

mkdirSync('src/design-system/brand/assets', { recursive: true })
mkdirSync('public/brand', { recursive: true })

const master = serializeMasterSvg()
writeFileSync('src/design-system/brand/assets/monogram-master.svg', master)
writeFileSync('public/brand/monogram-master.svg', master)
writeFileSync('public/favicon.svg', serializeFaviconSvg())

console.log('brand assets regenerated')

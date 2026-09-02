import type { ReactNode } from 'react'
import { PlayUpMark } from '../design-system/PlayUpMark'
import type { MarkVariant } from '../design-system/brand/monogram-geometry'

const SIZES = [16, 20, 24, 32, 48, 64, 96] as const
const VARIANTS: Array<{ key: MarkVariant; label: string }> = [
  { key: 'gradient', label: 'Dégradé' },
  { key: 'brand', label: 'Brand flat' },
  { key: 'ink', label: 'Encre' },
  { key: 'white', label: 'Blanc' },
]

const BACKGROUNDS = [
  { key: 'light', label: 'Clair', className: 'dlab-brand-bg--light' },
  { key: 'dark', label: 'Chrome', className: 'dlab-brand-bg--dark' },
  { key: 'brand', label: 'Brand', className: 'dlab-brand-bg--brand' },
] as const

/**
 * Design Lab — preuve visuelle Lot A (monogramme maître).
 * Valide géométrie, optique petite taille, variantes et cartouche.
 */
export function LabBrand() {
  return (
    <div className="ds-page dlab-brand">
      <header className="dlab-brand-intro">
        <h2 className="ds-page__title">Monogramme — géométrie maîtresse</h2>
        <p className="dlab-brand-intro__lead">
          Source de vérité : <code>design-system/brand/monogram-geometry.ts</code>.
          P + flèche en réserve, 12° natif, cartouche carré arrondi. Pas de{' '}
          <code>skewX()</code> CSS — optique 16 / 20 / 24 px via tiers de réserve.
        </p>
      </header>

      <section className="ds-panel dlab-brand-hero" aria-label="Géométrie agrandie">
        <h3 className="ds-panel__title">Inspecteur — 96 px</h3>
        <div className="dlab-brand-hero__grid">
          <BrandCell label="Sans cartouche · dégradé">
            <PlayUpMark size={96} variant="gradient" />
          </BrandCell>
          <BrandCell label="App mark · dégradé">
            <PlayUpMark size={96} variant="gradient" cartouche />
          </BrandCell>
          <BrandCell label="Sans cartouche · encre">
            <PlayUpMark size={96} variant="ink" />
          </BrandCell>
          <BrandCell label="App mark · brand flat">
            <PlayUpMark size={96} variant="brand" cartouche />
          </BrandCell>
        </div>
      </section>

      <section className="ds-panel" aria-label="Matrice tailles">
        <h3 className="ds-panel__title">Tailles — brand flat, sans cartouche</h3>
        <div className="dlab-brand-size-row">
          {SIZES.map((size) => (
            <div key={size} className="dlab-brand-size-cell">
              <PlayUpMark
                size={size}
                variant={size >= 24 ? 'gradient' : 'brand'}
              />
              <span className="dlab-brand-size-cell__label ds-num">{size}px</span>
            </div>
          ))}
        </div>
      </section>

      <section className="ds-panel" aria-label="Optique petite taille">
        <h3 className="ds-panel__title">Optique — 16 / 20 / 24 px (monochrome encre)</h3>
        <p className="dlab-brand-note">
          Réserve épaissie automatiquement (tiers tiny / small / master). Le juge
          de paix reste 16 px favicon / rail.
        </p>
        <div className="dlab-brand-optical">
          {[16, 20, 24].map((size) => (
            <div key={size} className="dlab-brand-optical__pair">
              <BrandCell label={`${size}px · sans cartouche`} compact>
                <PlayUpMark size={size} variant="ink" />
              </BrandCell>
              <BrandCell label={`${size}px · app mark`} compact>
                <PlayUpMark size={size} variant="brand" cartouche />
              </BrandCell>
            </div>
          ))}
        </div>
      </section>

      <section className="ds-panel" aria-label="Variantes couleur">
        <h3 className="ds-panel__title">4 déclinaisons — 48 px</h3>
        <div className="dlab-brand-variant-grid">
          {VARIANTS.map(({ key, label }) => (
            <BrandCell key={key} label={label}>
              <PlayUpMark
                size={48}
                variant={key}
                cartouche={key === 'gradient' || key === 'brand'}
              />
            </BrandCell>
          ))}
        </div>
      </section>

      <section className="ds-panel" aria-label="Fonds clair et sombre">
        <h3 className="ds-panel__title">Fonds — 32 px app mark</h3>
        <div className="dlab-brand-bg-grid">
          {BACKGROUNDS.map(({ key, label, className }) => (
            <div key={key} className={`dlab-brand-bg ${className}`}>
              <span className="dlab-brand-bg__label">{label}</span>
              <PlayUpMark size={32} variant="brand" cartouche />
              <PlayUpMark size={32} variant="white" cartouche />
            </div>
          ))}
        </div>
      </section>

      <section className="ds-panel" aria-label="Cartouche on off">
        <h3 className="ds-panel__title">Cartouche — avec / sans (24 px)</h3>
        <div className="dlab-brand-cartouche-row">
          <BrandCell label="Monogramme seul">
            <PlayUpMark size={24} variant="brand" />
          </BrandCell>
          <BrandCell label="App mark">
            <PlayUpMark size={24} variant="brand" cartouche />
          </BrandCell>
        </div>
      </section>
    </div>
  )
}

function BrandCell({
  label,
  children,
  compact,
}: {
  label: string
  children: ReactNode
  compact?: boolean
}) {
  return (
    <figure className={compact ? 'dlab-brand-cell dlab-brand-cell--compact' : 'dlab-brand-cell'}>
      {children}
      <figcaption className="dlab-brand-cell__caption">{label}</figcaption>
    </figure>
  )
}

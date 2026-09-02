import type { ReactNode } from 'react'
import { PlayUpMark } from '../design-system/PlayUpMark'
import { LabWordmark, TrendIcon } from './LabWordmark'

const SIZES = [16, 20, 24, 32, 48] as const
const ANGLES = [9, 12] as const

/**
 * Arbitrage final 9° vs 12° — dernier test du Design Lab avant gel.
 *
 * Compare le système d'angle aux tailles réelles d'usage (rail, wordmark,
 * tendances, monogramme). Une fois tranché, cette vue reste comme preuve
 * visuelle ; le lab ne cherche plus de nouvelle direction.
 */
export function LabAngleCompare() {
  return (
    <div className="ds-page">
      <header className="dlab-angle-intro">
        <h2 className="ds-page__title">Arbitrage d'angle — 9° vs 12°</h2>
        <p className="dlab-angle-intro__lead">
          Choisir le meilleur <strong>système global</strong>, pas le logo le
          plus joli isolé. Critères : lisibilité à petite taille, cohérence
          rail / wordmark / tendances / monogramme, caractère perceptible sans
          devenir un gimmick.
        </p>
        <p className="dlab-angle-intro__verdict">
          <strong>Arbitrage retenu : 12°.</strong> Plus affirmé et plus lisible
          aux petites tailles ; aligné sur le monogramme implémenté. Variable
          unique <code>--brand-skew: -12deg</code>.
        </p>
      </header>

      <section className="ds-panel dlab-angle-grid" aria-label="Comparaison 9° vs 12°">
        <div className="dlab-angle-grid__head">
          <span />
          {ANGLES.map((angle) => (
            <span key={angle} className="dlab-angle-grid__col-label ds-num">
              {angle}°
            </span>
          ))}
        </div>

        <AngleRow label="Wordmark">
          {ANGLES.map((angle) => (
            <LabWordmark key={angle} skewDeg={angle} />
          ))}
        </AngleRow>

        <AngleRow label="Tendance">
          {ANGLES.map((angle) => (
            <span key={angle} className="dlab-angle-trend-demo">
              <span className="ds-num">3e</span>
              <TrendIcon direction="up" skewDeg={angle} />
              <span className="ds-num">+2</span>
            </span>
          ))}
        </AngleRow>

        {SIZES.map((size) => (
          <AngleRow key={size} label={`Monogramme ${size}px`}>
            {ANGLES.map((angle) => (
              <PlayUpMark
                key={angle}
                size={size}
                variant={size >= 24 ? 'gradient' : 'brand'}
              />
            ))}
          </AngleRow>
        ))}

        <AngleRow label="Barre nav active">
          {ANGLES.map((angle) => (
            <span key={angle} className="dlab-angle-nav-demo" data-skew={angle}>
              <span className="dlab-angle-nav-demo__bar" />
              Vue d'ensemble
            </span>
          ))}
        </AngleRow>
      </section>
    </div>
  )
}

function AngleRow({
  label,
  children,
}: {
  label: string
  children: ReactNode
}) {
  return (
    <div className="dlab-angle-grid__row">
      <span className="dlab-angle-grid__row-label">{label}</span>
      {children}
    </div>
  )
}

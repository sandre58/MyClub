import { WaitMark } from '../design-system/components/WaitMark'
import { PanelHead } from '../design-system/components/PanelHead'

export type LabWaitKind = 'b' | 'c'
export type LabWaitScale = 'page' | 'region' | 'button'

/**
 * Atome d'attente Lab — B spinner + barre (rejeté) · C WaitMark (retenu 2026-09-03).
 */
export function LabWaitAtom({
  kind,
  scale,
  label,
}: {
  kind: LabWaitKind
  scale: LabWaitScale
  label?: string
}) {
  const text = label ?? (scale === 'button' ? 'Traitement…' : 'Chargement…')

  if (kind === 'c' && scale !== 'button') {
    return (
      <WaitMark size={scale === 'region' ? 'region' : 'page'}>{text}</WaitMark>
    )
  }

  const showBar = kind === 'b' && scale !== 'button'
  const glyph = <WaitSpinner />

  if (scale === 'button') {
    return (
      <button
        type="button"
        className={[
          'ds-btn',
          'ds-btn--primary',
          'dlab-wait',
          `dlab-wait--${kind}`,
          'dlab-wait--button',
        ].join(' ')}
        disabled
        aria-busy="true"
      >
        {glyph}
        {text}
      </button>
    )
  }

  return (
    <p
      className={[
        'dlab-wait',
        `dlab-wait--${kind}`,
        `dlab-wait--${scale}`,
      ].join(' ')}
      role="status"
      aria-live="polite"
    >
      <span className="dlab-wait__row">
        {glyph}
        <span className="dlab-wait__label">{text}</span>
      </span>
      {showBar ? <WaitBar /> : null}
    </p>
  )
}

function WaitSpinner() {
  return <span className="dlab-wait-spin" aria-hidden="true" />
}

function WaitBar() {
  return (
    <span className="dlab-wait-bar" aria-hidden="true">
      <span className="dlab-wait-bar__run" />
    </span>
  )
}

/**
 * Planche d'arbitrage — C retenu (2026-09-03). B reste visible comme alternative rejetée.
 */
export function LabWait() {
  return (
    <div className="dlab-wait-board ds-page">
      <header className="dlab-wait-board__intro">
        <h1 className="ds-heading">États d’attente</h1>
        <p className="ds-body">
          Retenu : C — mark Accueil statique, anneau qui tourne, label centré
          sous l’animation. Produit : <code>WaitMark</code> /{' '}
          <code>LoadingState</code>. Boutons : spinner (le PNG n’est pas un
          glyphe de contrôle).
        </p>
      </header>

      <div className="dlab-wait-board__grid">
        <div className="dlab-wait-board__col-head" aria-hidden="true" />
        <div className="dlab-wait-board__col-head">
          <p className="ds-label">B — spinner + barre</p>
          <p className="ds-meta">Anneau currentColor · piste 12° · Chargement…</p>
        </div>
        <div className="dlab-wait-board__col-head">
          <p className="ds-label">C — marque + anneau (retenu)</p>
          <p className="ds-meta">WaitMark · label centré sous l’orbite</p>
        </div>

        <p className="dlab-wait-board__row-label ds-eyebrow">Page</p>
        <div className="dlab-wait-board__cell">
          <LabWaitAtom kind="b" scale="page" />
        </div>
        <div className="dlab-wait-board__cell">
          <LabWaitAtom kind="c" scale="page" />
        </div>

        <p className="dlab-wait-board__row-label ds-eyebrow">Région</p>
        <div className="dlab-wait-board__cell">
          <RegionMock kind="b" />
        </div>
        <div className="dlab-wait-board__cell">
          <RegionMock kind="c" />
        </div>

        <p className="dlab-wait-board__row-label ds-eyebrow">Bouton</p>
        <div className="dlab-wait-board__cell">
          <LabWaitAtom kind="b" scale="button" />
        </div>
        <div className="dlab-wait-board__cell">
          <LabWaitAtom kind="c" scale="button" />
          <p className="ds-meta">
            Même spinner que B — le mark PNG n’est pas un glyphe de bouton.
          </p>
        </div>
      </div>
    </div>
  )
}

function RegionMock({ kind }: { kind: LabWaitKind }) {
  return (
    <div className="dlab-wait-region">
      <section className="ds-panel">
        <PanelHead title="Journée 3" aside="1 / 3 terminé" />
        <p className="dlab-wait-board__ghost ds-tabular">FC Nord 2–1 AS Montval</p>
        <p className="dlab-wait-board__ghost ds-tabular">United — Racing</p>
      </section>
      <section className="ds-panel">
        <PanelHead title="Classement" aside="Provisoire" />
        <LabWaitAtom kind={kind} scale="region" />
      </section>
    </div>
  )
}

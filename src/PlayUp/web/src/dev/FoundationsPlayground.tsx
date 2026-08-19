import { useState, type SVGProps } from 'react'
import '../design-system/fonts'
import '../design-system/index.css'
import './foundations-playground.css'

type Font = 'plex' | 'inter'
type Palette = 'warm-ink' | 'teal' | 'slate'
type Density = 'compact' | 'standard' | 'comfortable'

/**
 * Visual validation terrain for Design System foundations (14.5).
 * Not a component library and not the Shell.
 */
export function FoundationsPlayground() {
  const [font, setFont] = useState<Font>('plex')
  const [palette, setPalette] = useState<Palette>('warm-ink')
  const [grayscale, setGrayscale] = useState(false)
  const [density, setDensity] = useState<Density>('standard')

  return (
    <div
      className="ds-root"
      data-font={font}
      data-palette={palette}
      data-density={density}
    >
      <a className="ds-skip" href="#ds-preview">
        Aller au contenu
      </a>
      <form
        className="ds-toolbar"
        aria-label="Bake-off"
        onSubmit={(event) => event.preventDefault()}
      >
        <fieldset className="ds-toolbar__group">
          <legend>Police</legend>
          <label className="ds-toolbar__option">
            <input
              type="radio"
              name="font"
              checked={font === 'plex'}
              onChange={() => setFont('plex')}
            />
            IBM Plex Sans
          </label>
          <label className="ds-toolbar__option">
            <input
              type="radio"
              name="font"
              checked={font === 'inter'}
              onChange={() => setFont('inter')}
            />
            Inter
          </label>
        </fieldset>
        <fieldset className="ds-toolbar__group">
          <legend>Palette</legend>
          <label className="ds-toolbar__option">
            <input
              type="radio"
              name="palette"
              checked={palette === 'warm-ink'}
              onChange={() => setPalette('warm-ink')}
            />
            warm-ink
          </label>
          <label className="ds-toolbar__option">
            <input
              type="radio"
              name="palette"
              checked={palette === 'teal'}
              onChange={() => setPalette('teal')}
            />
            teal
          </label>
          <label className="ds-toolbar__option">
            <input
              type="radio"
              name="palette"
              checked={palette === 'slate'}
              onChange={() => setPalette('slate')}
            />
            slate
          </label>
        </fieldset>
        <fieldset className="ds-toolbar__group">
          <legend>Niveaux de gris</legend>
          <label className="ds-toolbar__option">
            <input
              type="checkbox"
              checked={grayscale}
              onChange={() => setGrayscale((value) => !value)}
            />
            Activer
          </label>
        </fieldset>
        <fieldset className="ds-toolbar__group">
          <legend>Densité</legend>
          <label className="ds-toolbar__option">
            <input
              type="radio"
              name="density"
              checked={density === 'compact'}
              onChange={() => setDensity('compact')}
            />
            compact
          </label>
          <label className="ds-toolbar__option">
            <input
              type="radio"
              name="density"
              checked={density === 'standard'}
              onChange={() => setDensity('standard')}
            />
            standard
          </label>
          <label className="ds-toolbar__option">
            <input
              type="radio"
              name="density"
              checked={density === 'comfortable'}
              onChange={() => setDensity('comfortable')}
            />
            comfortable
          </label>
        </fieldset>
      </form>

      <main
        id="ds-preview"
        className="ds-preview"
        data-grayscale={grayscale ? 'on' : 'off'}
      >
        <header className="ds-stack">
          <p className="ds-wordmark">Play’up</p>
          <p className="ds-meta">
            Terrain de validation 14.5 — foundations, pas le Shell.
          </p>
          <nav className="ds-nav-concept" aria-label="Navigation conceptuelle">
            <span className="ds-nav-concept__item ds-nav-concept__item--active" aria-current="page">
              Cockpit
            </span>
            <span className="ds-nav-concept__item">Calendrier</span>
            <span className="ds-nav-concept__item">Équipes</span>
          </nav>
        </header>

        <section className="ds-section" aria-labelledby="section-type">
          <p className="ds-section__kicker">A — Typography</p>
          <h1 id="section-type" className="ds-heading">
            Coupe du District — Seniors A
          </h1>
          <p className="ds-body">Racing Club de Strasbourg Alsace</p>
          <p className="ds-label">Prochain match</p>
          <p className="ds-meta">Stade de la Meinau · samedi</p>
        </section>

        <section className="ds-section" aria-labelledby="section-numbers">
          <p className="ds-section__kicker">B — Numbers</p>
          <h2 id="section-numbers" className="ds-heading">
            Chiffres
          </h2>
          <div className="ds-score-line">
            <p className="ds-body ds-score-line__team">
              Racing Club de Strasbourg Alsace
            </p>
            <p className="ds-display ds-tabular ds-score-line__score">2–1</p>
            <p className="ds-body ds-score-line__team ds-score-line__team--away">
              Étoile Filante de Metz
            </p>
          </div>
          <p>
            <span className="ds-heading ds-tabular">14:30</span>
            <span className="ds-meta"> · horaire, pas un scoreboard</span>
          </p>
          <div className="ds-state ds-state--attention" aria-label="3 à traiter">
            <span className="ds-state__figure">3</span>
            <AttentionIcon className="ds-state__icon" />
            <span className="ds-state__label">À traiter</span>
          </div>
          <div className="ds-group">
            <p className="ds-label">Classement (une ligne)</p>
            <div className="ds-standings" role="row">
              <span className="ds-body ds-tabular ds-standings__num">1</span>
              <span className="ds-body">Racing Club de Strasbourg Alsace</span>
              <span className="ds-meta ds-tabular ds-standings__num">12</span>
              <span className="ds-meta ds-tabular ds-standings__num">8</span>
              <span className="ds-meta ds-tabular ds-standings__num">2</span>
              <span className="ds-meta ds-tabular ds-standings__num">2</span>
              <span className="ds-body ds-tabular ds-standings__pts">26</span>
            </div>
          </div>
        </section>

        <section className="ds-section" aria-labelledby="section-surfaces">
          <p className="ds-section__kicker">C — Surfaces</p>
          <h2 id="section-surfaces" className="ds-heading">
            Canvas, groupe, panneau, overlay
          </h2>
          <div className="ds-surface-demo ds-canvas">
            <div className="ds-group">
              <p className="ds-label">Groupe</p>
              <p className="ds-body">Prochain match · 14:30</p>
              <p className="ds-meta">Lié par le titre et la proximité — pas une boîte.</p>
            </div>
            <div className="ds-group">
              <p className="ds-label">Autre groupe</p>
              <p className="ds-body">Phase de poules · 8 équipes</p>
            </div>
            <div className="ds-panel">
              <p className="ds-label">Panneau</p>
              <p className="ds-body">
                Zone vraiment délimitée. Pas chaque KPI, pas chaque match.
              </p>
            </div>
            <div className="ds-overlay-host ds-canvas">
              <p className="ds-meta">Canvas derrière l’overlay</p>
              <div className="ds-overlay" role="dialog" aria-label="Exemple d’overlay">
                <p className="ds-label">Overlay</p>
                <p className="ds-body">Ombre courte, pas de glow.</p>
              </div>
            </div>
          </div>
        </section>

        <section className="ds-section" aria-labelledby="section-d9">
          <p className="ds-section__kicker">D — Semantic states / D9</p>
          <h2 id="section-d9" className="ds-heading">
            Attention n’est pas une erreur
          </h2>
          <div className="ds-stack">
            <div className="ds-state ds-state--neutral" aria-label="8 équipes">
              <span className="ds-state__figure">8</span>
              <span className="ds-state__label">Équipes</span>
            </div>
            <div className="ds-state ds-state--info ds-state--block" aria-label="4 prêts">
              <span className="ds-state__figure">4</span>
              <InfoIcon className="ds-state__icon" />
              <span className="ds-state__label">Prêts</span>
            </div>
            <div
              className="ds-state ds-state--success ds-state--block"
              aria-label="12 complets"
            >
              <span className="ds-state__figure">12</span>
              <SuccessIcon className="ds-state__icon" />
              <span className="ds-state__label">Complets</span>
            </div>
            <div
              className="ds-state ds-state--attention ds-state--block"
              aria-label="3 à traiter"
            >
              <span className="ds-state__figure">3</span>
              <AttentionIcon className="ds-state__icon" />
              <span className="ds-state__label">À traiter</span>
            </div>
            <div className="ds-state ds-state--error ds-state--block" aria-label="1 bloqué">
              <span className="ds-state__figure">1</span>
              <ErrorIcon className="ds-state__icon" />
              <span className="ds-state__label">Bloqué</span>
            </div>
          </div>
        </section>

        <section className="ds-section" aria-labelledby="section-actions">
          <p className="ds-section__kicker">E — Actions</p>
          <h2 id="section-actions" className="ds-heading">
            Primaire = encre
          </h2>
          <div className="ds-group">
            <p className="ds-label">Une région · une primaire</p>
            <div className="ds-row">
              <button type="button" className="ds-btn ds-btn--primary">
                Saisir le résultat
              </button>
              <button type="button" className="ds-btn ds-btn--secondary">
                Plus tard
              </button>
              <button type="button" className="ds-btn ds-btn--ghost">
                Retour
              </button>
              <button
                type="button"
                className="ds-btn ds-icon-button"
                aria-label="Fermer"
              >
                <CloseIcon />
              </button>
            </div>
          </div>
          <div className="ds-group">
            <p className="ds-label">Destructive · pas un primaire rouge</p>
            <div className="ds-row">
              <button type="button" className="ds-btn ds-btn--destructive">
                Retirer l’équipe
              </button>
              <button type="button" className="ds-btn ds-btn--ghost">
                Annuler
              </button>
            </div>
          </div>
        </section>

        <section className="ds-section" aria-labelledby="section-density">
          <p className="ds-section__kicker">F — Density / accessibility</p>
          <h2 id="section-density" className="ds-heading">
            Densité et focus
          </h2>
          <p className="ds-body">
            La densité courante change la hauteur des contrôles de cette page (
            {density}). Les trois hauteurs restent visibles ci-dessous pour
            comparaison.
          </p>
          <div className="ds-stack">
            <div className="ds-density-rail ds-density-rail--compact">compact 32</div>
            <div className="ds-density-rail ds-density-rail--standard">standard 36</div>
            <div className="ds-density-rail ds-density-rail--comfortable">
              comfortable 40
            </div>
          </div>
          <div className="ds-row">
            <label className="ds-label" htmlFor="ds-sample-input">
              Contrôle
            </label>
            <input id="ds-sample-input" type="text" defaultValue="14:30" />
          </div>
          <p className="ds-meta">
            Tab jusqu’à un bouton ou un champ : anneau 2px + offset 2px. Couleur
            jamais seule (D9). prefers-reduced-motion est respecté.
          </p>
        </section>

        <section className="ds-section" aria-labelledby="section-tests">
          <p className="ds-section__kicker">Tests d’identité</p>
          <h2 id="section-tests" className="ds-heading">
            À juger dans le navigateur
          </h2>
          <ul className="ds-checks">
            <li>1 Grayscale — Play’up reste identifiable sans couleur de marque.</li>
            <li>2 No card — les groupes se tiennent sans boîte autour de chaque bloc.</li>
            <li>3 No grid — hors classement, pas de quadrillage par défaut.</li>
            <li>4 Numbers — le score et l’horaire sautent sans tout transformer en scoreboard.</li>
            <li>5 D9 — « À traiter » se lit sans dépendre de la couleur seule.</li>
            <li>6 Brand swap — changer la palette ne demande pas de modifier les exemples.</li>
          </ul>
        </section>
      </main>
    </div>
  )
}

function AttentionIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 20 20" aria-hidden="true" {...props}>
      <path
        d="M10 3.5 17.5 16.5H2.5L10 3.5Z"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.75"
        strokeLinejoin="round"
      />
      <path d="M10 8.5v4" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" />
      <circle cx="10" cy="14.25" r="0.8" fill="currentColor" />
    </svg>
  )
}

function SuccessIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 20 20" aria-hidden="true" {...props}>
      <path
        d="M4.5 10.5 8 14l7.5-8"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.75"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  )
}

function InfoIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 20 20" aria-hidden="true" {...props}>
      <circle cx="10" cy="10" r="7" fill="none" stroke="currentColor" strokeWidth="1.75" />
      <path d="M10 9v4.5" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" />
      <circle cx="10" cy="6.75" r="0.8" fill="currentColor" />
    </svg>
  )
}

function ErrorIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 20 20" aria-hidden="true" {...props}>
      <circle cx="10" cy="10" r="7" fill="none" stroke="currentColor" strokeWidth="1.75" />
      <path d="m7.5 7.5 5 5M12.5 7.5l-5 5" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" />
    </svg>
  )
}

function CloseIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 20 20" aria-hidden="true" {...props}>
      <path d="m5 5 10 10M15 5 5 15" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" />
    </svg>
  )
}

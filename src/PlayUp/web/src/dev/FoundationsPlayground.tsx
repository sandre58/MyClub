import { useState } from 'react'
import {
  Status,
  type StatusShape,
  type StatusTone,
  type StatusVariant,
} from '../design-system/components/Status'
import { PlayUpLockupMark } from '../design-system/PlayUpLockupMark'
import { PlayUpWordmark } from '../design-system/PlayUpWordmark'
import { TrendIcon } from '../design-system/TrendIcon'
import { LiveStatus } from '../design-system/components/LiveStatus'
import { TeamCrest } from '../design-system/TeamCrest'
import '../design-system/fonts'
import '../design-system/index.css'
import {
  AttentionBellIcon,
  AttentionIcon,
  ChevronRightIcon,
  ClassementsNavIcon,
  CloseIcon,
  MatchesNavIcon,
  OrganisationNavIcon,
  OverviewNavIcon,
  SettingsNavIcon,
  SidebarCollapseIcon,
  SwapIcon,
} from '../design-system/icons/shellIcons'
import {
  ArrowRightIcon,
  CheckIcon,
  CreateMatchesIcon,
  NextActionIcon,
  PendingCircleIcon,
  PlusIcon,
  PreparationIcon,
  RegulationIcon,
  StructureIcon,
} from '../design-system/icons/overviewIcons'
import './foundations-playground.css'

type Density = 'compact' | 'standard' | 'comfortable'

const semanticRoles: Array<{
  token: string
  role: string
  usage: string
  antiUsage: string
}> = [
  {
    token: '--color-canvas',
    role: 'Fond de page',
    usage: 'Surface de page / Shell content',
    antiUsage: 'Pas pour panneau ou chrome',
  },
  {
    token: '--color-surface',
    role: 'Surface élevée',
    usage: 'Contrôles, chips light, edge toggle',
    antiUsage: 'Pas le fond de page par défaut',
  },
  {
    token: '--color-surface-secondary',
    role: 'Surface secondaire',
    usage: 'Hover léger, zones regroupées',
    antiUsage: 'Pas une « card » universelle',
  },
  {
    token: '--color-ink',
    role: 'Encre forte',
    usage: 'Titres, liens de contenu, contraste max',
    antiUsage: 'Pas sur le rail chrome',
  },
  {
    token: '--color-text-primary',
    role: 'Texte principal',
    usage: 'Corps de texte contenu',
    antiUsage: 'Pas labels secondaires',
  },
  {
    token: '--color-text-secondary',
    role: 'Texte secondaire',
    usage: 'Meta, hints, labels discrets',
    antiUsage: 'Pas titres principaux',
  },
  {
    token: '--color-border',
    role: 'Bordure douce',
    usage: 'Séparations légères',
    antiUsage: 'Pas focus ring',
  },
  {
    token: '--color-border-strong',
    role: 'Bordure forte',
    usage: 'Contrôles, contours affirmés',
    antiUsage: 'Pas décoration seule',
  },
  {
    token: '--color-brand',
    role: 'Marque / primaire',
    usage: 'Fill bouton primary, une CTA / région',
    antiUsage: 'Pas identité seule (hiérarchie > couleur)',
  },
  {
    token: '--color-brand-hover',
    role: 'Marque hover',
    usage: 'Hover primary uniquement',
    antiUsage: 'Pas état sémantique D9',
  },
  {
    token: '--color-brand-deep',
    role: 'Brand profond',
    usage: 'Extrémité dégradé logo (#154a8f)',
    antiUsage: 'Pas fill CTA ni surface',
  },
  {
    token: '--color-brand-bright',
    role: 'Brand clair',
    usage: 'Extrémité dégradé logo · accent chrome',
    antiUsage: 'Pas texte body',
  },
  {
    token: '--color-live',
    role: 'En cours (live)',
    usage: 'Match en cours · pulse · ds-status-live',
    antiUsage: '≠ success · ≠ attention',
  },
  {
    token: '--color-success',
    role: 'Succès',
    usage: 'D9 / Status success',
    antiUsage: 'Pas CTA primaire',
  },
  {
    token: '--color-attention',
    role: 'Attention',
    usage: 'À traiter > 0 ; D9 attention',
    antiUsage: '≠ error',
  },
  {
    token: '--color-error',
    role: 'Erreur',
    usage: 'Cassé / invalide / bloqué',
    antiUsage: 'Pas « à traiter »',
  },
  {
    token: '--color-info',
    role: 'Info',
    usage: 'État neutre informatif',
    antiUsage: 'Pas marque',
  },
  {
    token: '--color-focus',
    role: 'Focus clavier',
    usage: 'Anneau focus-visible',
    antiUsage: 'Pas fill de bouton',
  },
  {
    token: '--color-on-ink',
    role: 'Sur encre',
    usage: 'Texte sur fond ink (skip link…)',
    antiUsage: 'Pas texte sur canvas',
  },
  {
    token: '--color-on-brand',
    role: 'Sur brand',
    usage: 'Texte sur fill primary',
    antiUsage: 'Pas texte sur canvas',
  },
  {
    token: '--color-chrome',
    role: 'Fond chrome',
    usage: 'Rail Shell sombre uniquement',
    antiUsage: 'Pas contenu métier',
  },
  {
    token: '--color-on-chrome',
    role: 'Sur chrome',
    usage: 'Texte / icônes du rail',
    antiUsage: 'Pas texte de page',
  },
  {
    token: '--color-chrome-border',
    role: 'Bordure chrome',
    usage: 'Séparateurs du rail',
    antiUsage: 'Pas bordures de contenu',
  },
  {
    token: '--color-chrome-accent',
    role: 'Accent chrome',
    usage: 'Nav active (barre + icône)',
    antiUsage: 'Pas CTA contenu ; ≠ brand page',
  },
]

const statusTones: StatusTone[] = [
  'neutral',
  'info',
  'success',
  'live',
  'done',
  'attention',
  'error',
]

/**
 * Visual validation terrain for Design System foundations.
 * Not a component library and not the product Shell.
 * Reveals the final language — does not invent tokens.
 */
export function FoundationsPlayground() {
  const [grayscale, setGrayscale] = useState(false)
  const [density, setDensity] = useState<Density>('standard')

  return (
    <div
      className="ds-root"
      data-font="plex"
      data-palette="slate"
      data-density={density}
    >
      <a className="ds-skip" href="#ds-preview">
        Aller au contenu
      </a>
      <form
        className="ds-toolbar"
        aria-label="Contrôles du terrain"
        onSubmit={(event) => event.preventDefault()}
      >
        <p className="ds-toolbar__meta ds-meta">Palette : slate (seule)</p>
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
          {(['compact', 'standard', 'comfortable'] as const).map((value) => (
            <label key={value} className="ds-toolbar__option">
              <input
                type="radio"
                name="density"
                checked={density === value}
                onChange={() => setDensity(value)}
              />
              {value}
            </label>
          ))}
        </fieldset>
      </form>

      <main
        id="ds-preview"
        className="ds-preview"
        data-grayscale={grayscale ? 'on' : 'off'}
      >
        <header className="ds-stack">
          <p className="ds-wordmark">Play’Up</p>
          <p className="ds-meta">
            Terrain de validation — foundations + primitives React. Les pages
            legacy restent hors scope.
          </p>
        </header>

        <section className="ds-section" aria-labelledby="section-brand-lockup">
          <p className="ds-section__kicker">Marque — Accueil lockup</p>
          <h2 id="section-brand-lockup" className="ds-heading">
            Play’Up · lockup Accueil
          </h2>
          <p className="ds-body">
            Planche 2026-09-02 : signe P+flèche+U, wordmark italique, dégradé
            Slate sur Up, flèche sur la hampe droite du U. Le rail chrome
            réutilise le même mark ; le wordmark a une colorway on-chrome.
          </p>
          <div className="ds-row">
            <PlayUpLockupMark />
            <PlayUpWordmark surface="home" />
          </div>
        </section>

        <section className="ds-section" aria-labelledby="section-brand-chrome">
          <p className="ds-section__kicker">Marque — lockup chrome</p>
          <h2 id="section-brand-chrome" className="ds-heading">
            Play’Up · rail navy
          </h2>
          <p className="ds-body">
            Même mark Accueil (flèche = transparence, le navy transparaît) et
            wordmark on-chrome. Pas de monogramme SVG.
          </p>
          <div className="ds-brand-chrome ds-row">
            <PlayUpLockupMark />
            <PlayUpWordmark surface="chrome" />
          </div>
          <p className="ds-meta">
            Les réserves sont de vraies transparences : la marque doit tenir sur
            n’importe quel fond, pas seulement sur `canvas`.
          </p>
        </section>

        <section className="ds-section" aria-labelledby="section-roles">
          <p className="ds-section__kicker">0 — Rôles sémantiques</p>
          <h1 id="section-roles" className="ds-heading">
            API publique `--color-*`
          </h1>
          <p className="ds-body">
            Les primitives (`--primitive-*`) ne sont pas l’API. Consommer
            uniquement les rôles ci-dessous.
          </p>
          <div className="ds-token-table-wrap">
            <table className="ds-token-table">
              <thead>
                <tr>
                  <th scope="col">Swatch</th>
                  <th scope="col">Token</th>
                  <th scope="col">Rôle</th>
                  <th scope="col">Cas d’usage</th>
                  <th scope="col">Anti-usage</th>
                </tr>
              </thead>
              <tbody>
                {semanticRoles.map((row) => (
                  <tr key={row.token}>
                    <td>
                      <span
                        className="ds-swatch"
                        style={{ background: `var(${row.token})` }}
                        title={row.token}
                      />
                    </td>
                    <td>
                      <code className="ds-code">{row.token}</code>
                    </td>
                    <td className="ds-body">{row.role}</td>
                    <td className="ds-meta">{row.usage}</td>
                    <td className="ds-meta">{row.antiUsage}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>

        <section className="ds-section" aria-labelledby="section-chrome">
          <p className="ds-section__kicker">1 — Shell rail</p>
          <h2 id="section-chrome" className="ds-heading">
            Rail chrome (ds-shell-rail)
          </h2>
          <p className="ds-body">
            Grammar canonique Shell A — lockup Accueil (mark + wordmark
            on-chrome) sur le rail navy. Barre active droite 3 px.
          </p>
          <aside
            className="ds-shell-rail ds-playground-rail"
            aria-label="Exemple de rail shell"
          >
            <a className="ds-shell-rail__brand" href="#ds-preview">
              <PlayUpLockupMark />
              <PlayUpWordmark surface="chrome" />
            </a>
            <nav className="ds-shell-rail__nav" aria-label="Exemple nav shell">
              <a className="ds-shell-rail__link" href="#ds-preview" data-active="true">
                <OverviewNavIcon className="ds-shell-rail__icon" />
                Vue d&apos;ensemble
              </a>
              <a className="ds-shell-rail__link" href="#ds-preview">
                <OrganisationNavIcon className="ds-shell-rail__icon" />
                Organisation
              </a>
              <a className="ds-shell-rail__link" href="#ds-preview">
                <MatchesNavIcon className="ds-shell-rail__icon" />
                Matchs
              </a>
            </nav>
          </aside>
        </section>

        <section className="ds-section" aria-labelledby="section-type">
          <p className="ds-section__kicker">A — Typography</p>
          <h2 id="section-type" className="ds-heading">
            Coupe du District — Seniors A
          </h2>
          <p className="ds-display">Display</p>
          <p className="ds-body">Racing Club de Strasbourg Alsace</p>
          <p className="ds-label">Prochain match</p>
          <p className="ds-meta">Stade de la Meinau · samedi</p>
          <p className="ds-body ds-tabular">14:30 · tabular</p>
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
          <div className="ds-state ds-state--attention" aria-label="3 à traiter">
            <span className="ds-state__figure">3</span>
            <AttentionIcon className="ds-state__icon" size="sm" />
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
              <PendingCircleIcon className="ds-state__icon" size="sm" />
              <span className="ds-state__label">Prêts</span>
            </div>
            <div
              className="ds-state ds-state--success ds-state--block"
              aria-label="12 complets"
            >
              <span className="ds-state__figure">12</span>
              <CheckIcon className="ds-state__icon" size="sm" />
              <span className="ds-state__label">Complets</span>
            </div>
            <div
              className="ds-state ds-state--attention ds-state--block"
              aria-label="3 à traiter"
            >
              <span className="ds-state__figure">3</span>
              <AttentionIcon className="ds-state__icon" size="sm" />
              <span className="ds-state__label">À traiter</span>
            </div>
            <div className="ds-state ds-state--error ds-state--block" aria-label="1 bloqué">
              <span className="ds-state__figure">1</span>
              <AttentionIcon className="ds-state__icon" size="sm" />
              <span className="ds-state__label">Bloqué</span>
            </div>
          </div>
        </section>

        <section className="ds-section" aria-labelledby="section-status">
          <p className="ds-section__kicker">D2 — Status</p>
          <h2 id="section-status" className="ds-heading">
            Primitive Status
          </h2>
          <p className="ds-label">Context · soft · rounded</p>
          <div className="ds-row">
            {statusTones.map((tone) => (
              <Status key={tone} density="context" tone={tone} variant="soft">
                {tone}
              </Status>
            ))}
          </div>
          <p className="ds-label">Context · soft · chrome</p>
          <div className="ds-row ds-icon-grid--chrome">
            {statusTones.map((tone) => (
              <Status
                key={`chrome-${tone}`}
                density="context"
                tone={tone}
                variant="soft"
              >
                {tone}
              </Status>
            ))}
          </div>
          <p className="ds-label">Compact · soft (header)</p>
          <div className="ds-row">
            {statusTones.map((tone) => (
              <Status key={`compact-${tone}`} density="compact" tone={tone} variant="soft">
                {tone}
              </Status>
            ))}
          </div>
          <div className="ds-row ds-icon-grid--chrome">
            {statusTones.map((tone) => (
              <Status
                key={`compact-chrome-${tone}`}
                density="compact"
                tone={tone}
                variant="soft"
              >
                {tone}
              </Status>
            ))}
          </div>
          <p className="ds-label">Context · outline · pill</p>
          <div className="ds-row">
            {statusTones.map((tone) => (
              <Status
                key={`outline-${tone}`}
                density="context"
                tone={tone}
                variant={'outline' satisfies StatusVariant}
                shape={'pill' satisfies StatusShape}
              >
                {tone}
              </Status>
            ))}
          </div>
          <p className="ds-label">Dense (texte secondaire, sans fill)</p>
          <div className="ds-row">
            {statusTones.map((tone) => (
              <Status key={`dense-${tone}`} density="dense" tone={tone}>
                {tone}
              </Status>
            ))}
          </div>
        </section>

        <section className="ds-section" aria-labelledby="section-actions">
          <p className="ds-section__kicker">E — Actions</p>
          <h2 id="section-actions" className="ds-heading">
            Primaire = brand
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

        <section className="ds-section" aria-labelledby="section-interactive-row">
          <p className="ds-section__kicker">E2 — Ligne interactive</p>
          <h2 id="section-interactive-row" className="ds-heading">
            Hover A — wash surface-secondary
          </h2>
          <p className="ds-body">
            Contrat canonique : wash full-bleed au hover / pressed ; chevron →
            encre ; focus-visible = anneau existant ; pas de barre brand, ombre,
            lift ni soulignement. Tab pour le focus.
          </p>
          <div className="ds-panel ds-interactive-row-demo">
            <a className="ds-interactive-row" href="#row-rest">
              <span className="ds-interactive-row-demo__label">
                Repos / hover / pressed
              </span>
              <span className="ds-interactive-row__chevron" aria-hidden="true">
                <ChevronRightIcon size="sm" />
              </span>
            </a>
            <a className="ds-interactive-row" href="#row-second">
              <span className="ds-interactive-row-demo__label">
                Deuxième ligne — Tab pour le focus
              </span>
              <span className="ds-interactive-row__chevron" aria-hidden="true">
                <ChevronRightIcon size="sm" />
              </span>
            </a>
            <button type="button" className="ds-interactive-row" disabled>
              <span className="ds-interactive-row-demo__label">
                Désactivée — hors Accueil V1
              </span>
              <span className="ds-interactive-row__chevron" aria-hidden="true">
                <ChevronRightIcon size="sm" />
              </span>
            </button>
          </div>
        </section>

        <section className="ds-section" aria-labelledby="section-lot1-brand">
          <p className="ds-section__kicker">Lot 1 — Brand</p>
          <h2 id="section-lot1-brand" className="ds-heading">
            Lockup PNG · tendances 12°
          </h2>
          <div className="ds-row ds-row--align-end">
            <PlayUpLockupMark />
            <PlayUpWordmark surface="home" />
            <PlayUpWordmark surface="chrome" />
          </div>
          <p className="ds-label">Tendances (classement)</p>
          <div className="ds-row">
            <span className="ds-num">
              3e <TrendIcon direction="up" /> +2
            </span>
            <span className="ds-num">
              5e <TrendIcon direction="down" /> −1
            </span>
            <span className="ds-num">
              2e <TrendIcon direction="flat" /> =
            </span>
          </div>
        </section>

        <section className="ds-section" aria-labelledby="section-numbers">
          <p className="ds-section__kicker">Lot 1 — Numbers</p>
          <h2 id="section-numbers" className="ds-heading">
            Rôles numériques
          </h2>
          <div className="ds-stack">
            <p className="ds-num ds-num-hero">J3 / 5</p>
            <p className="ds-num ds-num-score">2 – 1</p>
            <p className="ds-num ds-num-row-score">1 – 0</p>
            <p className="ds-num ds-num-pts">9 pts</p>
            <p className="ds-num ds-num-counter ds-state--attention">3</p>
          </div>
        </section>

        <section className="ds-section" aria-labelledby="section-live">
          <p className="ds-section__kicker">Lot 1 — Live</p>
          <h2 id="section-live" className="ds-heading">
            Live ≠ success
          </h2>
          <div className="ds-row">
            <LiveStatus>67&apos; · En cours</LiveStatus>
            <Status density="context" tone="live" variant="soft">
              live pill
            </Status>
            <Status density="context" tone="success" variant="soft">
              success
            </Status>
          </div>
        </section>

        <section className="ds-section" aria-labelledby="section-crests">
          <p className="ds-section__kicker">G — TeamCrest</p>
          <h2 id="section-crests" className="ds-heading">
            Écussons
          </h2>
          <div className="ds-row">
            {(['RCSA', 'EFM', 'FCSM', 'ASNL', 'OM'] as const).map((name) => (
              <TeamCrest key={`lg-${name}`} name={name} size="lg" />
            ))}
          </div>
          <div className="ds-row">
            {(['RCSA', 'EFM', 'FCSM', 'ASNL', 'OM'] as const).map((name) => (
              <TeamCrest key={name} name={name} size="md" />
            ))}
          </div>
          <div className="ds-row">
            {(['RCSA', 'EFM', 'FCSM'] as const).map((name) => (
              <TeamCrest key={`sm-${name}`} name={name} size="sm" />
            ))}
          </div>
        </section>

        <section className="ds-section" aria-labelledby="section-icons">
          <p className="ds-section__kicker">H — Icônes</p>
          <h2 id="section-icons" className="ds-heading">
            Shell et contenu
          </h2>
          <p className="ds-label">Shell (fond clair)</p>
          <div className="ds-icon-grid">
            <OverviewNavIcon />
            <OrganisationNavIcon />
            <MatchesNavIcon />
            <ClassementsNavIcon />
            <SettingsNavIcon />
            <SidebarCollapseIcon />
            <AttentionBellIcon />
            <AttentionIcon />
            <SwapIcon />
            <CloseIcon />
          </div>
          <p className="ds-label">Overview</p>
          <div className="ds-icon-grid">
            <NextActionIcon />
            <CreateMatchesIcon />
            <PlusIcon />
            <PreparationIcon />
            <RegulationIcon />
            <StructureIcon />
            <CheckIcon />
            <PendingCircleIcon />
            <ArrowRightIcon />
          </div>
          <p className="ds-label">Sur chrome</p>
          <div className="ds-icon-grid ds-icon-grid--chrome">
            <OverviewNavIcon />
            <OrganisationNavIcon />
            <MatchesNavIcon />
            <AttentionBellIcon />
          </div>
        </section>

        <section className="ds-section" aria-labelledby="section-density">
          <p className="ds-section__kicker">F — Density / controls</p>
          <h2 id="section-density" className="ds-heading">
            Densité et contrôles natifs
          </h2>
          <p className="ds-body">
            Densité courante : {density}. Les trois hauteurs restent visibles
            pour comparaison.
          </p>
          <div className="ds-stack">
            <div className="ds-density-rail ds-density-rail--compact">compact 32</div>
            <div className="ds-density-rail ds-density-rail--standard">standard 36</div>
            <div className="ds-density-rail ds-density-rail--comfortable">
              comfortable 40
            </div>
          </div>
          <div className="ds-controls-demo">
            <label className="ds-label" htmlFor="ds-sample-input">
              Texte
            </label>
            <input id="ds-sample-input" type="text" defaultValue="14:30" />
            <label className="ds-label" htmlFor="ds-sample-select">
              Liste
            </label>
            <select id="ds-sample-select" defaultValue="poules">
              <option value="poules">Phase de poules</option>
              <option value="elim">Élimination directe</option>
            </select>
            <label className="ds-label" htmlFor="ds-sample-textarea">
              Notes
            </label>
            <textarea id="ds-sample-textarea" rows={3} defaultValue="Terrain de validation." />
          </div>
          <p className="ds-meta">
            Tab jusqu’à un bouton ou un champ : anneau 2px + offset 2px.
            prefers-reduced-motion est respecté.
          </p>
        </section>

        <section className="ds-section" aria-labelledby="section-tests">
          <p className="ds-section__kicker">Tests d’identité</p>
          <h2 id="section-tests" className="ds-heading">
            À juger dans le navigateur
          </h2>
          <ul className="ds-checks">
            <li>1 Grayscale — Play’Up reste identifiable sans couleur de marque.</li>
            <li>2 No card — les groupes se tiennent sans boîte autour de chaque bloc.</li>
            <li>3 No grid — hors classement, pas de quadrillage par défaut.</li>
            <li>4 Numbers — le score et l’horaire sautent sans tout transformer en scoreboard.</li>
            <li>5 D9 — « À traiter » se lit sans dépendre de la couleur seule.</li>
            <li>6 Chrome — le rail sombre reste lisible (on-chrome, pas ink page).</li>
            <li>7 Status / Crest / Icons — primitives React branchées sur le DS.</li>
          </ul>
        </section>
      </main>
    </div>
  )
}

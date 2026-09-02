import { useState } from 'react'
import { Link } from 'react-router-dom'
import '../design-system/fonts'
import '../design-system/index.css'
import './design-lab.css'
import {
  AttentionBellIcon,
  ClassementsNavIcon,
  MatchesNavIcon,
  OrganisationNavIcon,
  OverviewNavIcon,
  SettingsNavIcon,
} from '../design-system/icons/shellIcons'
import { PlayUpMark } from '../design-system/PlayUpMark'
import { LabAngleCompare } from './LabAngleCompare'
import { LabBrand } from './LabBrand'
import { LabCockpit } from './LabCockpit'
import { LabHome } from './LabHome'
import { LabMatches } from './LabMatches'
import { LabMatchSheet } from './LabMatchSheet'
import { LabStandings } from './LabStandings'
import { LabWordmark } from './LabWordmark'
import type { LabLifecycle } from './labData'

type LabView =
  | 'home'
  | 'home-empty'
  | 'cockpit'
  | 'matches'
  | 'match'
  | 'standings'
  | 'angle'
  | 'brand'

/**
 * /design-lab — prototype de la direction « Grille de compétition, exécutée ».
 *
 * Référence visuelle, hors produit : données fictives, aucune API,
 * aucune surface existante modifiée. La barre du haut pilote le prototype
 * (surface + état du cycle) ; tout le reste est le produit proposé.
 */
export function DesignLabPage() {
  const [view, setView] = useState<LabView>('cockpit')
  const [lifecycle, setLifecycle] = useState<LabLifecycle>('live')

  const isHome = view === 'home' || view === 'home-empty'

  return (
    <div
      className="ds-root dlab"
      data-font="plex"
      data-palette="slate"
      data-density="standard"
    >
      <LabBar
        view={view}
        lifecycle={lifecycle}
        onView={setView}
        onLifecycle={setLifecycle}
      />

      {isHome ? (
        <LabHome empty={view === 'home-empty'} />
      ) : (
        <div className="dlab-shell">
          <LabRail view={view} onView={setView} />
          <div className="dlab-column">
            <LabHeader lifecycle={lifecycle} />
            <main className="dlab-main">
              {view === 'cockpit' && <LabCockpit lifecycle={lifecycle} />}
              {view === 'matches' && <LabMatches />}
              {view === 'match' && <LabMatchSheet lifecycle={lifecycle} />}
              {view === 'standings' && <LabStandings />}
              {view === 'angle' && <LabAngleCompare />}
              {view === 'brand' && <LabBrand />}
            </main>
          </div>
        </div>
      )}
    </div>
  )
}

/* ------------------------------------------------------------------ */
/* Barre lab (hors produit)                                            */
/* ------------------------------------------------------------------ */

const viewOptions: Array<{ key: LabView; label: string }> = [
  { key: 'home', label: 'Accueil' },
  { key: 'home-empty', label: 'Accueil vide' },
  { key: 'cockpit', label: "Vue d'ensemble" },
  { key: 'matches', label: 'Matchs' },
  { key: 'match', label: 'Fiche match' },
  { key: 'standings', label: 'Classements' },
  { key: 'angle', label: 'Arbitrage 9°/12°' },
  { key: 'brand', label: 'Marque' },
]

const lifecycleOptions: Array<{ key: LabLifecycle; label: string }> = [
  { key: 'preparation', label: 'Préparation' },
  { key: 'live', label: 'En cours' },
  { key: 'done', label: 'Terminée' },
]

function LabBar({
  view,
  lifecycle,
  onView,
  onLifecycle,
}: {
  view: LabView
  lifecycle: LabLifecycle
  onView: (v: LabView) => void
  onLifecycle: (l: LabLifecycle) => void
}) {
  return (
    <div className="dlab-bar">
      <span className="dlab-bar__title">Design Lab</span>
      <span className="dlab-bar__group">
        <span className="dlab-bar__group-label">Surface</span>
        {viewOptions.map((option) => (
          <button
            key={option.key}
            type="button"
            className="dlab-bar__chip"
            data-active={view === option.key}
            onClick={() => onView(option.key)}
          >
            {option.label}
          </button>
        ))}
      </span>
      <span className="dlab-bar__group">
        <span className="dlab-bar__group-label">Cycle</span>
        {lifecycleOptions.map((option) => (
          <button
            key={option.key}
            type="button"
            className="dlab-bar__chip"
            data-active={lifecycle === option.key}
            onClick={() => onLifecycle(option.key)}
          >
            {option.label}
          </button>
        ))}
      </span>
      <span className="dlab-bar__spacer" />
      <Link to="/">← Quitter le lab</Link>
    </div>
  )
}

/* ------------------------------------------------------------------ */
/* Shell proposé                                                       */
/* ------------------------------------------------------------------ */

const railDestinations: Array<{
  key: LabView
  label: string
  icon: typeof OverviewNavIcon
}> = [
  { key: 'cockpit', label: "Vue d'ensemble", icon: OverviewNavIcon },
  { key: 'matches', label: 'Matchs', icon: OrganisationNavIcon },
  { key: 'match', label: 'Fiche match', icon: MatchesNavIcon },
  { key: 'standings', label: 'Classements', icon: ClassementsNavIcon },
]

function LabRail({
  view,
  onView,
}: {
  view: LabView
  onView: (v: LabView) => void
}) {
  return (
    <aside className="ds-shell-rail dlab-rail" aria-label="Navigation">
      <button
        type="button"
        className="ds-shell-rail__brand"
        onClick={() => onView('cockpit')}
        style={{ background: 'none', border: 'none', cursor: 'pointer' }}
      >
        <PlayUpMark size={22} variant="on-chrome" />
        <LabWordmark />
      </button>

      <nav className="ds-shell-rail__nav">
        {railDestinations.map((destination) => {
          const Icon = destination.icon
          return (
            <button
              key={destination.key}
              type="button"
              className="ds-shell-rail__link"
              data-active={view === destination.key}
              onClick={() => onView(destination.key)}
            >
              <Icon className="ds-shell-rail__icon" />
              {destination.label}
            </button>
          )
        })}
      </nav>

      <div className="ds-shell-rail__footer">
        <button type="button" className="ds-shell-rail__link" disabled>
          <SettingsNavIcon className="ds-shell-rail__icon" />
          Paramètres
        </button>
      </div>
    </aside>
  )
}

function LabHeader({ lifecycle }: { lifecycle: LabLifecycle }) {
  const statusLabel =
    lifecycle === 'preparation'
      ? 'Préparation'
      : lifecycle === 'done'
        ? 'Terminée'
        : 'En cours'

  const statusTone =
    lifecycle === 'preparation'
      ? 'ds-status--tone-info'
      : lifecycle === 'done'
        ? 'ds-status--tone-neutral'
        : 'ds-status--tone-success'

  return (
    <header className="ds-shell-header dlab-header">
      <span className="ds-shell-header__crest dlab-header__crest" aria-hidden="true">
        CV
      </span>
      <div className="ds-shell-header__identity">
        <span className="ds-shell-header__name">
          Championnat des Vétérans — Automne 2026
        </span>
        <span className="ds-shell-header__meta">
          <span
            className={`ds-status ds-status--dense ds-status--rounded ds-status--soft ${statusTone}`}
            style={{ padding: '1px 8px' }}
          >
            {statusLabel}
          </span>
          <span>12 sept. — 10 oct. 2026</span>
        </span>
      </div>
      <span className="ds-shell-header__spacer" aria-hidden="true" />
      <button
        type="button"
        className="ds-btn ds-btn--ghost ds-icon-button ds-shell-header__bell"
        aria-label="À traiter (2)"
        disabled={lifecycle !== 'live'}
      >
        <AttentionBellIcon />
        {lifecycle === 'live' ? (
          <span className="ds-shell-header__badge ds-num">2</span>
        ) : null}
      </button>
    </header>
  )
}

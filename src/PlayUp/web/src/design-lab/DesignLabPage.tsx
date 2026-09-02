import { useState } from 'react'
import { Link } from 'react-router-dom'
import '../design-system/fonts'
import '../design-system/index.css'
import './design-lab.css'
import { Status } from '../design-system/components/Status'
import {
  AttentionBellIcon,
  ClassementsNavIcon,
  MatchesNavIcon,
  OrganisationNavIcon,
  OverviewNavIcon,
  RegulationNavIcon,
  SidebarCollapseIcon,
  SidebarExpandIcon,
  TeamsNavIcon,
  VenuesNavIcon,
} from '../design-system/icons/shellIcons'
import { PlayUpLockupMark } from '../design-system/PlayUpLockupMark'
import { PlayUpWordmark } from '../design-system/PlayUpWordmark'
import { TeamCrest } from '../design-system/TeamCrest'
import { LabAngleCompare } from './LabAngleCompare'
import { LabBrand } from './LabBrand'
import { LabCockpit } from './LabCockpit'
import { LabHome } from './LabHome'
import { LabMatches } from './LabMatches'
import { LabMatchSheet } from './LabMatchSheet'
import { LabStandings } from './LabStandings'
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
  const [railCollapsed, setRailCollapsed] = useState(false)
  const [workspaceRadius, setWorkspaceRadius] = useState<12 | 16 | 24>(12)

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
        railCollapsed={railCollapsed}
        workspaceRadius={workspaceRadius}
        onView={setView}
        onLifecycle={setLifecycle}
        onRailCollapsed={setRailCollapsed}
        onWorkspaceRadius={setWorkspaceRadius}
      />

      {isHome ? (
        <LabHome empty={view === 'home-empty'} />
      ) : (
        <div
          className="dlab-shell"
          style={{
            ['--shell-workspace-radius' as string]: `${workspaceRadius}px`,
          }}
        >
          <LabRail
            view={view}
            collapsed={railCollapsed}
            onView={setView}
            onToggleCollapse={() => setRailCollapsed((value) => !value)}
          />
          <div className="dlab-column">
            <LabHeader lifecycle={lifecycle} />
            <main className="dlab-main ds-shell-workspace">
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

const radiusOptions: Array<{ key: 12 | 16 | 24; label: string }> = [
  { key: 12, label: '12' },
  { key: 16, label: '16' },
  { key: 24, label: '24' },
]

function LabBar({
  view,
  lifecycle,
  railCollapsed,
  workspaceRadius,
  onView,
  onLifecycle,
  onRailCollapsed,
  onWorkspaceRadius,
}: {
  view: LabView
  lifecycle: LabLifecycle
  railCollapsed: boolean
  workspaceRadius: 12 | 16 | 24
  onView: (v: LabView) => void
  onLifecycle: (l: LabLifecycle) => void
  onRailCollapsed: (collapsed: boolean) => void
  onWorkspaceRadius: (radius: 12 | 16 | 24) => void
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
      <span className="dlab-bar__group">
        <span className="dlab-bar__group-label">Rail</span>
        <button
          type="button"
          className="dlab-bar__chip"
          data-active={!railCollapsed}
          onClick={() => onRailCollapsed(false)}
        >
          Déplié
        </button>
        <button
          type="button"
          className="dlab-bar__chip"
          data-active={railCollapsed}
          onClick={() => onRailCollapsed(true)}
        >
          Replié
        </button>
      </span>
      <span className="dlab-bar__group">
        <span className="dlab-bar__group-label">Coin workspace</span>
        {radiusOptions.map((option) => (
          <button
            key={option.key}
            type="button"
            className="dlab-bar__chip"
            data-active={workspaceRadius === option.key}
            onClick={() => onWorkspaceRadius(option.key)}
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

const labNavGroups: Array<{
  id: string
  label: string
  items: Array<{
    key: LabView | 'structure' | 'teams' | 'venues' | 'regulation'
    label: string
    icon: typeof OverviewNavIcon
    dest?: LabView
  }>
}> = [
  {
    id: 'pilotage',
    label: 'Pilotage',
    items: [{ key: 'cockpit', label: 'Cockpit', icon: OverviewNavIcon, dest: 'cockpit' }],
  },
  {
    id: 'competition',
    label: 'Compétition',
    items: [
      { key: 'structure', label: 'Structure', icon: OrganisationNavIcon, dest: 'cockpit' },
      { key: 'matches', label: 'Calendrier & matchs', icon: MatchesNavIcon, dest: 'matches' },
      { key: 'standings', label: 'Classements', icon: ClassementsNavIcon, dest: 'standings' },
    ],
  },
  {
    id: 'referentiel',
    label: 'Référentiel',
    items: [
      { key: 'teams', label: 'Équipes', icon: TeamsNavIcon },
      { key: 'venues', label: 'Stades', icon: VenuesNavIcon },
      { key: 'regulation', label: 'Règlement', icon: RegulationNavIcon },
    ],
  },
]

function LabRail({
  view,
  collapsed,
  onView,
  onToggleCollapse,
}: {
  view: LabView
  collapsed: boolean
  onView: (v: LabView) => void
  onToggleCollapse: () => void
}) {
  return (
    <aside
      className="ds-shell-rail dlab-rail"
      data-collapsed={collapsed ? 'true' : 'false'}
      aria-label="Navigation"
    >
      <button
        type="button"
        className="ds-shell-rail__brand"
        onClick={() => onView('home')}
      >
        <PlayUpLockupMark />
        <span className="ds-shell-rail__wordmark">
          <PlayUpWordmark surface="chrome" />
        </span>
      </button>

      <nav className="ds-shell-rail__nav">
        {labNavGroups.map((group) => (
          <div key={group.id} className="ds-shell-rail__group">
            <p className="ds-shell-rail__group-label">{group.label}</p>
            {group.items.map((item) => {
              const Icon = item.icon
              const isActive =
                item.dest !== undefined && item.dest === view && item.key === item.dest
              if (!item.dest) {
                return (
                  <button
                    key={item.key}
                    type="button"
                    className="ds-shell-rail__link"
                    disabled
                    title={`${item.label} — bientôt disponible`}
                  >
                    <Icon className="ds-shell-rail__icon" />
                    <span className="ds-shell-rail__label">{item.label}</span>
                  </button>
                )
              }
              return (
                <button
                  key={item.key}
                  type="button"
                  className="ds-shell-rail__link"
                  data-active={isActive}
                  onClick={() => onView(item.dest!)}
                >
                  <Icon className="ds-shell-rail__icon" />
                  <span className="ds-shell-rail__label">{item.label}</span>
                </button>
              )
            })}
          </div>
        ))}
      </nav>

      <div className="ds-shell-rail__footer">
        <button
          type="button"
          className="ds-shell-rail__collapse"
          aria-expanded={!collapsed}
          onClick={onToggleCollapse}
        >
          {collapsed ? (
            <SidebarExpandIcon size="sm" />
          ) : (
            <SidebarCollapseIcon size="sm" />
          )}
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
      ? 'info'
      : lifecycle === 'done'
        ? 'neutral'
        : 'live'

  return (
    <header className="ds-shell-header dlab-header">
      <span className="ds-shell-header__crest dlab-header__crest" aria-hidden="true">
        <TeamCrest name="Championnat des Vétérans — Automne 2026" size="lg" />
      </span>
      <div className="ds-shell-header__identity">
        <span className="ds-shell-header__name">
          Championnat des Vétérans — Automne 2026
        </span>
        <span className="ds-shell-header__meta">
          <Status density="compact" tone={statusTone} variant="soft">
            {statusLabel}
          </Status>
          <span>·</span>
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

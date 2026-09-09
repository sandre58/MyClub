import { useEffect, useLayoutEffect, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import '../design-system/fonts';
import '../design-system/index.css';
import './design-lab.css';
import { Status } from '../design-system/components/Status';
import {
  AttentionBellIcon,
  ClassementsNavIcon,
  MatchesNavIcon,
  OrganisationNavIcon,
  OverviewNavIcon,
  RegulationNavIcon,
  SidebarCollapseIcon,
  SidebarExpandIcon,
  CloseIcon,
  TeamsNavIcon,
  VenuesNavIcon,
} from '../design-system/icons/shellIcons';
import { PlayUpLockupMark } from '../design-system/PlayUpLockupMark';
import { PlayUpWordmark } from '../design-system/PlayUpWordmark';
import { TeamCrest } from '../design-system/TeamCrest';
import { LabOverview } from './LabOverview';
import { LabHome } from './LabHome';
import { LabMatches } from './LabMatches';
import { LabMatchSheet } from './LabMatchSheet';
import { LabStandings } from './LabStandings';
import { LabDialog } from './LabDialog';
import { LabToast } from './LabToast';
import { LabTooltip } from './LabTooltip';
import { LabForm } from './LabForm';
import { LabRegulation } from './LabRegulation';
import { LabWait, LabWaitAtom, type LabWaitKind } from './LabWait';
import { Toaster } from '../design-system/components/Toaster';
import { Tooltip } from '../design-system/components/Tooltip';
import type { LabLifecycle } from './labData';

type LabChromeVp = 'desktop' | 'tablet' | 'phone';

type LabView =
  | 'home'
  | 'home-empty'
  | 'home-loading'
  | 'wait'
  | 'dialog'
  | 'toast'
  | 'tooltip'
  | 'form'
  | 'regulation'
  | 'overview'
  | 'overview-loading'
  | 'matches'
  | 'match'
  | 'standings';

/**
 * /design-lab — prototype de la direction « Grille de compétition, exécutée ».
 *
 * Référence visuelle, hors produit : données fictives, aucune API,
 * aucune surface existante modifiée. La barre du haut pilote le prototype
 * (surface + état du cycle) ; tout le reste est le produit proposé.
 */
export function DesignLabPage() {
  const [view, setView] = useState<LabView>('overview');
  const [lifecycle, setLifecycle] = useState<LabLifecycle>('live');
  const [railCollapsed, setRailCollapsed] = useState(false);
  const [chromeVp, setChromeVp] = useState<LabChromeVp>('desktop');
  const [phoneNavOpen, setPhoneNavOpen] = useState(false);
  const [motionViewport, setMotionViewport] = useState(chromeVp);
  const [chromeReady, setChromeReady] = useState(chromeVp === 'desktop');
  const [waitKind, setWaitKind] = useState<LabWaitKind>('c');
  const labShellRef = useRef<HTMLDivElement>(null);

  const isHome =
    view === 'home' || view === 'home-empty' || view === 'home-loading';
  const isWaitBoard = view === 'wait';
  const isDialogBoard = view === 'dialog';
  const isToastBoard = view === 'toast';
  const isTooltipBoard = view === 'tooltip';
  const isFormBoard = view === 'form';
  const isRegulationBoard = view === 'regulation';
  const labCollapsed = chromeVp === 'phone' ? false : railCollapsed;

  useEffect(() => {
    if (chromeVp === 'desktop') {
      setMotionViewport('desktop');
      setChromeReady(true);
      return;
    }

    setChromeReady(false);
    setMotionViewport(chromeVp);
    let cancelled = false;
    requestAnimationFrame(() => {
      requestAnimationFrame(() => {
        if (!cancelled) {
          setChromeReady(true);
        }
      });
    });
    return () => {
      cancelled = true;
    };
  }, [chromeVp]);

  useLayoutEffect(() => {
    const root = labShellRef.current;
    if (!root || chromeVp !== 'phone') {
      root?.style.removeProperty('--shell-phone-chrome-end');
      return;
    }

    const header = root.querySelector('.ds-shell-header');
    if (
      !(header instanceof HTMLElement) ||
      typeof ResizeObserver === 'undefined'
    ) {
      return;
    }

    const sync = () => {
      root.style.setProperty(
        '--shell-phone-chrome-end',
        `${header.offsetHeight}px`,
      );
    };

    sync();
    const observer = new ResizeObserver(sync);
    observer.observe(header);
    return () => {
      observer.disconnect();
      root.style.removeProperty('--shell-phone-chrome-end');
    };
  }, [chromeVp]);

  const navReady = chromeVp === motionViewport && chromeReady;

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
        waitKind={waitKind}
        railCollapsed={railCollapsed}
        chromeVp={chromeVp}
        onView={setView}
        onLifecycle={setLifecycle}
        onWaitKind={setWaitKind}
        onRailCollapsed={setRailCollapsed}
        onChromeVp={(next) => {
          setChromeVp(next);
          setPhoneNavOpen(false);
          if (next === 'tablet') {
            setRailCollapsed(true);
          }
        }}
      />

      {isWaitBoard ? (
        <LabWait />
      ) : isDialogBoard ? (
        <main className="dlab-dialog-board ds-shell-workspace">
          <LabDialog />
        </main>
      ) : isToastBoard ? (
        <main className="dlab-toast-board ds-shell-workspace">
          <LabToast />
          <Toaster />
        </main>
      ) : isTooltipBoard ? (
        <main className="dlab-toast-board ds-shell-workspace">
          <LabTooltip />
        </main>
      ) : isFormBoard ? (
        <main className="dlab-form-board ds-shell-workspace">
          <LabForm />
        </main>
      ) : isRegulationBoard ? (
        <main className="dlab-form-board ds-shell-workspace">
          <LabRegulation />
        </main>
      ) : isHome ? (
        <LabHome
          empty={view === 'home-empty'}
          waiting={view === 'home-loading' ? waitKind : undefined}
        />
      ) : (
        <div
          ref={labShellRef}
          className="dlab-shell"
          data-shell-vp={chromeVp}
          data-nav-open={phoneNavOpen ? 'true' : 'false'}
          data-nav-ready={navReady ? 'true' : 'false'}
        >
          <LabRail
            view={view}
            collapsed={labCollapsed}
            hideCollapse={chromeVp === 'phone'}
            onView={(next) => {
              setView(next);
              setPhoneNavOpen(false);
            }}
            onToggleCollapse={() => setRailCollapsed((value) => !value)}
          />
          <div className="dlab-column">
            <LabHeader
              lifecycle={lifecycle}
              chromeVp={chromeVp}
              phoneNavOpen={phoneNavOpen}
              onTogglePhoneNav={() => setPhoneNavOpen((value) => !value)}
            />
            {chromeVp === 'phone' && phoneNavOpen ? (
              <button
                type="button"
                className="shell-nav-backdrop"
                tabIndex={-1}
                aria-hidden="true"
                onClick={() => setPhoneNavOpen(false)}
              />
            ) : null}
            <main className="dlab-main ds-shell-workspace">
              {view === 'overview' && <LabOverview lifecycle={lifecycle} />}
              {view === 'overview-loading' && (
                <LabWaitAtom kind={waitKind} scale="page" />
              )}
              {view === 'matches' && <LabMatches />}
              {view === 'match' && <LabMatchSheet lifecycle={lifecycle} />}
              {view === 'standings' && <LabStandings />}
            </main>
          </div>
        </div>
      )}
    </div>
  );
}

/* ------------------------------------------------------------------ */
/* Barre lab (hors produit)                                            */
/* ------------------------------------------------------------------ */

const viewOptions: Array<{ key: LabView; label: string }> = [
  { key: 'home', label: 'Accueil' },
  { key: 'home-empty', label: 'Accueil vide' },
  { key: 'home-loading', label: 'Accueil chargement' },
  { key: 'wait', label: 'Attente' },
  { key: 'dialog', label: 'Dialog' },
  { key: 'toast', label: 'Toast' },
  { key: 'tooltip', label: 'Tooltip' },
  { key: 'form', label: 'Form' },
  { key: 'regulation', label: 'Règlement' },
  { key: 'overview', label: "Vue d'ensemble" },
  { key: 'overview-loading', label: "Vue d'ensemble chargement" },
  { key: 'matches', label: 'Matchs' },
  { key: 'match', label: 'Fiche match' },
  { key: 'standings', label: 'Classements' },
];

const waitKindOptions: Array<{ key: LabWaitKind; label: string }> = [
  { key: 'b', label: 'B — spinner' },
  { key: 'c', label: 'C — marque' },
];

const lifecycleOptions: Array<{ key: LabLifecycle; label: string }> = [
  { key: 'preparation', label: 'Préparation' },
  { key: 'live', label: 'En cours' },
  { key: 'done', label: 'Terminée' },
];

const chromeVpOptions: Array<{ key: LabChromeVp; label: string }> = [
  { key: 'desktop', label: 'Desktop' },
  { key: 'tablet', label: 'Tablette 768' },
  { key: 'phone', label: 'Phone 390' },
];

function LabBar({
  view,
  lifecycle,
  waitKind,
  railCollapsed,
  chromeVp,
  onView,
  onLifecycle,
  onWaitKind,
  onRailCollapsed,
  onChromeVp,
}: {
  view: LabView;
  lifecycle: LabLifecycle;
  waitKind: LabWaitKind;
  railCollapsed: boolean;
  chromeVp: LabChromeVp;
  onView: (v: LabView) => void;
  onLifecycle: (l: LabLifecycle) => void;
  onWaitKind: (kind: LabWaitKind) => void;
  onRailCollapsed: (collapsed: boolean) => void;
  onChromeVp: (vp: LabChromeVp) => void;
}) {
  const showWaitKind = view === 'home-loading' || view === 'overview-loading';

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
      {showWaitKind ? (
        <span className="dlab-bar__group">
          <span className="dlab-bar__group-label">Atome</span>
          {waitKindOptions.map((option) => (
            <button
              key={option.key}
              type="button"
              className="dlab-bar__chip"
              data-active={waitKind === option.key}
              onClick={() => onWaitKind(option.key)}
            >
              {option.label}
            </button>
          ))}
        </span>
      ) : null}
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
        <span className="dlab-bar__group-label">Chrome</span>
        {chromeVpOptions.map((option) => (
          <button
            key={option.key}
            type="button"
            className="dlab-bar__chip"
            data-active={chromeVp === option.key}
            onClick={() => onChromeVp(option.key)}
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
      <span className="dlab-bar__spacer" />
      <Link to="/dev/foundations">Foundations</Link>
      <Link to="/">← Quitter le lab</Link>
    </div>
  );
}

/* ------------------------------------------------------------------ */
/* Shell proposé                                                       */
/* ------------------------------------------------------------------ */

const labNavGroups: Array<{
  id: string;
  label: string;
  items: Array<{
    key: LabView | 'structure' | 'teams' | 'venues' | 'regulation';
    label: string;
    icon: typeof OverviewNavIcon;
    dest?: LabView;
  }>;
}> = [
  {
    id: 'pilotage',
    label: 'Pilotage',
    items: [
      {
        key: 'overview',
        label: "Vue d'ensemble",
        icon: OverviewNavIcon,
        dest: 'overview',
      },
    ],
  },
  {
    id: 'competition',
    label: 'Compétition',
    items: [
      {
        key: 'structure',
        label: 'Structure',
        icon: OrganisationNavIcon,
        dest: 'overview',
      },
      {
        key: 'matches',
        label: 'Calendrier & matchs',
        icon: MatchesNavIcon,
        dest: 'matches',
      },
      {
        key: 'standings',
        label: 'Classements',
        icon: ClassementsNavIcon,
        dest: 'standings',
      },
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
];

function LabRail({
  view,
  collapsed,
  hideCollapse = false,
  onView,
  onToggleCollapse,
}: {
  view: LabView;
  collapsed: boolean;
  hideCollapse?: boolean;
  onView: (v: LabView) => void;
  onToggleCollapse: () => void;
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
              const Icon = item.icon;
              const railView = view === 'overview-loading' ? 'overview' : view;
              const isActive =
                item.dest !== undefined &&
                item.dest === railView &&
                item.key === item.dest;
              if (!item.dest) {
                return (
                  <Tooltip
                    key={item.key}
                    content={`${item.label} — bientôt disponible`}
                  >
                    <button
                      type="button"
                      className="ds-shell-rail__link"
                      disabled
                    >
                      <Icon className="ds-shell-rail__icon" />
                      <span className="ds-shell-rail__label">{item.label}</span>
                    </button>
                  </Tooltip>
                );
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
              );
            })}
          </div>
        ))}
      </nav>

      {hideCollapse ? null : (
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
      )}
    </aside>
  );
}

function LabHeader({
  lifecycle,
  chromeVp,
  phoneNavOpen,
  onTogglePhoneNav,
}: {
  lifecycle: LabLifecycle;
  chromeVp: LabChromeVp;
  phoneNavOpen: boolean;
  onTogglePhoneNav: () => void;
}) {
  const statusLabel =
    lifecycle === 'preparation'
      ? 'Préparation'
      : lifecycle === 'done'
        ? 'Terminée'
        : 'En cours';

  const statusTone =
    lifecycle === 'preparation'
      ? 'info'
      : lifecycle === 'done'
        ? 'neutral'
        : 'live';

  return (
    <header className="ds-shell-header dlab-header">
      {chromeVp === 'phone' ? (
        <button
          type="button"
          className="ds-shell-header__nav-toggle ds-btn ds-btn--ghost ds-icon-button"
          aria-expanded={phoneNavOpen}
          aria-label={phoneNavOpen ? 'Fermer le menu' : 'Ouvrir le menu'}
          onClick={onTogglePhoneNav}
        >
          {phoneNavOpen ? (
            <CloseIcon size="sm" aria-hidden="true" />
          ) : (
            <SidebarExpandIcon size="sm" aria-hidden="true" />
          )}
        </button>
      ) : null}
      <span
        className="ds-shell-header__crest dlab-header__crest"
        aria-hidden="true"
      >
        <TeamCrest
          name="Championnat des Vétérans — Automne 2026"
          size={chromeVp === 'phone' ? 'md' : 'lg'}
        />
      </span>
      <div className="ds-shell-header__identity">
        <span className="ds-shell-header__name">
          Championnat des Vétérans — Automne 2026
        </span>
        <span className="ds-shell-header__meta">
          <Status density="compact" tone={statusTone} variant="soft">
            {statusLabel}
          </Status>
          <span className="shell-header__meta-separator" aria-hidden="true">
            ·
          </span>
          <span className="shell-header__period">12 sept. — 10 oct. 2026</span>
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
  );
}

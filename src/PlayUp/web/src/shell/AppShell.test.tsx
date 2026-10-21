import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, describe, expect, it } from 'vitest';
import { AppLayout } from '../AppLayout';
import { SHELL_PHONE_QUERY, SHELL_TABLET_QUERY } from './useShellViewport';

const sidebarCollapsedStorageKey = 'playup:shell:sidebar-collapsed';

function stubShellViewport(viewport: 'phone' | 'tablet' | 'desktop') {
  window.matchMedia = (query: string) => ({
    matches:
      viewport === 'phone'
        ? query === SHELL_PHONE_QUERY
        : viewport === 'tablet'
          ? query === SHELL_TABLET_QUERY
          : false,
    media: query,
    onchange: null,
    addListener: () => {},
    removeListener: () => {},
    addEventListener: () => {},
    removeEventListener: () => {},
    dispatchEvent: () => false,
  });
}

function restoreDesktopViewport() {
  stubShellViewport('desktop');
}

function renderWithShell(initialEntry: string) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <AppShellRoutes initialEntry={initialEntry} />
    </QueryClientProvider>,
  );
}

function AppShellRoutes({ initialEntry }: { initialEntry: string }) {
  return (
    <MemoryRouter initialEntries={[initialEntry]}>
      <Routes>
        <Route element={<AppLayout />}>
          <Route
            path="/"
            element={
              <main id="main">
                <h1>Play’Up</h1>
              </main>
            }
          />
          <Route path="/competitions" element={<p>Competition list</p>} />
          <Route
            path="/competitions/:competitionId"
            element={<p>Workspace page</p>}
          />
          <Route
            path="/competitions/:competitionId/teams/:entryId"
            element={<p>Teams drawer page</p>}
          />
          <Route
            path="/competitions/:competitionId/teams"
            element={<p>Teams page</p>}
          />
          <Route
            path="/competitions/:competitionId/regulation"
            element={<p>Regulation page</p>}
          />
          <Route
            path="/competitions/:competitionId/teams/:entryId"
            element={<p>Roster page</p>}
          />
          <Route
            path="/competitions/:competitionId/structure"
            element={<p>Structure page</p>}
          />
          <Route
            path="/competitions/:competitionId/matches"
            element={<p>Competition matches page</p>}
          />
          <Route
            path="/competitions/:competitionId/classements"
            element={<p>Classements page</p>}
          />
          <Route path="/stages/:stageId" element={<p>Stage page</p>} />
          <Route
            path="/stages/:stageId/matches"
            element={<p>Stage matches page</p>}
          />
          <Route
            path="/stages/:stageId/matches"
            element={<p>Stage matches page</p>}
          />
          <Route path="/matches/:matchId" element={<p>Match deep link</p>} />
        </Route>
      </Routes>
    </MemoryRouter>
  );
}

describe('AppShell', () => {
  afterEach(() => {
    restoreDesktopViewport();
    window.localStorage.removeItem(sidebarCollapsedStorageKey);
  });
  it('renders the matched page through Outlet', () => {
    renderWithShell('/');

    expect(
      screen.getByRole('heading', { name: 'Play’Up' }),
    ).toBeInTheDocument();
  });

  it('exposes the main landmark from the page content', () => {
    renderWithShell('/');

    expect(document.querySelectorAll('#main')).toHaveLength(1);
  });

  it('exposes primary navigation and skip link', () => {
    renderWithShell('/');

    expect(
      screen.getByRole('navigation', { name: 'Navigation principale' }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('link', { name: 'Passer au contenu' }),
    ).toHaveAttribute('href', '#main');
  });

  it('renders grouped nav items', () => {
    renderWithShell('/');

    expect(screen.getByText('Pilotage')).toBeInTheDocument();
    expect(screen.getByText('Compétition')).toBeInTheDocument();
    expect(screen.getByText('Référentiel')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Équipes' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Règlement' })).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Stades/ }),
    ).not.toBeInTheDocument();
  });

  it('sends the Play’Up lockup to Accueil', () => {
    renderWithShell('/competitions/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa');

    expect(
      screen.getByRole('link', { name: 'Accueil Play’Up' }),
    ).toHaveAttribute('href', '/');
    const lockup = screen.getByRole('link', { name: 'Accueil Play’Up' });
    expect(lockup.querySelector('.ds-lockup-mark')).toHaveAttribute(
      'src',
      expect.stringContaining('/brand/accueil-mark.png'),
    );
    expect(lockup.querySelector('.ds-lockup-wordmark')).toHaveAttribute(
      'src',
      '/brand/accueil-wordmark-on-chrome.png',
    );
  });

  it('renders the live sidebar destinations', () => {
    renderWithShell('/');

    expect(
      screen.getByRole('link', { name: "Vue d'ensemble" }),
    ).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Structure' })).toBeInTheDocument();
    expect(
      screen.getByRole('link', { name: 'Calendrier & matchs' }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('link', { name: 'Classements' }),
    ).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Équipes' })).toBeInTheDocument();
  });

  it('keeps sidebar nav links outside content-link scope', () => {
    renderWithShell('/');

    const navLink = screen.getByRole('link', { name: "Vue d'ensemble" });
    expect(navLink).toHaveClass('ds-shell-rail__link');
    expect(navLink).toHaveAttribute('data-active');
    expect(navLink.closest('.shell-sidebar')).not.toBeNull();
    expect(navLink.closest('.shell-main')).toBeNull();
  });

  it("marks Vue d'ensemble active for workspace routes", () => {
    renderWithShell('/competitions/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa');

    expect(
      screen.getByRole('link', { name: "Vue d'ensemble" }),
    ).toHaveAttribute('aria-current', 'page');
    expect(screen.getByRole('link', { name: 'Structure' })).not.toHaveAttribute(
      'aria-current',
    );
  });

  it('marks Structure active for /structure routes', () => {
    renderWithShell(
      '/competitions/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/structure',
    );

    expect(screen.getByRole('link', { name: 'Structure' })).toHaveAttribute(
      'aria-current',
      'page',
    );
  });

  it('marks Équipes active for nested roster routes', () => {
    renderWithShell(
      '/competitions/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/teams/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
    );

    expect(screen.getByRole('link', { name: 'Équipes' })).toHaveAttribute(
      'aria-current',
      'page',
    );
    expect(screen.getByText('Teams drawer page')).toBeInTheDocument();
  });

  it('marks Règlement active for regulation routes', () => {
    renderWithShell(
      '/competitions/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/regulation',
    );

    expect(screen.getByRole('link', { name: 'Règlement' })).toHaveAttribute(
      'aria-current',
      'page',
    );
    expect(screen.getByText('Regulation page')).toBeInTheDocument();
  });

  it('maps stage deep links to Matchs', () => {
    renderWithShell('/stages/stage-id');

    expect(
      screen.getByRole('link', { name: 'Calendrier & matchs' }),
    ).toHaveAttribute('aria-current', 'page');
  });

  it('maps match deep links to Matchs', () => {
    renderWithShell('/matches/match-id');

    expect(
      screen.getByRole('link', { name: 'Calendrier & matchs' }),
    ).toHaveAttribute('aria-current', 'page');
  });

  it('marks Classements active for classements routes', () => {
    renderWithShell(
      '/competitions/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/classements',
    );

    expect(screen.getByRole('link', { name: 'Classements' })).toHaveAttribute(
      'aria-current',
      'page',
    );
  });

  it('does not mark a sidebar destination for overview', () => {
    renderWithShell(
      '/competitions/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/overview',
    );

    expect(
      screen.queryByRole('link', { current: 'page' }),
    ).not.toBeInTheDocument();
  });

  it('toggles sidebar expanded/collapsed state', async () => {
    const user = userEvent.setup();
    renderWithShell('/');

    const toggle = screen.getByRole('button', {
      name: 'Réduire la barre latérale',
    });
    expect(toggle).toHaveAttribute('aria-expanded', 'true');

    await user.click(toggle);

    expect(
      screen.getByRole('button', { name: 'Développer la barre latérale' }),
    ).toHaveAttribute('aria-expanded', 'false');
    expect(
      screen.getByRole('link', { name: "Vue d'ensemble" }),
    ).toBeInTheDocument();
  });

  it('maps stage matches deep links to Matchs', () => {
    renderWithShell('/stages/stage-id/matches');

    expect(
      screen.getByRole('link', { name: 'Calendrier & matchs' }),
    ).toHaveAttribute('aria-current', 'page');
  });

  it('continues to render deep-link routes inside the shell', () => {
    renderWithShell('/matches/11111111-1111-1111-1111-111111111111');

    expect(screen.getByText('Match deep link')).toBeInTheDocument();
    expect(document.getElementById('main')).not.toBeInTheDocument();
  });

  it('forces the icon rail on tablet without writing desktop preference', async () => {
    const user = userEvent.setup();
    window.localStorage.setItem(sidebarCollapsedStorageKey, 'false');
    stubShellViewport('tablet');
    renderWithShell('/');

    const rail = document.querySelector('.ds-shell-rail');
    expect(document.querySelector('.shell')).toHaveAttribute(
      'data-shell-vp',
      'tablet',
    );
    expect(rail).toHaveAttribute('data-collapsed', 'true');
    expect(
      screen.getByRole('button', { name: 'Développer la barre latérale' }),
    ).toBeInTheDocument();

    await user.click(
      screen.getByRole('button', { name: 'Développer la barre latérale' }),
    );

    expect(rail).toHaveAttribute('data-collapsed', 'false');
    expect(window.localStorage.getItem(sidebarCollapsedStorageKey)).toBe(
      'false',
    );
  });

  it('keeps Accueil in the phone overlay, not the header', () => {
    stubShellViewport('phone');
    renderWithShell('/competitions/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa');

    expect(
      screen.queryByRole('link', { name: 'Accueil Play’Up' }),
    ).not.toBeInTheDocument();
    expect(document.querySelector('.ds-shell-header__lockup')).toBeNull();
  });

  it('moves the rail off-canvas on phone and exposes it from the header menu', async () => {
    const user = userEvent.setup();
    stubShellViewport('phone');
    renderWithShell('/');

    expect(document.querySelector('.shell')).toHaveAttribute(
      'data-shell-vp',
      'phone',
    );
    expect(
      screen.queryByRole('button', { name: 'Réduire la barre latérale' }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('link', { name: "Vue d'ensemble" }),
    ).not.toBeInTheDocument();

    await user.click(
      screen.getByRole('button', { name: 'Ouvrir le menu de navigation' }),
    );

    expect(document.querySelector('.shell')).toHaveAttribute(
      'data-nav-open',
      'true',
    );
    const home = screen.getByRole('link', { name: 'Accueil Play’Up' });
    expect(home).toHaveClass('ds-shell-rail__brand');
    expect(home).toHaveAttribute('href', '/');
    expect(home.querySelector('.ds-lockup-mark')).toHaveAttribute(
      'src',
      expect.stringContaining('/brand/accueil-mark.png'),
    );
    expect(home.querySelector('.ds-lockup-wordmark')).toHaveAttribute(
      'src',
      '/brand/accueil-wordmark-on-chrome.png',
    );
    expect(
      screen.getByRole('link', { name: "Vue d'ensemble" }),
    ).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Structure' })).toBeInTheDocument();
    expect(
      screen.getByRole('link', { name: 'Calendrier & matchs' }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('link', { name: 'Classements' }),
    ).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Équipes' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Règlement' })).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Stades/ }),
    ).not.toBeInTheDocument();
  });
});

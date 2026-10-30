import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import {
  fetchCompetitionDetail,
  fetchCompetitions,
  fetchMatchDetail,
  fetchNeedsAttention,
  fetchStageOverview,
} from '../api';
import { AppLayout } from '../AppLayout';
import type { CompetitionStatus, NeedsAttentionItem } from '../types';

vi.mock('../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../api')>();
  return {
    ...actual,
    fetchCompetitions: vi.fn(),
    fetchCompetitionDetail: vi.fn(),
    fetchStageOverview: vi.fn(),
    fetchMatchDetail: vi.fn(),
    fetchNeedsAttention: vi.fn(),
  };
});

const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
const stageId = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
const matchId = 'cccccccc-cccc-cccc-cccc-cccccccccccc';

function needsAttention(items: Partial<NeedsAttentionItem>[] = []) {
  const normalized: NeedsAttentionItem[] = items.map((item) => ({
    source: item.source ?? 'ProgressionPending',
    severity: item.severity ?? 'Blocking',
    targetType: item.targetType ?? 'Stage',
    targetId: item.targetId ?? stageId,
    params: item.params ?? null,
  }));
  return {
    competitionId,
    items: normalized,
    count: normalized.length,
  };
}

function mockCompetitionStatus(status: CompetitionStatus) {
  vi.mocked(fetchCompetitions).mockResolvedValue([
    { id: competitionId, name: 'Coupe U18', status },
  ]);
  vi.mocked(fetchCompetitionDetail).mockResolvedValue({
    id: competitionId,
    name: 'Coupe U18',
    status,
    entries: [],
    stages: [],
    logoMediaId: 'dddddddd-dddd-dddd-dddd-dddddddddddd',
  });
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
            <Route
              path="/competitions"
              element={<p>Competition list page</p>}
            />
            <Route
              path="/competitions/:competitionId"
              element={<p>Workspace page</p>}
            />
            <Route
              path="/competitions/:competitionId/structure"
              element={<p>Structure page</p>}
            />
            <Route
              path="/competitions/:competitionId/structure"
              element={<p>Structure page</p>}
            />
            <Route
              path="/stages/:stageId"
              element={<p>Legacy stage redirect</p>}
            />
            <Route path="/matches/:matchId" element={<p>Match page</p>} />
          </Route>
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ShellHeader', () => {
  beforeEach(() => {
    vi.mocked(fetchCompetitions).mockResolvedValue([
      { id: competitionId, name: 'Coupe U18', status: 'Running' },
    ]);
    vi.mocked(fetchCompetitionDetail).mockResolvedValue({
      id: competitionId,
      name: 'Coupe U18',
      status: 'Running',
      entries: [],
      stages: [],
      logoMediaId: 'dddddddd-dddd-dddd-dddd-dddddddddddd',
    });
    vi.mocked(fetchNeedsAttention).mockResolvedValue(needsAttention());
    vi.mocked(fetchStageOverview).mockResolvedValue({
      id: stageId,
      competitionId,
      name: 'Group stage',
      status: 'Running',
      rounds: [],
      slots: [],
      draws: [],
    });
    vi.mocked(fetchMatchDetail).mockResolvedValue({
      matchId,
      competitionId,
      stageId,
      status: 'Scheduled',
      home: { entryId: 'h1', displayName: 'Home' },
      away: { entryId: 'a1', displayName: 'Away' },
      result: null,
      fixtureId: null,
      legIndex: null,
    });
  });

  it('is rendered inside AppShell', () => {
    renderWithShell('/');

    expect(document.querySelector('.shell-header')).toBeInTheDocument();
  });

  it("does not show the PLAY'UP wordmark in the header", () => {
    renderWithShell('/');

    expect(screen.queryByText("PLAY'UP")).not.toBeInTheDocument();
  });

  it('shows the current competition when available', async () => {
    renderWithShell(`/competitions/${competitionId}`);

    expect(await screen.findByText('Coupe U18')).toBeInTheDocument();
    expect(await screen.findByText('En cours')).toBeInTheDocument();
    expect(
      screen.getByRole('link', { name: 'Accueil Play’Up' }),
    ).toHaveAttribute('href', '/');
    const crest = document.querySelector('.shell-header__crest img');
    expect(crest).toHaveAttribute(
      'src',
      '/media/dddddddd-dddd-dddd-dddd-dddddddddddd/content',
    );
  });

  it('renders the no-selection state when competitions exist', async () => {
    renderWithShell('/');

    expect(
      await screen.findByText('Choisir une compétition'),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('link', { name: 'Accueil Play’Up' }),
    ).toHaveAttribute('href', '/');
  });

  it('renders the empty-host state when no competitions exist', async () => {
    vi.mocked(fetchCompetitions).mockResolvedValue([]);
    renderWithShell('/');

    expect(await screen.findByText('Aucune compétition')).toBeInTheDocument();
    expect(
      screen.getByRole('link', { name: 'Accueil Play’Up' }),
    ).toHaveAttribute('href', '/');
  });

  it('resolves competition context from a stage deep link', async () => {
    renderWithShell(`/stages/${stageId}`);

    expect(await screen.findByText('Coupe U18')).toBeInTheDocument();
    expect(fetchStageOverview).toHaveBeenCalledWith(stageId);
  });

  it('resolves competition context from a match deep link', async () => {
    renderWithShell(`/matches/${matchId}`);

    expect(await screen.findByText('Coupe U18')).toBeInTheDocument();
    expect(fetchMatchDetail).toHaveBeenCalledWith(matchId);
  });

  it('keeps À traiter visible but disabled when count is 0', async () => {
    renderWithShell(`/competitions/${competitionId}`);

    const trigger = await screen.findByRole('button', {
      name: 'À traiter, aucun élément',
    });
    expect(trigger).toBeInTheDocument();
    expect(trigger).toBeDisabled();
    expect(screen.queryByText('0')).not.toBeInTheDocument();
  });

  it('shows zero presentable count when only denylisted items are on the wire', async () => {
    vi.mocked(fetchNeedsAttention).mockResolvedValue(
      needsAttention([
        {
          source: 'MissingStage',
          severity: 'Blocking',
          targetType: 'Competition',
          targetId: competitionId,
        },
      ]),
    );
    renderWithShell(`/competitions/${competitionId}`);

    const trigger = await screen.findByRole('button', {
      name: 'À traiter, aucun élément',
    });
    expect(trigger).toBeDisabled();
    expect(screen.queryByText('1')).not.toBeInTheDocument();
  });

  it('shows attention state when count is greater than 0', async () => {
    vi.mocked(fetchNeedsAttention).mockResolvedValue(
      needsAttention([
        {
          source: 'ProgressionPending',
          severity: 'Blocking',
          targetType: 'Stage',
          targetId: stageId,
        },
      ]),
    );
    renderWithShell(`/competitions/${competitionId}`);

    const trigger = await screen.findByRole('button', {
      name: 'À traiter, 1 élément',
    });
    expect(trigger).toBeEnabled();
    expect(screen.getByText('1')).toBeInTheDocument();
  });

  it('uses plural aria-label when count is 2', async () => {
    vi.mocked(fetchNeedsAttention).mockResolvedValue(
      needsAttention([
        {
          source: 'ProgressionPending',
          severity: 'Blocking',
          targetType: 'Stage',
          targetId: stageId,
        },
        {
          source: 'QualificationPending',
          severity: 'Blocking',
          targetType: 'Stage',
          targetId: stageId,
        },
      ]),
    );
    renderWithShell(`/competitions/${competitionId}`);

    const trigger = await screen.findByRole('button', {
      name: 'À traiter, 2 éléments',
    });
    expect(trigger).toHaveAttribute('aria-label', 'À traiter, 2 éléments');
    expect(screen.getByText('2')).toBeInTheDocument();
  });

  it('links the attention trigger to the drawer panel', async () => {
    vi.mocked(fetchNeedsAttention).mockResolvedValue(
      needsAttention([
        {
          source: 'ProgressionPending',
          severity: 'Blocking',
          targetType: 'Stage',
          targetId: stageId,
        },
      ]),
    );
    const user = userEvent.setup();
    renderWithShell(`/competitions/${competitionId}`);

    const trigger = await screen.findByRole('button', {
      name: 'À traiter, 1 élément',
    });
    expect(trigger).toHaveAttribute(
      'aria-controls',
      'shell-attention-drawer-panel',
    );

    await user.click(trigger);

    expect(trigger).toHaveAttribute('aria-expanded', 'true');
    expect(
      document.getElementById('shell-attention-drawer-panel'),
    ).toBeInTheDocument();
  });

  it('links a suspended status badge to overview without a resume focus', async () => {
    mockCompetitionStatus('Suspended');
    const user = userEvent.setup();
    renderWithShell(`/competitions/${competitionId}/structure`);

    expect(await screen.findByText('Structure page')).toBeInTheDocument();
    const link = await screen.findByRole('link', { name: 'Suspendu' });
    expect(link).toHaveAttribute('href', `/competitions/${competitionId}`);

    await user.click(link);

    expect(await screen.findByText('Workspace page')).toBeInTheDocument();
    expect(screen.queryByText('Structure page')).not.toBeInTheDocument();
  });

  it('links a suspended badge resolved from a stage to overview', async () => {
    mockCompetitionStatus('Suspended');
    renderWithShell(`/stages/${stageId}`);

    const link = await screen.findByRole('link', { name: 'Suspendu' });
    expect(link).toHaveAttribute('href', `/competitions/${competitionId}`);
  });

  it.each([
    ['Draft', 'Brouillon'],
    ['Ready', 'Prêt'],
    ['Running', 'En cours'],
    ['Completed', 'Terminé'],
    ['Archived', 'Archivé'],
  ] as const)('keeps the %s status badge inert', async (status, label) => {
    mockCompetitionStatus(status);
    renderWithShell(`/competitions/${competitionId}/structure`);

    const badge = await screen.findByText(label);
    expect(badge.closest('a')).toBeNull();
    expect(screen.queryByRole('link', { name: label })).not.toBeInTheDocument();
  });

  it('does not change the route when rendering the header', async () => {
    renderWithShell('/');

    await waitFor(() => {
      expect(screen.getByText('Choisir une compétition')).toBeInTheDocument();
    });
    expect(
      screen.getByRole('heading', { name: 'Play’Up' }),
    ).toBeInTheDocument();
  });
});

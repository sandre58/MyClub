import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import {
  ApiError,
  fetchCompetitionDetail,
  fetchCompetitions,
  fetchMatchDetail,
  fetchNeedsAttention,
  fetchStageOverview,
} from '../api';
import { AppLayout } from '../AppLayout';

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
            <Route path="/stages/:stageId" element={<p>Stage page</p>} />
            <Route path="/matches/:matchId" element={<p>Match page</p>} />
          </Route>
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('Shell chrome context states', () => {
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
    });
    vi.mocked(fetchNeedsAttention).mockResolvedValue({
      competitionId,
      items: [],
      count: 0,
    });
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

  it('shows a stable loading state while competition context resolves', () => {
    vi.mocked(fetchCompetitionDetail).mockReturnValue(new Promise(() => {}));
    renderWithShell(`/competitions/${competitionId}`);

    expect(screen.getByText('Chargement')).toBeInTheDocument();
    expect(document.querySelector('.shell-header')?.textContent).not.toContain(
      'Compétition',
    );
  });

  it('shows unavailable context without inventing a competition name', async () => {
    vi.mocked(fetchCompetitionDetail).mockRejectedValue(
      new ApiError(404, 'Competition was not found.'),
    );
    renderWithShell(`/competitions/${competitionId}`);

    expect(
      await screen.findByText('Contexte indisponible'),
    ).toBeInTheDocument();
    expect(document.querySelector('.shell-header')?.textContent).not.toContain(
      'Compétition',
    );
  });

  it('routes sidebar competition links to the list without context', async () => {
    renderWithShell('/');

    await screen.findByText('Choisir une compétition');
    expect(screen.getByRole('link', { name: 'Structure' })).toHaveAttribute(
      'href',
      '/',
    );
    expect(
      screen.getByRole('link', { name: 'Calendrier & matchs' }),
    ).toHaveAttribute('href', '/');
  });

  it('updates sidebar organisation link after a stage deep link resolves', async () => {
    renderWithShell(`/stages/${stageId}`);

    await screen.findByText('Coupe U18');
    expect(screen.getByRole('link', { name: 'Structure' })).toHaveAttribute(
      'href',
      `/competitions/${competitionId}/organisation`,
    );
  });

  it('updates sidebar organisation link after a match deep link resolves', async () => {
    renderWithShell(`/matches/${matchId}`);

    await screen.findByText('Coupe U18');
    expect(screen.getByRole('link', { name: 'Structure' })).toHaveAttribute(
      'href',
      `/competitions/${competitionId}/organisation`,
    );
  });

  it('keeps À traiter visible but disabled at 0 without competition context', async () => {
    renderWithShell('/');

    expect(
      await screen.findByRole('button', { name: 'À traiter, aucun élément' }),
    ).toBeDisabled();
  });

  it('keeps À traiter disabled when the host has no competitions', async () => {
    vi.mocked(fetchCompetitions).mockResolvedValue([]);
    renderWithShell('/');

    await screen.findByText('Aucune compétition');
    expect(
      screen.getByRole('button', { name: 'À traiter, aucun élément' }),
    ).toBeDisabled();
  });

  it('keeps À traiter disabled while deep-link competition context resolves', async () => {
    vi.mocked(fetchStageOverview).mockReturnValue(new Promise(() => {}));
    renderWithShell(`/stages/${stageId}`);

    expect(
      await screen.findByRole('button', { name: 'À traiter, aucun élément' }),
    ).toBeDisabled();
  });
});

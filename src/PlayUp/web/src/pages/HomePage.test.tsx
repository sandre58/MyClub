import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError, createCompetition, fetchCompetitions } from '../api';
import type { CompetitionListItem, WorkspaceSummary } from '../types';
import { HomePage } from './HomePage';

vi.mock('../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../api')>();
  return {
    ...actual,
    fetchCompetitions: vi.fn(),
    createCompetition: vi.fn(),
  };
});

const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

function listItem(
  overrides: Partial<CompetitionListItem> = {},
): CompetitionListItem {
  return {
    id: competitionId,
    name: 'Spring Cup',
    status: 'Draft',
    ...overrides,
  };
}

function createdSummary(
  overrides: Partial<WorkspaceSummary> = {},
): WorkspaceSummary {
  return {
    id: competitionId,
    name: 'New Cup',
    status: 'Draft',
    nextActionCode: 'ContinueOrganisation',
    attentionCount: 0,
    completionMode: null,
    canCompleteNormally: false,
    completionBlockers: null,
    ...overrides,
  };
}

function renderHomePage() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/']}>
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route
            path="/competitions/:competitionId"
            element={<p>Vue d'ensemble route</p>}
          />
          <Route
            path="/competitions/:competitionId/organisation"
            element={<p>Organisation route</p>}
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('HomePage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders Accueil hub chrome outside Shell (ds-root, no marketing vestibule)', async () => {
    vi.mocked(fetchCompetitions).mockResolvedValue([]);

    renderHomePage();

    expect(document.querySelector('.ds-root.ds-home')).toBeInTheDocument();
    expect(document.querySelector('.shell')).not.toBeInTheDocument();
    expect(
      screen.getByRole('heading', { name: /Play’Up|Play'Up/i }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('heading', { name: 'Bienvenue' }),
    ).not.toBeInTheDocument();
    expect(
      await screen.findByRole('button', { name: /Créer une compétition/i }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: 'Ouvrir les préférences' }),
    ).toBeInTheDocument();
  });

  it('shows loading while the list is pending', () => {
    vi.mocked(fetchCompetitions).mockReturnValue(new Promise(() => {}));

    renderHomePage();

    const status = screen.getByRole('status');
    expect(status).toHaveTextContent('Chargement…');
    expect(status).toHaveClass('ds-wait--home');
  });

  it('shows first-run empty state with create CTA', async () => {
    vi.mocked(fetchCompetitions).mockResolvedValue([]);

    renderHomePage();

    expect(await screen.findByText(/Un nom suffit/i)).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /Créer une compétition/i }),
    ).toBeInTheDocument();
    expect(
      screen.queryByText(/Votre première compétition/i),
    ).not.toBeInTheDocument();
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it("renders competition rows with status and navigates to Vue d'ensemble", async () => {
    const user = userEvent.setup();
    vi.mocked(fetchCompetitions).mockResolvedValue([
      listItem({ status: 'Running' }),
      listItem({
        id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
        name: 'Autumn League',
        status: 'Ready',
      }),
    ]);

    renderHomePage();

    expect(
      await screen.findByRole('link', { name: /Spring Cup/i }),
    ).toBeInTheDocument();
    expect(
      screen.getByText(/Choisissez une compétition ou créez-en une nouvelle/i),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('heading', { name: /^Compétitions$/i }),
    ).toBeInTheDocument();
    expect(screen.getByText('En cours')).toBeInTheDocument();
    expect(screen.getByText('Autumn League')).toBeInTheDocument();
    expect(screen.getByText('Prêt')).toBeInTheDocument();

    await user.click(screen.getByRole('link', { name: /Spring Cup/i }));
    expect(screen.getByText("Vue d'ensemble route")).toBeInTheDocument();
  });

  it('shows declared schedule when present and omits the line when unset', async () => {
    vi.mocked(fetchCompetitions).mockResolvedValue([
      listItem({
        scheduledStart: '2026-10-04T00:00:00.000Z',
      }),
      listItem({
        id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
        name: 'No Dates Cup',
        status: 'Draft',
      }),
    ]);

    renderHomePage();

    expect(await screen.findByText(/À partir du/)).toBeInTheDocument();
    expect(
      screen.getByRole('link', { name: /No Dates Cup/i }),
    ).toBeInTheDocument();
    expect(screen.queryByText(/Jusqu’au|Jusqu'au/)).not.toBeInTheDocument();
  });

  it('keeps create CTA available when the list read fails (I4)', async () => {
    vi.mocked(fetchCompetitions).mockRejectedValue(
      new ApiError(500, 'Host unavailable'),
    );

    renderHomePage();

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Host unavailable (500)',
    );
    expect(
      screen.getByRole('button', { name: /Créer une compétition/i }),
    ).toBeInTheDocument();
  });

  it('opens name dialog from CTA and creates then navigates to Organisation', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchCompetitions).mockResolvedValue([]);
    vi.mocked(createCompetition).mockResolvedValue(
      createdSummary({ name: 'Tournoi printemps' }),
    );

    renderHomePage();

    await user.click(
      await screen.findByRole('button', { name: /Créer une compétition/i }),
    );

    const dialog = await screen.findByRole('dialog');
    expect(
      within(dialog).getByRole('heading', { name: /Nouvelle compétition/i }),
    ).toBeInTheDocument();
    expect(dialog.querySelector('.ds-form')).toBeInTheDocument();
    expect(within(dialog).getByText('0/100')).toBeInTheDocument();

    await user.type(
      within(dialog).getByRole('textbox', { name: /Nom/i }),
      'Tournoi printemps',
    );
    expect(within(dialog).getByText('17/100')).toBeInTheDocument();
    await user.click(within(dialog).getByRole('button', { name: /^Créer$/i }));

    await waitFor(() => {
      expect(createCompetition).toHaveBeenCalledWith({
        name: 'Tournoi printemps',
      });
    });

    expect(await screen.findByText('Organisation route')).toBeInTheDocument();
  });

  it('shows API error inside dialog and does not navigate on create failure', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchCompetitions).mockResolvedValue([]);
    vi.mocked(createCompetition).mockRejectedValue(
      new ApiError(400, 'Competition name cannot be empty.'),
    );

    renderHomePage();

    await user.click(
      await screen.findByRole('button', { name: /Créer une compétition/i }),
    );
    const dialog = await screen.findByRole('dialog');
    await user.type(within(dialog).getByRole('textbox', { name: /Nom/i }), 'X');
    await user.click(within(dialog).getByRole('button', { name: /^Créer$/i }));

    expect(await within(dialog).findByRole('alert')).toBeInTheDocument();
    expect(screen.queryByText('Organisation route')).not.toBeInTheDocument();
  });

  it('closes dialog on Escape and returns focus to create CTA', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchCompetitions).mockResolvedValue([listItem()]);

    renderHomePage();

    const createButton = await screen.findByRole('button', {
      name: /Créer une compétition/i,
    });
    await user.click(createButton);
    expect(await screen.findByRole('dialog')).toBeInTheDocument();

    await user.keyboard('{Escape}');
    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
    expect(createButton).toHaveFocus();
  });

  it('keeps submit disabled when the name is blank', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchCompetitions).mockResolvedValue([]);

    renderHomePage();

    await user.click(
      await screen.findByRole('button', { name: /Créer une compétition/i }),
    );
    expect(
      within(await screen.findByRole('dialog')).getByRole('button', {
        name: /^Créer$/i,
      }),
    ).toBeDisabled();
  });
});

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
import { HomePage } from '../pages/HomePage';
import type { NeedsAttentionItem } from '../types';

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

const oneItem: Partial<NeedsAttentionItem> = {
  source: 'ProgressionPending',
  severity: 'Blocking',
  targetType: 'Stage',
  targetId: stageId,
};

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
            <Route path="/" element={<HomePage />} />
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

describe('AttentionDrawer', () => {
  beforeEach(() => {
    vi.mocked(fetchCompetitions).mockResolvedValue([
      { id: competitionId, name: 'Coupe U18', status: 'Running' },
    ]);
    vi.mocked(fetchCompetitionDetail).mockResolvedValue({
      id: competitionId,
      name: 'Coupe U18',
      status: 'Running',
      entries: [],
      stages: [{ stageId, name: 'Group stage', status: 'Running' }],
    });
    vi.mocked(fetchNeedsAttention).mockResolvedValue(needsAttention([oneItem]));
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

  it('is closed by default', () => {
    renderWithShell(`/competitions/${competitionId}`);

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('disables the header trigger when count is 0', async () => {
    vi.mocked(fetchNeedsAttention).mockResolvedValue(needsAttention());
    renderWithShell(`/competitions/${competitionId}`);

    const trigger = await screen.findByRole('button', {
      name: 'À traiter, aucun élément',
    });
    expect(trigger).toBeDisabled();
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('opens from the header trigger when count is greater than 0', async () => {
    const user = userEvent.setup();
    renderWithShell(`/competitions/${competitionId}`);

    await user.click(
      await screen.findByRole('button', { name: 'À traiter, 1 élément' }),
    );

    expect(
      screen.getByRole('dialog', { name: /À traiter/ }),
    ).toBeInTheDocument();
    expect(
      document.querySelector('.shell-attention-drawer__count'),
    ).toHaveTextContent('1');
  });

  it('lists attention items when count is greater than 0', async () => {
    const user = userEvent.setup();
    renderWithShell(`/competitions/${competitionId}`);

    await user.click(
      await screen.findByRole('button', { name: 'À traiter, 1 élément' }),
    );

    expect(
      await screen.findByText('Progression en attente'),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('heading', { name: 'Bloquant' }),
    ).toBeInTheDocument();
    expect(screen.getByText(/Phase/i)).toBeInTheDocument();
    expect(
      screen.getByText('Situations qui demandent une action.'),
    ).toBeInTheDocument();
  });

  it('lists multiple blocking attention items together', async () => {
    vi.mocked(fetchNeedsAttention).mockResolvedValue(
      needsAttention([
        {
          source: 'QualificationPending',
          severity: 'Blocking',
          targetType: 'Stage',
          targetId: stageId,
        },
        oneItem,
      ]),
    );

    const user = userEvent.setup();
    renderWithShell(`/competitions/${competitionId}`);

    await user.click(
      await screen.findByRole('button', { name: 'À traiter, 2 éléments' }),
    );

    expect(
      await screen.findByRole('heading', { name: 'Bloquant' }),
    ).toBeInTheDocument();
    expect(screen.getByText('Progression en attente')).toBeInTheDocument();
    expect(screen.getByText(/Qualification en attente/i)).toBeInTheDocument();
    expect(
      screen.queryByRole('heading', { name: 'À traiter', level: 3 }),
    ).not.toBeInTheDocument();
  });

  it('closes via the close button', async () => {
    const user = userEvent.setup();
    renderWithShell(`/competitions/${competitionId}`);

    await user.click(
      await screen.findByRole('button', { name: 'À traiter, 1 élément' }),
    );
    await user.click(screen.getByRole('button', { name: 'Fermer' }));

    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
  });

  it('closes via Escape', async () => {
    const user = userEvent.setup();
    renderWithShell(`/competitions/${competitionId}`);

    await user.click(
      await screen.findByRole('button', { name: 'À traiter, 1 élément' }),
    );
    await user.keyboard('{Escape}');

    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
  });

  it('navigates to an item route and closes the drawer', async () => {
    const user = userEvent.setup();
    renderWithShell(`/competitions/${competitionId}`);

    await user.click(
      await screen.findByRole('button', { name: 'À traiter, 1 élément' }),
    );
    await user.click(
      await screen.findByRole('link', { name: /Progression en attente/i }),
    );

    expect(await screen.findByText('Stage page')).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
  });

  it('moves focus into the drawer when opened', async () => {
    const user = userEvent.setup();
    renderWithShell(`/competitions/${competitionId}`);

    const trigger = await screen.findByRole('button', {
      name: 'À traiter, 1 élément',
    });
    await user.click(trigger);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Fermer' })).toHaveFocus();
    });
  });

  it('returns focus to the trigger when closed', async () => {
    const user = userEvent.setup();
    renderWithShell(`/competitions/${competitionId}`);

    const trigger = await screen.findByRole('button', {
      name: 'À traiter, 1 élément',
    });
    await user.click(trigger);
    await user.click(screen.getByRole('button', { name: 'Fermer' }));

    await waitFor(() => {
      expect(trigger).toHaveFocus();
    });
  });

  it('keeps the trigger disabled without competition context', async () => {
    renderWithShell('/');

    expect(
      await screen.findByRole('button', { name: 'À traiter, aucun élément' }),
    ).toBeDisabled();
  });

  it('works on a stage deep link with resolved competition context', async () => {
    vi.mocked(fetchNeedsAttention).mockResolvedValue(
      needsAttention([
        {
          source: 'QualificationPending',
          severity: 'Blocking',
          targetType: 'Stage',
          targetId: stageId,
        },
      ]),
    );

    const user = userEvent.setup();
    renderWithShell(`/stages/${stageId}`);

    await user.click(
      await screen.findByRole('button', { name: 'À traiter, 1 élément' }),
    );

    expect(
      await screen.findByText(/Qualification en attente/i),
    ).toBeInTheDocument();
    expect(
      document.querySelector('.shell-attention-drawer__count'),
    ).toHaveTextContent('1');
    expect(
      document.querySelector('.ds-interactive-row.shell-attention-drawer__row'),
    ).toBeTruthy();
  });

  it('closes via the backdrop', async () => {
    const user = userEvent.setup();
    renderWithShell(`/competitions/${competitionId}`);

    await user.click(
      await screen.findByRole('button', { name: 'À traiter, 1 élément' }),
    );
    await user.click(screen.getByRole('button', { name: 'Fermer À traiter' }));

    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
  });

  it('isolates the shell frame while open', async () => {
    const user = userEvent.setup();
    renderWithShell(`/competitions/${competitionId}`);

    expect(document.querySelector('.shell__frame')).not.toHaveAttribute(
      'inert',
    );

    await user.click(
      await screen.findByRole('button', { name: 'À traiter, 1 élément' }),
    );

    expect(document.querySelector('.shell__frame')).toHaveAttribute('inert');
    expect(document.body.style.overflow).toBe('');
    expect(document.querySelector('.shell')?.scrollLeft).toBe(0);
  });

  it('does not introduce a new /attention route', async () => {
    const user = userEvent.setup();
    renderWithShell(`/competitions/${competitionId}`);

    await user.click(
      await screen.findByRole('button', { name: 'À traiter, 1 élément' }),
    );

    expect(screen.getByText('Workspace page')).toBeInTheDocument();
    expect(
      screen.getByRole('dialog', { name: /À traiter/ }),
    ).toBeInTheDocument();
  });

  it('routes Fixture attention items to the matches hub without matchId', async () => {
    const fixtureId = 'ffffffff-ffff-ffff-ffff-ffffffffffff';
    vi.mocked(fetchNeedsAttention).mockResolvedValue(
      needsAttention([
        {
          source: 'ProgressionPending',
          severity: 'Blocking',
          targetType: 'Fixture',
          targetId: fixtureId,
        },
      ]),
    );

    const user = userEvent.setup();
    renderWithShell(`/competitions/${competitionId}`);

    await user.click(
      await screen.findByRole('button', { name: 'À traiter, 1 élément' }),
    );
    const link = await screen.findByRole('link', {
      name: /Progression en attente/i,
    });
    expect(link).toHaveAttribute(
      'href',
      `/competitions/${competitionId}/matches`,
    );
  });
});

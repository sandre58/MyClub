import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fetchStageOverview } from '../../api';
import { StageLegacyRedirect } from './StageLegacyRedirect';

vi.mock('../../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../api')>();
  return {
    ...actual,
    fetchStageOverview: vi.fn(),
  };
});

const stageId = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

function renderRedirect(initialEntry = `/stages/${stageId}`) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[initialEntry]}>
        <Routes>
          <Route path="/" element={<p>Home</p>} />
          <Route
            path="/competitions/:competitionId/structure"
            element={<p>Structure hub</p>}
          />
          <Route path="/stages/:stageId" element={<StageLegacyRedirect />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('StageLegacyRedirect', () => {
  beforeEach(() => {
    vi.mocked(fetchStageOverview).mockResolvedValue({
      id: stageId,
      competitionId,
      name: 'League',
      status: 'Draft',
      rounds: [],
      slots: [],
      draws: [],
    });
  });

  it('redirects bound stages to Structure with stage query param', async () => {
    renderRedirect();

    expect(await screen.findByText('Structure hub')).toBeInTheDocument();
  });

  it('sends unbound stages to Accueil', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue({
      id: stageId,
      competitionId: '',
      name: 'Orphan',
      status: 'Draft',
      rounds: [],
      slots: [],
      draws: [],
    });

    renderRedirect();

    expect(await screen.findByText('Home')).toBeInTheDocument();
  });
});

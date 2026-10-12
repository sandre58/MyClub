import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fetchNeedsAttention } from '../../api';
import { StructureAnomalyHint } from './StructureAnomalyHint';

vi.mock('../../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../api')>();
  return {
    ...actual,
    fetchNeedsAttention: vi.fn(),
  };
});

const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

function renderHint(allowGenericOnError = false) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <StructureAnomalyHint
        competitionId={competitionId}
        allowGenericOnError={allowGenericOnError}
      />
    </QueryClientProvider>,
  );
}

describe('StructureAnomalyHint', () => {
  beforeEach(() => {
    vi.mocked(fetchNeedsAttention).mockResolvedValue({
      competitionId,
      count: 0,
      items: [],
    });
  });

  it('renders nothing when attention count is zero', async () => {
    renderHint();
    await waitFor(() => {
      expect(fetchNeedsAttention).toHaveBeenCalled();
    });
    expect(
      screen.queryByText(/Des anomalies nécessitent votre attention/i),
    ).not.toBeInTheDocument();
  });

  it('shows counted hint aligned with drawer', async () => {
    vi.mocked(fetchNeedsAttention).mockResolvedValue({
      competitionId,
      count: 2,
      items: [
        {
          source: 'CycleDetected',
          severity: 'Blocking',
          targetType: 'Stage',
          targetId: 's1',
        },
        {
          source: 'SlotFeedMissing',
          severity: 'Blocking',
          targetType: 'Stage',
          targetId: 's2',
        },
      ],
    });

    renderHint();

    expect(
      await screen.findByText(/Des anomalies nécessitent votre attention/i),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('link', { name: /Corriger/i }),
    ).not.toBeInTheDocument();
  });

  it('shows generic copy on fetch error when fallback allowed', async () => {
    vi.mocked(fetchNeedsAttention).mockRejectedValue(new Error('network'));

    renderHint(true);

    expect(
      await screen.findByText(/Des anomalies nécessitent votre attention/i),
    ).toBeInTheDocument();
  });
});

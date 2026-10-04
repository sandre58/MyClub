import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { dematerializePhase } from '../../api';
import { StructureDematerializeHero } from './StructureDematerializeHero';

vi.mock('../../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../api')>();
  return {
    ...actual,
    dematerializePhase: vi.fn(),
  };
});

function renderHero(dematerialize: {
  enabled: boolean;
  scope: 'FullPhase' | 'LastSwissRound';
  disabledReason?: string;
}) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <StructureDematerializeHero
        competitionId="aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
        stageId="cccccccc-cccc-cccc-cccc-cccccccccccc"
        dematerialize={dematerialize}
      />
    </QueryClientProvider>,
  );
}

describe('StructureDematerializeHero', () => {
  beforeEach(() => {
    vi.mocked(dematerializePhase).mockReset();
  });

  it('renders enabled full-phase CTA and calls dematerialize', async () => {
    vi.mocked(dematerializePhase).mockResolvedValue({
      removedMatchCount: 3,
      removedRoundNumber: null,
    });

    renderHero({ enabled: true, scope: 'FullPhase' });

    const button = screen.getByRole('button', { name: /Retirer les matchs/i });
    expect(button).toHaveAttribute('data-tone', 'destructive');

    await userEvent.click(button);
    expect(dematerializePhase).toHaveBeenCalledWith('cccccccc-cccc-cccc-cccc-cccccccccccc');
  });

  it('shows disabled caption from read reason', () => {
    renderHero({
      enabled: false,
      scope: 'FullPhase',
      disabledReason: 'MatchLifecycleStarted',
    });

    expect(
      screen.getByText(/Un match concerné a déjà été joué ou est en cours/i),
    ).toBeInTheDocument();
  });
});

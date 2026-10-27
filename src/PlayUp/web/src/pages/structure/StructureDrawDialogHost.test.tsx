import { readFileSync } from 'node:fs';
import path from 'node:path';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { fetchStageOverview } from '../../api';
import type { StageOverview, StructureStageHubSummary } from '../../types';
import { StructureDrawDialogHost } from './StructureDrawDialogHost';

vi.mock('../../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../api')>();
  return {
    ...actual,
    fetchStageOverview: vi.fn(),
  };
});

const stage = {
  stageId: 'stage-1',
  name: 'League',
  status: 'Ready',
  teamCount: 2,
  matchCount: 0,
  groupCount: 0,
  roundCount: 0,
  numberOfPeriods: 2,
  durationPerPeriod: 45,
  hasExtraTime: false,
  hasPenaltyShootout: false,
  hasStandingRules: true,
  hasDrawRules: true,
  hasQualificationRules: false,
  qualificationPathCount: 0,
  hasProgressionRules: false,
  progressionPathCount: 0,
  hasTieFormat: false,
  formatKind: 'Championship',
  defaultsBinding: {
    matchDuration: { isBound: true },
    extraTime: { isBound: true },
    penaltyShootout: { isBound: true },
    administrativeResult: { isBound: true },
    points: { isBound: true },
    rankingCriteria: { isBound: true },
  },
} as StructureStageHubSummary;

function overview(): StageOverview {
  return {
    id: stage.stageId,
    competitionId: 'comp-1',
    name: stage.name,
    status: 'Ready',
    slots: [],
    rounds: [],
    draws: [],
    bracketPairs: [],
  };
}

function renderHost(open: boolean, onClose = vi.fn()) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  function Wrapper({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={client}>{children}</QueryClientProvider>
    );
  }
  return render(
    <StructureDrawDialogHost
      open={open}
      onClose={onClose}
      competitionId="comp-1"
      competitionStatus="Ready"
      stage={stage}
      structureStages={[stage]}
    />,
    { wrapper: Wrapper },
  );
}

describe('StructureDrawDialogHost', () => {
  it('keeps the draw dialog out of the Structure fiche static graph', () => {
    const fiche = readFileSync(
      path.join(process.cwd(), 'src/pages/structure/StructurePhaseFiche.tsx'),
      'utf8',
    );
    const host = readFileSync(
      path.join(
        process.cwd(),
        'src/pages/structure/StructureDrawDialogHost.tsx',
      ),
      'utf8',
    );

    expect(fiche).not.toContain("from './StructureDrawDialog'");
    expect(fiche).toContain('StructureDrawDialogHost');
    expect(host).toContain("import('./StructureDrawDialog')");
  });

  it('loads the dialog on open and keeps it through the close transition', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    vi.mocked(fetchStageOverview).mockResolvedValue(overview());
    const view = renderHost(false, onClose);

    expect(
      screen.queryByRole('dialog', { name: /Exécutions de tirage/i }),
    ).not.toBeInTheDocument();

    view.rerender(
      <StructureDrawDialogHost
        open
        onClose={onClose}
        competitionId="comp-1"
        competitionStatus="Ready"
        stage={stage}
        structureStages={[stage]}
      />,
    );

    expect(
      await screen.findByRole('dialog', { name: /Exécutions de tirage/i }),
    ).toBeVisible();
    expect(await screen.findByText('Aucune exécution')).toBeInTheDocument();

    const dialog = screen.getByRole('dialog', {
      name: /Exécutions de tirage/i,
    });
    const close = dialog.querySelector<HTMLButtonElement>(
      '.ds-dialog__header button',
    );
    expect(close).not.toBeNull();
    await user.click(close!);
    expect(onClose).toHaveBeenCalledTimes(1);

    view.rerender(
      <StructureDrawDialogHost
        open={false}
        onClose={onClose}
        competitionId="comp-1"
        competitionStatus="Ready"
        stage={stage}
        structureStages={[stage]}
      />,
    );

    expect(
      screen.getByRole('dialog', { name: /Exécutions de tirage/i }),
    ).toBeInTheDocument();

    await waitFor(() => {
      expect(
        screen.queryByRole('dialog', { name: /Exécutions de tirage/i }),
      ).not.toBeInTheDocument();
    });
  });
});

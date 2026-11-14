import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import {
  confirmDefinitiveResolutionRelease,
  previewDefinitiveResolutionRelease,
  previewDeletedSourceDefinitiveRelease,
  type DefinitiveResolutionReleaseResponse,
} from '../../../api';
import { ApiError } from '../../../api/http';
import type {
  StructureResolutionOccupant,
  StructureStageHubSummary,
} from '../../../types';
import { I18nTestProvider } from '../../../test/renderWithI18n';
import { scheduleStructureRefresh } from '../structureInvalidation';
import { StructureDefinitiveReleaseBanner } from './StructureDefinitiveReleaseBanner';

vi.mock('../structureInvalidation', () => ({
  scheduleStructureRefresh: vi.fn(),
}));

vi.mock('../../../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../../api')>();
  return {
    ...actual,
    previewDefinitiveResolutionRelease: vi.fn(),
    confirmDefinitiveResolutionRelease: vi.fn(),
    previewDeletedSourceDefinitiveRelease: vi.fn(),
    confirmDeletedSourceDefinitiveRelease: vi.fn(),
  };
});

const sourceId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
const destinationId = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
const nord = 'cccccccc-cccc-cccc-cccc-cccccccccccc';
const sud = 'dddddddd-dddd-dddd-dddd-dddddddddddd';

function occupant(
  entryId: string,
  slotKey: string,
): StructureResolutionOccupant {
  return {
    target: 'Slot',
    entryId,
    provenanceKind: 'Qualification',
    sourcePathId: 'Q|path',
    state: 'Indetermine',
    slotKey,
    indeterminacyCause: 'SelectionVide',
    upstreamStageId: sourceId,
    cascadeReplaceable: false,
    releasable: true,
    definitive: true,
  };
}

const preview: DefinitiveResolutionReleaseResponse = {
  versionToken: 'token-1',
  releases: [
    {
      stageId: destinationId,
      target: 'Slot',
      slotKey: 'A',
      groupId: null,
      entryId: nord,
      entryName: 'Nord',
      stageName: 'Terminal',
      placeLabel: 'A',
      populationRemoved: true,
      sideEffect: false,
    },
  ],
  detachedMatches: [
    {
      matchId: 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee',
      stageId: destinationId,
      legIndex: 1,
      side: 'Home',
      homeEntryId: nord,
      awayEntryId: sud,
      homeName: 'Nord',
      awayName: 'Sud',
    },
  ],
  exclusions: [
    {
      stageId: destinationId,
      target: 'Slot',
      slotKey: 'B',
      groupId: null,
      entryId: sud,
      entryName: 'Sud',
      stageName: 'Terminal',
      placeLabel: 'B',
      code: 'Application.ResolutionReleaseBlockedByCancelledMatch',
    },
  ],
  affectedOccupants: [],
};

function stage(): StructureStageHubSummary {
  return {
    stageId: destinationId,
    name: 'Terminal',
    resolutionOccupants: [occupant(nord, 'A'), occupant(sud, 'B')],
  } as StructureStageHubSummary;
}

function renderBanner(
  occupants = stage().resolutionOccupants,
  knownStages: StructureStageHubSummary[] = [
    { stageId: sourceId, name: 'League' } as StructureStageHubSummary,
    stage(),
  ],
) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={client}>
      <I18nTestProvider>{children}</I18nTestProvider>
    </QueryClientProvider>
  );
  const view = render(
    <StructureDefinitiveReleaseBanner
      stage={{ ...stage(), resolutionOccupants: occupants }}
      stages={knownStages}
      competitionId="competition-1"
    />,
    { wrapper },
  );
  return { ...view, client };
}

describe('StructureDefinitiveReleaseBanner', () => {
  beforeEach(() => {
    vi.mocked(previewDefinitiveResolutionRelease).mockReset();
    vi.mocked(confirmDefinitiveResolutionRelease).mockReset();
    vi.mocked(previewDeletedSourceDefinitiveRelease).mockReset();
    vi.mocked(scheduleStructureRefresh).mockReset();
  });

  it('counts releasable places and lists the ones the preview excludes', async () => {
    vi.mocked(previewDefinitiveResolutionRelease).mockResolvedValue(preview);
    renderBanner();

    expect(
      await screen.findByText('1 place ne pourra plus être confirmée.'),
    ).toBeInTheDocument();
    expect(
      screen.getByText(/Sud — B : Un match annulé implique cette équipe/),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: 'Libérer…' }),
    ).toBeInTheDocument();
    expect(previewDefinitiveResolutionRelease).toHaveBeenCalledWith(sourceId, {
      destinationStageId: destinationId,
    });
  });

  it('keeps the summary until the dialog is closed', async () => {
    vi.mocked(previewDefinitiveResolutionRelease).mockResolvedValue(preview);
    vi.mocked(confirmDefinitiveResolutionRelease).mockResolvedValue(preview);
    const user = userEvent.setup();
    renderBanner();

    await user.click(await screen.findByRole('button', { name: 'Libérer…' }));
    const confirmDialog = await screen.findByRole('dialog', {
      name: 'Libérer ces places ?',
    });
    expect(
      within(confirmDialog).getByText(
        /Manche 1 \(Nord – Sud\) : le côté domicile redevient à déterminer/,
      ),
    ).toBeInTheDocument();
    await user.click(
      within(confirmDialog).getByRole('button', { name: /^Libérer$/ }),
    );

    const summary = await screen.findByRole('dialog', {
      name: 'Places libérées',
    });
    expect(
      within(summary).getByText(/Nord — A \(Terminal\)/),
    ).toBeInTheDocument();
    expect(
      within(summary).getByText(/Population retirée : Nord/),
    ).toBeInTheDocument();
    expect(scheduleStructureRefresh).not.toHaveBeenCalled();

    await user.click(within(summary).getByRole('button', { name: 'Fermer' }));
    expect(scheduleStructureRefresh).toHaveBeenCalled();
  });

  it('shows a conflict and refreshes the structure', async () => {
    vi.mocked(previewDefinitiveResolutionRelease).mockResolvedValue(preview);
    vi.mocked(confirmDefinitiveResolutionRelease).mockRejectedValue(
      new ApiError(
        409,
        'conflict',
        'changed',
        'Application.ResolutionReleaseBatchChanged',
      ),
    );
    const user = userEvent.setup();
    renderBanner();

    await user.click(await screen.findByRole('button', { name: 'Libérer…' }));
    await user.click(screen.getByRole('button', { name: 'Libérer' }));

    expect(
      await screen.findByText(/Les places à libérer ont changé/),
    ).toBeInTheDocument();
    expect(scheduleStructureRefresh).toHaveBeenCalled();
    expect(
      screen.getByRole('heading', { name: 'Libérer ces places ?' }),
    ).toBeInTheDocument();
  });

  it('keeps the summary when a refetch returns an empty preview', async () => {
    vi.mocked(previewDefinitiveResolutionRelease).mockResolvedValue(preview);
    vi.mocked(confirmDefinitiveResolutionRelease).mockResolvedValue(preview);
    const user = userEvent.setup();
    const { client } = renderBanner();

    await user.click(await screen.findByRole('button', { name: 'Libérer…' }));
    await user.click(screen.getByRole('button', { name: /^Libérer$/ }));
    expect(
      await screen.findByRole('dialog', { name: 'Places libérées' }),
    ).toBeVisible();

    vi.mocked(previewDefinitiveResolutionRelease).mockResolvedValue({
      ...preview,
      releases: [],
      exclusions: [],
    });
    await client.invalidateQueries({ queryKey: ['stages'] });
    await waitFor(() => {
      expect(previewDefinitiveResolutionRelease).toHaveBeenCalledTimes(2);
    });
    const summary = screen.getByRole('dialog', { name: 'Places libérées' });
    expect(summary).toBeVisible();
    expect(within(summary).getByText(/Nord — A \(Terminal\)/)).toBeVisible();
  });

  it('counts places whose source phase was deleted', async () => {
    vi.mocked(previewDeletedSourceDefinitiveRelease).mockResolvedValue(preview);
    renderBanner(
      [{ ...occupant(nord, 'A'), upstreamStageId: null }],
      [stage()],
    );

    expect(
      await screen.findByText('1 place ne pourra plus être confirmée.'),
    ).toBeInTheDocument();
    expect(previewDeletedSourceDefinitiveRelease).toHaveBeenCalledWith(
      destinationId,
    );
    expect(previewDefinitiveResolutionRelease).not.toHaveBeenCalled();
  });

  it('names a place without a label in French', async () => {
    vi.mocked(previewDefinitiveResolutionRelease).mockResolvedValue({
      ...preview,
      releases: [],
      exclusions: [
        {
          ...preview.exclusions[0],
          placeLabel: null,
          slotKey: null,
          target: 'Group',
        },
      ],
    });
    renderBanner();

    expect(await screen.findByText(/Sud — Groupe :/)).toBeInTheDocument();
  });
});

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { replaceStageAffectationAuthoring } from '../../../api';
import {
  clearToasts,
  getToastsSnapshot,
} from '../../../design-system/toastStore';
import type { StructureEntry, StructureStageHubSummary } from '../../../types';
import { StructureCompositionDialog } from './StructureCompositionDialog';

vi.mock('../../../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../../api')>();
  return {
    ...actual,
    replaceStageAffectationAuthoring: vi.fn(),
  };
});

const stageId = 'cccccccc-cccc-cccc-cccc-cccccccccccc';
const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
const entryId = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee';

function stage(): StructureStageHubSummary {
  return {
    stageId,
    name: 'Demi-finales',
    status: 'Draft',
    teamCount: 1,
    matchCount: 0,
    numberOfPeriods: 2,
    durationPerPeriod: 45,
    hasExtraTime: false,
    hasPenaltyShootout: false,
    hasDrawRules: false,
    hasQualificationRules: false,
    qualificationPathCount: 0,
    hasProgressionRules: false,
    progressionPathCount: 0,
    hasTieFormat: false,
    formatKind: 'Championship',
    compositionCapacity: 8,
    affectationEntryIds: [],
    isRootComposition: false,
  };
}

function entries(): StructureEntry[] {
  return [
    {
      entryId,
      displayName: 'Olympique',
      status: 'Active',
    },
  ];
}

function renderDialog(onClose = vi.fn()) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  render(
    <QueryClientProvider client={client}>
      <StructureCompositionDialog
        open
        onClose={onClose}
        competitionId={competitionId}
        stage={stage()}
        stages={[stage()]}
        entries={entries()}
      />
    </QueryClientProvider>,
  );
  return { onClose };
}

describe('StructureCompositionDialog', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    clearToasts();
    vi.mocked(replaceStageAffectationAuthoring).mockResolvedValue(undefined);
  });

  it('keeps the Équipes title and lede at size lg for a non-root phase', () => {
    renderDialog();

    expect(
      screen.getByRole('heading', { name: 'Équipes — Demi-finales' }),
    ).toBeInTheDocument();
    expect(
      screen.getByText(
        'Ajoutez des équipes à cette phase. Les places déjà attendues via Qualification / Progression sont réservées.',
      ),
    ).toBeInTheDocument();
    expect(screen.getByRole('dialog').closest('.ds-dialog')).toHaveAttribute(
      'data-size',
      'lg',
    );
  });

  it('toasts Équipes mises à jour and then closes after save', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn(() => {
      expect(getToastsSnapshot()).toEqual(
        expect.arrayContaining([
          expect.objectContaining({
            tone: 'success',
            message: 'Équipes mises à jour',
          }),
        ]),
      );
    });
    renderDialog(onClose);

    await user.click(screen.getByRole('checkbox', { name: /Olympique/ }));
    await user.click(screen.getByRole('button', { name: 'Enregistrer' }));

    await waitFor(() => {
      expect(replaceStageAffectationAuthoring).toHaveBeenCalledWith(stageId, [
        entryId,
      ]);
    });
    await waitFor(() => {
      expect(onClose).toHaveBeenCalledTimes(1);
    });
    expect(getToastsSnapshot()).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          tone: 'success',
          message: 'Équipes mises à jour',
        }),
      ]),
    );
  });
});

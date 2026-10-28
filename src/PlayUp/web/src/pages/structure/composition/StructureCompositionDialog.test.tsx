import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import {
  cleanup,
  render,
  screen,
  waitFor,
  within,
} from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { replaceStageAffectationAuthoring } from '../../../api';
import {
  clearToasts,
  getToastsSnapshot,
} from '../../../design-system/toastStore';
import type {
  EntryStatus,
  StructureEntry,
  StructureStageHubSummary,
} from '../../../types';
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

function stage(
  partial: Partial<StructureStageHubSummary> = {},
): StructureStageHubSummary {
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
    ...partial,
  };
}

function entry(
  id: string,
  displayName: string,
  status: EntryStatus = 'Active',
): StructureEntry {
  return { entryId: id, displayName, status };
}

function entries(): StructureEntry[] {
  return [entry(entryId, 'Olympique')];
}

function renderDialog({
  onClose = vi.fn(),
  entryList = entries(),
  affectationEntryIds = [] as string[],
  reservedFromFeeds = 0,
  compositionCapacity = 8,
}: {
  onClose?: () => void;
  entryList?: StructureEntry[];
  affectationEntryIds?: string[];
  reservedFromFeeds?: number;
  compositionCapacity?: number;
} = {}) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  const current = stage({ compositionCapacity, affectationEntryIds });
  render(
    <QueryClientProvider client={client}>
      <MemoryRouter>
        <StructureCompositionDialog
          open
          onClose={onClose}
          competitionId={competitionId}
          stage={current}
          stages={[current]}
          entries={entryList}
          reservedFromFeeds={reservedFromFeeds}
        />
      </MemoryRouter>
    </QueryClientProvider>,
  );
  return { onClose };
}

const amiens = '11111111-1111-1111-1111-111111111111';
const bordeaux = '22222222-2222-2222-2222-222222222222';
const caen = '33333333-3333-3333-3333-333333333333';
const dijon = '44444444-4444-4444-4444-444444444444';
const evian = '55555555-5555-5555-5555-555555555555';
const france = '66666666-6666-6666-6666-666666666666';
const withdrawnId = '77777777-7777-7777-7777-777777777777';

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
    renderDialog({ onClose });

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

  it('lists Sélectionnées then Disponibles and hides Sélectionnées when none are selected', () => {
    renderDialog({
      entryList: [entry(amiens, 'Amiens'), entry(bordeaux, 'Bordeaux')],
      affectationEntryIds: [amiens],
    });

    const selectedRegion = screen.getByRole('region', {
      name: 'Sélectionnées',
    });
    const availableRegion = screen.getByRole('region', { name: 'Disponibles' });
    expect(
      selectedRegion.compareDocumentPosition(availableRegion) &
        Node.DOCUMENT_POSITION_FOLLOWING,
    ).toBeTruthy();
    expect(
      within(selectedRegion).getByRole('checkbox', { name: /Amiens/ }),
    ).toBeChecked();
    expect(
      within(availableRegion).getByRole('checkbox', { name: /Bordeaux/ }),
    ).not.toBeChecked();
    expect(
      within(availableRegion).queryByRole('checkbox', { name: /Amiens/ }),
    ).not.toBeInTheDocument();

    cleanup();
    renderDialog();

    expect(
      screen.queryByRole('region', { name: 'Sélectionnées' }),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole('region', { name: 'Disponibles' }),
    ).toBeInTheDocument();
  });

  it('scopes Toutes, Sélectionnées, and Disponibles with search', async () => {
    const user = userEvent.setup();
    renderDialog({
      entryList: [
        entry(amiens, 'Amiens'),
        entry(bordeaux, 'Bordeaux'),
        entry(caen, 'Caen'),
      ],
      affectationEntryIds: [amiens],
    });

    await user.click(screen.getByRole('radio', { name: 'Disponibles' }));
    expect(
      screen.queryByRole('region', { name: 'Sélectionnées' }),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole('region', { name: 'Disponibles' }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('checkbox', { name: /Amiens/ }),
    ).not.toBeInTheDocument();

    const search = screen.getByRole('searchbox', {
      name: 'Rechercher une équipe',
    });
    await user.type(search, 'Amiens');
    expect(
      screen.getByText('Aucune équipe ne correspond à cette recherche'),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('checkbox', { name: /Bordeaux/ }),
    ).not.toBeInTheDocument();

    await user.clear(search);
    await user.type(search, 'Caen');
    const available = screen.getByRole('region', { name: 'Disponibles' });
    expect(
      within(available).getByRole('checkbox', { name: /Caen/ }),
    ).toBeInTheDocument();
    expect(
      within(available).queryByRole('checkbox', { name: /Bordeaux/ }),
    ).not.toBeInTheDocument();

    await user.click(screen.getByRole('radio', { name: 'Toutes' }));
    await user.clear(search);
    await user.type(search, 'en');
    expect(
      within(screen.getByRole('region', { name: 'Sélectionnées' })).getByRole(
        'checkbox',
        { name: /Amiens/ },
      ),
    ).toBeInTheDocument();
    expect(
      within(screen.getByRole('region', { name: 'Disponibles' })).getByRole(
        'checkbox',
        { name: /Caen/ },
      ),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('checkbox', { name: /Bordeaux/ }),
    ).not.toBeInTheDocument();

    await user.click(screen.getByRole('radio', { name: 'Sélectionnées' }));
    expect(
      screen.queryByRole('region', { name: 'Disponibles' }),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole('checkbox', { name: /Amiens/ }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('checkbox', { name: /Caen/ }),
    ).not.toBeInTheDocument();
  });

  it('selects every filtered team up to the affectation cap and meters k plus reserved', async () => {
    const user = userEvent.setup();
    renderDialog({
      compositionCapacity: 8,
      reservedFromFeeds: 3,
      entryList: [
        entry(amiens, 'Amiens'),
        entry(bordeaux, 'Bordeaux'),
        entry(caen, 'Caen'),
        entry(dijon, 'Dijon'),
        entry(evian, 'Evian'),
        entry(france, 'France'),
      ],
    });

    expect(screen.getByRole('img', { name: '3/8 places' })).toBeInTheDocument();
    expect(
      screen.getByText('3 places réservées par les sources'),
    ).toBeInTheDocument();
    expect(document.querySelector('.ds-alert--danger')).toBeNull();

    const selectAll = screen.getByRole('button', { name: 'Sélectionner tout' });
    expect(selectAll).toHaveTextContent('Sélectionner tout');
    expect(
      screen.queryByRole('button', { name: 'Tout sélectionner' }),
    ).not.toBeInTheDocument();

    const search = screen.getByRole('searchbox', {
      name: 'Rechercher une équipe',
    });
    await user.type(search, 'Caen');
    await user.click(selectAll);
    expect(screen.getByRole('checkbox', { name: /Caen/ })).toBeChecked();
    await user.clear(search);
    expect(screen.getByRole('checkbox', { name: /Amiens/ })).not.toBeChecked();
    expect(screen.getByRole('img', { name: '4/8 places' })).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Sélectionner tout' }));

    for (const name of ['Amiens', 'Bordeaux', 'Dijon', 'Evian']) {
      expect(
        screen.getByRole('checkbox', { name: new RegExp(name) }),
      ).toBeChecked();
    }
    expect(screen.getByRole('checkbox', { name: /France/ })).not.toBeChecked();
    expect(screen.getByRole('checkbox', { name: /France/ })).toBeDisabled();
    expect(
      screen.getByRole('button', { name: 'Sélectionner tout' }),
    ).toBeDisabled();
    expect(screen.getByRole('img', { name: '8/8 places' })).toBeInTheDocument();
    expect(screen.getByText('Complet')).toBeInTheDocument();
  });

  it('warns at capacity and names reserved source places without a danger tone', () => {
    renderDialog({
      compositionCapacity: 4,
      reservedFromFeeds: 2,
      affectationEntryIds: [amiens, bordeaux],
      entryList: [
        entry(amiens, 'Amiens'),
        entry(bordeaux, 'Bordeaux'),
        entry(caen, 'Caen'),
      ],
    });

    const guidance = screen.getByRole('status');
    expect(guidance).toHaveClass('ds-alert--warning');
    expect(guidance).not.toHaveClass('ds-alert--danger');
    expect(guidance).toHaveTextContent(
      'Plus de place pour des équipes (2 max).',
    );
    expect(guidance).toHaveTextContent('2 places réservées par les sources');
    expect(document.querySelector('.ds-alert--danger')).toBeNull();
    expect(screen.getByRole('img', { name: '4/4 places' })).toBeInTheDocument();
    expect(screen.getByRole('checkbox', { name: /Caen/ })).toBeDisabled();
  });

  it('shows a dedicated empty state when the search matches nothing', async () => {
    const user = userEvent.setup();
    renderDialog({
      entryList: [entry(amiens, 'Amiens'), entry(bordeaux, 'Bordeaux')],
      affectationEntryIds: [amiens],
    });

    await user.type(
      screen.getByRole('searchbox', { name: 'Rechercher une équipe' }),
      'zzz',
    );

    expect(
      screen.getByText('Aucune équipe ne correspond à cette recherche'),
    ).toBeInTheDocument();
    expect(screen.queryByRole('checkbox')).not.toBeInTheDocument();
    expect(
      screen.queryByRole('region', { name: 'Sélectionnées' }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('region', { name: 'Disponibles' }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByText('Aucune équipe éligible'),
    ).not.toBeInTheDocument();
  });

  it('keeps withdrawn teams in Sélectionnées and out of Disponibles', async () => {
    const user = userEvent.setup();
    renderDialog({
      entryList: [
        entry(amiens, 'Amiens'),
        entry(withdrawnId, 'Ancien', 'Withdrawn'),
      ],
      affectationEntryIds: [withdrawnId],
    });

    const selectedRegion = screen.getByRole('region', {
      name: 'Sélectionnées',
    });
    const availableRegion = screen.getByRole('region', { name: 'Disponibles' });
    expect(
      within(selectedRegion).getByRole('checkbox', { name: /Ancien/ }),
    ).toBeChecked();
    expect(
      within(selectedRegion).getByRole('img', {
        name: 'Équipe retirée de la compétition — ne participe plus à cette phase.',
      }),
    ).toBeInTheDocument();
    expect(
      within(availableRegion).queryByRole('checkbox', { name: /Ancien/ }),
    ).not.toBeInTheDocument();
    expect(
      within(availableRegion).getByRole('checkbox', { name: /Amiens/ }),
    ).toBeInTheDocument();

    await user.click(screen.getByRole('radio', { name: 'Disponibles' }));
    expect(
      screen.queryByRole('checkbox', { name: /Ancien/ }),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole('checkbox', { name: /Amiens/ }),
    ).toBeInTheDocument();

    await user.click(screen.getByRole('radio', { name: 'Sélectionnées' }));
    expect(
      screen.getByRole('checkbox', { name: /Ancien/ }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('checkbox', { name: /Amiens/ }),
    ).not.toBeInTheDocument();
  });
});

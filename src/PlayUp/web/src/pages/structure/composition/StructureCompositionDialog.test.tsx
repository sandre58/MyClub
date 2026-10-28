import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
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

const compositionCss = readFileSync(
  join(dirname(fileURLToPath(import.meta.url)), '../structure-composition.css'),
  'utf8',
);

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

function factOrder(): string[] {
  return [...document.querySelectorAll('[data-fact]')].map(
    (node) => node.getAttribute('data-fact') ?? '',
  );
}

function fact(key: string): HTMLElement {
  const node = document.querySelector(`[data-fact="${key}"]`);
  if (!(node instanceof HTMLElement)) {
    throw new Error(`missing fact ${key}`);
  }
  return node;
}

const amiens = '11111111-1111-1111-1111-111111111111';
const bordeaux = '22222222-2222-2222-2222-222222222222';
const caen = '33333333-3333-3333-3333-333333333333';
const dijon = '44444444-4444-4444-4444-444444444444';
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

  it('orders Places, Sélectionnées, Restantes, then reserved, using cap minus k', () => {
    renderDialog({
      compositionCapacity: 8,
      reservedFromFeeds: 3,
      affectationEntryIds: [amiens],
      entryList: [entry(amiens, 'Amiens'), entry(bordeaux, 'Bordeaux')],
    });

    expect(factOrder()).toEqual([
      'places',
      'selected',
      'remaining',
      'reserved',
    ]);
    expect(fact('places')).toHaveTextContent('8');
    expect(fact('places')).toHaveTextContent('places');
    expect(fact('selected')).toHaveTextContent('1');
    expect(fact('selected')).toHaveTextContent('sélectionnée');
    expect(fact('remaining')).toHaveTextContent('4');
    expect(fact('remaining')).toHaveTextContent('restantes');
    expect(fact('reserved')).toHaveTextContent('3');
    expect(fact('reserved')).toHaveTextContent('réservées par les sources');
    expect(screen.queryByText('Complet')).not.toBeInTheDocument();
    expect(
      document.querySelector('.structure-composition__facts'),
    ).toHaveAttribute('data-tone', 'short');
  });

  it('tones Restantes short when more than one place is open and near on the last place', () => {
    renderDialog({
      compositionCapacity: 4,
      entryList: [entry(amiens, 'Amiens')],
    });
    expect(
      document.querySelector('.structure-composition__facts'),
    ).toHaveAttribute('data-tone', 'short');
    expect(fact('remaining')).toHaveTextContent('4');
    cleanup();

    renderDialog({
      compositionCapacity: 4,
      affectationEntryIds: [amiens, bordeaux, caen],
      entryList: [
        entry(amiens, 'Amiens'),
        entry(bordeaux, 'Bordeaux'),
        entry(caen, 'Caen'),
      ],
    });
    expect(
      document.querySelector('.structure-composition__facts'),
    ).toHaveAttribute('data-tone', 'near');
    expect(fact('remaining')).toHaveTextContent('1');
    expect(fact('remaining')).toHaveTextContent('restante');
    expect(screen.queryByText('Complet')).not.toBeInTheDocument();
  });

  it('marks a full affectation exact and an over-cap set as attention, never Complet', () => {
    renderDialog({
      compositionCapacity: 2,
      affectationEntryIds: [amiens, bordeaux],
      entryList: [entry(amiens, 'Amiens'), entry(bordeaux, 'Bordeaux')],
    });
    expect(
      document.querySelector('.structure-composition__facts'),
    ).toHaveAttribute('data-tone', 'exact');
    expect(fact('remaining')).toHaveTextContent('0');
    expect(screen.queryByText('Complet')).not.toBeInTheDocument();
    cleanup();

    renderDialog({
      compositionCapacity: 2,
      affectationEntryIds: [amiens, bordeaux, caen],
      entryList: [
        entry(amiens, 'Amiens'),
        entry(bordeaux, 'Bordeaux'),
        entry(caen, 'Caen'),
      ],
    });
    expect(
      document.querySelector('.structure-composition__facts'),
    ).toHaveAttribute('data-tone', 'over');
    expect(fact('remaining')).toHaveTextContent('-1');
    expect(screen.queryByText('Complet')).not.toBeInTheDocument();
    expect(document.querySelector('.ds-alert')).toBeNull();
  });

  it('does not render an occupancy meter inside the dialog', () => {
    renderDialog({ reservedFromFeeds: 2 });

    expect(
      document.querySelector('.structure-occupancy-meter'),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('img', { name: /places/ }),
    ).not.toBeInTheDocument();
  });

  it('keeps a labeled Sélectionner tout ghost button beside search and stops at the cap', async () => {
    const user = userEvent.setup();
    renderDialog({
      compositionCapacity: 4,
      reservedFromFeeds: 1,
      entryList: [
        entry(amiens, 'Amiens'),
        entry(bordeaux, 'Bordeaux'),
        entry(caen, 'Caen'),
        entry(dijon, 'Dijon'),
      ],
    });

    const selectAll = screen.getByRole('button', { name: 'Sélectionner tout' });
    expect(selectAll).toHaveTextContent('Sélectionner tout');
    expect(selectAll).toHaveClass('ds-btn--ghost');
    expect(
      screen.queryByRole('button', { name: 'Tout sélectionner' }),
    ).not.toBeInTheDocument();
    expect(screen.queryByRole('radio')).not.toBeInTheDocument();
    expect(
      screen.queryByRole('region', { name: 'Sélectionnées' }),
    ).not.toBeInTheDocument();

    await user.click(selectAll);

    for (const name of ['Amiens', 'Bordeaux', 'Caen']) {
      expect(
        screen.getByRole('checkbox', { name: new RegExp(name) }),
      ).toBeChecked();
    }
    const locked = screen.getByRole('checkbox', { name: /Dijon/ });
    expect(locked).not.toBeChecked();
    expect(locked).toBeDisabled();
    expect(fact('remaining')).toHaveTextContent('0');
    expect(
      screen.getByRole('button', { name: 'Sélectionner tout' }),
    ).toBeDisabled();
    expect(document.querySelector('.ds-alert')).toBeNull();
  });

  it('paints selected cells with the shared tile, locks with a description, and chips withdrawn teams', () => {
    renderDialog({
      compositionCapacity: 1,
      affectationEntryIds: [withdrawnId],
      entryList: [
        entry(amiens, 'Amiens'),
        entry(withdrawnId, 'Ancien', 'Withdrawn'),
      ],
    });

    const selectedBox = screen.getByRole('checkbox', { name: /Ancien/ });
    expect(selectedBox).toBeChecked();
    expect(selectedBox.closest('.ds-selectable-tile')).toHaveAttribute(
      'data-selected',
      'true',
    );
    expect(
      within(selectedBox.closest('li') as HTMLElement).getByText(
        'Plus éligible',
      ),
    ).toHaveClass('ds-chip');

    const locked = screen.getByRole('checkbox', { name: /Amiens/ });
    expect(locked).toBeDisabled();
    expect(locked).toHaveAccessibleDescription(
      'Plus de place pour des équipes (1 max).',
    );
    expect(locked.closest('.structure-composition__row')).toHaveClass(
      'structure-composition__row--locked',
    );
    expect(locked.closest('.ds-selectable-tile')).toHaveAttribute(
      'data-selected',
      'false',
    );
    expect(
      within(locked.closest('li') as HTMLElement).queryByText('Plus éligible'),
    ).not.toBeInTheDocument();

    const lockedRule = compositionCss.match(
      /\.structure-composition__row--locked\s*\{([^}]*)\}/,
    );
    expect(lockedRule?.[1]).toBeDefined();
    expect(lockedRule?.[1]).not.toMatch(/opacity/);
  });

  it('scrolls only the team list and uses two columns, one at 36rem', () => {
    renderDialog({
      entryList: [entry(amiens, 'Amiens'), entry(bordeaux, 'Bordeaux')],
      affectationEntryIds: [amiens],
    });

    const list = screen.getByRole('list', { name: 'Équipes' });
    const selectAll = screen.getByRole('button', { name: 'Sélectionner tout' });
    const facts = document.querySelector('.structure-composition__facts');
    expect(list.contains(selectAll)).toBe(false);
    expect(facts).not.toBeNull();
    expect(list.contains(facts as Node)).toBe(false);
    expect(
      list.compareDocumentPosition(selectAll) &
        Node.DOCUMENT_POSITION_PRECEDING,
    ).toBeTruthy();

    const listRule = compositionCss.match(
      /\.structure-composition__list\s*\{([^}]*)\}/,
    );
    expect(listRule?.[1]).toMatch(/overflow:\s*auto/);
    expect(listRule?.[1]).toMatch(
      /grid-template-columns:\s*repeat\(2,\s*minmax\(0,\s*1fr\)\)/,
    );
    expect(compositionCss).toMatch(
      /\.ds-dialog:has\(\.structure-composition\) \.ds-dialog__body\s*\{[^}]*overflow:\s*hidden/,
    );
    expect(compositionCss).toMatch(
      /\.structure-composition\s*\{[^}]*overflow:\s*hidden/,
    );
    const narrow = compositionCss.slice(
      compositionCss.indexOf('@media (max-width: 36rem)'),
    );
    expect(narrow).toMatch(
      /\.structure-composition__list\s*\{[^}]*grid-template-columns:\s*1fr/,
    );
  });

  it('shows a dedicated empty state when the search matches nothing', async () => {
    const user = userEvent.setup();
    renderDialog({
      entryList: [entry(amiens, 'Amiens'), entry(bordeaux, 'Bordeaux')],
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
      screen.queryByRole('list', { name: 'Équipes' }),
    ).not.toBeInTheDocument();
  });
});

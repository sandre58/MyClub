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

function factPart(key: string, part: 'value' | 'label'): HTMLElement {
  const node = fact(key).querySelector(
    `.structure-qualification__fact-${part}`,
  );
  if (!(node instanceof HTMLElement)) {
    throw new Error(`missing fact ${part} ${key}`);
  }
  return node;
}

function teamTile(name: RegExp): HTMLElement {
  return screen.getByRole('checkbox', { name });
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

    await user.click(teamTile(/Olympique/));
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
    expect(fact('places')).toHaveClass(
      'structure-qualification__fact--secondary',
    );
    expect(fact('places')).not.toHaveClass(
      'structure-qualification__fact--primary',
    );
    expect(fact('selected')).toHaveTextContent('1');
    expect(fact('selected')).toHaveTextContent('sélectionnée');
    expect(fact('selected')).toHaveClass(
      'structure-qualification__fact--primary',
    );
    expect(fact('selected')).not.toHaveClass(
      'structure-qualification__fact--secondary',
    );
    expect(factPart('selected', 'value')).not.toHaveAttribute('data-zero');
    expect(fact('remaining')).toHaveTextContent('4');
    expect(fact('remaining')).toHaveTextContent('restantes');
    expect(fact('remaining')).toHaveClass(
      'structure-qualification__fact--secondary',
    );
    expect(fact('reserved')).toHaveTextContent('3');
    expect(fact('reserved')).toHaveTextContent('réservées par les sources');
    expect(fact('reserved')).toHaveClass(
      'structure-qualification__fact--secondary',
    );
    expect(fact('reserved')).not.toHaveClass(
      'structure-qualification__fact--primary',
    );
    expect(screen.queryByText('Complet')).not.toBeInTheDocument();
    expect(
      document.querySelector('.structure-composition__facts'),
    ).toHaveAttribute('data-tone', 'attention');
  });

  it('keeps Sélectionnées primary at zero and tones only its value secondary', () => {
    renderDialog({
      compositionCapacity: 4,
      entryList: [entry(amiens, 'Amiens')],
    });
    expect(
      document.querySelector('.structure-composition__facts'),
    ).toHaveAttribute('data-tone', 'attention');
    expect(fact('remaining')).toHaveTextContent('4');
    expect(fact('selected')).toHaveClass(
      'structure-qualification__fact--primary',
    );
    expect(fact('selected')).not.toHaveClass(
      'structure-qualification__fact--secondary',
    );
    expect(factPart('selected', 'value')).toHaveAttribute('data-zero', 'true');
    expect(factPart('selected', 'value')).toHaveTextContent('0');
    expect(factPart('selected', 'label')).not.toHaveAttribute('data-zero');
    const zeroRule = compositionCss.match(
      /\.structure-composition__facts \[data-fact='selected'\] \.structure-qualification__fact-value\[data-zero='true'\]\s*\{([^}]*)\}/,
    );
    expect(zeroRule?.[1]).toMatch(/color:\s*var\(--color-text-secondary\)/);
    expect(zeroRule?.[1]).not.toMatch(/font-size|font-weight|letter-spacing/);
    expect(compositionCss).not.toMatch(
      /\[data-fact='selected'\].*structure-qualification__fact-label/,
    );
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
    ).toHaveAttribute('data-tone', 'attention');
    expect(fact('remaining')).toHaveTextContent('1');
    expect(fact('remaining')).toHaveTextContent('restante');
    expect(fact('selected')).toHaveClass(
      'structure-qualification__fact--primary',
    );
    expect(screen.queryByText('Complet')).not.toBeInTheDocument();
  });

  it('tones a full affectation secondary and an over-cap set attention, never Complet or green', () => {
    renderDialog({
      compositionCapacity: 2,
      affectationEntryIds: [amiens, bordeaux],
      entryList: [entry(amiens, 'Amiens'), entry(bordeaux, 'Bordeaux')],
    });
    expect(
      document.querySelector('.structure-composition__facts'),
    ).toHaveAttribute('data-tone', 'secondary');
    expect(fact('remaining')).toHaveTextContent('0');
    expect(fact('remaining')).toHaveClass(
      'structure-qualification__fact--secondary',
    );
    expect(fact('selected')).toHaveClass(
      'structure-qualification__fact--primary',
    );
    expect(factPart('selected', 'value')).not.toHaveAttribute('data-zero');
    expect(factPart('remaining', 'value')).not.toHaveAttribute('data-zero');
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
    ).toHaveAttribute('data-tone', 'attention');
    expect(fact('remaining')).toHaveTextContent('-1');
    expect(screen.queryByText('Complet')).not.toBeInTheDocument();
    expect(document.querySelector('.ds-alert')).toBeNull();
    expect(compositionCss).not.toMatch(/--color-success/);
    expect(compositionCss).not.toMatch(/data-tone='exact'/);
    expect(compositionCss).not.toMatch(/data-tone='short'/);
    expect(compositionCss).not.toMatch(/data-tone='near'/);
    expect(compositionCss).not.toMatch(/data-tone='over'/);
    expect(compositionCss).toMatch(/data-tone='attention'/);
    const attentionRule = compositionCss.match(
      /\.structure-composition__facts\[data-tone='attention'\] \[data-fact='remaining'\] \.structure-qualification__fact-value\s*\{([^}]*)\}/,
    );
    expect(attentionRule?.[1]).toMatch(/color:\s*var\(--color-attention\)/);
    expect(compositionCss).not.toMatch(
      /\[data-fact='remaining'\].*structure-qualification__fact-label/,
    );
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
      const tile = teamTile(new RegExp(name));
      expect(tile).toHaveAttribute('aria-checked', 'true');
      expect(tile.querySelector('.ds-choice-tile__check')).not.toBeNull();
    }
    const locked = teamTile(/Dijon/);
    expect(locked).toHaveAttribute('aria-checked', 'false');
    expect(locked.querySelector('.ds-choice-tile__check')).toBeNull();
    expect(locked).toBeDisabled();
    expect(fact('remaining')).toHaveTextContent('0');
    expect(
      screen.getByRole('button', { name: 'Sélectionner tout' }),
    ).toBeDisabled();
    expect(document.querySelector('.ds-alert')).toBeNull();
  });

  it('uses ChoiceTiles with the check glyph, locks with a description, and chips withdrawn teams', () => {
    renderDialog({
      compositionCapacity: 1,
      affectationEntryIds: [withdrawnId],
      entryList: [
        entry(amiens, 'Amiens'),
        entry(withdrawnId, 'Ancien', 'Withdrawn'),
      ],
    });

    const selectedTile = teamTile(/Ancien/);
    expect(selectedTile).toHaveClass('ds-choice-tile');
    expect(selectedTile).toHaveAttribute('aria-checked', 'true');
    expect(selectedTile.querySelector('.ds-choice-tile__check')).not.toBeNull();
    expect(selectedTile.querySelector('input[type="checkbox"]')).toBeNull();
    expect(selectedTile.querySelector('.ds-selectable-tile')).toBeNull();
    expect(within(selectedTile).getByText('Plus éligible')).toHaveClass(
      'ds-chip',
    );
    expect(selectedTile.querySelector('.team-crest')).not.toBeNull();
    expect(
      selectedTile.querySelector('.ds-choice-tile__title'),
    ).toHaveTextContent('Ancien');

    const locked = teamTile(/Amiens/);
    expect(locked).toBeDisabled();
    expect(locked).toHaveAccessibleDescription(
      'Plus de place pour des équipes (1 max).',
    );
    expect(locked).toHaveAttribute('aria-checked', 'false');
    expect(locked.querySelector('.ds-choice-tile__check')).toBeNull();
    expect(within(locked).queryByText('Plus éligible')).not.toBeInTheDocument();
    expect(document.querySelector('input[type="checkbox"]')).toBeNull();
    expect(document.querySelector('.ds-selectable-tile')).toBeNull();

    const choiceLayout = compositionCss.match(
      /\.structure-composition__list \.ds-choice-tile\s*\{([^}]*)\}/,
    );
    expect(choiceLayout?.[1]).toMatch(/width:\s*100%/);
    expect(choiceLayout?.[1]).not.toMatch(
      /border|background|opacity|color:|ds-choice-tile__check/,
    );
    expect(compositionCss).not.toMatch(/ds-choice-tile__check/);
    expect(compositionCss).not.toMatch(/ds-selectable-tile/);
    expect(compositionCss).not.toMatch(/structure-composition__row--locked/);
  });

  it('scrolls only the team list and fills columns from 12rem', () => {
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
      /grid-template-columns:\s*repeat\(auto-fill,\s*minmax\(12rem,\s*1fr\)\)/,
    );
    expect(compositionCss).not.toMatch(/@media \(max-width: 36rem\)/);
    expect(compositionCss).not.toMatch(/structure-composition__check/);
    expect(compositionCss).toMatch(
      /\.ds-dialog:has\(\.structure-composition\) \.ds-dialog__body\s*\{[^}]*overflow:\s*hidden/,
    );
    expect(compositionCss).toMatch(
      /\.structure-composition\s*\{[^}]*overflow:\s*hidden/,
    );
  });

  it('toggles a team tile with click and keyboard and keeps Sélectionnées primary', async () => {
    const user = userEvent.setup();
    renderDialog({
      entryList: [entry(amiens, 'Amiens')],
    });

    const tile = teamTile(/Amiens/);
    expect(tile.tagName).toBe('BUTTON');
    expect(tile).toHaveAttribute('aria-checked', 'false');
    expect(tile.querySelector('.ds-choice-tile__check')).toBeNull();
    expect(fact('selected')).toHaveClass(
      'structure-qualification__fact--primary',
    );
    expect(factPart('selected', 'value')).toHaveAttribute('data-zero', 'true');

    await user.click(tile);
    expect(tile).toHaveAttribute('aria-checked', 'true');
    expect(tile.querySelector('.ds-choice-tile__check')).not.toBeNull();
    expect(fact('selected')).toHaveClass(
      'structure-qualification__fact--primary',
    );
    expect(factPart('selected', 'value')).not.toHaveAttribute('data-zero');

    tile.focus();
    await user.keyboard('{Enter}');
    expect(tile).toHaveAttribute('aria-checked', 'false');
    expect(tile.querySelector('.ds-choice-tile__check')).toBeNull();
    expect(fact('selected')).toHaveClass(
      'structure-qualification__fact--primary',
    );
    expect(factPart('selected', 'value')).toHaveAttribute('data-zero', 'true');

    tile.focus();
    await user.keyboard(' ');
    expect(tile).toHaveAttribute('aria-checked', 'true');
    expect(tile.querySelector('.ds-choice-tile__check')).not.toBeNull();
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
      screen.queryByRole('button', { name: /Amiens/ }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('list', { name: 'Équipes' }),
    ).not.toBeInTheDocument();
  });
});

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { ApiError } from '../../../api/http';
import {
  confirmCascadeReplace,
  previewCascadeReplace,
  type CascadePlayedMatch,
} from '../../../api';
import i18n from '../../../i18n';
import type {
  StructureResolutionOccupant,
  StructureStageHubSummary,
} from '../../../types';
import { notify } from '../../../design-system/toastStore';
import { StructureCascadeReplaceAction } from './StructureCascadeReplaceAction';
import { useStructureCascadeReplace } from './structureCascadeReplaceContext';
import { StructureResolutionFreshnessBadge } from './StructureResolutionFreshnessBadge';

vi.mock('../../../design-system/toastStore', () => ({
  notify: { error: vi.fn(), success: vi.fn(), info: vi.fn() },
}));

vi.mock('../../../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../../api')>();
  return {
    ...actual,
    previewCascadeReplace: vi.fn(),
    confirmCascadeReplace: vi.fn(),
  };
});

const stageId = 'cccccccc-cccc-cccc-cccc-cccccccccccc';
const otherStageId = 'ffffffff-ffff-ffff-ffff-ffffffffffff';
const entryId = 'dddddddd-dddd-dddd-dddd-dddddddddddd';
const matchId = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee';

const occupant: StructureResolutionOccupant = {
  target: 'Slot',
  entryId,
  provenanceKind: 'Qualification',
  sourcePathId: 'Q1',
  state: 'Perime',
  slotKey: 'Champ',
  stalenessCause: 'ResultatAmont',
  cascadeReplaceable: true,
};

const stage = {
  stageId,
  name: 'Finale',
  status: 'Running',
  teamCount: 2,
  matchCount: 1,
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
  resolutionOccupants: [occupant],
} as StructureStageHubSummary;

function ReplaceBadge() {
  const cascade = useStructureCascadeReplace();
  return (
    <StructureResolutionFreshnessBadge
      occupant={occupant}
      onReplace={cascade?.requestReplace}
    />
  );
}

function renderAction() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <StructureCascadeReplaceAction
        stage={stage}
        competitionId="bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"
      >
        <ReplaceBadge />
      </StructureCascadeReplaceAction>
    </QueryClientProvider>,
  );
}

function played(
  overrides: Partial<CascadePlayedMatch> = {},
): CascadePlayedMatch {
  return {
    matchId,
    stageId,
    homeEntryId: entryId,
    awayEntryId: null,
    homeName: 'Alpha',
    awayName: 'Beta',
    stageName: 'Finale',
    legIndex: null,
    ...overrides,
  };
}

const frenchLabels = [
  ['two teams', played(), 'Alpha – Beta'],
  ['home only', played({ awayName: null }), 'Alpha – à déterminer'],
  ['away only', played({ homeName: null }), 'à déterminer – Beta'],
  [
    'neither team',
    played({ homeName: null, awayName: null }),
    'Match à déterminer',
  ],
  ['return leg', played({ legIndex: 2 }), 'Manche 2 — Alpha – Beta'],
  [
    'other phase',
    played({ stageId: otherStageId, stageName: 'Demi-finale' }),
    'Demi-finale — Alpha – Beta',
  ],
  [
    'other phase and leg',
    played({ stageId: otherStageId, stageName: 'Finale', legIndex: 2 }),
    'Finale — Manche 2 — Alpha – Beta',
  ],
  [
    'same phase single leg',
    played({ stageName: 'Phase courante', legIndex: null }),
    'Alpha – Beta',
  ],
] as const;

const englishLabels = [
  ['two teams', played(), 'Alpha – Beta'],
  ['home only', played({ awayName: null }), 'Alpha – to be determined'],
  ['away only', played({ homeName: null }), 'to be determined – Beta'],
  [
    'neither team',
    played({ homeName: null, awayName: null }),
    'Match to be determined',
  ],
  ['return leg', played({ legIndex: 2 }), 'Leg 2 — Alpha – Beta'],
  [
    'other phase',
    played({ stageId: otherStageId, stageName: 'Demi-finale' }),
    'Demi-finale — Alpha – Beta',
  ],
] as const;

function staleButtonName() {
  return i18n.language.startsWith('en')
    ? 'Stale — Upstream result changed. Click to replace the team with the expected occupant'
    : "Périmé — Résultat amont modifié. Cliquer pour remplacer l'équipe par l'occupant attendu";
}

async function openPlayed(match: CascadePlayedMatch) {
  vi.mocked(previewCascadeReplace).mockResolvedValue({
    versionToken: 'token-1',
    playedMatches: [match],
    replacements: [],
  });
  const user = userEvent.setup();
  renderAction();
  await user.click(screen.getByRole('button', { name: staleButtonName() }));
  const item = await screen.findByRole('listitem');
  return { user, item };
}

function expectPlainLabel(item: HTMLElement, label: string) {
  expect(item).toHaveTextContent(label);
  const dialog = item.closest('[role="dialog"]');
  expect(dialog).not.toBeNull();
  expect(dialog!.outerHTML).not.toContain(matchId);
  const labelled = [
    dialog!,
    ...dialog!.querySelectorAll('[aria-label], [title]'),
  ];
  for (const node of labelled) {
    expect(node.getAttribute('aria-label') ?? '').not.toContain(matchId);
    expect(node.getAttribute('title') ?? '').not.toContain(matchId);
  }
  const text = item.textContent ?? '';
  expect(text).not.toMatch(/domicile|extérieur|home|away/i);
}

describe('StructureCascadeReplaceAction', () => {
  afterEach(async () => {
    await i18n.changeLanguage('fr');
  });

  it('lists the played match by team name before confirming', async () => {
    vi.mocked(confirmCascadeReplace).mockResolvedValue({
      versionToken: 'token-1',
      playedMatches: [],
      replacements: [],
    });
    const { user, item } = await openPlayed(played());
    expect(
      screen.queryByRole('button', { name: 'Remplacer Champ' }),
    ).toBeNull();
    expectPlainLabel(item, 'Alpha – Beta');
    expect(item).not.toHaveTextContent('Finale');
    expect(item).not.toHaveTextContent('Manche');

    await user.click(screen.getByRole('button', { name: 'Remplacer' }));
    expect(confirmCascadeReplace).toHaveBeenCalledWith(stageId, {
      target: 'Slot',
      entryId,
      slotKey: 'Champ',
      groupId: null,
      versionToken: 'token-1',
    });
  });

  it.each(frenchLabels)(
    'labels a played match in French (%s)',
    async (_name, match, label) => {
      const { item } = await openPlayed(match);
      expectPlainLabel(item, label);
      if (match.legIndex == null) {
        expect(item).not.toHaveTextContent('Manche');
      }
      if (match.stageId === stageId) {
        expect(item).not.toHaveTextContent(match.stageName ?? '');
      }
    },
  );

  it.each(englishLabels)(
    'labels a played match in English (%s)',
    async (_name, match, label) => {
      await i18n.changeLanguage('en');
      const { item } = await openPlayed(match);
      expectPlainLabel(item, label);
      if (match.legIndex == null) {
        expect(item).not.toHaveTextContent('Leg');
      }
    },
  );

  it('shows a preview refusal as toast instead of a page banner', async () => {
    vi.mocked(previewCascadeReplace).mockRejectedValue(
      new ApiError(
        409,
        'Direct assignment',
        undefined,
        'Application.CascadeReplaceDirectAssignment',
      ),
    );
    const user = userEvent.setup();
    renderAction();

    await user.click(
      screen.getByRole('button', {
        name: "Périmé — Résultat amont modifié. Cliquer pour remplacer l'équipe par l'occupant attendu",
      }),
    );

    expect(vi.mocked(notify.error)).toHaveBeenCalledWith(
      'Une affectation directe n’est jamais remplacée.',
    );
    expect(
      screen.queryByText('Une affectation directe n’est jamais remplacée.'),
    ).toBeNull();
    expect(screen.queryByRole('dialog')).toBeNull();
  });

  it('shows the server refusal and disables confirm', async () => {
    vi.mocked(previewCascadeReplace).mockResolvedValue({
      versionToken: 'token-1',
      playedMatches: [],
      replacements: [],
    });
    vi.mocked(confirmCascadeReplace).mockRejectedValue(
      new ApiError(
        409,
        'Completed phase blocks the cascade',
        undefined,
        'Application.CascadeReplaceCompleted',
      ),
    );
    const user = userEvent.setup();
    renderAction();

    expect(
      screen.queryByRole('button', { name: 'Remplacer Champ' }),
    ).toBeNull();
    await user.click(
      screen.getByRole('button', {
        name: "Périmé — Résultat amont modifié. Cliquer pour remplacer l'équipe par l'occupant attendu",
      }),
    );
    await user.click(await screen.findByRole('button', { name: 'Remplacer' }));

    expect(
      await screen.findByText(
        'Une phase terminée bloque le remplacement en cascade. (409)',
      ),
    ).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Remplacer' })).toBeDisabled();
  });
});

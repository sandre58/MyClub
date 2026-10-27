import { screen } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';
import { ApiError } from '../api';
import { resolveLifecycleMutationMessage } from '../pages/structure/problems/resolveStructureProblemMessage';
import { renderWithI18n } from '../test/renderWithI18n';
import {
  EmptyState,
  ErrorState,
  LoadingState,
  MutationError,
  PendingLabel,
} from '../ui';
import {
  apiErrorLabel,
  genericNetworkLabel,
  isGenericNetworkFailure,
} from './apiErrorLabel';
import i18n from './index';

/**
 * Locks Structure, Overview, and draw:confirmApply* error wiring.
 * NA_CELLS records scenarios that do not apply on that surface.
 */
const MATRIX = [
  {
    parcours: 'structure',
    scenario: 'business400',
    disposition: 'wired',
  },
  {
    parcours: 'structure',
    scenario: 'empty',
    disposition: 'wired',
  },
  {
    parcours: 'structure',
    scenario: 'loading',
    disposition: 'wired',
  },
  {
    parcours: 'structure',
    scenario: 'network',
    disposition: 'wired',
  },
  {
    parcours: 'overview',
    scenario: 'business400',
    disposition: 'wired',
  },
  {
    parcours: 'overview',
    scenario: 'empty',
    disposition: 'wired',
  },
  {
    parcours: 'overview',
    scenario: 'loading',
    disposition: 'wired',
  },
  {
    parcours: 'overview',
    scenario: 'network',
    disposition: 'wired',
  },
  {
    parcours: 'drawConfirmApply',
    scenario: 'business400',
    disposition: 'wired',
  },
  {
    parcours: 'drawConfirmApply',
    scenario: 'empty',
    disposition: 'wired',
  },
  {
    parcours: 'drawConfirmApply',
    scenario: 'loading',
    disposition: 'wired',
  },
  {
    parcours: 'drawConfirmApply',
    scenario: 'network',
    disposition: 'wired',
  },
] as const;

/** Scenarios that do not apply, or apply only beside the wired cell. */
const NA_CELLS = [
  {
    id: 'overview.pageEmptyState',
    note: 'A successful Overview read always renders the shell. Métier empty is the sport-unit copy, not EmptyState and not ProblemDetails.',
  },
  {
    id: 'drawConfirm.footerEmpty',
    note: 'The confirm dialog opens only for a selected execution. Idle empty is the draw-dialog EmptyState, not the confirm footer.',
  },
  {
    id: 'missingPotRules.problemDetails',
    note: 'MissingPotRules is a readiness fact on Structure and Overview. It is not a ProblemDetails code.',
  },
  {
    id: 'network.perStatus',
    note: 'Timeout, offline, and HTTP 5xx share one sentence. Distinct copy per status does not apply.',
  },
] as const;

const PARCOURS_CODES = [
  'Application.StartBlockedGroupsNotReady',
  'Application.StartBlockedUnmaterialized',
  'Application.StartBlockedIncompleteEntryRound',
  'Application.StartBlockedNoDeterminedConfrontations',
  'Application.StartBlockedNoExploitableInitialConfrontations',
  'Application.StartBlockedPhaseRosterIncomplete',
  'Application.DrawApplyFailure',
  'Application.DrawKindNotSupported',
  'Application.StructureNotMutable',
  'Application.ReadyConsumerWouldBreak',
  'Application.DrawObsoleteForConsumption',
  'Application.AppliedDrawPoolChangeBlocked',
  'Application.AppliedDrawFeedRulesChangeBlocked',
  'Application.AppliedDrawSlotPlacementProtected',
  'Application.DrawLaunchGridNotVacant',
  'Application.DrawLaunchDirectAssignmentPresent',
  'Application.CupCanonicalTopologyInvalid',
  'Application.QualificationMatchesRequired',
  'Application.QualificationCandidatesEmpty',
  'Application.QualificationStandingMissing',
  'Application.QualificationGroupNotFound',
  'Application.QualificationCandidateDuplicate',
  'Application.ProgressionFeedConsumed',
  'Application.ProgressionFeedImpactAmbiguous',
  'Application.ProgressionDownstreamProductionBlocked',
  'Application.ProgressionProductionParticipationAmbiguous',
  'Stage.NonCancelledDrawAlreadyExists',
] as const;

const NETWORK_FR = 'Connexion impossible. Vérifiez le réseau, puis réessayez.';
const GROUPS_NOT_READY_FR =
  'Complétez l’affectation des groupes avant de démarrer la phase.';

function problem(status: number, code: string, detail: string): ApiError {
  return new ApiError(status, detail, detail, code);
}

describe('parcours error matrix', () => {
  afterEach(async () => {
    await i18n.changeLanguage('fr');
  });

  it('covers three parcours and four scenarios, and names N/A cells', () => {
    expect(MATRIX).toHaveLength(12);
    const keys = MATRIX.map((cell) => `${cell.parcours}:${cell.scenario}`);
    expect(new Set(keys).size).toBe(12);
    expect(MATRIX.every((cell) => cell.disposition === 'wired')).toBe(true);
    expect(
      NA_CELLS.map((cell) => cell.note).every((note) => note.length > 0),
    ).toBe(true);
  });

  it('maps parcours wire codes in French and English', async () => {
    for (const code of PARCOURS_CODES) {
      const french = apiErrorLabel(problem(400, code, 'diagnostic'));
      expect(french, code).toBeTruthy();
      expect(french?.toLowerCase()).not.toContain('poule');
    }

    await i18n.changeLanguage('en');
    for (const code of PARCOURS_CODES) {
      const english = apiErrorLabel(problem(400, code, 'diagnostic'));
      expect(english, code).toBeTruthy();
      expect(english).not.toBe(
        apiErrorLabel(problem(400, 'Application.NotARealCode', 'diagnostic')),
      );
    }
  });

  it('uses Groupe, not poule, for StartBlockedGroupsNotReady', () => {
    expect(
      apiErrorLabel(
        problem(400, 'Application.StartBlockedGroupsNotReady', 'groups'),
      ),
    ).toBe(GROUPS_NOT_READY_FR);
  });

  it('does not invent a ProblemDetails label for MissingPotRules', () => {
    expect(
      apiErrorLabel(problem(400, 'MissingPotRules', 'Missing pot rules')),
    ).toBeUndefined();
    expect(i18n.t('enums:attentionSource.MissingPotRules')).toBe(
      'Pots de tirage manquants',
    );
  });

  it('shows Structure and Overview business 400 through MutationError and ErrorState', () => {
    const { rerender } = renderWithI18n(
      <MutationError
        error={problem(
          400,
          'Application.StartBlockedGroupsNotReady',
          'Complete group assignments before starting the phase.',
        )}
      />,
    );
    expect(screen.getByRole('alert')).toHaveTextContent(
      `${GROUPS_NOT_READY_FR} (400)`,
    );
    expect(screen.getByRole('alert')).not.toHaveTextContent('Complete group');

    rerender(
      <MutationError
        error={problem(
          400,
          'Application.StructureNotMutable',
          'Structure is not mutable.',
        )}
        structureStages={[]}
      />,
    );
    expect(screen.getByRole('alert')).toHaveTextContent(
      'La structure ne peut plus être modifiée tant que la compétition est en cours / close.',
    );
    expect(screen.getByRole('alert')).not.toHaveTextContent('(400)');
    expect(screen.getByRole('alert')).not.toHaveTextContent(
      'Structure is not mutable',
    );
  });

  it('shows apply-draw confirmation failures in the confirm footer MutationError', () => {
    const { rerender } = renderWithI18n(
      <MutationError
        error={problem(
          400,
          'Application.DrawApplyFailure',
          'Unable to apply the draw.',
        )}
        structureStages={[]}
      />,
    );
    expect(screen.getByRole('alert')).toHaveTextContent(
      'Impossible d’appliquer le tirage.',
    );
    expect(screen.getByRole('alert')).not.toHaveTextContent(
      'Unable to apply the draw',
    );

    rerender(
      <MutationError
        error={problem(
          409,
          'Application.DrawObsoleteForConsumption',
          'Draw is obsolete for consumption.',
        )}
      />,
    );
    expect(screen.getByRole('alert')).toHaveTextContent(
      'Ce tirage ne correspond plus à la phase — annulez-le ou rétablissez la cohérence avant d’appliquer ou de produire des matchs. (409)',
    );

    rerender(
      <MutationError
        error={problem(
          409,
          'Stage.NonCancelledDrawAlreadyExists',
          'A non-cancelled draw already exists.',
        )}
        structureStages={[]}
      />,
    );
    expect(screen.getByRole('alert')).toHaveTextContent(
      'Cette phase compte déjà un tirage non annulé. Annulez-le avant d’en créer un autre.',
    );
  });

  it('keeps métier empty distinct from ProblemDetails', () => {
    const { rerender } = renderWithI18n(
      <EmptyState title={i18n.t('structure:structure.emptyTitle')}>
        {i18n.t('structure:structure.emptyBody')}
      </EmptyState>,
    );
    expect(screen.getByText('Aucune phase')).toBeInTheDocument();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();

    rerender(
      <EmptyState
        variant="idle"
        title={i18n.t('structure:fiche.drawWorkflow.emptyTitle')}
      >
        {i18n.t('structure:fiche.drawWorkflow.emptyBody')}
      </EmptyState>,
    );
    expect(screen.getByText('Aucune exécution')).toBeInTheDocument();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();

    expect(i18n.t('overview:sport.recentEmpty')).toBe(
      'Aucune unité engagée pour le moment',
    );
    expect(i18n.t('overview:sport.nextEmpty')).toBe(
      'Aucune prochaine journée n’est encore générée',
    );
    expect(i18n.t('overview:sport.recentEmpty')).not.toBe(NETWORK_FR);
  });

  it('uses LoadingState for Structure and Overview, and PendingLabel while apply is in flight', () => {
    const { rerender } = renderWithI18n(<LoadingState />);
    expect(screen.getByRole('status')).toHaveTextContent('Chargement…');

    rerender(<PendingLabel>{i18n.t('draw:applying')}</PendingLabel>);
    expect(screen.getByText('Application…')).toBeInTheDocument();
    expect(document.querySelector('.ds-pending-label')).not.toBeNull();
  });

  it('uses one network sentence for timeout, offline, and 5xx on all three surfaces', () => {
    const cases = [
      new ApiError(500, 'Host unavailable', 'Host unavailable'),
      new ApiError(503, 'upstream timeout', 'upstream timeout'),
      new ApiError(408, 'Request timeout', 'Request timeout'),
      new TypeError('Failed to fetch'),
      new TypeError('Load failed'),
      new DOMException('The operation was aborted.', 'AbortError'),
    ];

    for (const error of cases) {
      expect(isGenericNetworkFailure(error)).toBe(true);
      expect(genericNetworkLabel()).toBe(NETWORK_FR);
    }

    expect(isGenericNetworkFailure(new Error('network'))).toBe(false);
    expect(isGenericNetworkFailure(new Error('Prepare blocked'))).toBe(false);
    expect(
      isGenericNetworkFailure(
        problem(400, 'Application.DrawApplyFailure', 'Unable to apply'),
      ),
    ).toBe(false);

    const { rerender } = renderWithI18n(
      <ErrorState
        error={problem(
          500,
          'Application.DrawApplyFailure',
          'Unable to apply the draw.',
        )}
      />,
    );
    expect(screen.getByRole('alert')).toHaveTextContent(NETWORK_FR);
    expect(screen.getByRole('alert')).not.toHaveTextContent('Unable to apply');
    expect(screen.getByRole('alert')).not.toHaveTextContent('(500)');

    rerender(<MutationError error={new TypeError('Failed to fetch')} />);
    expect(screen.getByRole('alert')).toHaveTextContent(NETWORK_FR);

    rerender(
      <MutationError
        error={new ApiError(502, 'Bad gateway', 'Bad gateway')}
        structureStages={[]}
      />,
    );
    expect(screen.getByRole('alert')).toHaveTextContent(NETWORK_FR);
    expect(screen.getByRole('alert')).not.toHaveTextContent('Bad gateway');
    expect(
      resolveLifecycleMutationMessage(
        i18n.getFixedT('fr', 'structure'),
        new TypeError('NetworkError when attempting to fetch resource.'),
      ),
    ).toBe(NETWORK_FR);
  });

  it('resolves the shared network sentence in English', async () => {
    await i18n.changeLanguage('en');
    renderWithI18n(
      <ErrorState error={new ApiError(500, 'Host unavailable')} />,
    );
    expect(screen.getByRole('alert')).toHaveTextContent(
      'The connection failed. Check the network, then try again.',
    );
  });
});

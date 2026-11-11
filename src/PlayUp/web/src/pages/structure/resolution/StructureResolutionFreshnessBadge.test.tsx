import { act, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { TOOLTIP_DELAY_OPEN_MS } from '../../../design-system/components/Tooltip';
import { renderWithI18n } from '../../../test/renderWithI18n';
import { StructureResolutionFreshnessBadge } from './StructureResolutionFreshnessBadge';

function NavigationSpy() {
  const location = useLocation();
  return (
    <div
      data-testid="navigation-spy"
      data-path={`${location.pathname}${location.search}`}
    />
  );
}

describe('StructureResolutionFreshnessBadge', () => {
  it('renders nothing for AJour or missing occupant', () => {
    const { container } = renderWithI18n(
      <StructureResolutionFreshnessBadge
        occupant={{
          target: 'Slot',
          entryId: 'e1',
          provenanceKind: 'Directe',
          sourcePathId: 'p1',
          state: 'AJour',
          slotKey: 'A',
          cascadeReplaceable: false,
        }}
      />,
    );
    expect(container).toBeEmptyDOMElement();
  });

  it('renders Perime with accessible cause label', () => {
    renderWithI18n(
      <StructureResolutionFreshnessBadge
        occupant={{
          target: 'Slot',
          entryId: 'e1',
          provenanceKind: 'Qualification',
          sourcePathId: 'p1',
          state: 'Perime',
          slotKey: 'A',
          stalenessCause: 'RegleQualification',
          cascadeReplaceable: false,
        }}
      />,
    );
    expect(screen.getByLabelText(/Périmé/)).toBeInTheDocument();
    expect(screen.getByText('Périmé')).toBeInTheDocument();
  });

  it('renders Indetermine and NonVerifiee labels', () => {
    renderWithI18n(
      <>
        <StructureResolutionFreshnessBadge
          occupant={{
            target: 'Population',
            entryId: 'e1',
            provenanceKind: 'Progression',
            sourcePathId: 'p1',
            state: 'Indetermine',
            cascadeReplaceable: false,
          }}
        />
        <StructureResolutionFreshnessBadge
          occupant={{
            target: 'Population',
            entryId: 'e2',
            provenanceKind: 'NonVerifiee',
            sourcePathId: 'p2',
            state: 'NonVerifiee',
            cascadeReplaceable: false,
          }}
        />
      </>,
    );
    expect(screen.getByText('Non confirmée')).toBeInTheDocument();
    expect(screen.queryByText('Non vérifiée')).toBeNull();
  });

  it('makes a stale Qual or Prog badge the replace action', async () => {
    const onReplace = vi.fn();
    const user = userEvent.setup();
    renderWithI18n(
      <>
        <StructureResolutionFreshnessBadge
          occupant={{
            target: 'Slot',
            entryId: 'e1',
            provenanceKind: 'Qualification',
            sourcePathId: 'p1',
            state: 'Perime',
            slotKey: 'A',
            stalenessCause: 'ResultatAmont',
            cascadeReplaceable: true,
          }}
          onReplace={onReplace}
        />
        <StructureResolutionFreshnessBadge
          occupant={{
            target: 'Population',
            entryId: 'e2',
            provenanceKind: 'Progression',
            sourcePathId: 'p2',
            state: 'Indetermine',
            cascadeReplaceable: false,
          }}
          onReplace={onReplace}
        />
        <StructureResolutionFreshnessBadge
          occupant={{
            target: 'Slot',
            entryId: 'e3',
            provenanceKind: 'Directe',
            sourcePathId: 'p3',
            state: 'Perime',
            slotKey: 'B',
            cascadeReplaceable: false,
          }}
          onReplace={onReplace}
        />
      </>,
    );

    const button = screen.getByRole('button', {
      name: "Périmé — Résultat amont modifié. Cliquer pour remplacer l'équipe par l'occupant attendu",
    });
    expect(button).toBeInTheDocument();
    expect(
      button.querySelector('.structure-freshness-badge__chevron'),
    ).not.toBeNull();
    expect(
      document.querySelectorAll('.structure-freshness-badge__chevron'),
    ).toHaveLength(1);
    expect(button).toHaveTextContent('Périmé');
    expect(screen.getAllByRole('button')).toHaveLength(1);
    await user.click(button);
    expect(onReplace).toHaveBeenCalledOnce();
    expect(onReplace.mock.calls[0][0].entryId).toBe('e1');
  });

  it('tooltip shows the staleness cause then the replace action', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
    Object.defineProperty(window, 'matchMedia', {
      writable: true,
      value: (query: string) => ({
        matches:
          query.includes('hover: hover') && query.includes('pointer: fine'),
        media: query,
        onchange: null,
        addListener: () => undefined,
        removeListener: () => undefined,
        addEventListener: () => undefined,
        removeEventListener: () => undefined,
        dispatchEvent: () => false,
      }),
    });
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderWithI18n(
      <StructureResolutionFreshnessBadge
        occupant={{
          target: 'Slot',
          entryId: 'e1',
          provenanceKind: 'Qualification',
          sourcePathId: 'p1',
          state: 'Perime',
          slotKey: 'A',
          stalenessCause: 'ResultatAmont',
          cascadeReplaceable: true,
        }}
        onReplace={() => undefined}
      />,
    );

    await user.hover(
      screen.getByRole('button', {
        name: "Périmé — Résultat amont modifié. Cliquer pour remplacer l'équipe par l'occupant attendu",
      }),
    );
    await act(async () => {
      vi.advanceTimersByTime(TOOLTIP_DELAY_OPEN_MS);
    });

    const tip = await screen.findByRole('tooltip');
    const cause = tip.querySelector('.structure-freshness-replace__cause');
    const action = tip.querySelector('.structure-freshness-replace__action');
    expect(cause).toHaveTextContent('Résultat amont modifié');
    expect(action).toHaveTextContent(
      "Cliquer pour remplacer l'équipe par l'occupant attendu",
    );
    expect(action).toHaveClass('structure-freshness-replace__action');
    expect(
      cause!.compareDocumentPosition(action!) &
        Node.DOCUMENT_POSITION_FOLLOWING,
    ).toBeTruthy();
  });

  it('navigates upstream when AmontPerime is clicked', async () => {
    const user = userEvent.setup();
    renderWithI18n(
      <MemoryRouter initialEntries={['/competitions/comp/structure']}>
        <Routes>
          <Route
            path="/competitions/:competitionId/structure"
            element={
              <>
                <NavigationSpy />
                <StructureResolutionFreshnessBadge
                  occupant={{
                    target: 'Slot',
                    entryId: 'e1',
                    provenanceKind: 'Progression',
                    sourcePathId: 'p1',
                    state: 'Perime',
                    slotKey: 'A',
                    stalenessCause: 'AmontPerime',
                    upstreamStageId: 'phase-a',
                    cascadeReplaceable: false,
                  }}
                />
              </>
            }
          />
        </Routes>
      </MemoryRouter>,
    );

    const badge = screen.getByRole('link', {
      name: /Périmé.*Phase amont périmée.*Aller à la phase amont/,
    });
    await user.click(badge);
    expect(screen.getByTestId('navigation-spy')).toHaveAttribute(
      'data-path',
      '/competitions/comp/structure?stage=phase-a',
    );
  });

  it('navigates to the match when an unplayed match is clicked', async () => {
    const user = userEvent.setup();
    renderWithI18n(
      <MemoryRouter initialEntries={['/competitions/comp/structure']}>
        <Routes>
          <Route
            path="/competitions/:competitionId/structure"
            element={
              <>
                <NavigationSpy />
                <StructureResolutionFreshnessBadge
                  occupant={{
                    target: 'Slot',
                    entryId: 'e1',
                    provenanceKind: 'Progression',
                    sourcePathId: 'p1',
                    state: 'Indetermine',
                    indeterminacyCause: 'MatchNonJoue',
                    actionMatchId: 'match-9',
                    actionMatchLabel: 'Nord – Est',
                    cascadeReplaceable: false,
                  }}
                />
              </>
            }
          />
          <Route path="/matches/:matchId" element={<NavigationSpy />} />
        </Routes>
      </MemoryRouter>,
    );

    await user.click(
      screen.getByRole('link', {
        name: /Non confirmée.*n'est pas joué.*Jouer le match Nord/,
      }),
    );
    expect(screen.getByTestId('navigation-spy')).toHaveAttribute(
      'data-path',
      '/matches/match-9',
    );
  });

  it('shows the hourglass on Indetermine full variant', () => {
    renderWithI18n(
      <MemoryRouter initialEntries={['/competitions/comp/structure']}>
        <Routes>
          <Route
            path="/competitions/:competitionId/structure"
            element={
              <StructureResolutionFreshnessBadge
                occupant={{
                  target: 'Population',
                  entryId: 'e1',
                  provenanceKind: 'Progression',
                  sourcePathId: 'p1',
                  state: 'Indetermine',
                  indeterminacyCause: 'Autre',
                  upstreamStageId: 'phase-a',
                  cascadeReplaceable: false,
                }}
              />
            }
          />
        </Routes>
      </MemoryRouter>,
    );
    const badge = screen.getByLabelText(
      /Non confirmée.*La sélection ne peut pas être calculée/,
    );
    expect(
      badge.querySelector('.structure-freshness-badge__icon'),
    ).not.toBeNull();
    expect(screen.queryByRole('link')).toBeNull();
  });

  it('schematic variant keeps the state word in the tooltip and the chevron only when clickable', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
    Object.defineProperty(window, 'matchMedia', {
      writable: true,
      value: (query: string) => ({
        matches:
          query.includes('hover: hover') && query.includes('pointer: fine'),
        media: query,
        onchange: null,
        addListener: () => undefined,
        removeListener: () => undefined,
        addEventListener: () => undefined,
        removeEventListener: () => undefined,
        dispatchEvent: () => false,
      }),
    });
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderWithI18n(
      <>
        <StructureResolutionFreshnessBadge
          variant="icon"
          occupant={{
            target: 'Slot',
            entryId: 'e1',
            provenanceKind: 'Qualification',
            sourcePathId: 'p1',
            state: 'Perime',
            slotKey: 'A',
            stalenessCause: 'ResultatAmont',
            cascadeReplaceable: true,
          }}
          onReplace={() => undefined}
        />
        <StructureResolutionFreshnessBadge
          variant="icon"
          occupant={{
            target: 'Population',
            entryId: 'e2',
            provenanceKind: 'Progression',
            sourcePathId: 'p2',
            state: 'Indetermine',
            cascadeReplaceable: false,
          }}
          onReplace={() => undefined}
        />
      </>,
    );

    expect(screen.queryByText('Périmé')).toBeNull();
    expect(screen.queryByText('Non confirmée')).toBeNull();
    const button = screen.getByRole('button', {
      name: "Périmé — Résultat amont modifié. Cliquer pour remplacer l'équipe par l'occupant attendu",
    });
    expect(
      button.querySelector('.structure-freshness-badge__chevron'),
    ).not.toBeNull();
    expect(screen.getByLabelText(/Non confirmée/)).toBeInTheDocument();
    expect(
      screen
        .getByLabelText(/Non confirmée/)
        .querySelector('.structure-freshness-badge__chevron'),
    ).toBeNull();

    await user.hover(button);
    await act(async () => {
      vi.advanceTimersByTime(TOOLTIP_DELAY_OPEN_MS);
    });
    const tip = await screen.findByRole('tooltip');
    expect(
      tip.querySelector('.structure-freshness-replace__state'),
    ).toHaveTextContent('Périmé');
    expect(
      tip.querySelector('.structure-freshness-replace__cause'),
    ).toHaveTextContent('Résultat amont modifié');
    expect(
      tip.querySelector('.structure-freshness-replace__action'),
    ).toHaveTextContent(
      "Cliquer pour remplacer l'équipe par l'occupant attendu",
    );
  });

  it('renders no button when a changed qualification rule cannot be replayed', () => {
    renderWithI18n(
      <StructureResolutionFreshnessBadge
        occupant={{
          target: 'Slot',
          entryId: 'e1',
          provenanceKind: 'Qualification',
          sourcePathId: 'p1',
          state: 'Perime',
          slotKey: 'A',
          stalenessCause: 'RegleQualification',
          cascadeReplaceable: false,
        }}
        onReplace={() => undefined}
      />,
    );
    expect(screen.queryByRole('button')).toBeNull();
    expect(screen.queryByRole('link')).toBeNull();
    expect(
      screen.getByLabelText(/Périmé.*Règle de qualification modifiée/),
    ).toBeInTheDocument();
  });

  it('keeps an empty selection on the badge without a dead-end link', () => {
    renderWithI18n(
      <MemoryRouter initialEntries={['/competitions/comp/structure']}>
        <Routes>
          <Route
            path="/competitions/:competitionId/structure"
            element={
              <StructureResolutionFreshnessBadge
                occupant={{
                  target: 'Slot',
                  entryId: 'e1',
                  provenanceKind: 'Qualification',
                  sourcePathId: 'p1',
                  state: 'Indetermine',
                  slotKey: 'A',
                  indeterminacyCause: 'SelectionVide',
                  upstreamStageId: 'poules',
                  cascadeReplaceable: false,
                }}
                entryName="Nord"
              />
            }
          />
        </Routes>
      </MemoryRouter>,
    );

    expect(screen.queryByRole('link')).toBeNull();
    expect(
      screen.getByLabelText(
        /Nord n'est plus confirmée — le classement ne désigne aucune équipe/,
      ),
    ).toBeInTheDocument();
  });

  it('names the team and the unplayed match in the tooltip sentence', () => {
    renderWithI18n(
      <MemoryRouter initialEntries={['/competitions/comp/structure']}>
        <Routes>
          <Route
            path="/competitions/:competitionId/structure"
            element={
              <StructureResolutionFreshnessBadge
                occupant={{
                  target: 'Slot',
                  entryId: 'e1',
                  provenanceKind: 'Progression',
                  sourcePathId: 'p1',
                  state: 'Indetermine',
                  indeterminacyCause: 'MatchNonJoue',
                  actionMatchId: 'match-9',
                  actionMatchLabel: 'Est – Sud',
                  cascadeReplaceable: false,
                }}
                entryName="Nord"
              />
            }
          />
          <Route path="/matches/:matchId" element={<NavigationSpy />} />
        </Routes>
      </MemoryRouter>,
    );

    expect(
      screen.getByRole('link', {
        name: /Nord n'est plus confirmée — le match Est – Sud n'est pas joué.*Jouer le match Est – Sud/,
      }),
    ).toBeInTheDocument();
  });

  it('opens the release when the occupant can be released', async () => {
    const onRelease = vi.fn();
    const user = userEvent.setup();
    const releasable = {
      target: 'Slot' as const,
      entryId: 'e1',
      provenanceKind: 'Qualification' as const,
      sourcePathId: 'p1',
      state: 'Indetermine' as const,
      slotKey: 'Dernier',
      indeterminacyCause: 'SelectionVide' as const,
      releasable: true,
      cascadeReplaceable: false,
    };
    renderWithI18n(
      <StructureResolutionFreshnessBadge
        occupant={releasable}
        entryName="Centre"
        onRelease={onRelease}
      />,
    );

    await user.click(
      screen.getByRole('button', {
        name: /Centre n'est plus confirmée — le classement ne désigne aucune équipe.*Libérer la place/,
      }),
    );
    expect(onRelease).toHaveBeenCalledWith(releasable);
  });
});

afterEach(() => {
  vi.useRealTimers();
});

import { act, fireEvent, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';

import {
  Tooltip,
  TOOLTIP_DELAY_OPEN_MS,
  TOOLTIP_LONG_PRESS_MS,
} from '../../../design-system/components/Tooltip';
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
    expect(
      screen.getByRole('img', {
        name: /Périmé.*Règle de qualification modifiée/,
      }),
    ).toBeInTheDocument();
    expect(screen.queryByText('Périmé')).toBeNull();
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
    expect(
      screen.getByRole('img', { name: /Non confirmée/ }),
    ).toBeInTheDocument();
    expect(screen.queryByText('Non confirmée')).toBeNull();
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
    expect(button).toHaveClass('structure-freshness-badge__action');
    expect(screen.queryByText('Périmé')).toBeNull();
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
    expect(badge.querySelector('.lucide-corner-left-up')).not.toBeNull();
    expect(badge.querySelector('.lucide-chevron-right')).toBeNull();
    await user.click(badge);
    expect(screen.getByTestId('navigation-spy')).toHaveAttribute(
      'data-path',
      '/competitions/comp/structure?stage=phase-a',
    );
  });

  it('shows neutral cascade copy when replace is unavailable', () => {
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
        onReplace={vi.fn()}
      />,
    );
    const inert = screen.getByRole('img', {
      name: /Périmé.*ne peut pas être remplacée automatiquement/i,
    });
    expect(inert.getAttribute('aria-label')).not.toMatch(/placement manuel/i);
    expect(inert).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Remplacer/ })).toBeNull();
  });

  it('shows the manual-placement hint on schematic inert Périmé badges', () => {
    renderWithI18n(
      <StructureResolutionFreshnessBadge
        manualPlacementHint
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
        onReplace={vi.fn()}
      />,
    );
    expect(
      screen.getByRole('img', {
        name: /Périmé.*placement manuel reste possible/i,
      }),
    ).toBeInTheDocument();
  });

  it('uses ChevronRight on destination navigate controls', () => {
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
                  indeterminacyCause: 'EgaliteClassement',
                  upstreamStageId: 'poules',
                  cascadeReplaceable: false,
                }}
              />
            }
          />
        </Routes>
      </MemoryRouter>,
    );
    const link = screen.getByRole('link', { name: /Voir le classement/ });
    expect(link.querySelector('.lucide-chevron-right')).not.toBeNull();
    expect(link.querySelector('.lucide-corner-left-up')).toBeNull();
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
        name: /Non confirmée.*match Nord – Est n'est pas joué.*Jouer le match Nord/,
      }),
    );
    expect(screen.getByTestId('navigation-spy')).toHaveAttribute(
      'data-path',
      '/matches/match-9',
    );
  });

  it('shows the hourglass on inert Indetermine badges', () => {
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
    const badge = screen.getByRole('img', {
      name: /Non confirmée.*sélection ne peut pas être calculée/i,
    });
    expect(badge).toHaveClass('structure-freshness-badge__glyph');
    expect(
      badge.querySelector('.structure-freshness-badge__icon'),
    ).not.toBeNull();
    expect(screen.queryByRole('link')).toBeNull();
  });

  it('keeps the state word in the tooltip for clickable and inert badges', async () => {
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
    expect(button).toHaveClass('structure-freshness-badge__action');
    const inert = screen.getByRole('img', { name: /Non confirmée/ });
    expect(inert).toHaveClass('structure-freshness-badge__glyph');

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

  it('shows that a definitive place can no longer be confirmed', () => {
    renderWithI18n(
      <StructureResolutionFreshnessBadge
        occupant={{
          target: 'Slot',
          entryId: 'e1',
          provenanceKind: 'Qualification',
          sourcePathId: 'p1',
          state: 'Indetermine',
          slotKey: 'Dernier',
          indeterminacyCause: 'SelectionVide',
          releasable: true,
          definitive: true,
          cascadeReplaceable: false,
        }}
        entryName="Centre"
        onRelease={() => undefined}
      />,
    );

    const button = screen.getByRole('button', {
      name: /Centre.*Ne pourra plus être confirmée.*Libérer la place/,
    });
    expect(button.querySelector('svg')).toBeTruthy();
    expect(screen.queryByText('Ne pourra plus être confirmée')).toBeNull();
  });

  it('renders action containers for links and buttons and glyphs when inert', () => {
    renderWithI18n(
      <MemoryRouter initialEntries={['/competitions/comp/structure']}>
        <Routes>
          <Route
            path="/competitions/:competitionId/structure"
            element={
              <>
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
                />
                <StructureResolutionFreshnessBadge
                  occupant={{
                    target: 'Slot',
                    entryId: 'e2',
                    provenanceKind: 'Qualification',
                    sourcePathId: 'p2',
                    state: 'Perime',
                    slotKey: 'B',
                    stalenessCause: 'ResultatAmont',
                    cascadeReplaceable: true,
                  }}
                  onReplace={() => undefined}
                />
                <StructureResolutionFreshnessBadge
                  occupant={{
                    target: 'Slot',
                    entryId: 'e3',
                    provenanceKind: 'Progression',
                    sourcePathId: 'p3',
                    state: 'Perime',
                    slotKey: 'C',
                    stalenessCause: 'AmontPerime',
                    upstreamStageId: 'upstream',
                    cascadeReplaceable: false,
                  }}
                />
              </>
            }
          />
        </Routes>
      </MemoryRouter>,
    );

    expect(
      screen.getByRole('img', { name: /Périmé.*Règle de qualification/ }),
    ).toHaveClass('structure-freshness-badge__glyph');
    expect(
      screen.getByRole('button', { name: /Périmé.*remplacer/i }),
    ).toHaveClass('structure-freshness-badge__action');
    expect(
      screen.getByRole('link', { name: /Périmé.*phase amont/i }),
    ).toHaveClass('structure-freshness-badge__action');
    // CA7: Enter activates buttons and links; Space activates buttons only (native <a>).
  });

  it('uses Unlock on release, Refresh on replace, and state icons when inert', () => {
    renderWithI18n(
      <>
        <StructureResolutionFreshnessBadge
          occupant={{
            target: 'Slot',
            entryId: 'e1',
            provenanceKind: 'Qualification',
            sourcePathId: 'p1',
            state: 'Indetermine',
            slotKey: 'A',
            indeterminacyCause: 'SelectionVide',
            releasable: true,
            cascadeReplaceable: false,
          }}
          onRelease={() => undefined}
        />
        <StructureResolutionFreshnessBadge
          occupant={{
            target: 'Slot',
            entryId: 'e2',
            provenanceKind: 'Qualification',
            sourcePathId: 'p2',
            state: 'Perime',
            slotKey: 'B',
            stalenessCause: 'ResultatAmont',
            cascadeReplaceable: true,
            releasable: false,
          }}
          onReplace={() => undefined}
        />
        <StructureResolutionFreshnessBadge
          occupant={{
            target: 'Slot',
            entryId: 'e3',
            provenanceKind: 'Qualification',
            sourcePathId: 'p3',
            state: 'Indetermine',
            slotKey: 'C',
            indeterminacyCause: 'Autre',
            cascadeReplaceable: false,
          }}
        />
      </>,
    );

    expect(
      screen
        .getByRole('button', { name: /Libérer la place/ })
        .querySelector('.lucide-unlock'),
    ).toBeTruthy();
    expect(
      screen
        .getByRole('button', { name: /remplacer/i })
        .querySelector('.lucide-refresh-cw'),
    ).toBeTruthy();
    expect(
      screen
        .getByRole('img', { name: /Non confirmée/ })
        .querySelector('.lucide-hourglass'),
    ).toBeTruthy();
  });

  it('uses UnconfirmableIcon on definitive inert and Unlock on definitive release', () => {
    renderWithI18n(
      <>
        <StructureResolutionFreshnessBadge
          occupant={{
            target: 'Slot',
            entryId: 'e1',
            provenanceKind: 'Qualification',
            sourcePathId: 'p1',
            state: 'Indetermine',
            slotKey: 'A',
            definitive: true,
            cascadeReplaceable: false,
          }}
        />
        <StructureResolutionFreshnessBadge
          occupant={{
            target: 'Slot',
            entryId: 'e2',
            provenanceKind: 'Qualification',
            sourcePathId: 'p2',
            state: 'Indetermine',
            slotKey: 'B',
            indeterminacyCause: 'SelectionVide',
            releasable: true,
            definitive: true,
            cascadeReplaceable: false,
          }}
          onRelease={() => undefined}
        />
      </>,
    );

    expect(
      screen.getByRole('img', {
        name: /Non confirmée.*Ne pourra plus être confirmée/,
      }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', {
        name: /Non confirmée.*Ne pourra plus être confirmée.*Libérer/,
      }),
    ).toBeInTheDocument();
    expect(
      screen
        .getByRole('img', {
          name: /Non confirmée.*Ne pourra plus être confirmée/,
        })
        .querySelector('.lucide-timer-off'),
    ).toBeTruthy();
    expect(
      screen
        .getByRole('button', {
          name: /Non confirmée.*Ne pourra plus être confirmée.*Libérer/,
        })
        .querySelector('.lucide-unlock'),
    ).toBeTruthy();
    expect(
      screen
        .getByRole('button', {
          name: /Non confirmée.*Ne pourra plus être confirmée.*Libérer/,
        })
        .querySelector('.lucide-timer-off'),
    ).toBeNull();
  });

  it('opens badge tooltip on long-press without firing release (coarse pointer)', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
    Object.defineProperty(window, 'matchMedia', {
      writable: true,
      value: (query: string) => ({
        matches: false,
        media: query,
        onchange: null,
        addListener: () => undefined,
        removeListener: () => undefined,
        addEventListener: () => undefined,
        removeEventListener: () => undefined,
        dispatchEvent: () => false,
      }),
    });
    const onRelease = vi.fn();
    const releasable = {
      target: 'Slot' as const,
      entryId: 'e1',
      provenanceKind: 'Qualification' as const,
      sourcePathId: 'p1',
      state: 'Indetermine' as const,
      slotKey: 'Jouer',
      indeterminacyCause: 'SelectionVide' as const,
      releasable: true,
      cascadeReplaceable: false,
    };
    renderWithI18n(
      <StructureResolutionFreshnessBadge
        occupant={releasable}
        entryName="Nord"
        onRelease={onRelease}
      />,
    );

    fireEvent.pointerDown(
      screen.getByRole('button', { name: /Libérer la place/ }),
    );
    await act(async () => {
      vi.advanceTimersByTime(TOOLTIP_LONG_PRESS_MS + 80);
    });
    expect(await screen.findByRole('tooltip')).toBeInTheDocument();
    expect(onRelease).not.toHaveBeenCalled();
  });

  it('opens inert glyph tooltip on tap when pointer is coarse', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
    Object.defineProperty(window, 'matchMedia', {
      writable: true,
      value: (query: string) => ({
        matches: false,
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
      <span className="schematic-slot__freshness">
        <StructureResolutionFreshnessBadge
          occupant={{
            target: 'Slot',
            entryId: 'e1',
            provenanceKind: 'Qualification',
            sourcePathId: 'p1',
            state: 'Indetermine',
            slotKey: 'A',
            indeterminacyCause: 'Autre',
            cascadeReplaceable: false,
          }}
        />
      </span>,
    );

    await user.click(screen.getByRole('img', { name: /Non confirmée/ }));
    expect(await screen.findByRole('tooltip')).toBeInTheDocument();
    expect(screen.getAllByRole('tooltip')).toHaveLength(1);
  });

  it('shows only the badge tooltip when long-pressing a button inside a schematic case', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
    Object.defineProperty(window, 'matchMedia', {
      writable: true,
      value: (query: string) => ({
        matches: false,
        media: query,
        onchange: null,
        addListener: () => undefined,
        removeListener: () => undefined,
        addEventListener: () => undefined,
        removeEventListener: () => undefined,
        dispatchEvent: () => false,
      }),
    });
    renderWithI18n(
      <Tooltip content="Case tooltip" activation="long-press">
        <div className="schematic-slot schematic-slot--interactive">
          <button
            type="button"
            className="schematic-slot__hit"
            aria-label="Jouer"
          >
            hit
          </button>
          <span className="schematic-slot__freshness">
            <StructureResolutionFreshnessBadge
              occupant={{
                target: 'Slot',
                entryId: 'e1',
                provenanceKind: 'Progression',
                sourcePathId: 'p1',
                state: 'Indetermine',
                slotKey: 'Jouer',
                indeterminacyCause: 'SelectionVide',
                releasable: true,
                cascadeReplaceable: false,
              }}
              entryName="Nord"
              onRelease={() => undefined}
            />
          </span>
        </div>
      </Tooltip>,
    );

    fireEvent.pointerDown(
      screen.getByRole('button', { name: /Libérer la place/ }),
    );
    await act(async () => {
      vi.advanceTimersByTime(TOOLTIP_LONG_PRESS_MS + 80);
    });
    expect(await screen.findByRole('tooltip')).toBeInTheDocument();
    expect(screen.getAllByRole('tooltip')).toHaveLength(1);
    expect(screen.queryByText('Case tooltip')).toBeNull();
  });
});

afterEach(() => {
  vi.useRealTimers();
});

import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';

import type { StructureResolutionOccupant } from '../../../types';
import { renderWithI18n } from '../../../test/renderWithI18n';
import { StructurePhaseTeamLine } from './StructurePhaseTeamLine';
import { ResolutionReleaseContext } from '../resolution/structureResolutionReleaseContext';

function occupant(
  overrides: Partial<StructureResolutionOccupant> = {},
): StructureResolutionOccupant {
  return {
    target: 'Slot',
    entryId: 'e1',
    provenanceKind: 'Qualification',
    sourcePathId: 'Q|p|1',
    state: 'Indetermine',
    slotKey: 'Tete',
    indeterminacyCause: 'SelectionVide',
    releasable: true,
    cascadeReplaceable: false,
    ...overrides,
  };
}

describe('StructurePhaseTeamLine', () => {
  it('renders a normal team row without action', () => {
    renderWithI18n(
      <MemoryRouter>
        <ul>
          <li className="structure-entries__team structure-entries__team--line">
            <StructurePhaseTeamLine displayName="Sud" />
          </li>
        </ul>
      </MemoryRouter>,
    );
    expect(screen.getByText('Sud')).toBeInTheDocument();
    expect(screen.queryByRole('button')).toBeNull();
  });

  it('shows forfait icon and freshness state with a single action button', async () => {
    const requestRelease = vi.fn();
    const user = userEvent.setup();
    renderWithI18n(
      <MemoryRouter initialEntries={['/competitions/comp/structure']}>
        <ResolutionReleaseContext.Provider value={{ requestRelease }}>
          <ul>
            <li className="structure-entries__team structure-entries__team--line">
              <StructurePhaseTeamLine
                displayName="Centre"
                withdrawn
                ineligible
                provenanceMeta="1er · Poules"
                resolutionOccupant={occupant()}
                onRelease={requestRelease}
              />
            </li>
          </ul>
        </ResolutionReleaseContext.Provider>
      </MemoryRouter>,
    );
    expect(screen.getByLabelText('Forfait')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /Libérer/ }));
    expect(requestRelease).toHaveBeenCalled();
  });

  it('renders pending band row with hourglass and unresolved label', () => {
    renderWithI18n(
      <MemoryRouter>
        <ul>
          <li className="structure-entries__team structure-entries__team--line">
            <StructurePhaseTeamLine
              displayName=""
              pending
              pendingLabel="Non résolue"
            />
          </li>
        </ul>
      </MemoryRouter>,
    );
    expect(screen.getByText('Non résolue')).toBeInTheDocument();
    expect(screen.queryByRole('button')).toBeNull();
  });

  it('shows placed hint on the qualifier hint row', () => {
    const { container } = renderWithI18n(
      <MemoryRouter>
        <div className="structure-composition-qualifiers__outcome">
          <StructurePhaseTeamLine
            layout="qualifierHint"
            displayName="Nord"
            placed
            placedPlaceLabel="Tête"
          />
        </div>
      </MemoryRouter>,
    );
    expect(
      container.querySelector(
        '.structure-entries__placed-hint span[aria-hidden="true"]',
      ),
    ).toHaveTextContent('Tête');
    expect(
      container.querySelector(
        '.structure-entries__placed-hint .ds-visually-hidden',
      ),
    ).toHaveTextContent('Placée — Tête');
  });

  it('announces placed without duplicating visible copy when place name is missing', () => {
    renderWithI18n(
      <MemoryRouter>
        <div className="structure-composition-qualifiers__outcome">
          <StructurePhaseTeamLine
            layout="qualifierHint"
            displayName="Nord"
            placed
          />
        </div>
      </MemoryRouter>,
    );
    expect(screen.getAllByText('Placée')).toHaveLength(1);
    expect(screen.getByText('Placée')).toHaveClass('ds-visually-hidden');
  });

  it('centers the name on the crest when no meta is shown', () => {
    const { container } = renderWithI18n(
      <MemoryRouter>
        <ul>
          <li className="structure-entries__team structure-entries__team--line">
            <StructurePhaseTeamLine displayName="Sud" />
          </li>
        </ul>
      </MemoryRouter>,
    );
    expect(
      container.querySelector('.structure-entries__identity-head--solo'),
    ).not.toBeNull();
  });

  it('shows Perime state without a focusable action on list rows', () => {
    renderWithI18n(
      <MemoryRouter>
        <ul>
          <li className="structure-entries__team structure-entries__team--line">
            <StructurePhaseTeamLine
              displayName="Nord"
              resolutionOccupant={occupant({
                state: 'Perime',
                releasable: false,
                cascadeReplaceable: false,
                stalenessCause: 'ResultatAmont',
              })}
            />
          </li>
        </ul>
      </MemoryRouter>,
    );
    expect(screen.queryByRole('button')).toBeNull();
    expect(screen.queryByRole('link')).toBeNull();
  });

  it('stacks place, state and action on the qualifier hint row', async () => {
    const requestRelease = vi.fn();
    const user = userEvent.setup();
    renderWithI18n(
      <MemoryRouter initialEntries={['/competitions/comp/structure']}>
        <ResolutionReleaseContext.Provider value={{ requestRelease }}>
          <div className="structure-composition-qualifiers__outcome">
            <StructurePhaseTeamLine
              layout="qualifierHint"
              displayName="Nord"
              placed
              placedPlaceLabel="Jouer"
              resolutionOccupant={occupant()}
              onRelease={requestRelease}
            />
          </div>
        </ResolutionReleaseContext.Provider>
      </MemoryRouter>,
    );
    expect(screen.getByText('Jouer')).toBeInTheDocument();
    expect(screen.getByText('Non déterminé')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /Libérer/ }));
    expect(requestRelease).toHaveBeenCalled();
  });

  it('qualifier hint uses spaced units without middle-dot separators', () => {
    const { container } = renderWithI18n(
      <MemoryRouter>
        <div className="structure-composition-qualifiers__outcome">
          <StructurePhaseTeamLine
            layout="qualifierHint"
            displayName="Nord"
            placed
            placedPlaceLabel="Tête"
            resolutionOccupant={occupant()}
          />
        </div>
      </MemoryRouter>,
    );
    expect(
      container.querySelectorAll('.structure-composition-qualifiers__hint-sep'),
    ).toHaveLength(0);
    expect(
      container.querySelectorAll('.structure-composition-qualifiers__hint-unit')
        .length,
    ).toBeGreaterThanOrEqual(2);
  });

  it('shows placed unit on unresolved qualifier hint rows', () => {
    const { container } = renderWithI18n(
      <MemoryRouter>
        <div className="structure-composition-qualifiers__outcome">
          <StructurePhaseTeamLine
            layout="qualifierHint"
            displayName=""
            pending
            pendingLabel="Non résolue"
            placed
            placedPlaceLabel="Tete"
          />
        </div>
      </MemoryRouter>,
    );
    expect(screen.getByText('Non résolue')).toBeInTheDocument();
    expect(screen.getByText('Tete')).toBeInTheDocument();
    expect(
      container.querySelectorAll('.structure-composition-qualifiers__hint-sep'),
    ).toHaveLength(0);
  });

  it('default layout shows team name on the first line and caption meta on the second', () => {
    const { container } = renderWithI18n(
      <MemoryRouter>
        <StructurePhaseTeamLine
          displayName="Nord"
          placed
          placedPlaceLabel="Groupe A"
          placedTooltip="Placée en Groupe A, place 3"
          ineligible
          provenanceMeta="1er"
          provenanceTitle="1er de Poule A"
          resolutionOccupant={occupant({ state: 'Perime' })}
        />
      </MemoryRouter>,
    );
    expect(screen.getByText('Nord')).toBeInTheDocument();
    expect(
      container.querySelector('.structure-entries__meta--caption'),
    ).not.toBeNull();
    expect(screen.getByText('Plus éligible')).toBeInTheDocument();
    expect(screen.getByText('1er')).toBeInTheDocument();
    expect(screen.getByText('Groupe A')).toBeInTheDocument();
    expect(screen.getByText('Périmé')).toBeInTheDocument();
    const hint = container.querySelector('.structure-entries__placed-hint');
    expect(hint).toHaveAttribute('title', 'Placée en Groupe A, place 3');
    expect(hint?.querySelector('.ds-visually-hidden')).toHaveTextContent(
      'Placée en Groupe A, place 3',
    );
    expect(hint?.querySelector('span[aria-hidden="true"]')).toHaveTextContent(
      'Groupe A',
    );
  });

  it('qualifier hint pending row has no middle-dot separators', () => {
    const { container } = renderWithI18n(
      <MemoryRouter>
        <div className="structure-composition-qualifiers__outcome">
          <StructurePhaseTeamLine
            layout="qualifierHint"
            displayName=""
            pending
            pendingLabel="Non résolue"
          />
        </div>
      </MemoryRouter>,
    );
    expect(
      container.querySelectorAll('.structure-composition-qualifiers__hint-sep'),
    ).toHaveLength(0);
  });

  it('qualifier hint shows place reminder, freshness state and release action', async () => {
    const requestRelease = vi.fn();
    const user = userEvent.setup();
    const { container } = renderWithI18n(
      <MemoryRouter initialEntries={['/competitions/comp/structure']}>
        <ResolutionReleaseContext.Provider value={{ requestRelease }}>
          <div className="structure-composition-qualifiers__outcome">
            <StructurePhaseTeamLine
              layout="qualifierHint"
              displayName="Nord"
              placed
              placedPlaceLabel="Tête"
              resolutionOccupant={occupant()}
              onRelease={requestRelease}
            />
          </div>
        </ResolutionReleaseContext.Provider>
      </MemoryRouter>,
    );
    expect(
      container.querySelector('.structure-composition-qualifiers__hint'),
    ).not.toBeNull();
    expect(screen.getByText('Tête')).toBeInTheDocument();
    expect(screen.getByText('Non déterminé')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /Libérer/ }));
    expect(requestRelease).toHaveBeenCalled();
  });

  it('shows no-longer-eligible meta for inactive non-withdrawn entries', () => {
    const { container } = renderWithI18n(
      <MemoryRouter>
        <div className="structure-composition-qualifiers__outcome">
          <StructurePhaseTeamLine
            layout="qualifierHint"
            displayName="Archive"
            ineligible
          />
        </div>
      </MemoryRouter>,
    );
    const meta = container.querySelector(
      '.structure-entries__eligibility-meta',
    );
    expect(meta).toHaveTextContent('Plus éligible');
    expect(meta?.querySelector('.lucide-list-minus')).not.toBeNull();
    expect(screen.queryByLabelText('Forfait')).toBeNull();
  });

  it('shows status icons on qualifier hint freshness units', () => {
    const cases: {
      occupant: StructureResolutionOccupant;
      iconSelector: string;
    }[] = [
      {
        occupant: occupant({
          state: 'Perime',
          releasable: false,
          cascadeReplaceable: false,
        }),
        iconSelector: '.lucide-circle-alert',
      },
      {
        occupant: occupant({ state: 'Indetermine' }),
        iconSelector: 'svg[stroke-dasharray="3 3"]',
      },
      {
        occupant: occupant({
          state: 'Indetermine',
          definitive: true,
          releasable: false,
        }),
        iconSelector: '.lucide-timer-off',
      },
    ];

    for (const { occupant: occ, iconSelector } of cases) {
      const { container, unmount } = renderWithI18n(
        <MemoryRouter>
          <div className="structure-composition-qualifiers__outcome">
            <StructurePhaseTeamLine
              layout="qualifierHint"
              displayName="Nord"
              resolutionOccupant={occ}
            />
          </div>
        </MemoryRouter>,
      );
      const freshness = container.querySelector(
        '.structure-entries__freshness-state',
      );
      expect(freshness?.querySelector(iconSelector)).not.toBeNull();
      unmount();
    }
  });
});

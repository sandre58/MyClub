import { act, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { TOOLTIP_DELAY_OPEN_MS } from '../../../design-system/components/Tooltip';
import { renderWithI18n } from '../../../test/renderWithI18n';
import { StructureResolutionFreshnessBadge } from './StructureResolutionFreshnessBadge';

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
          }}
        />
        <StructureResolutionFreshnessBadge
          occupant={{
            target: 'Population',
            entryId: 'e2',
            provenanceKind: 'NonVerifiee',
            sourcePathId: 'p2',
            state: 'NonVerifiee',
          }}
        />
      </>,
    );
    expect(screen.getByText('Indéterminé')).toBeInTheDocument();
    expect(screen.getByText('Non vérifiée')).toBeInTheDocument();
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
          }}
          onReplace={onReplace}
        />
      </>,
    );

    const button = screen.getByRole('button', {
      name: "Résultat amont modifié. Cliquer pour remplacer l'équipe par l'occupant attendu",
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
        }}
        onReplace={() => undefined}
      />,
    );

    await user.hover(
      screen.getByRole('button', {
        name: "Résultat amont modifié. Cliquer pour remplacer l'équipe par l'occupant attendu",
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
          }}
          onReplace={() => undefined}
        />
      </>,
    );

    expect(screen.queryByText('Périmé')).toBeNull();
    expect(screen.queryByText('Indéterminé')).toBeNull();
    const button = screen.getByRole('button', {
      name: /Périmé.*Cliquer pour remplacer l'équipe par l'occupant attendu/,
    });
    expect(
      button.querySelector('.structure-freshness-badge__chevron'),
    ).not.toBeNull();
    expect(screen.getByLabelText(/Indéterminé/)).toBeInTheDocument();
    expect(
      screen
        .getByLabelText(/Indéterminé/)
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
});

afterEach(() => {
  vi.useRealTimers();
});

import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';

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

    expect(
      screen.getByRole('button', { name: 'Remplacer…' }),
    ).toBeInTheDocument();
    expect(screen.getAllByRole('button')).toHaveLength(1);
    await user.click(screen.getByRole('button', { name: 'Remplacer…' }));
    expect(onReplace).toHaveBeenCalledOnce();
    expect(onReplace.mock.calls[0][0].entryId).toBe('e1');
  });
});

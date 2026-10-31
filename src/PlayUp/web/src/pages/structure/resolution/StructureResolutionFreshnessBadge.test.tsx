import { screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

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
});

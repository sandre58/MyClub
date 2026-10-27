import { screen } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';
import i18n from '../../../i18n';
import { renderWithI18n } from '../../../test/renderWithI18n';
import { StructureDrawReadinessChecklist } from './StructureDrawReadinessChecklist';

const sampleItems = [
  { id: 'teamsFillPlaces' as const, met: false, current: 4, capacity: 8 },
  { id: 'teamsAllKnown' as const, met: true, current: 4, capacity: 4 },
  {
    id: 'bracketPlacesFree' as const,
    met: false,
    current: 2,
  },
];

describe('StructureDrawReadinessChecklist', () => {
  afterEach(async () => {
    await i18n.changeLanguage('fr');
  });

  it('renders neutral French factual labels', () => {
    renderWithI18n(<StructureDrawReadinessChecklist items={sampleItems} />);

    expect(screen.getByText('Équipes dans la phase : 4/8')).toBeInTheDocument();
    expect(
      screen.getByText('Équipes connues depuis l’amont : 4/4'),
    ).toBeInTheDocument();
    expect(
      screen.getByText('Places occupées sur le tableau : 2'),
    ).toBeInTheDocument();
  });

  it('exposes met and unmet state on list items (French)', () => {
    renderWithI18n(<StructureDrawReadinessChecklist items={sampleItems} />);

    const list = screen.getByRole('list', {
      name: /Prérequis pour effectuer le tirage/i,
    });
    const items = list.querySelectorAll('.structure-draw-readiness__item');
    expect(items).toHaveLength(3);
    expect(items[0]).toHaveAttribute(
      'aria-label',
      'À compléter. Équipes dans la phase : 4/8',
    );
    expect(items[1]).toHaveAttribute(
      'aria-label',
      'Satisfait. Équipes connues depuis l’amont : 4/4',
    );
    expect(items[2]).toHaveAttribute(
      'aria-label',
      'À compléter. Places occupées sur le tableau : 2',
    );
  });

  it('renders neutral English factual labels and accessible state', async () => {
    await i18n.changeLanguage('en');
    renderWithI18n(<StructureDrawReadinessChecklist items={sampleItems} />);

    expect(screen.getByText('Teams in phase: 4/8')).toBeInTheDocument();
    expect(
      screen.getByText('Teams known from previous phases: 4/4'),
    ).toBeInTheDocument();
    expect(
      screen.getByText('Occupied places on the draw: 2'),
    ).toBeInTheDocument();

    const list = screen.getByRole('list', {
      name: /Requirements to run the draw/i,
    });
    const first = list.querySelector('.structure-draw-readiness__item');
    expect(first).toHaveAttribute(
      'aria-label',
      'To complete. Teams in phase: 4/8',
    );
  });

  it('uses none wording when manual placement count is zero', () => {
    renderWithI18n(
      <StructureDrawReadinessChecklist
        items={[
          {
            id: 'bracketPlacesFree',
            met: true,
            current: 0,
          },
        ]}
      />,
    );

    expect(
      screen.getByText('Places occupées sur le tableau : aucun'),
    ).toBeInTheDocument();
  });
});

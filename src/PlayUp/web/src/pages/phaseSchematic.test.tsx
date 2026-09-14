import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import i18n from '../i18n';
import type { StageSchematic } from '../types';
import { PhaseSchematic } from './phaseSchematic';

await i18n.changeLanguage('fr');

function cupSchematic(overrides?: Partial<StageSchematic>): StageSchematic {
  return {
    stageId: 's1',
    competitionId: 'c1',
    name: 'KO',
    status: 'Draft',
    formatKind: 'Cup',
    cases: [
      {
        formPosition: { kind: 'CupSlot', slotKey: 'A' },
        entry: null,
        assignment: null,
      },
      {
        formPosition: { kind: 'CupSlot', slotKey: 'B' },
        entry: { entryId: 'e1', displayName: 'Alpha' },
        assignment: {
          entryId: 'e1',
          displayName: 'Alpha',
        },
      },
    ],
    connections: [],
    ...overrides,
  };
}

describe('PhaseSchematic', () => {
  it('does not invent match numbers without connections', () => {
    const { container } = render(<PhaseSchematic schematic={cupSchematic()} />);
    expect(container.querySelector('.schematic-cup__match-n')).toBeNull();
  });

  it('shows match number only from real connection', () => {
    render(
      <PhaseSchematic
        schematic={cupSchematic({
          connections: [
            {
              fixtureId: 'f1',
              roundOrder: 0,
              slotAKey: 'A',
              slotBKey: 'B',
              matchNumber: 7,
            },
          ],
        })}
      />,
    );
    expect(screen.getByText('#7')).toBeInTheDocument();
  });

  it('shows match number for pairing-draw sides without slot keys', () => {
    render(
      <PhaseSchematic
        schematic={cupSchematic({
          cases: [
            {
              formPosition: { kind: 'CupSlot', fixtureId: 'f1', side: 'A' },
              entry: { entryId: 'e1', displayName: 'Alpha' },
              assignment: { entryId: 'e1', displayName: 'Alpha' },
            },
            {
              formPosition: { kind: 'CupSlot', fixtureId: 'f1', side: 'B' },
              entry: { entryId: 'e2', displayName: 'Beta' },
              assignment: { entryId: 'e2', displayName: 'Beta' },
            },
          ],
          connections: [
            {
              fixtureId: 'f1',
              roundOrder: 0,
              slotAKey: null,
              slotBKey: null,
              matchNumber: 3,
            },
          ],
        })}
      />,
    );
    expect(screen.getByText('#3')).toBeInTheDocument();
    expect(screen.getByText('Alpha')).toBeInTheDocument();
    expect(screen.getByText('Beta')).toBeInTheDocument();
  });

  it('championship roster stays empty when cases have no entry (no k projection)', () => {
    const schematic: StageSchematic = {
      stageId: 's1',
      competitionId: 'c1',
      name: 'Ligue',
      status: 'Draft',
      formatKind: 'Championship',
      cases: [
        { formPosition: { kind: 'RosterPlace', index: 1 } },
        { formPosition: { kind: 'RosterPlace', index: 2 } },
      ],
      connections: [],
    };
    const { container } = render(<PhaseSchematic schematic={schematic} />);
    expect(container.querySelectorAll('.schematic-slot--empty')).toHaveLength(
      2,
    );
    expect(container.querySelector('.schematic-slot__primary')).toBeNull();
  });

  it('swiss shows planned round count, distinct from championship', () => {
    const schematic: StageSchematic = {
      stageId: 's1',
      competitionId: 'c1',
      name: 'Suisse',
      status: 'Draft',
      formatKind: 'Swiss',
      cases: [
        { formPosition: { kind: 'RosterPlace', index: 1 } },
        { formPosition: { kind: 'RosterPlace', index: 2 } },
      ],
      connections: [],
      swissRoundCount: 5,
    };
    const { container } = render(<PhaseSchematic schematic={schematic} />);
    expect(
      container.querySelector('.regulation-schematic--swiss'),
    ).not.toBeNull();
    expect(screen.getByText(/5\s*rondes/i)).toBeInTheDocument();
    expect(
      container.querySelector('.regulation-schematic__league-rank'),
    ).toBeNull();
  });

  it('cup slot shows progression origin with source match number', () => {
    render(
      <PhaseSchematic
        schematic={cupSchematic({
          cases: [
            {
              formPosition: { kind: 'CupSlot', slotKey: 'A' },
              feedOrigin: {
                kind: 'Progression',
                outcome: 'Winner',
                sourceFixtureNumber: 4,
              },
              entry: null,
              assignment: null,
            },
            {
              formPosition: { kind: 'CupSlot', slotKey: 'B' },
              entry: null,
              assignment: null,
            },
          ],
        })}
      />,
    );
    expect(screen.getByText(/Vainqueur · Match #4/)).toBeInTheDocument();
  });
});

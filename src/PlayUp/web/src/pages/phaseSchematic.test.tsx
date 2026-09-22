import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import i18n from '../i18n';
import type { StageSchematic } from '../types';
import { PhaseSchematic } from './phaseSchematic';

await i18n.changeLanguage('fr');

class ResizeObserverStub {
  observe() {}
  unobserve() {}
  disconnect() {}
}
globalThis.ResizeObserver =
  globalThis.ResizeObserver ?? (ResizeObserverStub as typeof ResizeObserver);

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
  it('does not invent Match # without connections (pair ordinal ok)', () => {
    const { container } = render(<PhaseSchematic schematic={cupSchematic()} />);
    expect(container.querySelector('.schematic-cup__match-n')).toBeNull();
    expect(container.querySelector('.schematic-cup__pair-ordinal')).not.toBeNull();
  });

  it('shows Match # from real connection', () => {
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

  it('championship roster shows composition entries without league ranks', () => {
    const schematic: StageSchematic = {
      stageId: 's1',
      competitionId: 'c1',
      name: 'Ligue',
      status: 'Draft',
      formatKind: 'Championship',
      cases: [
        {
          formPosition: { kind: 'RosterPlace', index: 1 },
          entry: { entryId: 'e1', displayName: 'Alpha' },
          assignment: { entryId: 'e1', displayName: 'Alpha' },
        },
        { formPosition: { kind: 'RosterPlace', index: 2 } },
      ],
      connections: [],
    };
    const { container } = render(<PhaseSchematic schematic={schematic} />);
    expect(container.querySelectorAll('.schematic-slot')).toHaveLength(2);
    expect(screen.getByText('Alpha')).toBeInTheDocument();
    expect(container.querySelectorAll('.schematic-slot--empty')).toHaveLength(1);
    expect(
      container.querySelector('.regulation-schematic__league-rank'),
    ).toBeNull();
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

  it('cup multi-round shows one slot column per round (8+4+2)', () => {
    const cases = Array.from({ length: 14 }, (_, i) => ({
      formPosition: { kind: 'CupSlot' as const, slotKey: `S${i + 1}` },
      entry: null,
      assignment: null,
    }));
    const { container } = render(
      <PhaseSchematic
        schematic={cupSchematic({
          cases,
          cupRoundCount: 3,
          connections: [],
        })}
      />,
    );
    expect(container.querySelector('.schematic-cup--multi')).not.toBeNull();
    expect(container.querySelectorAll('.schematic-cup__round')).toHaveLength(3);
    expect(container.querySelectorAll('.schematic-slot')).toHaveLength(14);
    expect(
      screen.getByLabelText(/tableau|bracket|3/i),
    ).toBeInTheDocument();
  });

  it('cup multi-round shows structural pair ordinals without fixtures', () => {
    const cases = Array.from({ length: 14 }, (_, i) => ({
      formPosition: { kind: 'CupSlot' as const, slotKey: `S${i + 1}` },
      entry: null,
      assignment: null,
    }));
    const { container } = render(
      <PhaseSchematic
        schematic={cupSchematic({
          cases,
          cupRoundCount: 3,
          connections: [],
        })}
      />,
    );
    const labels = [
      ...container.querySelectorAll('.schematic-cup__pair-ordinal'),
    ].map((el) => el.textContent);
    // 4 QF + 2 SF + 1 F — ordinals, not Match #
    expect(labels).toEqual(['1', '2', '3', '4', '1', '2', '1']);
    expect(container.querySelector('.schematic-cup__match-n')).toBeNull();
  });

  it('cup multi-round infers 3 columns from 14 slots without cupRoundCount', () => {
    const cases = Array.from({ length: 14 }, (_, i) => ({
      formPosition: { kind: 'CupSlot' as const, slotKey: `S${i + 1}` },
      entry: null,
      assignment: null,
    }));
    const { container } = render(
      <PhaseSchematic
        schematic={cupSchematic({
          cases,
          connections: [],
        })}
      />,
    );
    expect(container.querySelector('.schematic-cup--multi')).not.toBeNull();
    expect(container.querySelectorAll('.schematic-cup__round')).toHaveLength(3);
  });

  it('cup single-round with 8 slots stays single (no imaginary later rounds)', () => {
    const cases = Array.from({ length: 8 }, (_, i) => ({
      formPosition: { kind: 'CupSlot' as const, slotKey: `S${i + 1}` },
      entry: null,
      assignment: null,
    }));
    const { container } = render(
      <PhaseSchematic
        schematic={cupSchematic({
          cases,
          cupRoundCount: 1,
          connections: [],
        })}
        cupRoundCount={1}
      />,
    );
    expect(container.querySelector('.schematic-cup--multi')).toBeNull();
    // One wire column only — not 3 projected KO rounds.
    expect(container.querySelectorAll('.schematic-cup__wire-line')).toHaveLength(4);
  });

  it('cup multi-round uses round hint from structure hub', () => {
    const cases = Array.from({ length: 14 }, (_, i) => ({
      formPosition: { kind: 'CupSlot' as const, slotKey: `S${i + 1}` },
      entry: null,
      assignment: null,
    }));
    const { container } = render(
      <PhaseSchematic
        schematic={cupSchematic({ cases, connections: [] })}
        cupRoundCount={3}
      />,
    );
    expect(container.querySelectorAll('.schematic-cup__round')).toHaveLength(3);
  });
});

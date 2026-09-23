import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import i18n from '../i18n';
import type { StageSchematic } from '../types';
import { buildSchematicCaseTooltipModel, PhaseSchematic } from './phaseSchematic';

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

  it('cup Qual/Prog feed stays primary; resolved team is not shown (U4 B1)', () => {
    const { container } = render(
      <PhaseSchematic
        schematic={cupSchematic({
          cases: [
            {
              formPosition: { kind: 'CupSlot', slotKey: 'A' },
              feedOrigin: {
                kind: 'Progression',
                outcome: 'Winner',
                sourceFixtureNumber: 2,
                sourceStageName: 'Demi-finales',
              },
              entry: { entryId: 'e1', displayName: 'FC Nice' },
              assignment: { entryId: 'e1', displayName: 'FC Nice' },
            },
          ],
        })}
      />,
    );
    expect(screen.getByText(/Vainqueur · Match #2/)).toBeInTheDocument();
    expect(screen.queryByText('FC Nice')).toBeNull();
    expect(
      container.querySelector('.schematic-slot__secondary'),
    ).toBeNull();
    expect(container.querySelector('.schematic-slot-tip')).toBeTruthy();
    expect(container.querySelector('.ds-tooltip-trigger')).toBeTruthy();
  });

  it('cup Draw with occupant shows occupant primary (Published resolution provenance)', () => {
    const { container } = render(
      <PhaseSchematic
        schematic={cupSchematic({
          cases: [
            {
              formPosition: { kind: 'CupSlot', slotKey: 'A' },
              feedOrigin: { kind: 'Draw', drawId: 'd1' },
              entry: { entryId: 'e1', displayName: 'Alpha' },
              assignment: { entryId: 'e1', displayName: 'Alpha' },
            },
          ],
        })}
      />,
    );
    expect(
      container.querySelector('.schematic-slot__primary')?.textContent,
    ).toBe('Alpha');
    expect(container.querySelector('.schematic-slot-tip')).toBeTruthy();
  });

  it('cup Direct feed does not show Affectation over the occupant', () => {
    render(
      <PhaseSchematic
        schematic={cupSchematic({
          cases: [
            {
              formPosition: { kind: 'CupSlot', slotKey: 'A' },
              feedOrigin: {
                kind: 'Direct',
                configuredEntryId: 'e1',
              },
              entry: { entryId: 'e1', displayName: 'FC Nice' },
              assignment: { entryId: 'e1', displayName: 'FC Nice' },
            },
          ],
        })}
      />,
    );
    expect(screen.getByText('FC Nice')).toBeInTheDocument();
    expect(screen.queryByText(/Affectation|affectation/i)).toBeNull();
  });

  it('buildSchematicCaseTooltipModel omits duplicate identity for Affectation', () => {
    const model = buildSchematicCaseTooltipModel({
      address: '1·A',
      primary: 'FC Nice',
      resolvedName: 'FC Nice',
      feed: { kind: 'Direct', configuredEntryId: 'e1' },
      t: (key) => {
        if (key === 'structure:fiche.schematicTooltipFrom') return 'Vient de';
        if (key === 'structure:fiche.schematicTooltipByDraw')
          return 'Placé par tirage';
        if (key === 'structure:fiche.schematicTooltipByAffectation')
          return 'Affectation manuelle';
        return key;
      },
    });
    expect(model).toEqual({
      address: '1·A',
      subject: { kind: 'team', name: 'FC Nice' },
      origin: { kind: 'affectation', text: 'Affectation manuelle' },
    });
  });

  it('buildSchematicCaseTooltipModel skips address-only empty chrome', () => {
    expect(
      buildSchematicCaseTooltipModel({
        address: '1·A',
        primary: null,
        resolvedName: null,
        feed: null,
        t: (key) => key,
      }),
    ).toBeNull();
  });

  it('buildSchematicCaseTooltipModel stacks address / team / from-phase', () => {
    const model = buildSchematicCaseTooltipModel({
      address: '1·A',
      primary: 'Vainqueur · Match #2',
      resolvedName: 'FC Nice',
      feed: {
        kind: 'Progression',
        sourceStageName: 'Demi-finales',
        outcome: 'Winner',
        sourceFixtureNumber: 2,
      },
      crest: { logoMediaId: 'logo-1', primaryColor: '#123' },
      t: (key) => {
        if (key === 'structure:fiche.schematicTooltipFrom') return 'Vient de';
        if (key === 'structure:fiche.schematicTooltipByDraw')
          return 'Placé par tirage';
        if (key === 'structure:fiche.schematicTooltipByAffectation')
          return 'Affectation manuelle';
        return key;
      },
    });
    expect(model).toEqual({
      address: '1·A',
      subject: {
        kind: 'team',
        name: 'FC Nice',
        logoMediaId: 'logo-1',
        primaryColor: '#123',
      },
      origin: {
        kind: 'from',
        lead: 'Vient de',
        text: 'Demi-finales',
      },
    });
  });

  it('buildSchematicCaseTooltipModel uses path label when team unresolved', () => {
    const model = buildSchematicCaseTooltipModel({
      address: 'B-A',
      primary: 'Perdant · Match #1',
      resolvedName: null,
      feed: {
        kind: 'Progression',
        sourceStageName: 'Tour 1',
        outcome: 'Loser',
        sourceFixtureNumber: 1,
      },
      t: (key) => {
        if (key === 'structure:fiche.schematicTooltipFrom') return 'Vient de';
        return key;
      },
    });
    expect(model).toEqual({
      address: 'B-A',
      subject: { kind: 'label', name: 'Perdant · Match #1' },
      origin: { kind: 'from', lead: 'Vient de', text: 'Tour 1' },
    });
  });

  it('buildSchematicCaseTooltipModel marks draw placement', () => {
    const model = buildSchematicCaseTooltipModel({
      address: 'R16-1-A',
      primary: 'OGC Nice',
      resolvedName: 'OGC Nice',
      feed: { kind: 'Draw', drawId: 'd1' },
      t: (key) => {
        if (key === 'structure:fiche.schematicTooltipByDraw')
          return 'Placé par tirage';
        return key;
      },
    });
    expect(model).toEqual({
      address: 'R16-1-A',
      subject: { kind: 'team', name: 'OGC Nice' },
      origin: { kind: 'draw', text: 'Placé par tirage' },
    });
  });

  it('championship bag projects resolved occupant and pending ForForm into cases', () => {
    render(
      <PhaseSchematic
        schematic={{
          stageId: 'champ',
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
            {
              formPosition: { kind: 'RosterPlace', index: 2 },
              entry: null,
              assignment: null,
              feedOrigin: {
                kind: 'Qualification',
                selectionMode: 'Position',
                selectionValue: 1,
                rankingScope: 'Overall',
              },
            },
          ],
          connections: [],
        }}
      />,
    );
    expect(screen.getByText('Alpha')).toBeInTheDocument();
    expect(screen.getByText(/classement|général|1/i)).toBeInTheDocument();
  });

  it('championship has no separate alimentation zone — pending lives on cases', () => {
    const { container } = render(
      <PhaseSchematic
        schematic={{
          stageId: 'champ',
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
            {
              formPosition: { kind: 'RosterPlace', index: 2 },
              entry: null,
              assignment: null,
              feedOrigin: {
                kind: 'Qualification',
                selectionMode: 'Position',
                selectionValue: 1,
                rankingScope: 'Group',
                groupName: 'A',
                pathOrder: 1,
              },
            },
            {
              formPosition: { kind: 'RosterPlace', index: 3 },
              entry: null,
              assignment: null,
              feedOrigin: {
                kind: 'Qualification',
                selectionMode: 'Position',
                selectionValue: 1,
                rankingScope: 'Group',
                groupName: 'B',
                pathOrder: 2,
              },
            },
            {
              formPosition: { kind: 'RosterPlace', index: 4 },
              entry: null,
              assignment: null,
            },
          ],
          connections: [],
        }}
      />,
    );
    const root = container.querySelector('.regulation-schematic--championship');
    expect(root).not.toBeNull();
    expect(root!.querySelector('.regulation-schematic__form-feeds')).toBeNull();
    expect(root!.querySelector('.regulation-schematic__league')).not.toBeNull();
    expect(screen.getByText(/1er du groupe A/i)).toBeInTheDocument();
    expect(screen.getByText(/1er du groupe B/i)).toBeInTheDocument();
    expect(screen.getByText('Alpha')).toBeInTheDocument();
    // WhoFeeds pending = solid chrome; only the blank cell is dashed empty.
    expect(root!.querySelectorAll('.schematic-slot--empty')).toHaveLength(1);
  });

  it('groups ForGroup WhoFeeds fills seats — no title chrome under poule name', () => {
    const { container } = render(
      <PhaseSchematic
        schematic={{
          stageId: 'groups',
          competitionId: 'c1',
          name: 'Poules',
          status: 'Draft',
          formatKind: 'Groups',
          cases: [
            {
              formPosition: {
                kind: 'GroupPlace',
                groupId: 'ga',
                groupName: 'A',
                index: 1,
              },
              entry: null,
              assignment: null,
              feedOrigin: {
                kind: 'Progression',
                outcome: 'Winner',
                sourceFixtureNumber: 1,
                destinationGroupId: 'ga',
              },
            },
            {
              formPosition: {
                kind: 'GroupPlace',
                groupId: 'ga',
                groupName: 'A',
                index: 2,
              },
              entry: null,
              assignment: null,
            },
          ],
          connections: [],
          groupFeeds: [
            {
              groupId: 'ga',
              feedOrigin: {
                kind: 'Progression',
                outcome: 'Winner',
                sourceFixtureNumber: 1,
                destinationGroupId: 'ga',
              },
            },
          ],
        }}
      />,
    );
    const card = container.querySelector('.regulation-schematic__card');
    expect(card).not.toBeNull();
    expect(card!.querySelector('.regulation-schematic__card-feed')).toBeNull();
    expect(screen.getByText(/Vainqueur · Match #1/)).toBeInTheDocument();
    expect(card!.querySelectorAll('.schematic-slot--empty')).toHaveLength(1);
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

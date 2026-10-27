import { describe, expect, it } from 'vitest';
import { compositionQualifierFeedGroups } from './structureCompositionQualifierFeeds';
import { inboundEntryProvenanceByEntryId } from './compositionTeamsInboundRows';
import type {
  StructureInboundComposition,
  StructureView,
} from '../../../types';

describe('compositionQualifierFeedGroups', () => {
  it('labels each hub path from backend qualificationPath projection', () => {
    const sourceId = 'groups-phase';
    const inbound = {
      configuredVolume: 2,
      resolvedCount: 0,
      rules: [
        {
          feedKey: `q-${sourceId}-1`,
          sourceStageId: sourceId,
          seats: [{ entryId: null, displayName: null, placed: true }],
          qualificationPath: {
            order: 1,
            selectionMode: 'Range' as const,
            selectionValue: 1,
            selectionEndValue: 2,
            destinationStageId: 'final',
            groupId: 'g1',
            groupName: 'Poule A',
            rankingScope: 'Group',
          },
        },
        {
          feedKey: `q-${sourceId}-2`,
          sourceStageId: sourceId,
          seats: [{ entryId: null, displayName: null, placed: false }],
          qualificationPath: {
            order: 2,
            selectionMode: 'Position' as const,
            selectionValue: 1,
            destinationStageId: 'final',
            groupId: 'g2',
            groupName: 'Poule B',
            rankingScope: 'Group',
          },
        },
      ],
    } as StructureInboundComposition;
    const data: StructureView = {
      competitionId: 'c1',
      participants: { entries: [] },
      stages: [
        { stageId: sourceId, name: 'Phase de groupes', status: 'Ready' },
        {
          stageId: 'final',
          name: 'Finale',
          status: 'Draft',
          inboundComposition: inbound,
        },
      ],
    } as unknown as StructureView;

    const groups = compositionQualifierFeedGroups({
      data,
      inbound,
      t: (key, opts) =>
        key === 'fiche.rule.contextGroup' ? `Poule ${opts?.group}` : key,
      intentFallbackGroups: [],
    });

    expect(groups[0].rules).toHaveLength(2);
    expect(groups[0].rules[0].context).toContain('Poule A');
    expect(groups[0].rules[1].context).toContain('Poule B');
    expect(groups[0].rules[0].hubSeats?.[0]?.placed).toBe(true);

    const inboundWithEntry = {
      ...inbound,
      rules: [
        {
          ...inbound.rules[0],
          seats: [{ entryId: 'entry-a', displayName: 'Team A', placed: true }],
        },
      ],
    };
    const groupsWithEntry = compositionQualifierFeedGroups({
      data,
      inbound: inboundWithEntry,
      t: (key, opts) =>
        key === 'fiche.rule.contextGroup' ? `Poule ${opts?.group}` : key,
      intentFallbackGroups: [],
    });
    const provenance = inboundEntryProvenanceByEntryId(groupsWithEntry);
    expect(provenance.get('entry-a')?.full).toContain('Phase de groupes');
    expect(provenance.get('entry-a')?.full).toContain('Poule A');
    expect(provenance.get('entry-a')?.badge.length).toBeGreaterThan(0);
  });
});

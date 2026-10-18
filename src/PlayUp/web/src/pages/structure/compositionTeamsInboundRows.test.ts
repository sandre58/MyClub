import { describe, expect, it } from 'vitest';
import {
  listCompositionTeamRows,
  provenanceLabelsForQualifierRule,
  resolveCompositionResolvedGaugeStats,
  compositionTeamRowsResolutionStats,
} from './compositionTeamsInboundRows';
import type { CompositionQualifierFeedGroup } from './structureCompositionQualifierFeeds';
import type { StructureStageHubSummary } from '../../types';

describe('compositionTeamsInboundRows', () => {
  it('lists resolved and unresolved inbound seats with compact badge provenance', () => {
    const group: CompositionQualifierFeedGroup = {
      peerId: 'src',
      peerName: 'Phase poules',
      peerOrder: 0,
      volume: 2,
      rules: [
        {
          key: 'q-src-1',
          volume: 2,
          badge: '1er',
          badgeTone: 'accent',
          context: 'Poule A',
          family: 'place',
          sortPrimary: 1,
          sortSecondary: '1',
          qualPathOrder: 1,
          hubSeats: [
            { entryId: 'e1', teamName: 'Alpha', placed: false, resolved: true },
            { entryId: null, teamName: null, placed: false, resolved: false },
          ],
        },
      ],
    };
    const labels = provenanceLabelsForQualifierRule(group, group.rules[0]);
    expect(labels.full).toContain('Phase poules');
    expect(labels.full).toContain('Poule A');
    expect(labels.badge).toBe('1er·A');

    const stage = {
      status: 'Running',
      compositionEntryIds: ['e1'],
      compositionEntryCount: 1,
      formatKind: 'Groups',
    } as StructureStageHubSummary;

    const rows = listCompositionTeamRows(
      'exploitation',
      stage,
      [{ entryId: 'e1', displayName: 'Alpha', status: 'Active' }],
      [group],
      null,
    );
    expect(rows).toHaveLength(2);
    expect(rows?.[0].kind).toBe('resolved');
    expect(rows?.[1].kind).toBe('unresolved');
  });

  it('compositionTeamRowsResolutionStats counts withdrawn seats as resolved', () => {
    const rows = listCompositionTeamRows(
      'exploitation',
      {
        status: 'Running',
        compositionEntryIds: ['e1', 'e2'],
        compositionEntryCount: 2,
        formatKind: 'Groups',
      } as import('../../types').StructureStageHubSummary,
      [
        { entryId: 'e1', displayName: 'Active', status: 'Active' },
        { entryId: 'e2', displayName: 'Out', status: 'Withdrawn' },
      ],
      [
        {
          peerId: 'src',
          peerName: 'Poules',
          peerOrder: 0,
          volume: 2,
          rules: [
            {
              key: 'q-src-1',
              volume: 1,
              badge: '1er',
              badgeTone: 'accent',
              context: '',
              family: 'place',
              sortPrimary: 1,
              sortSecondary: '1',
              qualPathOrder: 1,
              hubSeats: [
                {
                  entryId: 'e1',
                  teamName: 'Active',
                  placed: true,
                  resolved: true,
                },
              ],
            },
            {
              key: 'q-src-2',
              volume: 1,
              badge: '1er',
              badgeTone: 'accent',
              context: '',
              family: 'place',
              sortPrimary: 1,
              sortSecondary: '2',
              qualPathOrder: 2,
              hubSeats: [
                {
                  entryId: 'e2',
                  teamName: 'Out',
                  placed: true,
                  resolved: true,
                },
              ],
            },
          ],
        },
      ],
      null,
    );
    expect(compositionTeamRowsResolutionStats(rows)).toEqual({
      resolved: 2,
      total: 2,
    });
  });

  it('resolveCompositionResolvedGaugeStats prefers inbound rows in exploitation', () => {
    const rows = listCompositionTeamRows(
      'exploitation',
      {
        status: 'Running',
        compositionEntryIds: ['e1'],
        compositionEntryCount: 1,
        formatKind: 'Groups',
      } as StructureStageHubSummary,
      [{ entryId: 'e1', displayName: 'Alpha', status: 'Active' }],
      [
        {
          peerId: 'src',
          peerName: 'Poules',
          peerOrder: 0,
          volume: 1,
          rules: [
            {
              key: 'q',
              volume: 1,
              badge: '1er',
              badgeTone: 'accent',
              context: '',
              family: 'place',
              sortPrimary: 1,
              sortSecondary: '1',
              qualPathOrder: 1,
              hubSeats: [
                {
                  entryId: 'e1',
                  teamName: 'Alpha',
                  placed: false,
                  resolved: true,
                },
              ],
            },
          ],
        },
      ],
      null,
    );
    expect(
      resolveCompositionResolvedGaugeStats('exploitation', rows, {
        resolved: 99,
        capacity: 8,
      }),
    ).toEqual({ resolved: 1, total: 1 });
  });

  it('resolveCompositionResolvedGaugeStats uses engaged pool vs places when no inbound rows', () => {
    expect(
      resolveCompositionResolvedGaugeStats('exploitation', null, {
        resolved: 6,
        capacity: 8,
      }),
    ).toEqual({ resolved: 6, total: 8 });
    expect(resolveCompositionResolvedGaugeStats('prep', null, null)).toBeNull();
    expect(
      resolveCompositionResolvedGaugeStats('historical', null, {
        resolved: 8,
        capacity: 8,
      }),
    ).toEqual({ resolved: 8, total: 8 });
  });
});

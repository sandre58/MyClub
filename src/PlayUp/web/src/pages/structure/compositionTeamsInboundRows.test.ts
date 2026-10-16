import { describe, expect, it } from 'vitest';
import {
  listCompositionTeamRows,
  provenanceLabelsForQualifierRule,
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
    expect(labels.badge).toBe('1er');

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
});

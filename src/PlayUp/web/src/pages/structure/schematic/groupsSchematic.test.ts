import { describe, expect, it } from 'vitest';
import type { StageSchematic } from '../../../types';
import {
  groupsSchematicFromDrawDraft,
  groupsSchematicFromStageSchematic,
} from './groupsSchematic';

const label = (name: string) => `Groupe ${name}`;

describe('groupsSchematicFromStageSchematic', () => {
  it('orders groups and keeps empty seats as capacity beyond resolved teams', () => {
    const schematic: Pick<StageSchematic, 'cases'> = {
      cases: [
        {
          formPosition: {
            kind: 'GroupPlace',
            groupId: 'gb',
            groupName: 'B',
            index: 1,
          },
          entry: { entryId: 'e1', displayName: 'Bravo' },
          assignment: { entryId: 'e1', displayName: 'Bravo' },
        },
        {
          formPosition: {
            kind: 'GroupPlace',
            groupId: 'ga',
            groupName: 'A',
            index: 1,
          },
          entry: null,
          assignment: null,
        },
        {
          formPosition: {
            kind: 'GroupPlace',
            groupId: 'ga',
            groupName: 'A',
            index: 2,
          },
          entry: { entryId: 'e2', displayName: 'Alpha' },
          assignment: { entryId: 'e2', displayName: 'Alpha' },
        },
      ],
    };

    const { groups, casesByGroupId } = groupsSchematicFromStageSchematic(
      schematic,
      label,
    );

    expect(groups.map((group) => group.name)).toEqual(['Groupe A', 'Groupe B']);
    expect(groups[0]).toMatchObject({
      id: 'ga',
      capacity: 2,
      entries: [{ id: 'e2', displayName: 'Alpha', seat: 1 }],
    });
    expect(casesByGroupId.get('ga')).toHaveLength(2);
  });
});

describe('groupsSchematicFromDrawDraft', () => {
  it('pads each group to placesPerGroup with ghost seats', () => {
    const groups = groupsSchematicFromDrawDraft({
      placements: [
        {
          groupId: 'gb',
          groupDisplayName: 'B',
          entryId: 'e2',
          displayName: 'Beta',
        },
        {
          groupId: 'ga',
          groupDisplayName: 'A',
          entryId: 'e1',
          displayName: 'Alpha',
        },
      ],
      stageGroups: [],
      placesPerGroup: 3,
      unknownEntry: 'Inconnue',
      unknownGroup: 'Groupe',
      groupLabel: label,
    });

    expect(groups.map((group) => group.name)).toEqual(['Groupe A', 'Groupe B']);
    expect(groups[0]).toMatchObject({
      capacity: 3,
      entries: [{ id: 'e1', displayName: 'Alpha' }],
    });
    expect(groups[0]?.entries[0]?.seat).toBeUndefined();
  });
});

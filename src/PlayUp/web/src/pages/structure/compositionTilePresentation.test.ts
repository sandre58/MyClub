import { describe, expect, it } from 'vitest';
import {
  resolveCompositionTileMode,
  resolveEngagedPoolResolution,
  resolvePlacesStripLayout,
  resolveTeamsPoolSource,
  resolveTeamsSectionRuntimePresentation,
  shouldShowCompositionPrepActions,
  shouldShowCompositionTeamsSection,
  listEngagedCompositionEntries,
  hasEngagedTeamsSectionContent,
} from './compositionTilePresentation';

const withActions = (actions: string[]) => ({ actions });

describe('compositionTilePresentation', () => {
  it('resolves prep for Draft and Ready with affectation edit', () => {
    expect(
      resolveCompositionTileMode({ status: 'Draft', actions: ['ReplaceAffectationAuthoring'] }),
    ).toBe('prep');
    expect(
      resolveCompositionTileMode({
        status: 'Ready',
        actions: ['ReplaceAffectationAuthoring'],
      }),
    ).toBe('prep');
  });

  it('resolves exploitation for Running, Suspended, and Ready without affectation edit', () => {
    expect(resolveCompositionTileMode({ status: 'Running', actions: [] })).toBe(
      'exploitation',
    );
    expect(resolveCompositionTileMode({ status: 'Suspended', actions: [] })).toBe(
      'exploitation',
    );
    expect(resolveCompositionTileMode({ status: 'Ready', actions: ['RenameStage'] })).toBe(
      'exploitation',
    );
  });

  it('resolves historical for Completed', () => {
    expect(resolveCompositionTileMode({ status: 'Completed', actions: [] })).toBe(
      'historical',
    );
  });

  it('maps mode to teams source and strip layout', () => {
    expect(resolveTeamsPoolSource('prep')).toBe('authoring');
    expect(resolveTeamsPoolSource('exploitation')).toBe('runtimeOrUndetermined');
    expect(resolvePlacesStripLayout('prep')).toBe('full');
    expect(resolvePlacesStripLayout('exploitation')).toBe('compactSummary');
    expect(resolvePlacesStripLayout('historical')).toBe('historical');
    expect(shouldShowCompositionPrepActions('prep')).toBe(true);
    expect(shouldShowCompositionPrepActions('exploitation')).toBe(false);
  });

  it('resolveEngagedPoolResolution returns undetermined without hub ids and empty runtime', () => {
    expect(
      resolveEngagedPoolResolution({
        compositionEntryIds: null,
        compositionEntryCount: 0,
        compositionPreviewNames: null,
        formatKind: 'Groups',
      }),
    ).toEqual({ kind: 'undetermined' });
  });

  it('listEngagedCompositionEntries sorts hub ids', () => {
    const entries = [
      { entryId: 'b', displayName: 'Bravo', status: 'Active' as const },
      { entryId: 'a', displayName: 'Alpha', status: 'Active' as const },
    ];
    expect(
      listEngagedCompositionEntries(
        {
          compositionEntryIds: ['b', 'a'],
          compositionEntryCount: 2,
          compositionPreviewNames: null,
          formatKind: 'Groups',
        } as import('../../types').StructureStageHubSummary,
        entries,
      ).map((e) => e.entryId),
    ).toEqual(['a', 'b']);
  });

  it('hasEngagedTeamsSectionContent is true for undetermined exploitation pool', () => {
    expect(
      hasEngagedTeamsSectionContent(
        'exploitation',
        {
          compositionEntryIds: null,
          compositionEntryCount: 0,
          compositionPreviewNames: null,
          compositionIneligibleCount: 0,
          affectationEntryIds: ['x'],
          affectationPreviewNames: null,
          affectationIneligibleCount: 0,
          formatKind: 'Groups',
        },
        [],
      ),
    ).toBe(true);
  });

  it('shouldShowCompositionTeamsSection is always true in exploitation and historical', () => {
    expect(
      shouldShowCompositionTeamsSection('exploitation', false, false, false),
    ).toBe(true);
    expect(
      shouldShowCompositionTeamsSection('historical', false, false, false),
    ).toBe(true);
  });

  it('shouldShowCompositionTeamsSection keeps prep visibility rules', () => {
    expect(
      shouldShowCompositionTeamsSection('prep', false, false, false),
    ).toBe(false);
    expect(
      shouldShowCompositionTeamsSection('prep', true, false, false),
    ).toBe(true);
    expect(
      shouldShowCompositionTeamsSection('prep', false, true, false),
    ).toBe(true);
  });

  it('resolveTeamsSectionRuntimePresentation classifies empty and undetermined pools', () => {
    const stageBase = {
      compositionEntryIds: [] as string[],
      compositionEntryCount: 0,
      compositionPreviewNames: null,
      compositionIneligibleCount: 0,
      formatKind: 'Groups' as const,
    };
    expect(
      resolveTeamsSectionRuntimePresentation('exploitation', stageBase, [], null),
    ).toEqual({ kind: 'empty' });
    expect(
      resolveTeamsSectionRuntimePresentation(
        'exploitation',
        {
          ...stageBase,
          compositionEntryIds: null,
        },
        [],
        null,
      ),
    ).toEqual({ kind: 'undetermined' });
    expect(
      resolveTeamsSectionRuntimePresentation('prep', stageBase, [], null),
    ).toEqual({ kind: 'list' });
  });

  it('resolveEngagedPoolResolution uses ids when present', () => {
    expect(
      resolveEngagedPoolResolution({
        compositionEntryIds: ['a', 'b'],
        compositionEntryCount: 2,
        compositionPreviewNames: null,
        formatKind: 'Cup',
      }),
    ).toEqual({ kind: 'list', entryIds: ['a', 'b'] });
  });
});

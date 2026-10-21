// -----------------------------------------------------------------------
// Composition tile — lifecycle presentation mode (UI only; not authorization).
// SoT: docs/design/structure-lifecycle/composition-tile-lifecycle-presentation-2026-10-16.md
// -----------------------------------------------------------------------

import type {
  StructureEntry,
  StructureFormatKind,
  StructureStageHubSummary,
} from '../../types';

const EMPTY_ENTRY_IDS: string[] = [];

export type CompositionTileMode = 'prep' | 'exploitation' | 'historical';

export type TeamsPoolSource = 'authoring' | 'runtimeOrUndetermined';

export type PlacesStripLayout = 'full' | 'compactSummary' | 'historical';

const ACTION_REPLACE_AFFECTATION = 'ReplaceAffectationAuthoring';

export function canEditAffectationAuthoring(
  stage: Pick<StructureStageHubSummary, 'actions'>,
): boolean {
  return (stage.actions ?? []).includes(ACTION_REPLACE_AFFECTATION);
}

export function resolveCompositionTileMode(
  stage: Pick<StructureStageHubSummary, 'status' | 'actions'>,
): CompositionTileMode {
  if (stage.status === 'Completed') {
    return 'historical';
  }
  if (stage.status === 'Running' || stage.status === 'Suspended') {
    return 'exploitation';
  }
  if (stage.status === 'Ready' && !canEditAffectationAuthoring(stage)) {
    return 'exploitation';
  }
  if (stage.status === 'Draft' || stage.status === 'Ready') {
    return 'prep';
  }
  return 'exploitation';
}

export function resolveTeamsPoolSource(
  mode: CompositionTileMode,
): TeamsPoolSource {
  return mode === 'prep' ? 'authoring' : 'runtimeOrUndetermined';
}

export function resolvePlacesStripLayout(
  mode: CompositionTileMode,
): PlacesStripLayout {
  if (mode === 'prep') {
    return 'full';
  }
  if (mode === 'historical') {
    return 'historical';
  }
  return 'compactSummary';
}

export function shouldShowCompositionPrepActions(
  mode: CompositionTileMode,
): boolean {
  return mode === 'prep';
}

/** Hub exposes runtime pool lists for structure GET stages (Draw / Live pool). */
export function isEngagedPoolSignalReliable(
  _formatKind: StructureFormatKind | null | undefined,
): boolean {
  return true;
}

export type EngagedPoolResolution =
  { kind: 'list'; entryIds: readonly string[] } | { kind: 'undetermined' };

export function resolveEngagedPoolResolution(
  stage: Pick<
    StructureStageHubSummary,
    | 'compositionEntryIds'
    | 'compositionEntryCount'
    | 'compositionPreviewNames'
    | 'formatKind'
  >,
): EngagedPoolResolution {
  if (!isEngagedPoolSignalReliable(stage.formatKind)) {
    return { kind: 'undetermined' };
  }
  const ids = stage.compositionEntryIds;
  if (ids != null) {
    return { kind: 'list', entryIds: ids };
  }
  const count = stage.compositionEntryCount ?? 0;
  const previews = stage.compositionPreviewNames ?? [];
  if (count > 0 || previews.length > 0) {
    return { kind: 'list', entryIds: [] };
  }
  return { kind: 'undetermined' };
}

export function resolveEngagedPoolMeterCurrent(
  stage: Pick<StructureStageHubSummary, 'compositionEntryCount'>,
): number {
  return stage.compositionEntryCount ?? 0;
}

export function resolveCompositionPopulationGaugeCurrent(
  mode: CompositionTileMode,
  stage: StructureStageHubSummary,
  expectedPopulationOccupancy: number,
): number {
  if (mode === 'prep') {
    return expectedPopulationOccupancy;
  }
  return resolveEngagedPoolMeterCurrent(stage);
}

export function listEngagedCompositionEntries(
  stage: StructureStageHubSummary,
  entries: readonly StructureEntry[],
): StructureEntry[] {
  const resolution = resolveEngagedPoolResolution(stage);
  if (resolution.kind === 'undetermined') {
    return [];
  }
  const byId = new Map(entries.map((entry) => [entry.entryId, entry]));
  return resolution.entryIds
    .map((id) => byId.get(id))
    .filter((entry): entry is StructureEntry => entry != null)
    .sort((a, b) =>
      a.displayName.localeCompare(b.displayName, undefined, {
        sensitivity: 'base',
      }),
    );
}

/** Composition band — Équipes section visibility (prep rules unchanged). */
export function shouldShowCompositionTeamsSection(
  mode: CompositionTileMode,
  teamsHasListContent: boolean,
  showQualifiersSection: boolean,
  showAffectationSection: boolean,
): boolean {
  if (mode === 'exploitation' || mode === 'historical') {
    return true;
  }
  return teamsHasListContent || showQualifiersSection || showAffectationSection;
}

export type TeamsSectionRuntimePresentation =
  { kind: 'list' } | { kind: 'empty' } | { kind: 'undetermined' };

export function resolveTeamsSectionRuntimePresentation(
  mode: CompositionTileMode,
  stage: Pick<
    StructureStageHubSummary,
    | 'compositionEntryIds'
    | 'compositionEntryCount'
    | 'compositionPreviewNames'
    | 'compositionIneligibleCount'
    | 'formatKind'
  >,
  entries: readonly StructureEntry[],
  compositionTeamRows:
    import('./compositionTeamsInboundRows').CompositionTeamRow[] | null,
): TeamsSectionRuntimePresentation {
  if (mode === 'prep') {
    return { kind: 'list' };
  }
  if (compositionTeamRows != null && compositionTeamRows.length > 0) {
    return { kind: 'list' };
  }
  if (resolveEngagedPoolResolution(stage).kind === 'undetermined') {
    return { kind: 'undetermined' };
  }
  const engaged = listEngagedCompositionEntries(
    stage as StructureStageHubSummary,
    entries,
  );
  const previewNames = stage.compositionPreviewNames ?? [];
  const ineligible = stage.compositionIneligibleCount ?? 0;
  if (engaged.length > 0 || previewNames.length > 0 || ineligible > 0) {
    return { kind: 'list' };
  }
  return { kind: 'empty' };
}

export function hasEngagedTeamsSectionContent(
  mode: CompositionTileMode,
  stage: Pick<
    StructureStageHubSummary,
    | 'affectationEntryIds'
    | 'affectationPreviewNames'
    | 'affectationIneligibleCount'
    | 'compositionEntryIds'
    | 'compositionEntryCount'
    | 'compositionPreviewNames'
    | 'compositionIneligibleCount'
    | 'formatKind'
  >,
  entries: readonly StructureEntry[],
  options?: { mergeInboundQualProg?: boolean },
): boolean {
  if (options?.mergeInboundQualProg) {
    return true;
  }
  if (mode === 'prep') {
    const composedIds = stage.affectationEntryIds ?? EMPTY_ENTRY_IDS;
    return (
      composedIds.length > 0 ||
      (stage.affectationPreviewNames?.length ?? 0) > 0 ||
      (stage.affectationIneligibleCount ?? 0) > 0
    );
  }
  if (resolveEngagedPoolResolution(stage).kind === 'undetermined') {
    return true;
  }
  const engaged = listEngagedCompositionEntries(
    stage as StructureStageHubSummary,
    entries,
  );
  return (
    engaged.length > 0 ||
    (stage.compositionPreviewNames?.length ?? 0) > 0 ||
    (stage.compositionIneligibleCount ?? 0) > 0 ||
    (stage.compositionEntryCount ?? 0) > 0
  );
}

import type { StructureEntry, StructureStageHubSummary } from '../../types';

const EMPTY_ENTRY_IDS: readonly string[] = [];

export function listComposedAffectationEntries(
  stage: StructureStageHubSummary,
  entries: readonly StructureEntry[],
): StructureEntry[] {
  const composedIds = stage.affectationEntryIds ?? EMPTY_ENTRY_IDS;
  const byId = new Map(entries.map((entry) => [entry.entryId, entry]));
  return composedIds
    .map((id) => byId.get(id))
    .filter((entry): entry is StructureEntry => entry != null)
    .sort((a, b) =>
      a.displayName.localeCompare(b.displayName, undefined, {
        sensitivity: 'base',
      }),
    );
}

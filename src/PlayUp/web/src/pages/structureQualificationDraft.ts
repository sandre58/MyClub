// -----------------------------------------------------------------------
// Qualification Intent drafts — Expand/Map client preview (mirrors Domain).
// -----------------------------------------------------------------------

import type {
  QualificationIntentSourceKind,
  QualificationMappingMode,
  RankingScope,
  StructureQualificationIntent,
  StructureQualificationPath,
  StructureQualificationSlotOverride,
} from '../types';

export type QualIntentDraft = {
  id: string;
  validated: boolean;
  sourceKind: QualificationIntentSourceKind;
  groupId: string;
  groupName: string;
  positionFrom: string;
  positionTo: string;
  acrossGroupsPosition: string;
  conditionKind: 'none' | 'points';
  minimumPoints: string;
  destinationStageId: string;
  mappingMode: QualificationMappingMode;
  slotOverrides: StructureQualificationSlotOverride[];
  /** UI: destinations list expanded (Canonical starts collapsed). */
  showDestinations: boolean;
};

export type SourceOccurrence = {
  scope: RankingScope;
  position: number;
  groupId?: string | null;
  groupName?: string | null;
  acrossGroupsPosition?: number | null;
};

export type MappedDestination = {
  occurrence: SourceOccurrence;
  slotKey: string;
  label: string;
};

export function newIntentId(): string {
  return crypto.randomUUID();
}

export function emptyQualIntent(destinationStageId = ''): QualIntentDraft {
  return {
    id: newIntentId(),
    validated: false,
    sourceKind: 'EachGroup',
    groupId: '',
    groupName: '',
    positionFrom: '1',
    positionTo: '1',
    acrossGroupsPosition: '1',
    conditionKind: 'none',
    minimumPoints: '',
    destinationStageId,
    mappingMode: 'Canonical',
    slotOverrides: [],
    showDestinations: false,
  };
}

export function intentFromApi(
  intent: StructureQualificationIntent,
): QualIntentDraft {
  return {
    id: intent.intentId,
    validated: true,
    sourceKind: intent.sourceKind,
    groupId: intent.groupId ?? '',
    groupName: intent.groupName ?? '',
    positionFrom: String(intent.positionFrom),
    positionTo: String(intent.positionTo),
    acrossGroupsPosition:
      intent.acrossGroupsPosition != null
        ? String(intent.acrossGroupsPosition)
        : '1',
    conditionKind:
      intent.minimumPoints != null && intent.minimumPoints >= 0
        ? 'points'
        : 'none',
    minimumPoints:
      intent.minimumPoints != null ? String(intent.minimumPoints) : '',
    destinationStageId: intent.destinationStageId,
    mappingMode: intent.mappingMode,
    slotOverrides: intent.slotOverrides ? [...intent.slotOverrides] : [],
    showDestinations: intent.mappingMode === 'Custom',
  };
}

/** Legacy fallback: one singleton intent per path. */
export function pathToSingletonIntent(
  path: StructureQualificationPath,
): QualIntentDraft {
  const scope = path.rankingScope;
  let sourceKind: QualificationIntentSourceKind = 'Overall';
  if (scope === 'AcrossGroups' || path.acrossGroupsPosition != null) {
    sourceKind = 'AcrossGroups';
  } else if (scope === 'Group' || path.groupId) {
    sourceKind = 'SingleGroup';
  }

  const k = path.selectionValue;
  const occurrence: StructureQualificationSlotOverride = {
    scope:
      sourceKind === 'AcrossGroups'
        ? 'AcrossGroups'
        : sourceKind === 'SingleGroup'
          ? 'Group'
          : 'Overall',
    position: k,
    slotKey: path.destinationSlotKey,
    groupId: path.groupId ?? null,
    acrossGroupsPosition: path.acrossGroupsPosition ?? null,
  };

  return {
    id: newIntentId(),
    validated: true,
    sourceKind,
    groupId: path.groupId ?? '',
    groupName: path.groupName ?? '',
    positionFrom: String(k),
    positionTo: String(k),
    acrossGroupsPosition:
      path.acrossGroupsPosition != null
        ? String(path.acrossGroupsPosition)
        : '1',
    conditionKind:
      path.minimumPoints != null && path.minimumPoints >= 0 ? 'points' : 'none',
    minimumPoints:
      path.minimumPoints != null ? String(path.minimumPoints) : '',
    destinationStageId: path.destinationStageId,
    mappingMode: 'Custom',
    slotOverrides: [occurrence],
    showDestinations: true,
  };
}

export function parsePositiveInt(raw: string): number | null {
  const n = Number(raw);
  if (!Number.isInteger(n) || n < 1) return null;
  return n;
}

export function expandOccurrences(
  draft: QualIntentDraft,
  groups: { id: string; name: string }[],
): SourceOccurrence[] {
  const from = parsePositiveInt(draft.positionFrom);
  const to = parsePositiveInt(draft.positionTo);
  if (from == null || to == null || to < from) return [];

  const list: SourceOccurrence[] = [];
  if (draft.sourceKind === 'EachGroup') {
    for (const g of groups) {
      for (let k = from; k <= to; k++) {
        list.push({
          scope: 'Group',
          position: k,
          groupId: g.id,
          groupName: g.name,
        });
      }
    }
  } else if (draft.sourceKind === 'SingleGroup') {
    if (!draft.groupId.trim()) return [];
    for (let k = from; k <= to; k++) {
      list.push({
        scope: 'Group',
        position: k,
        groupId: draft.groupId,
        groupName: draft.groupName || draft.groupId,
      });
    }
  } else if (draft.sourceKind === 'Overall') {
    for (let k = from; k <= to; k++) {
      list.push({ scope: 'Overall', position: k });
    }
  } else {
    const p = parsePositiveInt(draft.acrossGroupsPosition);
    if (p == null) return [];
    for (let k = from; k <= to; k++) {
      list.push({
        scope: 'AcrossGroups',
        position: k,
        acrossGroupsPosition: p,
      });
    }
  }
  return list;
}

function occurrenceKey(o: SourceOccurrence): string {
  return [
    o.scope,
    o.position,
    o.groupId ?? '',
    o.acrossGroupsPosition ?? '',
  ].join('\0');
}

export function mapDestinations(
  draft: QualIntentDraft,
  groups: { id: string; name: string }[],
  slotKeys: string[],
  ordinal: (n: number) => string,
  t: (key: string, opts?: Record<string, unknown>) => string,
): MappedDestination[] {
  const occurrences = expandOccurrences(draft, groups);
  if (occurrences.length === 0) return [];

  const overrideMap = new Map(
    draft.slotOverrides.map((o) => [
      occurrenceKey({
        scope: o.scope,
        position: o.position,
        groupId: o.groupId,
        acrossGroupsPosition: o.acrossGroupsPosition,
      }),
      o.slotKey,
    ]),
  );

  return occurrences.map((occurrence, index) => {
    const canonical = slotKeys[index] ?? '';
    const slotKey =
      draft.mappingMode === 'Custom'
        ? (overrideMap.get(occurrenceKey(occurrence)) ?? canonical)
        : canonical;
    return {
      occurrence,
      slotKey,
      label: occurrenceLabel(occurrence, ordinal, t),
    };
  });
}

export function occurrenceLabel(
  o: SourceOccurrence,
  ordinal: (n: number) => string,
  t: (key: string, opts?: Record<string, unknown>) => string,
): string {
  const rank = ordinal(o.position);
  if (o.scope === 'Group') {
    return t('qualification.summary.group', {
      rank,
      group: o.groupName || t('qualification.unnamedGroup'),
    });
  }
  if (o.scope === 'AcrossGroups') {
    const place = ordinal(o.acrossGroupsPosition ?? 1);
    return o.position <= 1
      ? t('qualification.summary.acrossBest', { place })
      : t('qualification.summary.acrossNth', { rank, place });
  }
  return t('qualification.summary.overall', { rank });
}

export function summarizeIntentWho(
  draft: QualIntentDraft,
  locale: string,
  t: (key: string, opts?: Record<string, unknown>) => string,
): string {
  const from = parsePositiveInt(draft.positionFrom) ?? 0;
  const to = parsePositiveInt(draft.positionTo) ?? 0;
  const ord = (n: number) => ordinalRank(Math.max(n, 1), locale);

  let who: string;
  if (draft.sourceKind === 'EachGroup') {
    who =
      from === to
        ? t('qualification.summary.eachGroupOne', { rank: ord(from) })
        : to === from + 1
          ? t('qualification.summary.eachGroupRange', {
              from: ord(from),
              to: ord(to),
            })
          : t('qualification.summary.eachGroupSpan', {
              from: ord(from),
              to: ord(to),
            });
  } else if (draft.sourceKind === 'SingleGroup') {
    const group =
      draft.groupName.trim() || t('qualification.unnamedGroup');
    who =
      from === to
        ? t('qualification.summary.group', { rank: ord(from), group })
        : t('qualification.summary.groupRange', {
            from: ord(from),
            to: ord(to),
            group,
          });
  } else if (draft.sourceKind === 'Overall') {
    who =
      from === to
        ? t('qualification.summary.overall', { rank: ord(from) })
        : t('qualification.summary.overallRange', {
            from: ord(from),
            to: ord(to),
          });
  } else {
    const place = ord(parsePositiveInt(draft.acrossGroupsPosition) ?? 1);
    who =
      from === to && from <= 1
        ? t('qualification.summary.acrossBest', { place })
        : from === to
          ? t('qualification.summary.acrossNth', {
              rank: ord(from),
              place,
            })
          : t('qualification.summary.acrossRange', {
              from: ord(from),
              to: ord(to),
              place,
            });
  }

  if (draft.conditionKind === 'points') {
    const pts = Number(draft.minimumPoints);
    if (Number.isFinite(pts)) {
      who = t('qualification.summary.withPoints', { who, points: pts });
    }
  }
  return who;
}

export function ordinalRank(n: number, locale: string): string {
  if (locale.startsWith('fr')) {
    return n === 1 ? '1er' : `${n}e`;
  }
  const mod100 = n % 100;
  if (mod100 >= 11 && mod100 <= 13) return `${n}th`;
  switch (n % 10) {
    case 1:
      return `${n}st`;
    case 2:
      return `${n}nd`;
    case 3:
      return `${n}rd`;
    default:
      return `${n}th`;
  }
}

export function isIntentComplete(
  draft: QualIntentDraft,
  groups: { id: string; name: string }[],
  slotKeys: string[],
): boolean {
  const occurrences = expandOccurrences(draft, groups);
  if (occurrences.length === 0) return false;
  if (!draft.destinationStageId.trim()) return false;
  if (draft.sourceKind === 'SingleGroup' && !draft.groupId.trim()) return false;
  if (draft.conditionKind === 'points') {
    const pts = Number(draft.minimumPoints);
    if (!Number.isFinite(pts) || pts < 0) return false;
  }
  if (occurrences.length > slotKeys.length) return false;
  const mapped = mapDestinations(
    draft,
    groups,
    slotKeys,
    (n) => String(n),
    () => '',
  );
  if (mapped.some((m) => !m.slotKey.trim())) return false;
  const slots = mapped.map((m) => m.slotKey);
  if (new Set(slots).size !== slots.length) return false;
  return true;
}

export function findDuplicateSlotsAcrossIntents(
  intents: QualIntentDraft[],
  groups: { id: string; name: string }[],
  slotKeysByStage: Map<string, string[]>,
): Set<string> {
  const seen = new Map<string, number>();
  const dup = new Set<string>();
  for (const intent of intents) {
    if (!intent.validated) continue;
    const slots = slotKeysByStage.get(intent.destinationStageId) ?? [];
    const mapped = mapDestinations(
      intent,
      groups,
      slots,
      (n) => String(n),
      () => '',
    );
    for (const m of mapped) {
      if (!m.slotKey.trim()) continue;
      const key = `${intent.destinationStageId}\0${m.slotKey}`;
      const count = (seen.get(key) ?? 0) + 1;
      seen.set(key, count);
      if (count > 1) dup.add(key);
    }
  }
  return dup;
}

export function toApiIntent(draft: QualIntentDraft, order: number) {
  return {
    intentId: draft.id,
    order,
    sourceKind: draft.sourceKind,
    positionFrom: parsePositiveInt(draft.positionFrom) ?? 1,
    positionTo: parsePositiveInt(draft.positionTo) ?? 1,
    destinationStageId: draft.destinationStageId,
    mappingMode: draft.mappingMode,
    groupId:
      draft.sourceKind === 'SingleGroup' ? draft.groupId || null : null,
    acrossGroupsPosition:
      draft.sourceKind === 'AcrossGroups'
        ? (parsePositiveInt(draft.acrossGroupsPosition) ?? 1)
        : null,
    minimumPoints:
      draft.conditionKind === 'points' ? Number(draft.minimumPoints) : null,
    slotOverrides:
      draft.mappingMode === 'Custom' && draft.slotOverrides.length > 0
        ? draft.slotOverrides
        : null,
  };
}

export function applySlotOverride(
  draft: QualIntentDraft,
  occurrence: SourceOccurrence,
  slotKey: string,
  groups: { id: string; name: string }[],
  slotKeys: string[],
): QualIntentDraft {
  const mapped = mapDestinations(
    { ...draft, mappingMode: 'Canonical' },
    groups,
    slotKeys,
    (n) => String(n),
    () => '',
  );
  const overrides: StructureQualificationSlotOverride[] = mapped.map((m) => {
    const isTarget =
      occurrenceKey(m.occurrence) === occurrenceKey(occurrence);
    return {
      scope: m.occurrence.scope,
      position: m.occurrence.position,
      slotKey: isTarget ? slotKey : m.slotKey,
      groupId: m.occurrence.groupId ?? null,
      acrossGroupsPosition: m.occurrence.acrossGroupsPosition ?? null,
    };
  });
  return {
    ...draft,
    mappingMode: 'Custom',
    slotOverrides: overrides,
    showDestinations: true,
  };
}

export function resetToCanonical(draft: QualIntentDraft): QualIntentDraft {
  return {
    ...draft,
    mappingMode: 'Canonical',
    slotOverrides: [],
    showDestinations: false,
  };
}

// -----------------------------------------------------------------------
// Qualification Intent drafts — Expand client preview (Qual V2 population).
// -----------------------------------------------------------------------

import type {
  QualificationIntentSourceKind,
  RankingScope,
  StructureQualificationIntent,
  StructureQualificationPath,
} from '../types';

export type QualIntentDraft = {
  id: string;
  sourceKind: QualificationIntentSourceKind;
  groupId: string;
  groupName: string;
  positionFrom: string;
  positionTo: string;
  acrossGroupsPosition: string;
  conditionKind: 'none' | 'points';
  minimumPoints: string;
  destinationStageId: string;
};

export type SourceOccurrence = {
  scope: RankingScope;
  position: number;
  groupId?: string | null;
  groupName?: string | null;
  acrossGroupsPosition?: number | null;
};

export function newIntentId(): string {
  return crypto.randomUUID();
}

export function emptyQualIntent(destinationStageId = ''): QualIntentDraft {
  return {
    id: newIntentId(),
    sourceKind: 'EachGroup',
    groupId: '',
    groupName: '',
    positionFrom: '1',
    positionTo: '1',
    acrossGroupsPosition: '1',
    conditionKind: 'none',
    minimumPoints: '',
    destinationStageId,
  };
}

export function intentFromApi(
  intent: StructureQualificationIntent,
): QualIntentDraft {
  return {
    id: intent.intentId,
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
  return {
    id: newIntentId(),
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
): boolean {
  return incompleteIntentReason(draft, groups) == null;
}

/**
 * Dominant incompleteness cause for collapsed-row hint (one message).
 * Returns an i18n key suffix under `qualification.incompleteHint*`.
 */
export function incompleteIntentReason(
  draft: QualIntentDraft,
  groups: { id: string; name: string }[],
):
  | 'Destination'
  | 'Group'
  | 'Selection'
  | 'Points'
  | null {
  if (!draft.destinationStageId.trim()) return 'Destination';
  if (draft.sourceKind === 'SingleGroup' && !draft.groupId.trim()) {
    return 'Group';
  }
  if (draft.conditionKind === 'points') {
    const pts = Number(draft.minimumPoints);
    if (!Number.isFinite(pts) || pts < 0) return 'Points';
  }
  if (expandOccurrences(draft, groups).length === 0) return 'Selection';
  return null;
}

export function toApiIntent(draft: QualIntentDraft, order: number) {
  return {
    intentId: draft.id,
    order,
    sourceKind: draft.sourceKind,
    positionFrom: parsePositiveInt(draft.positionFrom) ?? 1,
    positionTo: parsePositiveInt(draft.positionTo) ?? 1,
    destinationStageId: draft.destinationStageId,
    groupId:
      draft.sourceKind === 'SingleGroup' ? draft.groupId || null : null,
    acrossGroupsPosition:
      draft.sourceKind === 'AcrossGroups'
        ? (parsePositiveInt(draft.acrossGroupsPosition) ?? 1)
        : null,
    minimumPoints:
      draft.conditionKind === 'points' ? Number(draft.minimumPoints) : null,
  };
}

/** Stable fingerprint of intents (save payload shape) for dirty detection. */
export function serializeIntents(intents: QualIntentDraft[]): string {
  return JSON.stringify(
    intents.map((intent, index) => toApiIntent(intent, index + 1)),
  );
}

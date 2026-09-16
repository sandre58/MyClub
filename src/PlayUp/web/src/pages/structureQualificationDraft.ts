import type { RankingScope, StructureQualificationPath } from '../types';

export type QualScopeUi = 'Group' | 'Overall' | 'AcrossGroups';

export type QualRowDraft = {
  /** Stable local id (not Domain Order). */
  id: string;
  /** Committed into the local path list (Valider). */
  validated: boolean;
  scope: QualScopeUi;
  groupId: string;
  groupName: string;
  /** Selection Position k. */
  position: string;
  /** AcrossGroupsPosition P (AcrossGroups only). */
  acrossGroupsPosition: string;
  conditionKind: 'none' | 'points';
  minimumPoints: string;
  destinationStageId: string;
  destinationSlotKey: string;
};

export function newQualRowId(): string {
  return `qual-${Math.random().toString(36).slice(2, 10)}`;
}

export function emptyQualRow(destinationStageId = ''): QualRowDraft {
  return {
    id: newQualRowId(),
    validated: false,
    scope: 'Group',
    groupId: '',
    groupName: '',
    position: '',
    acrossGroupsPosition: '',
    conditionKind: 'none',
    minimumPoints: '',
    destinationStageId,
    destinationSlotKey: '',
  };
}

export function pathToQualRow(path: StructureQualificationPath): QualRowDraft {
  const scope = normalizeScope(path.rankingScope, path.groupId);
  return {
    id: newQualRowId(),
    validated: true,
    scope,
    groupId: path.groupId ?? '',
    groupName: path.groupName ?? '',
    position: String(path.selectionValue),
    acrossGroupsPosition:
      path.acrossGroupsPosition != null
        ? String(path.acrossGroupsPosition)
        : '',
    conditionKind:
      path.minimumPoints != null && path.minimumPoints >= 0 ? 'points' : 'none',
    minimumPoints:
      path.minimumPoints != null ? String(path.minimumPoints) : '',
    destinationStageId: path.destinationStageId,
    destinationSlotKey: path.destinationSlotKey,
  };
}

function normalizeScope(
  rankingScope: RankingScope | null | undefined,
  groupId: string | null | undefined,
): QualScopeUi {
  if (rankingScope === 'AcrossGroups') return 'AcrossGroups';
  if (rankingScope === 'Group' || groupId) return 'Group';
  return 'Overall';
}

export function parsePositiveInt(raw: string): number | null {
  const n = Number(raw);
  if (!Number.isInteger(n) || n < 1) return null;
  return n;
}

export function isQualRowComplete(row: QualRowDraft): boolean {
  const position = parsePositiveInt(row.position);
  if (position == null) return false;
  if (!row.destinationStageId.trim() || !row.destinationSlotKey.trim()) {
    return false;
  }
  if (row.scope === 'Group' && !row.groupId.trim()) return false;
  if (row.scope === 'AcrossGroups') {
    if (parsePositiveInt(row.acrossGroupsPosition) == null) return false;
  }
  if (row.conditionKind === 'points') {
    const pts = Number(row.minimumPoints);
    if (!Number.isFinite(pts) || pts < 0) return false;
  }
  return true;
}

export function findDuplicateSlotKeys(
  rows: QualRowDraft[],
): Set<string> {
  const seen = new Map<string, number>();
  const dup = new Set<string>();
  for (const row of rows) {
    if (!row.validated) continue;
    const stageId = row.destinationStageId.trim();
    const slot = row.destinationSlotKey.trim();
    if (!stageId || !slot) continue;
    const key = `${stageId}\0${slot}`;
    const count = (seen.get(key) ?? 0) + 1;
    seen.set(key, count);
    if (count > 1) dup.add(key);
  }
  return dup;
}

export function destinationKey(row: QualRowDraft): string {
  return `${row.destinationStageId.trim()}\0${row.destinationSlotKey.trim()}`;
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

export function summarizeWho(
  row: QualRowDraft,
  locale: string,
  t: (key: string, opts?: Record<string, unknown>) => string,
): string {
  const k = parsePositiveInt(row.position) ?? 0;
  const ordK = ordinalRank(Math.max(k, 1), locale);

  let who: string;
  if (row.scope === 'Group') {
    const group =
      row.groupName.trim() ||
      t('qualification.unnamedGroup');
    who = t('qualification.summary.group', { rank: ordK, group });
  } else if (row.scope === 'Overall') {
    who = t('qualification.summary.overall', { rank: ordK });
  } else {
    const p = parsePositiveInt(row.acrossGroupsPosition) ?? 0;
    const ordP = ordinalRank(Math.max(p, 1), locale);
    who =
      k <= 1
        ? t('qualification.summary.acrossBest', { place: ordP })
        : t('qualification.summary.acrossNth', {
            rank: ordK,
            place: ordP,
          });
  }

  if (row.conditionKind === 'points') {
    const pts = Number(row.minimumPoints);
    if (Number.isFinite(pts)) {
      who = t('qualification.summary.withPoints', {
        who,
        points: pts,
      });
    }
  }
  return who;
}

export function toApiPath(
  row: QualRowDraft,
  order: number,
): StructureQualificationPath {
  const selectionValue = parsePositiveInt(row.position) ?? 1;
  const path: StructureQualificationPath = {
    order,
    selectionMode: 'Position',
    selectionValue,
    destinationStageId: row.destinationStageId.trim(),
    destinationSlotKey: row.destinationSlotKey.trim(),
    rankingScope: row.scope,
  };
  if (row.scope === 'Group') {
    path.groupId = row.groupId.trim();
    path.groupName = row.groupName.trim() || null;
  }
  if (row.scope === 'AcrossGroups') {
    path.acrossGroupsPosition =
      parsePositiveInt(row.acrossGroupsPosition) ?? 1;
  }
  if (row.conditionKind === 'points') {
    path.minimumPoints = Number(row.minimumPoints);
  }
  return path;
}

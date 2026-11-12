import type {
  DefinitiveResolutionReleasePlace,
  DefinitiveResolutionReleaseResponse,
  DefinitiveResolutionReleaseRow,
} from '../../../api';

export function primaryDefinitiveReleases(
  preview: DefinitiveResolutionReleaseResponse,
): DefinitiveResolutionReleaseRow[] {
  return preview.releases.filter((row) => !row.sideEffect);
}

export function definitivePlaceText(
  t: (key: string) => string,
  row: {
    placeLabel?: string | null;
    slotKey?: string | null;
    target: string;
  },
): string {
  const label = row.placeLabel?.trim();
  if (label) {
    return label;
  }
  const slot = row.slotKey?.trim();
  if (slot) {
    return slot;
  }
  const translate = t as (
    key: string,
    options?: { defaultValue: string },
  ) => string;
  return translate(`resolution.release.target.${row.target}`, {
    defaultValue: row.target,
  });
}

export function definitiveReleasePlaces(
  preview: DefinitiveResolutionReleaseResponse,
): DefinitiveResolutionReleasePlace[] {
  return primaryDefinitiveReleases(preview).map((row) => ({
    stageId: row.stageId,
    target: row.target,
    entryId: row.entryId,
    slotKey: row.slotKey,
    groupId: row.groupId,
  }));
}

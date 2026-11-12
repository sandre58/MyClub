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

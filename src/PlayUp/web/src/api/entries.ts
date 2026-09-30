/**
 * Competition entries and declared members.
 */
import type {
  AddDeclaredMemberRequest,
  AddEntryRequest,
  EntryIdsRequest,
  MemberIdsRequest,
  RenameDeclaredMemberRequest,
  RenameEntryRequest,
  StructureView,
  UpdateEntryPresentationRequest,
} from '../types';
import { sendJson } from './http';

/** POST /competitions/{id}/entries → StructureView */
export function addCompetitionEntry(
  competitionId: string,
  request: AddEntryRequest,
): Promise<StructureView> {
  return sendJson('POST', `/competitions/${competitionId}/entries`, request);
}

/** POST .../entries/{entryId}/presentation → StructureView */
export function updateEntryPresentation(
  competitionId: string,
  entryId: string,
  request: UpdateEntryPresentationRequest,
): Promise<StructureView> {
  return sendJson(
    'POST',
    `/competitions/${competitionId}/entries/${entryId}/presentation`,
    request,
  );
}

/** POST .../entries/{entryId}/rename → StructureView */
export function renameCompetitionEntry(
  competitionId: string,
  entryId: string,
  request: RenameEntryRequest,
): Promise<StructureView> {
  return sendJson(
    'POST',
    `/competitions/${competitionId}/entries/${entryId}/rename`,
    request,
  );
}

/** POST .../entries/{entryId}/withdraw → StructureView */
export function withdrawCompetitionEntry(
  competitionId: string,
  entryId: string,
): Promise<StructureView> {
  return sendJson(
    'POST',
    `/competitions/${competitionId}/entries/${entryId}/withdraw`,
  );
}

/** POST .../entries/{entryId}/declared-members → StructureView */
export function addDeclaredMember(
  competitionId: string,
  entryId: string,
  request: AddDeclaredMemberRequest,
): Promise<StructureView> {
  return sendJson(
    'POST',
    `/competitions/${competitionId}/entries/${entryId}/declared-members`,
    request,
  );
}

/** DELETE .../declared-members/{memberId} → StructureView */
export function removeDeclaredMember(
  competitionId: string,
  entryId: string,
  memberId: string,
): Promise<StructureView> {
  return sendJson(
    'DELETE',
    `/competitions/${competitionId}/entries/${entryId}/declared-members/${memberId}`,
  );
}

/** POST .../declared-member-lots/remove → StructureView */
export function removeDeclaredMembers(
  competitionId: string,
  entryId: string,
  request: MemberIdsRequest,
): Promise<StructureView> {
  return sendJson(
    'POST',
    `/competitions/${competitionId}/entries/${entryId}/declared-member-lots/remove`,
    request,
  );
}

/** POST .../declared-members/{memberId}/rename → StructureView */
export function renameDeclaredMember(
  competitionId: string,
  entryId: string,
  memberId: string,
  request: RenameDeclaredMemberRequest,
): Promise<StructureView> {
  return sendJson(
    'POST',
    `/competitions/${competitionId}/entries/${entryId}/declared-members/${memberId}/rename`,
    request,
  );
}

/** POST .../entries/{entryId}/delete → StructureView */
export function deleteCompetitionEntry(
  competitionId: string,
  entryId: string,
): Promise<StructureView> {
  return sendJson(
    'POST',
    `/competitions/${competitionId}/entries/${entryId}/delete`,
  );
}

/** POST .../entry-lots/delete → StructureView */
export function deleteCompetitionEntries(
  competitionId: string,
  request: EntryIdsRequest,
): Promise<StructureView> {
  return sendJson(
    'POST',
    `/competitions/${competitionId}/entry-lots/delete`,
    request,
  );
}

/** POST .../entry-lots/withdraw → StructureView */
export function withdrawCompetitionEntries(
  competitionId: string,
  request: EntryIdsRequest,
): Promise<StructureView> {
  return sendJson(
    'POST',
    `/competitions/${competitionId}/entry-lots/withdraw`,
    request,
  );
}

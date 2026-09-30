/**
 * Competition list/detail, workspace create payload, entries, presentation, regulation.
 */
import type {
  CompetitionStatus,
  CompletionMode,
  DeclaredMemberRole,
  DisciplinaryType,
  EntryStatus,
  RankingCriterion,
  StageStatus,
} from './enums';

export interface CompetitionEntrySummary {
  entryId: string;
  displayName: string;
  status: EntryStatus;
  shortName?: string | null;
  logoMediaId?: string | null;
  primaryColor?: string | null;
  secondaryColor?: string | null;
}

export interface CompetitionStageSummary {
  stageId: string;
  name: string;
  status: StageStatus;
}

/** GET /competitions — one row in the organizer list. */
export interface CompetitionListItem {
  id: string;
  name: string;
  status: CompetitionStatus;
  shortName?: string | null;
  logoMediaId?: string | null;
  scheduledStart?: string | null;
  scheduledEnd?: string | null;
}

/** POST /competitions — create Draft competition (name only). */
export interface CreateCompetitionRequest {
  name: string;
}

/** GET structure `entries[].declaredMembers[]`. */
export interface DeclaredMember {
  memberId: string;
  displayName: string;
  role: DeclaredMemberRole;
  /** True when still listed on a match composition sheet — remove is blocked. */
  referencedOnMatchSheet?: boolean;
}

/** POST .../declared-members — SPA always sends Player. */
export interface AddDeclaredMemberRequest {
  displayName: string;
  role: DeclaredMemberRole;
}

/** POST .../declared-members/{memberId}/rename */
export interface RenameDeclaredMemberRequest {
  displayName: string;
}

/** POST .../entry-lots/delete | withdraw */
export interface EntryIdsRequest {
  entryIds: string[];
}

/** POST .../declared-member-lots/remove */
export interface MemberIdsRequest {
  memberIds: string[];
}

/**
 * GET /competitions/{id}/workspace — Home / competition landing.
 * nextActionCode and attention/completion fields are Read facts from the Host.
 */
export interface WorkspaceSummary {
  id: string;
  name: string;
  status: CompetitionStatus;
  nextActionCode: string | null;
  attentionCount: number;
  completionMode: CompletionMode | null;
  canCompleteNormally: boolean;
  completionBlockers: string[] | null;
  shortName?: string | null;
  logoMediaId?: string | null;
}

export interface CompetitionDetail {
  id: string;
  name: string;
  status: CompetitionStatus;
  entries: CompetitionEntrySummary[];
  stages: CompetitionStageSummary[];
  completionMode?: CompletionMode | null;
  shortName?: string | null;
  logoMediaId?: string | null;
  scheduledStart?: string | null;
  scheduledEnd?: string | null;
}

/** POST /competitions/{id}/entries */
export interface AddEntryRequest {
  displayName: string;
  teamId?: string | null;
  shortName?: string | null;
  logoMediaId?: string | null;
  primaryColor?: string | null;
  secondaryColor?: string | null;
}

/** POST /competitions/{id}/presentation */
export interface UpdateCompetitionPresentationRequest {
  shortName: string | null;
  logoMediaId: string | null;
}

/** POST /competitions/{id}/schedule */
export interface SetCompetitionScheduleRequest {
  scheduledStart: string | null;
  scheduledEnd: string | null;
}

/** POST .../entries/{entryId}/presentation */
export interface UpdateEntryPresentationRequest {
  shortName: string;
  logoMediaId: string | null;
  primaryColor: string | null;
  secondaryColor: string | null;
}

/** POST .../entries/{entryId}/rename */
export interface RenameEntryRequest {
  displayName: string;
}

/** PUT /competitions/{id}/regulation — full competition regulation + bound-stage propagation. */
export interface ReplaceRegulationRequest {
  minimumTeams: number;
  maximumTeams: number;
  durationPerPeriod: number;
  numberOfPeriods: number;
  halfTimeDuration: number;
  winPoints: number;
  drawPoints: number;
  lossPoints: number;
  forfeitWinnerGoals?: number;
  forfeitLoserGoals?: number;
  /**
   * Omit to preserve server DisciplinaryRules; send [] for None;
   * send explicit catalogue to replace.
   */
  allowedTypes?: DisciplinaryType[] | null;
  /** Ordered ranking criteria; omit/empty → Host bootstrap baseline. */
  rankingCriteria?: RankingCriterion[] | null;
  hasExtraTime?: boolean;
  extraTimeDurationPerPeriod?: number | null;
  extraTimeNumberOfPeriods?: number | null;
  hasPenaltyShootout?: boolean;
  penaltyInitialKicksPerTeam?: number | null;
}

/**
 * Match reads, sheet/events requests, match hub, score helpers.
 */
import i18n from '../i18n';
import type {
  CompositionStatus,
  DisciplinaryType,
  MatchSide,
  MatchStatus,
  ResultType,
  StageStatus,
} from './enums';
import type { CompetitionDetail } from './competitions';

export interface EntrySide {
  entryId: string;
  displayName: string | null;
  shortName?: string | null;
  logoMediaId?: string | null;
  primaryColor?: string | null;
  secondaryColor?: string | null;
}

export interface MatchScore {
  homeGoals: number;
  awayGoals: number;
}

/** GET match detail declaredParticipations[] */
export interface DeclaredParticipation {
  memberId: string;
  displayName: string | null;
  side: MatchSide;
  compositionStatus: CompositionStatus;
  jerseyNumber: number | null;
}

/** POST /matches/{id}/declared-participations */
export interface AddDeclaredParticipationRequest {
  memberId: string;
  side: MatchSide;
  compositionStatus: CompositionStatus;
  jerseyNumber?: number | null;
}

/** GET match detail recordedGoals[] */
export interface RecordedGoal {
  goalId: string;
  scorerMemberId: string;
  scorerDisplayName: string | null;
  creditedSide: MatchSide;
  assisterMemberId: string | null;
  assisterDisplayName: string | null;
  isOwnGoal: boolean;
}

/** POST/PUT /matches/{id}/recorded-goals */
export interface RecordGoalRequest {
  scorerMemberId: string;
  creditedSide: MatchSide;
  assisterMemberId?: string | null;
}

/** GET match detail recordedSubstitutions[] */
export interface RecordedSubstitution {
  substitutionId: string;
  side: MatchSide;
  outMemberId: string;
  outDisplayName: string | null;
  inMemberId: string;
  inDisplayName: string | null;
}

/** POST/PUT /matches/{id}/recorded-substitutions */
export interface RecordSubstitutionRequest {
  outMemberId: string;
  inMemberId: string;
  side: MatchSide;
}

/** GET match detail recordedDisciplinaryEvents[] */
export interface RecordedDisciplinaryEvent {
  disciplinaryEventId: string;
  memberId: string;
  memberDisplayName: string | null;
  type: DisciplinaryType;
}

/** POST/PUT /matches/{id}/recorded-disciplinary-events */
export interface RecordDisciplinaryEventRequest {
  memberId: string;
  type: DisciplinaryType;
}

export interface MatchResult {
  type: ResultType;
  homeGoals: number;
  awayGoals: number;
  extraTimePlayed: boolean;
  shootout: MatchScore | null;
}

export interface MatchSummary {
  matchId: string;
  stageId: string;
  status: MatchStatus;
  home: EntrySide;
  away: EntrySide;
  score: MatchScore | null;
  fixtureId: string | null;
  roundId: string | null;
  /** Kickoff from Stage placement when scheduled. */
  scheduledAt?: string | null;
  matchdayNumber?: number | null;
  /** Knockout round display name from the Read. */
  roundName?: string | null;
  /** Present when Domain has a result; never inferred in React. */
  resultType?: ResultType | null;
}

export interface MatchDetail {
  matchId: string;
  competitionId: string;
  stageId: string;
  status: MatchStatus;
  home: EntrySide;
  away: EntrySide;
  result: MatchResult | null;
  fixtureId: string | null;
  legIndex: number | null;
  scheduledAt?: string | null;
  hasObservedLive?: boolean;
  runningScore?: MatchScore | null;
  declaredParticipations?: DeclaredParticipation[];
  recordedGoals?: RecordedGoal[];
  recordedSubstitutions?: RecordedSubstitution[];
  recordedDisciplinaryEvents?: RecordedDisciplinaryEvent[];
}

/**
 * Body for POST /matches/{id}/finish — mirrors Host FinishMatchRequest (camelCase JSON).
 * Shootout fields: send both or neither (Host rejects a partial pair).
 * `type` is a ResultType string enum member name (e.g. "Played").
 */
export interface FinishMatchRequest {
  type: ResultType;
  homeGoals: number;
  awayGoals: number;
  extraTimePlayed: boolean;
  penaltyShootoutHomeGoals?: number | null;
  penaltyShootoutAwayGoals?: number | null;
}

/** GET /competitions/{id}/matches-hub — Match Hub single load. */
export interface MatchHubView {
  detail: CompetitionDetail;
  stages: MatchHubStageMatches[];
}

export interface MatchHubStageMatches {
  stageId: string;
  name: string;
  status: StageStatus;
  matches: MatchSummary[];
}

export function sideLabel(side: EntrySide): string {
  return side.displayName?.trim() || i18n.t('unknownEntry');
}

export function formatScore(score: MatchScore): string {
  return `${score.homeGoals}–${score.awayGoals}`;
}

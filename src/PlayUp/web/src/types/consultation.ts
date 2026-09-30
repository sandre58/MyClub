/**
 * GET /competitions/{id}/consultation — multi-consumer Read.
 */
import type {
  CompetitionStatus,
  CompletionMode,
  MatchGenerationFormat,
  MatchStatus,
  ResultType,
  StageStatus,
  StructureFormatKind,
} from './enums';
import type { EntrySide, MatchScore } from './matches';

/** GET /competitions/{id}/consultation — multi-consumer Read (camelCase wire). */
export interface ConsultationView {
  competitionId: string;
  name: string;
  status: CompetitionStatus;
  completionMode: CompletionMode | null;
  formatKind: StructureFormatKind | null;
  formatLabel: string;
  results: ConsultationResult[];
  standings: ConsultationStandingsSection;
  structure: ConsultationStructure;
}

export interface ConsultationResult {
  matchId: string;
  stageId: string;
  fixtureId: string | null;
  roundId: string | null;
  matchdayNumber: number | null;
  contextLabel: string | null;
  status: MatchStatus;
  home: EntrySide;
  away: EntrySide;
  score: MatchScore | null;
  resultType: ResultType | null;
  scheduledAt: string | null;
}

export interface ConsultationStandingsSection {
  applicable: boolean;
  notApplicableReason: string | null;
  tables: ConsultationStandingTable[];
}

export interface ConsultationStandingTable {
  scope: string;
  stageId: string;
  stageName: string;
  groupId: string | null;
  groupName: string | null;
  rows: ConsultationStandingRow[];
}

export interface ConsultationStandingRow {
  position: number;
  entryId: string;
  displayName: string;
  played: number;
  wins: number;
  draws: number;
  losses: number;
  goalsFor: number;
  goalsAgainst: number;
  goalDifference: number;
  points: number;
}

/** Structure section — typed for contract fidelity; Standings does not render it. */
export interface ConsultationStructure {
  formatKind: StructureFormatKind | null;
  stages: ConsultationStageStructure[];
}

export interface ConsultationStageStructure {
  stageId: string;
  name: string;
  status: StageStatus;
  formatKind: StructureFormatKind | null;
  matchGenerationFormat: MatchGenerationFormat;
  groups: unknown[];
  matchdays: unknown[];
  rounds: unknown[];
  slots: unknown[];
}

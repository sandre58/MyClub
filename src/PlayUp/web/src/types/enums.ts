/**
 * Manual mirrors of Application Reads DTOs.
 * ASP.NET Core JSON uses camelCase property names.
 * Enums are JSON strings (enum member names), not numbers.
 * See docs/guides/http-api-contract.md.
 *
 * TypeScript types document the expected shape at compile time.
 * They do NOT validate JSON at runtime — a mismatched API still type-checks.
 */

/** Shared Host / Application wire enums and client length hints. */

export type CompetitionStatus =
  'Draft' | 'Ready' | 'Running' | 'Suspended' | 'Completed' | 'Archived';

/** Host CompletionMode — string enum member names. */
export type CompletionMode = 'Normal' | 'Administrative' | 'Abandoned';

export type EntryStatus = 'Active' | 'Withdrawn';

export type StageStatus =
  'Draft' | 'Ready' | 'Running' | 'Suspended' | 'Completed';

export type MatchStatus =
  'Scheduled' | 'Live' | 'Finished' | 'Postponed' | 'Cancelled';

export type ResultType = 'Played' | 'Forfeit' | 'WalkOver' | 'Administrative';

export type DrawStatus = 'Draft' | 'Published' | 'Cancelled';

export type DrawResolutionKind = 'Slot' | 'Group';

export type DrawResolutionState = 'NotResolved' | 'Resolved' | 'NoSolution';

/** Application StructureFormatKind — structure format intent (string on wire). */
export type StructureFormatKind = 'Championship' | 'Groups' | 'Cup' | 'Swiss';

/** Domain MatchGenerationFormat — Championship / Groups RR mode (string on wire). */
export type MatchGenerationFormat = 'SingleRoundRobin' | 'DoubleRoundRobin';

/** Domain CompetitionName.MaxLength — client hint; Host remains authority. */
export const COMPETITION_NAME_MAX_LENGTH = 100;

/** Domain DeclaredMember.DisplayNameMaxLength — client hint; Host remains authority. */
export const MEMBER_DISPLAY_NAME_MAX_LENGTH = 100;

/** Host DeclaredMemberRole — string enum member names. */
export type DeclaredMemberRole = 'Player' | 'Staff';

export type RankingCriterion =
  | 'Points'
  | 'GoalDifference'
  | 'GoalsFor'
  | 'GoalsAgainst'
  | 'Wins'
  | 'HeadToHead';

/** Host DisciplinaryType — string enum member names. */
export type DisciplinaryType = 'Yellow' | 'Red' | 'White';

/** Host DrawMode — string enum member names. */
export type DrawMode = 'Random';

export type SelectionMode =
  'Position' | 'Top' | 'Bottom' | 'Best' | 'Worst' | 'Range';

export type RankingScope = 'Overall' | 'Group' | 'AcrossGroups';

/** Host ProgressionOutcome — placement / progression selector. */
export type ProgressionOutcome = 'Winner' | 'Loser';

/** Host DrawConstraintType — string enum member names. */
export type DrawConstraintType =
  | 'SameTeamAvoidance'
  | 'SameGroupAvoidance'
  | 'SameAssociationAvoidance'
  | 'MaxSameAssociationPerGroup';

/** Host ConstraintEnforcement — string enum member names. */
export type ConstraintEnforcement = 'Preferred' | 'Required';

/** Host Side — string enum member names. */
export type MatchSide = 'Home' | 'Away';

/** Host CompositionStatus — string enum member names. */
export type CompositionStatus = 'Starter' | 'Bench';

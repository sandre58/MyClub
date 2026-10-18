/** ProblemDetails code when Complete is blocked by open matches. */
export const STAGE_COMPLETION_BLOCKED = 'Application.StageCompletionBlocked';

export const PROBLEMS_LIFECYCLE_PREFIX = 'problems.lifecycle.';
export const PROBLEMS_MUTATION_PREFIX = 'problems.mutation.';

export const LIFECYCLE_UNKNOWN_KEY = 'problems.lifecycle._unknown';
export const MUTATION_UNKNOWN_KEY = 'problems.mutation._unknown';

/** Wire codes with FR/EN entries under `structure.problems.lifecycle` (Matchs inventory). */
export const MATCHS_LIFECYCLE_WIRE_CODES = [
  'CompetitionClosed',
  'ConstructionFrozen',
  'Places',
  'InitialRoundNotPlayable',
  'IncompleteEntryRound',
  'StageNotReady',
  'StructureGraphInvalid',
  'OpenMatches',
  'ConfrontationUndecidable',
  'OverCapacityEdge',
  'OverExtraction',
  'SlotFeedMissing',
  'SlotFeedInvalid',
  'SlotFeedMultiple',
  'CycleDetected',
  'RoutingNondeterminism',
  'UnmaterializedConfrontations',
  'NoExploitableInitialConfrontations',
  'ProductionPresent',
  'MatchLifecycleStarted',
  'MatchLiveInProgress',
] as const;

/** Prepare checklist disabled reasons (read model) — `problems.lifecycle` copy. */
export const LIFECYCLE_CHECKLIST_WIRE_CODES = [
  'Entries',
  'Places',
  'Draw',
  'Matches',
  'Exits',
  'Attribution',
] as const;

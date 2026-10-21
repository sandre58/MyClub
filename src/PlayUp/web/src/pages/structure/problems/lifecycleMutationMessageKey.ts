import {
  MATCHS_LIFECYCLE_WIRE_CODES,
  PROBLEMS_LIFECYCLE_PREFIX,
  PROBLEMS_MUTATION_PREFIX,
} from './constants';

const APPLICATION_PREFIX = 'Application.';

/** Prepare / lifecycle POST codes that share copy with anticipated `problems.lifecycle.*`. */
const MUTATION_WIRE_TO_LIFECYCLE: Record<string, string> = {
  'Application.CompetitionClosed': 'CompetitionClosed',
  'Application.ConfrontationUndecidable': 'ConfrontationUndecidable',
  'Application.Attribution': 'Attribution',
  'Application.OverCapacityEdge': 'OverCapacityEdge',
  'Application.OverExtraction': 'OverExtraction',
  'Application.RevertToDraftBlocked': 'ProductionPresent',
  'Application.RevertToReadyBlocked': 'MatchLifecycleStarted',
  'Application.RoutingNondeterminism': 'RoutingNondeterminism',
  'Application.StartBlockedUnmaterialized': 'UnmaterializedConfrontations',
  'Application.StartBlockedIncompleteEntryRound': 'IncompleteEntryRound',
  'Application.StartBlockedNoDeterminedConfrontations':
    'NoDeterminedConfrontations',
  'Application.StartBlockedNoExploitableInitialConfrontations':
    'NoExploitableInitialConfrontations',
  'Application.StartBlockedPhaseRosterIncomplete': 'PhaseRosterIncomplete',
  'Application.StartBlockedGroupsNotReady': 'GroupsNotReady',
  'Application.StageExploitationClosed': 'CompetitionClosed',
  'Application.StructureCycleDetected': 'CycleDetected',
  'Application.ReadyConsumerWouldBreak': 'ReadyConsumerWouldBreak',
  'Application.AppliedDrawPoolChangeBlocked': 'AppliedDrawPoolChangeBlocked',
  'Application.AppliedDrawFeedRulesChangeBlocked':
    'AppliedDrawFeedRulesChangeBlocked',
  'Application.ProgressionFeedConsumed': 'ProgressionFeedConsumed',
  'Application.ProgressionFeedImpactAmbiguous':
    'ProgressionFeedImpactAmbiguous',
  'Application.ProgressionDownstreamProductionBlocked':
    'ProgressionDownstreamProductionBlocked',
  'Application.ProgressionProductionParticipationAmbiguous':
    'ProgressionProductionParticipationAmbiguous',
  'Application.AppliedDrawSlotPlacementProtected':
    'AppliedDrawSlotPlacementProtected',
  'Application.DrawObsoleteForConsumption': 'DrawObsoleteForConsumption',
};

export function resolveLifecycleMutationMessageKey(wireCode: string): string {
  const mapped = MUTATION_WIRE_TO_LIFECYCLE[wireCode];
  if (mapped) {
    return `${PROBLEMS_LIFECYCLE_PREFIX}${mapped}`;
  }

  if (wireCode.startsWith(APPLICATION_PREFIX)) {
    const short = wireCode.slice(APPLICATION_PREFIX.length);
    if ((MATCHS_LIFECYCLE_WIRE_CODES as readonly string[]).includes(short)) {
      return `${PROBLEMS_LIFECYCLE_PREFIX}${short}`;
    }
  }

  return `${PROBLEMS_MUTATION_PREFIX}${wireCode}`;
}

import type { StructureStageHubSummary, StructureView } from '../../../types';

/** Prefer Host `stages[]`; synthesize one card from format summary when empty. */
export function resolveStructureHubStages(
  data: StructureView,
): StructureStageHubSummary[] {
  if (data.stages.length > 0) {
    return data.stages;
  }
  const primaryStageId = data.format.primaryStageId;
  if (!primaryStageId) {
    return [];
  }
  const kind = data.format.kind;
  return [
    {
      stageId: primaryStageId,
      name: data.format.primaryStageName ?? '—',
      status: data.format.primaryStageStatus ?? 'Draft',
      teamCount: data.participants.occupyingCount,
      matchCount: data.readiness.attachedMatchCount,
      groupCount: data.structure.groupCount,
      roundCount: data.structure.roundCount,
      numberOfPeriods: data.regulation.numberOfPeriods,
      durationPerPeriod: data.regulation.durationPerPeriod,
      hasExtraTime: data.regulation.hasExtraTime ?? false,
      hasPenaltyShootout: data.regulation.hasPenaltyShootout ?? false,
      hasStandingRules:
        kind === 'Championship' || kind === 'Groups' || kind === 'Swiss',
      hasDrawRules: data.structure.hasDrawRules,
      numberOfPots: data.structure.numberOfPots,
      hasQualificationRules: false,
      qualificationPathCount: 0,
      hasProgressionRules: false,
      progressionPathCount: 0,
      hasTieFormat: kind === 'Cup',
      formatKind: kind,
      swissRoundCount: data.structure.swissRoundCount,
    },
  ];
}

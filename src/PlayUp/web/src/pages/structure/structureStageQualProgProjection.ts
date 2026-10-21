// -----------------------------------------------------------------------
// Qual / Prog authoring edges — hub intents only (paths synthesized server-side).
// -----------------------------------------------------------------------

import type {
  StructureProgressionIntent,
  StructureQualificationIntent,
  StructureStageHubSummary,
} from '../../types';

export type QualificationEdge = {
  kind: 'intent';
  intent: StructureQualificationIntent;
};

export type ProgressionEdge = {
  kind: 'intent';
  intent: StructureProgressionIntent;
};

export function qualificationEdges(
  stage: StructureStageHubSummary,
): QualificationEdge[] {
  return (stage.qualificationIntents ?? []).map((intent) => ({
    kind: 'intent',
    intent,
  }));
}

export function progressionEdges(
  stage: StructureStageHubSummary,
): ProgressionEdge[] {
  return (stage.progressionIntents ?? []).map((intent) => ({
    kind: 'intent',
    intent,
  }));
}

export function qualificationEdgeDestination(edge: QualificationEdge): string {
  return edge.intent.destinationStageId;
}

export function progressionEdgeDestination(edge: ProgressionEdge): string {
  return edge.intent.destinationStageId;
}

export function qualificationEdgeVolume(edge: QualificationEdge): number {
  return Math.max(1, edge.intent.destinationCount ?? 1);
}

export function progressionEdgeVolume(edge: ProgressionEdge): number {
  return Math.max(1, edge.intent.expandedPathCount ?? 1);
}

export function qualificationEdgeOrder(edge: QualificationEdge): number {
  return edge.intent.order;
}

export function progressionEdgeKeyParts(edge: ProgressionEdge): {
  sourcePairKey?: string;
  outcome?: string;
} {
  return {
    sourcePairKey: edge.intent.roundId,
    outcome: edge.intent.outcome,
  };
}

/** Distinct outbound destination stage ids (Qual + Prog). */
export function outboundDestinationStageIds(
  stage: StructureStageHubSummary,
): string[] {
  const destinations: string[] = [];
  const seen = new Set<string>();
  for (const edge of progressionEdges(stage)) {
    const id = progressionEdgeDestination(edge);
    if (id !== stage.stageId && !seen.has(id)) {
      seen.add(id);
      destinations.push(id);
    }
  }
  for (const edge of qualificationEdges(stage)) {
    const id = qualificationEdgeDestination(edge);
    if (id !== stage.stageId && !seen.has(id)) {
      seen.add(id);
      destinations.push(id);
    }
  }
  return destinations;
}

export function stageHasInboundQualProgTo(
  stages: StructureStageHubSummary[],
  destinationStageId: string,
): boolean {
  for (const source of stages) {
    if (
      qualificationEdges(source).some(
        (edge) => qualificationEdgeDestination(edge) === destinationStageId,
      )
    ) {
      return true;
    }
    if (
      progressionEdges(source).some(
        (edge) => progressionEdgeDestination(edge) === destinationStageId,
      )
    ) {
      return true;
    }
  }
  return false;
}

export function countInboundQualProgRules(
  stages: StructureStageHubSummary[],
  destinationStageId: string,
  excludeStageId?: string,
): { qual: number; prog: number } {
  let qual = 0;
  let prog = 0;
  for (const peer of stages) {
    if (peer.stageId === excludeStageId) continue;
    qual += qualificationEdges(peer).filter(
      (edge) => qualificationEdgeDestination(edge) === destinationStageId,
    ).length;
    prog += progressionEdges(peer).filter(
      (edge) => progressionEdgeDestination(edge) === destinationStageId,
    ).length;
  }
  return { qual, prog };
}

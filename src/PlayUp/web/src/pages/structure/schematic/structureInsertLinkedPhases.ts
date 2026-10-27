import type { StageInsertPosition } from '../../../types';

/** Stage fields the insert-cut warning reads. Qual/Prog wires stay on the source. */
export type LinkedPhaseInsertStage = {
  stageId: string;
  qualificationIntents?: readonly { destinationStageId: string }[] | null;
  progressionIntents?: readonly { destinationStageId: string }[] | null;
};

/**
 * True when the insert cut falls strictly between a qualification or progression
 * source and its destination, in either direction.
 * Before the first stage and after the last stage do not split a link.
 */
export function insertSplitsLinkedPhases(
  stages: readonly LinkedPhaseInsertStage[],
  placement: { anchorStageId: string; position: StageInsertPosition },
): boolean {
  const anchorIndex = stages.findIndex(
    (stage) => stage.stageId === placement.anchorStageId,
  );
  if (anchorIndex < 0) {
    return false;
  }

  const insertAt =
    placement.position === 'Before' ? anchorIndex : anchorIndex + 1;
  const indexById = new Map(
    stages.map((stage, index) => [stage.stageId, index] as const),
  );

  for (let sourceIndex = 0; sourceIndex < stages.length; sourceIndex += 1) {
    const stage = stages[sourceIndex];
    const destinations = [
      ...(stage.qualificationIntents ?? []),
      ...(stage.progressionIntents ?? []),
    ];
    for (const intent of destinations) {
      const destinationIndex = indexById.get(intent.destinationStageId);
      if (destinationIndex === undefined || destinationIndex === sourceIndex) {
        continue;
      }

      const low = Math.min(sourceIndex, destinationIndex);
      const high = Math.max(sourceIndex, destinationIndex);
      if (low < insertAt && insertAt <= high) {
        return true;
      }
    }
  }

  return false;
}

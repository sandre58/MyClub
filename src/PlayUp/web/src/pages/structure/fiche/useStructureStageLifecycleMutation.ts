import { useMutation, useQueryClient } from '@tanstack/react-query';

import {
  completeStage,
  prepareStage,
  revertStageToDraft,
  revertStageToReady,
  resumeStage,
  startStage,
  suspendStage,
} from '../../../api';
import type { StructureStageLifecycleMutationCode } from '../../../types';
import { scheduleStructureRefresh } from '../structureInvalidation';

export function useStructureStageLifecycleMutation(
  competitionId: string,
  stageId: string,
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (code: StructureStageLifecycleMutationCode) => {
      switch (code) {
        case 'PrepareStage':
          await prepareStage(stageId);
          break;
        case 'StartStage':
          await startStage(stageId);
          break;
        case 'CompleteStage':
          await completeStage(stageId);
          break;
        case 'RevertToDraftStage':
          await revertStageToDraft(stageId);
          break;
        case 'SuspendStage':
          await suspendStage(stageId);
          break;
        case 'RevertToReadyStage':
          await revertStageToReady(stageId);
          break;
        case 'ResumeStage':
          await resumeStage(stageId);
          break;
        default:
          break;
      }
    },
    onSuccess: () => {
      // The phase status is the feedback. A success toast would repeat it.
      scheduleStructureRefresh(
        queryClient,
        competitionId,
        ['hubStageShell', 'competitionReads'],
        { stageId },
      );
    },
  });
}

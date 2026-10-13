import { useMutation, useQueryClient } from '@tanstack/react-query';

import {
  completeStage,
  prepareStage,
  revertStageToDraft,
  revertStageToReady,
  resumeStage,
  startStage,
  suspendStage,
} from '../../api';
import { notify } from '../../design-system/toastStore';
import type { StructureStageLifecycleMutationCode } from '../../types';
import { scheduleStructureRefresh } from './structureInvalidation';
import { mutationSuccessToast } from './structurePhaseLifecycleActions';
import type { TFunction } from 'i18next';

export function useStructureStageLifecycleMutation(
  competitionId: string,
  stageId: string,
  t: TFunction<'structure'>,
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
    onSuccess: (_data, code) => {
      notify.success(mutationSuccessToast(t, code));
      scheduleStructureRefresh(queryClient, competitionId, ['hubStageShell'], {
        stageId,
      });
    },
  });
}

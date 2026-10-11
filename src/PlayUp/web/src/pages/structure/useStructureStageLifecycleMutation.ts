import { useMutation, useQueryClient } from '@tanstack/react-query';

import {
  completeStage,
  prepareStage,
  startStage,
} from '../../api';
import { notify } from '../../design-system/toastStore';
import type { StructureStageLifecycleTransitionCode } from '../../types';
import { invalidateAfterStructureMutation } from './structureInvalidation';
import {
  transitionSuccessToast,
} from './structurePhaseLifecycleActions';
import type { TFunction } from 'i18next';

export function useStructureStageLifecycleMutation(
  competitionId: string,
  stageId: string,
  t: TFunction<'structure'>,
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (code: StructureStageLifecycleTransitionCode) => {
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
        default:
          break;
      }
    },
    onSuccess: async (_data, code) => {
      notify.success(transitionSuccessToast(t, code));
      await invalidateAfterStructureMutation(queryClient, competitionId, {
        stageId,
      });
    },
  });
}

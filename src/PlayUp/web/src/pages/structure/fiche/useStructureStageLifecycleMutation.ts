import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';

import {
  completeStage,
  prepareStage,
  previewDefinitiveResolutionRelease,
  revertStageToDraft,
  revertStageToReady,
  resumeStage,
  startStage,
  suspendStage,
  type DefinitiveResolutionReleaseResponse,
} from '../../../api';
import type { StructureStageLifecycleMutationCode } from '../../../types';
import { primaryDefinitiveReleases } from '../resolution/structureDefinitiveRelease';
import { scheduleStructureRefresh } from '../structureInvalidation';

export function useStructureStageLifecycleMutation(
  competitionId: string,
  stageId: string,
) {
  const queryClient = useQueryClient();
  const [definitiveOffer, setDefinitiveOffer] =
    useState<DefinitiveResolutionReleaseResponse | null>(null);

  const mutation = useMutation({
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
    onSuccess: (_result, code) => {
      // The phase status is the feedback. A success toast would repeat it.
      scheduleStructureRefresh(
        queryClient,
        competitionId,
        ['hubStageShell', 'competitionReads'],
        { stageId },
      );
      if (code !== 'CompleteStage') {
        return;
      }

      // A separate read. Closing the phase does not wait on it and does not fail with it.
      void previewDefinitiveResolutionRelease(stageId, {
        destinationStageId: null,
      })
        .then((preview) => {
          if (primaryDefinitiveReleases(preview).length > 0) {
            setDefinitiveOffer(preview);
          }
        })
        .catch(() => undefined);
    },
  });

  return {
    ...mutation,
    definitiveOffer,
    dismissDefinitiveOffer: () => setDefinitiveOffer(null),
  };
}

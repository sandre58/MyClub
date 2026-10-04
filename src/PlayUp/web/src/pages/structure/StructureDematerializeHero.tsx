import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';

import { dematerializePhase } from '../../api';
import { TrashIcon } from '../../design-system/icons/contentIcons';
import { notify } from '../../design-system/toastStore';
import { MutationError, PendingLabel } from '../../ui';
import { queryKeys } from '../../queryKeys';
import type { StructureDematerialize } from '../../types';
import { invalidateAfterStructureMutation } from './structureInvalidation';
import { StructureDrawCta } from './StructureDrawCta';
import { dematerializeDisabledCaption } from './structureDematerializeCopy';

export function StructureDematerializeHero({
  competitionId,
  stageId,
  dematerialize,
}: {
  competitionId: string;
  stageId: string;
  dematerialize: StructureDematerialize;
}) {
  const { t } = useTranslation('structure');
  const { t: tActions } = useTranslation('actions');
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: () => dematerializePhase(stageId),
    onSuccess: async (result) => {
      if (result.removedMatchCount === 0) {
        notify.success(t('fiche.dematerialize.successNoOp'));
      } else if (dematerialize.scope === 'LastSwissRound' && result.removedRoundNumber != null) {
        notify.success(
          t('fiche.dematerialize.successSwissRound', {
            round: result.removedRoundNumber,
            count: result.removedMatchCount,
          }),
        );
      } else {
        notify.success(
          t('fiche.dematerialize.successRemoved', { count: result.removedMatchCount }),
        );
      }

      await invalidateAfterStructureMutation(queryClient, competitionId, { stageId });
      await queryClient.invalidateQueries({
        queryKey: queryKeys.matches.byStage(stageId),
      });
      await queryClient.invalidateQueries({
        queryKey: queryKeys.competitions.matchHub(competitionId),
      });
      await queryClient.invalidateQueries({
        queryKey: queryKeys.stages.schematic(stageId),
      });
    },
  });

  const title =
    dematerialize.scope === 'LastSwissRound'
      ? tActions('DematerializeLastSwissRound')
      : tActions('DematerializePhase');

  if (!dematerialize.enabled) {
    return (
      <div className="structure-draw-block__col">
        <StructureDrawCta
          tone="destructive"
          title={title}
          leading={<TrashIcon size="sm" />}
          body={
            <span className="structure-draw-cta__caption">
              {dematerializeDisabledCaption(dematerialize, t)}
            </span>
          }
          disabled
          onClick={() => {}}
        />
      </div>
    );
  }

  return (
    <div className="structure-draw-block__col">
      <StructureDrawCta
        tone="destructive"
        title={title}
        leading={<TrashIcon size="sm" />}
        body={
          mutation.isPending ? (
            <PendingLabel>{t('fiche.dematerialize.submitting')}</PendingLabel>
          ) : (
            <span className="structure-draw-cta__caption">
              {dematerialize.scope === 'LastSwissRound'
                ? t('fiche.dematerialize.hintLastSwissRound')
                : t('fiche.dematerialize.hintFullPhase')}
            </span>
          )
        }
        disabled={mutation.isPending}
        onClick={() => mutation.mutate()}
      />
      {mutation.isError ? <MutationError error={mutation.error} /> : null}
    </div>
  );
}

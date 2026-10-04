import { useMutation, useQueryClient } from '@tanstack/react-query';

import { useTranslation } from 'react-i18next';

import {
  materializeCupFromOccupiedSlots,
  materializeMatches,
} from '../../api';

import { CreateMatchesIcon } from '../../design-system/icons/contentIcons';

import { notify } from '../../design-system/toastStore';

import { MutationError, PendingLabel } from '../../ui';

import { queryKeys } from '../../queryKeys';

import type {
  CompetitionStatus,
  StructureMaterialize,
  StructureStageHubSummary,
} from '../../types';

import { invalidateAfterStructureMutation } from './structureInvalidation';

import { DrawCtaActionBody, StructureDrawCta } from './StructureDrawCta';

import {
  classifyMaterializePresentation,
  materializeDisabledCaption,
  materializeProductionTarget,
  materializeRatioTone,
  showMaterializeActionBlock,
} from './structureMaterializeCopy';

export function StructureMaterializeHero({
  competitionId,
  competitionStatus,
  stage,
}: {
  competitionId: string;
  competitionStatus: CompetitionStatus;
  stage: StructureStageHubSummary;
}) {
  const materialize = stage.materialize;
  if (!materialize) {
    return null;
  }

  const presentation = classifyMaterializePresentation(
    materialize,
    competitionStatus,
    stage.status,
  );
  if (!showMaterializeActionBlock(presentation)) {
    return null;
  }

  return (
    <StructureMaterializeHeroBody
      competitionId={competitionId}
      stageId={stage.stageId}
      materialize={materialize}
      presentation={presentation}
    />
  );
}

function StructureMaterializeHeroBody({
  competitionId,
  stageId,
  materialize,
  presentation,
}: {
  competitionId: string;
  stageId: string;
  materialize: StructureMaterialize;
  presentation: 'actionable' | 'blocked';
}) {
  const { t } = useTranslation('structure');
  const { t: tActions } = useTranslation('actions');
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: () =>
      materialize.commandCode === 'MaterializeFromOccupiedSlots'
        ? materializeCupFromOccupiedSlots(stageId)
        : materializeMatches(stageId),
    onSuccess: async (result) => {
      notify.success(
        result.alreadyComplete
          ? t('fiche.materialize.successAlreadyComplete')
          : t('fiche.materialize.successCreated', { count: result.createdCount }),
      );
      await invalidateAfterStructureMutation(queryClient, competitionId, {
        stageId,
      });
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

  const target = materializeProductionTarget(materialize);
  const ratioTone = materializeRatioTone(materialize);
  const actionLabel = actionButtonLabel(materialize, tActions);
  const ratioBody =
    target > 0 ? (
      <DrawCtaActionBody
        filled={materialize.materializedCount}
        capacity={target}
        teamsCaption={t('fiche.materialize.ratioCaption')}
        poolTone={ratioTone}
      />
    ) : null;

  if (presentation === 'blocked') {
    return (
      <div className="structure-draw-block__col">
        <StructureDrawCta
          tone="emphasis"
          title={actionLabel}
          leading={<CreateMatchesIcon size="sm" />}
          body={
            <>
              {ratioBody}
              <span className="structure-draw-cta__caption">
                {materializeDisabledCaption(materialize, t)}
              </span>
            </>
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
        tone="emphasis"
        title={actionLabel}
        leading={<CreateMatchesIcon size="sm" />}
        body={
          <>
            {ratioBody}
            {mutation.isPending ? (
              <PendingLabel>{t('fiche.materialize.submitting')}</PendingLabel>
            ) : null}
          </>
        }
        disabled={mutation.isPending}
        onClick={() => mutation.mutate()}
      />
      {mutation.isError ? <MutationError error={mutation.error} /> : null}
    </div>
  );
}

function actionButtonLabel(
  materialize: StructureMaterialize,
  tActions: (key: string) => string,
): string {
  const complete = materialize.labelVariant === 'complete';
  if (materialize.commandCode === 'MaterializeFromOccupiedSlots') {
    return complete
      ? tActions('MaterializeFromOccupiedSlotsComplete')
      : tActions('MaterializeFromOccupiedSlots');
  }
  return complete
    ? tActions('MaterializeMatchesComplete')
    : tActions('MaterializeMatches');
}

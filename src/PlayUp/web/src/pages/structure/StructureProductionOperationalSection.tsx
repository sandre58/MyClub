import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { dematerializePhase, materializeCupFromOccupiedSlots, materializeMatches } from '../../api';
import { ConfirmDialog } from '../../design-system/components/ConfirmDialog';
import { TextLink } from '../../design-system/components/TextLink';
import { Tooltip } from '../../design-system/components/Tooltip';
import { CreateMatchesIcon, MatchesStatIcon, TrashIcon } from '../../design-system/icons/contentIcons';
import { notify } from '../../design-system/toastStore';
import { MutationError, PendingLabel } from '../../ui';
import { queryKeys } from '../../queryKeys';
import type {
  CompetitionStatus,
  StructureMaterialize,
  StructureStageHubSummary,
} from '../../types';
import { invalidateAfterStructureMutation } from './structureInvalidation';
import {
  materializeDisabledCaption,
  materializeRatioTone,
  showMaterializeActionBlock,
  type MaterializePresentation,
} from './structureMaterializeCopy';
import { dematerializeDisabledCaption } from './structureDematerializeCopy';
import { productionRatio } from './structureOperationalCopy';
import { StructureOperationalRatioGauge } from './StructureOperationalGauge';
import { StructureOperationalTile } from './StructureOperationalTile';
import { resolveDrawCtaPoolTone } from './structureDrawCtaPool';

function materializeActionLabel(
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

export function StructureProductionOperationalSection({
  competitionId,
  competitionStatus: _competitionStatus,
  stage,
  presentation,
  structureHubRefreshing = false,
}: {
  competitionId: string;
  competitionStatus: CompetitionStatus;
  stage: StructureStageHubSummary;
  presentation: MaterializePresentation | null;
  structureHubRefreshing?: boolean;
}) {
  const materialize = stage.materialize;
  const dematerialize = stage.dematerialize;
  const { t } = useTranslation('structure');
  const { t: tActions } = useTranslation('actions');
  const { t: tCommon } = useTranslation('common');
  const queryClient = useQueryClient();
  const [dematerializeConfirmOpen, setDematerializeConfirmOpen] = useState(false);

  const materializeMutation = useMutation({
    mutationFn: () =>
      materialize!.commandCode === 'MaterializeFromOccupiedSlots'
        ? materializeCupFromOccupiedSlots(stage.stageId)
        : materializeMatches(stage.stageId),
    onSuccess: async (result) => {
      notify.success(
        result.alreadyComplete
          ? t('fiche.materialize.successAlreadyComplete')
          : t('fiche.materialize.successCreated', { count: result.createdCount }),
      );
      await invalidateAfterStructureMutation(queryClient, competitionId, {
        stageId: stage.stageId,
      });
      await queryClient.invalidateQueries({
        queryKey: queryKeys.matches.byStage(stage.stageId),
      });
      await queryClient.invalidateQueries({
        queryKey: queryKeys.competitions.matchHub(competitionId),
      });
      await queryClient.invalidateQueries({
        queryKey: queryKeys.stages.schematic(stage.stageId),
      });
    },
  });

  const dematerializeMutation = useMutation({
    mutationFn: () => dematerializePhase(stage.stageId),
    onSuccess: async (result) => {
      if (result.removedMatchCount === 0) {
        notify.success(t('fiche.dematerialize.successNoOp'));
      } else if (
        dematerialize?.scope === 'LastSwissRound' &&
        result.removedRoundNumber != null
      ) {
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
      await invalidateAfterStructureMutation(queryClient, competitionId, {
        stageId: stage.stageId,
      });
      await queryClient.invalidateQueries({
        queryKey: queryKeys.matches.byStage(stage.stageId),
      });
      await queryClient.invalidateQueries({
        queryKey: queryKeys.competitions.matchHub(competitionId),
      });
      await queryClient.invalidateQueries({
        queryKey: queryKeys.stages.schematic(stage.stageId),
      });
      setDematerializeConfirmOpen(false);
    },
  });

  if (!materialize && !dematerialize) {
    return null;
  }

  const ratio = productionRatio(materialize ?? undefined);
  const poolTone =
    ratio != null
      ? resolveDrawCtaPoolTone(ratio.filled, ratio.capacity)
      : 'neutral';
  const showMaterializePrimary =
    materialize != null &&
    presentation != null &&
    showMaterializeActionBlock(presentation);
  const dematerializeTitle =
    dematerialize?.scope === 'LastSwissRound'
      ? tActions('DematerializeLastSwissRound')
      : tActions('DematerializePhase');

  const dematerializeCaption =
    dematerialize && !dematerialize.enabled
      ? dematerializeDisabledCaption(dematerialize, t)
      : null;

  const blockedCaption =
    showMaterializePrimary && presentation === 'blocked' && materialize
      ? materializeDisabledCaption(materialize, t)
      : null;

  const hasNotes =
    blockedCaption != null ||
    materializeMutation.isError ||
    dematerializeMutation.isError;
  const notes = hasNotes ? (
    <>
      {blockedCaption ? (
        <p className="structure-operational-tile__note" role="status">
          {blockedCaption}
        </p>
      ) : null}
      {materializeMutation.isError ? (
        <MutationError error={materializeMutation.error} />
      ) : null}
      {dematerializeMutation.isError ? (
        <MutationError error={dematerializeMutation.error} />
      ) : null}
    </>
  ) : undefined;

  const hasGauge = ratio != null;
  const meterCaption = t('fiche.operational.production.meterCaptionMatches');

  const dematerializeTooltip =
    dematerializeMutation.isPending
      ? t('fiche.dematerialize.submitting')
      : !dematerialize?.enabled && dematerializeCaption
        ? dematerializeCaption
        : dematerializeTitle;

  const dematerializeConfirmMessage =
    dematerialize?.scope === 'LastSwissRound'
      ? t('fiche.dematerialize.hintLastSwissRound')
      : t('fiche.dematerialize.hintFullPhase');

  const dematerializeToolbarButton = dematerialize ? (
    <Tooltip content={dematerializeTooltip}>
      <span className="structure-operational-tile__toolbar-action">
        <button
          type="button"
          className="ds-btn ds-btn--destructive ds-icon-button ds-icon-button--compact"
          disabled={!dematerialize.enabled || dematerializeMutation.isPending}
          onClick={() => setDematerializeConfirmOpen(true)}
          aria-label={
            dematerializeMutation.isPending
              ? t('fiche.dematerialize.submitting')
              : dematerializeTitle
          }
        >
          {dematerializeMutation.isPending ? (
            <span className="ds-spinner ds-spinner--sm" aria-hidden="true" />
          ) : (
            <TrashIcon size="sm" />
          )}
        </button>
      </span>
    </Tooltip>
  ) : null;

  const showViewMatchesLink =
    materialize != null && materialize.materializedCount > 0;

  const productionGaugeSyncLabel = dematerializeMutation.isPending
    ? t('fiche.dematerialize.submitting')
    : structureHubRefreshing
      ? t('fiche.operational.syncing')
      : null;

  return (
    <>
    <StructureOperationalTile
      id="operational-production"
      title={t('fiche.operational.production.titleMatches')}
      icon={<MatchesStatIcon size="sm" />}
      toolbar={dematerializeToolbarButton}
      gauge={
        <>
          {hasGauge ? (
            <StructureOperationalRatioGauge
              current={ratio.filled}
              capacity={ratio.capacity}
              caption={meterCaption}
              ariaLabel={t('fiche.operational.production.meterAria')}
              poolTone={
                materialize ? materializeRatioTone(materialize) : poolTone
              }
            />
          ) : (
            <p className="structure-operational-gauge__status" role="status">
              {t('fiche.operational.production.statusUnknown')}
            </p>
          )}
          {productionGaugeSyncLabel ? (
            <p className="structure-operational-tile__sync-status" role="status">
              <PendingLabel>{productionGaugeSyncLabel}</PendingLabel>
            </p>
          ) : null}
        </>
      }
      notes={notes}
      actions={
        (showMaterializePrimary && materialize) || showViewMatchesLink ? (
          <div className="structure-operational-tile__production-actions">
            {showMaterializePrimary && materialize ? (
              <div className="structure-operational-tile__primary-action">
                <button
                  type="button"
                  className="ds-btn ds-btn--primary ds-btn--sm"
                  disabled={materializeMutation.isPending || presentation === 'blocked'}
                  onClick={() => materializeMutation.mutate()}
                >
                  {materializeMutation.isPending ? (
                    <PendingLabel>{t('fiche.materialize.submitting')}</PendingLabel>
                  ) : (
                    <>
                      <CreateMatchesIcon size="sm" />
                      {materializeActionLabel(materialize, tActions)}
                    </>
                  )}
                </button>
              </div>
            ) : null}
            {showViewMatchesLink ? (
              <p className="structure-operational-tile__matches-link">
                <TextLink to={`/stages/${stage.stageId}/matches`}>
                  {t('fiche.materialize.viewPhaseMatches')}
                </TextLink>
              </p>
            ) : null}
          </div>
        ) : null
      }
    />
    {dematerialize ? (
      <ConfirmDialog
        open={dematerializeConfirmOpen}
        title={dematerializeTitle}
        message={dematerializeConfirmMessage}
        confirmLabel={dematerializeTitle}
        cancelLabel={tCommon('cancel')}
        closeLabel={tCommon('close')}
        danger
        confirmPending={dematerializeMutation.isPending}
        confirmPendingLabel={t('fiche.dematerialize.submitting')}
        footerStatus={
          dematerializeMutation.isError ? (
            <MutationError error={dematerializeMutation.error} />
          ) : null
        }
        onConfirm={() => dematerializeMutation.mutate()}
        onCancel={() => {
          if (!dematerializeMutation.isPending) {
            setDematerializeConfirmOpen(false);
          }
        }}
      />
    ) : null}
    </>
  );
}

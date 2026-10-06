import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { dematerializePhase, materializeCupFromOccupiedSlots, materializeMatches } from '../../api';
import { ConfirmDialog } from '../../design-system/components/ConfirmDialog';
import { TextLink } from '../../design-system/components/TextLink';
import { Tooltip } from '../../design-system/components/Tooltip';
import {
  CreateMatchesIcon,
  MatchesStatIcon,
  OverviewAttentionIcon,
  TrashIcon,
} from '../../design-system/icons/contentIcons';
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
  embeddedInSection = false,
}: {
  competitionId: string;
  competitionStatus: CompetitionStatus;
  stage: StructureStageHubSummary;
  presentation: MaterializePresentation | null;
  structureHubRefreshing?: boolean;
  embeddedInSection?: boolean;
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
    materialize != null && presentation === 'actionable';
  const dematerializeTitle =
    dematerialize?.scope === 'LastSwissRound'
      ? tActions('DematerializeLastSwissRound')
      : tActions('DematerializePhase');

  const dematerializeCaption =
    dematerialize && !dematerialize.enabled
      ? dematerializeDisabledCaption(dematerialize, t)
      : null;

  const blockedCaption =
    presentation === 'blocked' && materialize
      ? materializeDisabledCaption(materialize, t)
      : null;

  const hasNotes =
    blockedCaption != null ||
    materializeMutation.isError ||
    dematerializeMutation.isError;
  const notes = hasNotes ? (
    <>
      {blockedCaption ? (
        <div className="structure-operational-tile__diagnostics">
          <ul className="structure-phase-rail__issues" role="list">
            <li
              className="structure-phase-rail__issue"
              data-tone="attention"
              role="status"
            >
              <span
                className="structure-phase-rail__issue-icon"
                aria-hidden="true"
              >
                <OverviewAttentionIcon size="sm" />
              </span>
              <span>{blockedCaption}</span>
            </li>
          </ul>
        </div>
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

  const dematerializeActionButton =
    dematerialize?.enabled ? (
    <Tooltip content={dematerializeTooltip}>
      <span className="structure-operational-tile__toolbar-action">
        <button
          type="button"
          className="ds-btn ds-btn--destructive ds-btn--sm"
          disabled={!dematerialize.enabled || dematerializeMutation.isPending}
          onClick={() => setDematerializeConfirmOpen(true)}
        >
          {dematerializeMutation.isPending ? (
            <PendingLabel>{t('fiche.dematerialize.submitting')}</PendingLabel>
          ) : (
            <>
              <TrashIcon size="sm" />
              {dematerializeTitle}
            </>
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
      showHead={!embeddedInSection}
      gauge={
        <>
          <div className="structure-operational-production__gauge-row">
            <div className="structure-operational-production__gauge">
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
              ) : null}
            </div>
          </div>
          {productionGaugeSyncLabel ? (
            <p className="structure-operational-tile__sync-status" role="status">
              <PendingLabel>{productionGaugeSyncLabel}</PendingLabel>
            </p>
          ) : null}
          {(showMaterializePrimary && materialize) ||
          dematerializeActionButton ||
          showViewMatchesLink ? (
            <div className="structure-operational-production__footer">
              <div className="structure-operational-production__footer-actions">
                {showMaterializePrimary && materialize ? (
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
                ) : null}
                {dematerializeActionButton}
              </div>
              {showViewMatchesLink ? (
                <TextLink to={`/stages/${stage.stageId}/matches`}>
                  {t('fiche.materialize.viewPhaseMatches')}
                </TextLink>
              ) : null}
            </div>
          ) : null}
        </>
      }
      notes={notes}
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

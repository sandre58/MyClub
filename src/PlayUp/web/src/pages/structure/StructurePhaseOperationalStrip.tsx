import {
  useMutation,
  useQueryClient,
  type UseMutationResult,
} from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import {
  dematerializePhase,
  materializeCupFromOccupiedSlots,
  materializeMatches,
} from '../../api';
import { ConfirmDialog } from '../../design-system/components/ConfirmDialog';
import { TextLink } from '../../design-system/components/TextLink';
import { Tooltip } from '../../design-system/components/Tooltip';
import {
  CreateMatchesIcon,
  DrawPendingIcon,
  MatchesStatIcon,
  PersonIcon,
  TrashIcon,
  UnlockIcon,
  ViewIcon,
} from '../../design-system/icons/contentIcons';
import { SettingsNavIcon } from '../../design-system/icons/shellIcons';
import { notify } from '../../design-system/toastStore';
import { queryKeys } from '../../queryKeys';
import type { CompetitionStatus, StructureStageHubSummary } from '../../types';
import { EmptyState, LoadingState, MutationError, PendingLabel } from '../../ui';
import { StructureOperationalRatioGauge } from './StructureOperationalGauge';
import type { StructurePlacementOperationalSectionProps } from './StructurePlacementOperationalSection';
import {
  materializeDisabledCaption,
  materializeRatioTone,
  showViewMaterializedMatchesLink,
  type MaterializePresentation,
} from './structureMaterializeCopy';
import { dematerializeDisabledCaption } from './structureDematerializeCopy';
import {
  productionRatio,
  resolvePlaceOccupancyTone,
  resolveResolvedPoolTone,
} from './structureOperationalCopy';
import { resolveDrawCtaPoolTone } from './structureDrawCtaPool';
import { invalidateAfterStructureMutation } from './structureInvalidation';
import {
  joinOperationalStripSections,
  operationalStripGridColumns,
} from './structureOperationalStripLayout';
import {
  resolveDrawPoolMeterCopy,
  resolvePlaceOccupancyMeterCopy,
  resolveProductionMeterCopy,
} from './structureOperationalStripMessages';
import { ratioToneForOperationalMeter } from './structureOperationalStripRatioTone';
import { StripSection } from './structureOperationalStripSection';
import {
  StripDrawAppliedPanel,
  StripOperationalMessage,
  type StripOperationalMessageKind,
} from './structureOperationalStripUi';

const primaryBtn = 'ds-btn ds-btn--primary ds-btn--sm';
const secondaryBtn = 'ds-btn ds-btn--secondary ds-btn--sm';

type MaterializeMutationResult = UseMutationResult<
  {
    createdCount: number;
    attachedMatchIds: string[];
    alreadyComplete: boolean;
  },
  Error,
  void,
  unknown
>;

type DematerializeMutationResult = UseMutationResult<
  {
    removedMatchCount: number;
    removedRoundNumber: number | null;
  },
  Error,
  void,
  unknown
>;

type StripSectionKind = 'places' | 'draw' | 'matches';

function materializeActionLabel(
  materialize: NonNullable<StructureStageHubSummary['materialize']>,
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

function RatioAside({
  current,
  capacity,
  label,
  poolTone,
  loading,
}: {
  current: number | null;
  capacity: number | null;
  label: string;
  poolTone: 'neutral' | 'partial' | 'complete';
  loading?: boolean;
}) {
  if (loading) {
    return (
      <div className="structure-operational-strip__aside">
        <PendingLabel>{label}</PendingLabel>
      </div>
    );
  }

  const unknown = capacity == null || capacity <= 0 || current == null;

  return (
    <div
      className="structure-operational-strip__aside"
      data-pool-tone={poolTone}
    >
      {unknown ? (
        <span className="structure-operational-strip__ratio structure-operational-strip__ratio--unknown">
          —
        </span>
      ) : (
        <span className="structure-operational-strip__ratio">
          <span className="structure-operational-strip__ratio-filled">{current}</span>
          <span className="structure-operational-strip__ratio-sep">/</span>
          <span className="structure-operational-strip__ratio-cap">{capacity}</span>
        </span>
      )}
      <span className="structure-operational-strip__ratio-label">{label}</span>
    </div>
  );
}

function MetricRow({
  aside,
  meter,
}: {
  aside: ReactNode;
  meter: ReactNode;
}) {
  return (
    <div className="structure-operational-strip__metric-row">
      {aside}
      <div className="structure-operational-strip__metric-main">{meter}</div>
    </div>
  );
}

function MetricGaugeBlock({
  message,
  messageKind = 'info',
  gauge,
  footer,
}: {
  message: ReactNode;
  messageKind?: StripOperationalMessageKind;
  gauge?: ReactNode;
  footer?: ReactNode;
}) {
  const messageNode =
    typeof message === 'string' ? (
      <StripOperationalMessage kind={messageKind}>{message}</StripOperationalMessage>
    ) : (
      message
    );

  return (
    <>
      {messageNode}
      {gauge}
      {footer}
    </>
  );
}

function SectionUpdatingBody({
  updating,
  label,
  children,
}: {
  updating: boolean;
  label: string;
  children: ReactNode;
}) {
  if (updating) {
    return (
      <div className="structure-operational-strip__section-sync">
        <LoadingState size="region" label={label} />
      </div>
    );
  }
  return children;
}

export function StructurePhaseOperationalStrip({
  placement,
  showPlaces,
  showDraw,
  competitionId,
  competitionStatus,
  stage,
  presentation,
  structureHubRefreshing = false,
  drawApplied,
}: {
  placement: StructurePlacementOperationalSectionProps | null;
  showPlaces: boolean;
  showDraw: boolean;
  competitionId: string;
  competitionStatus: CompetitionStatus;
  stage: StructureStageHubSummary;
  presentation: MaterializePresentation | null;
  structureHubRefreshing?: boolean;
  drawApplied: boolean;
}) {
  const { t } = useTranslation('structure');
  const { t: tActions } = useTranslation('actions');
  const { t: tCommon } = useTranslation('common');
  const queryClient = useQueryClient();
  const [dematerializeConfirmOpen, setDematerializeConfirmOpen] = useState(false);

  const materialize = stage.materialize;
  const dematerialize = stage.dematerialize;

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

  const visibleSections: StripSectionKind[] = [
    ...(showPlaces ? (['places'] as const) : []),
    ...(showDraw ? (['draw'] as const) : []),
    'matches',
  ];

  const sectionIndex = (kind: StripSectionKind) =>
    visibleSections.indexOf(kind) + 1;

  const placesColumn = showPlaces && placement ? (
    <PlacesColumn
      index={sectionIndex('places')}
      {...placement}
      structureHubRefreshing={structureHubRefreshing}
    />
  ) : null;

  const drawColumn =
    showDraw && placement ? (
      <DrawColumn
        index={sectionIndex('draw')}
        {...placement}
        drawApplied={drawApplied}
        structureHubRefreshing={structureHubRefreshing}
      />
    ) : null;

  const matchesColumn = (
    <MatchesColumn
      index={sectionIndex('matches')}
      competitionId={competitionId}
      competitionStatus={competitionStatus}
      stage={stage}
      presentation={presentation}
      structureHubRefreshing={structureHubRefreshing}
      materializeMutation={materializeMutation}
      dematerializeMutation={dematerializeMutation}
      dematerializeConfirmOpen={dematerializeConfirmOpen}
      setDematerializeConfirmOpen={setDematerializeConfirmOpen}
    />
  );

  const stripSections = joinOperationalStripSections([
    placesColumn,
    drawColumn,
    matchesColumn,
  ]);

  return (
    <>
      <div
        className="structure-operational-strip"
        data-section-count={visibleSections.length}
        style={{
          gridTemplateColumns: operationalStripGridColumns(visibleSections.length),
        }}
      >
        {stripSections}
      </div>
      {dematerialize ? (
        <ConfirmDialog
          open={dematerializeConfirmOpen}
          title={
            dematerialize.scope === 'LastSwissRound'
              ? tActions('DematerializeLastSwissRound')
              : tActions('DematerializePhase')
          }
          message={
            dematerialize.scope === 'LastSwissRound'
              ? t('fiche.dematerialize.hintLastSwissRound')
              : t('fiche.dematerialize.hintFullPhase')
          }
          confirmLabel={
            dematerialize.scope === 'LastSwissRound'
              ? tActions('DematerializeLastSwissRound')
              : tActions('DematerializePhase')
          }
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

function PlacesColumn(
  props: StructurePlacementOperationalSectionProps & {
    index: number;
    structureHubRefreshing?: boolean;
  },
) {
  const { t } = useTranslation('structure');
  const capacity = props.placesN;
  const unknownCapacity = capacity == null || capacity <= 0;
  const occupancyTone =
    !unknownCapacity &&
    capacity != null &&
    props.occupiedPlacesCount != null
      ? resolvePlaceOccupancyTone(props.occupiedPlacesCount, capacity)
      : 'neutral';

  const ratioLabel = t('fiche.operational.placement.ratioLabel', {
    count: props.occupiedPlacesCount ?? 0,
  });

  const placeMeter = resolvePlaceOccupancyMeterCopy({
    t,
    occupancyReady: props.occupancyReady,
    unknownCapacity,
    capacity,
    occupiedPlacesCount: props.occupiedPlacesCount,
    slotFeedMissingActive: props.slotFeedMissingActive ?? false,
  });

  const placesUpdating =
    !props.occupancyReady ||
    Boolean(props.structureHubRefreshing && props.occupancyReady);
  const placesUpdatingLabel = !props.occupancyReady
    ? t('fiche.operational.placement.stateMessage.loading')
    : t('fiche.operational.syncing');

  const gauge =
    !unknownCapacity &&
    props.occupancyReady &&
    props.occupiedPlacesCount != null ? (
      <StructureOperationalRatioGauge
        current={props.occupiedPlacesCount}
        capacity={capacity}
        caption={t('fiche.operational.placement.meterCaptionOccupied')}
        ariaLabel={t('fiche.operational.placement.meterAria')}
        poolTone={occupancyTone}
        layout="trackOnly"
      />
    ) : null;

  return (
    <StripSection
      index={props.index}
      title={t('fiche.operational.cluster.places')}
    >
      <SectionUpdatingBody updating={placesUpdating} label={placesUpdatingLabel}>
        <>
          <MetricRow
            aside={
              <RatioAside
                current={props.occupiedPlacesCount}
                capacity={capacity}
                label={ratioLabel}
                poolTone={occupancyTone}
                loading={!props.occupancyReady && !unknownCapacity}
              />
            }
            meter={
              <>
                {props.placesDiagnostics ? (
                  <div className="structure-operational-strip__message-stack">
                    {props.placesDiagnostics}
                  </div>
                ) : null}
                <MetricGaugeBlock
                  message={placeMeter.text}
                  messageKind={placeMeter.kind}
                  gauge={gauge}
                />
              </>
            }
          />
          {props.canManualPlace ? (
            <div className="structure-operational-strip__section-actions">
              <button type="button" className={primaryBtn} onClick={props.onManualPlace}>
                <PersonIcon size="sm" />
                {t('fiche.manualPlacementCta')}
              </button>
            </div>
          ) : null}
        </>
      </SectionUpdatingBody>
    </StripSection>
  );
}

function DrawColumn(
  props: StructurePlacementOperationalSectionProps & {
    index: number;
    drawApplied: boolean;
    structureHubRefreshing?: boolean;
  },
) {
  const compactIcon =
    'ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact';
  const compactDangerIcon =
    'ds-btn ds-btn--destructive ds-icon-button ds-icon-button--compact';

  const { t } = useTranslation('structure');
  const { t: tDraw } = useTranslation('draw');

  const capacity = props.placesN;
  const unknownCapacity = capacity == null || capacity <= 0;
  const poolTone =
    !unknownCapacity && capacity != null
      ? resolveResolvedPoolTone(props.poolFilled, capacity)
      : 'neutral';

  const showActivate = props.showActivateDrawCta;
  const drawEngaged = props.showDrawCta;
  const deactivateBlocked =
    props.hasNonCancelledDraw || props.deactivateDrawPending;

  const toolbar =
    drawEngaged && props.canEditDraw ? (
      <>
        <Tooltip content={t('fiche.drawParamsAction')}>
          <button
            type="button"
            className={compactIcon}
            aria-label={t('fiche.drawParamsAction')}
            onClick={props.onEditDrawParams}
          >
            <SettingsNavIcon size="sm" />
          </button>
        </Tooltip>
        <Tooltip
          content={
            props.hasNonCancelledDraw
              ? t('regulation.deactivateDrawBlockedHint')
              : t('fiche.deactivateDraw')
          }
        >
          <span className="structure-operational-tile__action-wrap">
            <button
              type="button"
              className={compactDangerIcon}
              disabled={deactivateBlocked}
              aria-label={t('fiche.deactivateDraw')}
              onClick={props.onDeactivateDraw}
            >
              <TrashIcon size="sm" />
            </button>
          </span>
        </Tooltip>
      </>
    ) : null;

  const drawBlockedNote =
    props.showCreateBlockedCaption && props.createBlockedShort ? (
      <StripOperationalMessage kind="attention">
        {props.createBlockedShort}
        {props.createBlockedReason === 'belowMinimumTeams' ? (
          <>
            {' '}
            <Link
              className="ds-text-link"
              to={`/competitions/${props.competitionId}/teams`}
            >
              {t('fiche.drawWorkflow.createBlocked.echoTeams')}
            </Link>
          </>
        ) : null}
      </StripOperationalMessage>
    ) : null;

  const drawNeedsOverview = props.showDrawCta && props.drawOperationalReady === false;
  const drawUpdating =
    props.deactivateDrawPending ||
    Boolean(props.structureHubRefreshing) ||
    drawNeedsOverview;
  const drawUpdatingLabel = props.deactivateDrawPending
    ? t('fiche.operational.placement.deactivatingDraw')
    : drawNeedsOverview
      ? t('fiche.operational.draw.loading')
      : t('fiche.operational.syncing');

  let body: ReactNode;

  if (showActivate) {
    body = (
      <div className="structure-operational-strip__empty">
        <EmptyState
          variant="idle"
          iconAccessible
          icon={
            <button type="button" className={primaryBtn} onClick={props.onActivateDraw}>
              <DrawPendingIcon size="sm" />
              {t('fiche.activateDraw')}
            </button>
          }
        >
          {t('fiche.operational.draw.activateHint')}
        </EmptyState>
      </div>
    );
  } else if (drawEngaged && props.drawApplied) {
    body = (
      <StripDrawAppliedPanel
        message={t('fiche.operational.draw.appliedMessage')}
        action={
          <button
            type="button"
            className={secondaryBtn}
            onClick={props.onOpenDrawWorkflow}
          >
            <ViewIcon size="sm" />
            {t('fiche.viewDrawWorkflow')}
          </button>
        }
      />
    );
  } else if (drawEngaged) {
    const drawLabel = props.activeDraw
      ? t('fiche.openDrawWorkflow')
      : t('fiche.performDraw');
    const poolMeter = resolveDrawPoolMeterCopy({
      t,
      filled: props.poolFilled,
      capacity,
      unknownCapacity,
    });

    body = (
      <>
        {props.showResolvedPoolGauge && !unknownCapacity ? (
          <MetricRow
            aside={
              <RatioAside
                current={props.poolFilled}
                capacity={capacity}
                label={t('fiche.drawCtaTeamsCaption')}
                poolTone={poolTone}
              />
            }
            meter={
              <>
                {drawBlockedNote ? (
                  <div className="structure-operational-strip__message-stack">
                    {drawBlockedNote}
                  </div>
                ) : null}
                <MetricGaugeBlock
                  message={poolMeter.text}
                  messageKind={poolMeter.kind}
                  gauge={
                    <StructureOperationalRatioGauge
                      current={props.poolFilled}
                      capacity={capacity}
                      caption={t('fiche.drawCtaTeamsCaption')}
                      ariaLabel={t('fiche.operational.placement.poolMeterAria')}
                      poolTone={poolTone}
                      layout="trackOnly"
                    />
                  }
                />
              </>
            }
          />
        ) : (
          <>
            {drawBlockedNote ? (
              <div className="structure-operational-strip__message-stack">
                {drawBlockedNote}
              </div>
            ) : null}
            <StripOperationalMessage kind={poolMeter.kind}>
              {poolMeter.text}
            </StripOperationalMessage>
          </>
        )}
        <div className="structure-operational-strip__section-actions">
          {props.showDrawCta ? (
            props.blockPerformDrawCta ? (
              <Tooltip content={props.createBlockedShort ?? drawLabel}>
                <span className="structure-operational-tile__action-wrap">
                  <button type="button" className={primaryBtn} disabled>
                    <DrawPendingIcon size="sm" />
                    {drawLabel}
                  </button>
                </span>
              </Tooltip>
            ) : (
              <button
                type="button"
                className={primaryBtn}
                onClick={
                  props.activeDraw
                    ? props.onOpenDrawWorkflow
                    : props.onOpenDrawWorkflow
                }
              >
                <DrawPendingIcon size="sm" />
                {drawLabel}
              </button>
            )
          ) : null}
          {props.showReleaseCta ? (
            <button
              type="button"
              className={secondaryBtn}
              disabled={props.releasePending}
              onClick={props.onReleasePlacements}
            >
              {props.releasePending ? (
                <PendingLabel>{tDraw('releasingPlacements')}</PendingLabel>
              ) : (
                <>
                  <UnlockIcon size="sm" />
                  {tDraw('releasePlacementsCount', {
                    aligned: props.releasableAlignedCount,
                    total: props.releasableTotal,
                  })}
                </>
              )}
            </button>
          ) : null}
        </div>
      </>
    );
  } else {
    body = (
      <StripOperationalMessage kind="info">
        {t('fiche.operational.draw.unavailable')}
      </StripOperationalMessage>
    );
  }

  return (
    <StripSection
      index={props.index}
      title={t('fiche.operational.cluster.draw')}
      toolbar={toolbar}
    >
      <SectionUpdatingBody updating={drawUpdating} label={drawUpdatingLabel}>
        {body}
      </SectionUpdatingBody>
    </StripSection>
  );
}

function MatchesColumn({
  index,
  stage,
  presentation,
  structureHubRefreshing,
  materializeMutation,
  dematerializeMutation,
  dematerializeConfirmOpen: _dematerializeConfirmOpen,
  setDematerializeConfirmOpen,
}: {
  index: number;
  competitionId: string;
  competitionStatus: CompetitionStatus;
  stage: StructureStageHubSummary;
  presentation: MaterializePresentation | null;
  structureHubRefreshing?: boolean;
  materializeMutation: MaterializeMutationResult;
  dematerializeMutation: DematerializeMutationResult;
  dematerializeConfirmOpen: boolean;
  setDematerializeConfirmOpen: (open: boolean) => void;
}) {
  const { t } = useTranslation('structure');
  const { t: tActions } = useTranslation('actions');

  const materialize = stage.materialize;
  const dematerialize = stage.dematerialize;
  const gaugeRatio = productionRatio(materialize ?? undefined);
  const poolTone =
    gaugeRatio != null
      ? resolveDrawCtaPoolTone(gaugeRatio.filled, gaugeRatio.capacity)
      : 'neutral';
  const showMaterializePrimary =
    materialize != null && presentation === 'actionable';
  const showViewMatchesLink = showViewMaterializedMatchesLink(stage, materialize);

  const blockedCaption =
    presentation === 'blocked' && materialize
      ? materializeDisabledCaption(materialize, t)
      : null;

  const dematerializeTitle =
    dematerialize?.scope === 'LastSwissRound'
      ? tActions('DematerializeLastSwissRound')
      : tActions('DematerializePhase');

  const dematerializeCaption =
    dematerialize && !dematerialize.enabled
      ? dematerializeDisabledCaption(dematerialize, t)
      : null;

  const matchMeter = resolveProductionMeterCopy({
    t,
    blockedCaption,
    gaugeRatio,
    hasProduction: materialize != null || dematerialize != null,
  });

  const productionRatioTone =
    gaugeRatio != null
      ? ratioToneForOperationalMeter(
          gaugeRatio.filled,
          gaugeRatio.capacity,
          matchMeter.kind,
        )
      : materialize
        ? materializeRatioTone(materialize)
        : poolTone;

  const matchesUpdating =
    materializeMutation.isPending ||
    dematerializeMutation.isPending ||
    Boolean(structureHubRefreshing);
  const matchesUpdatingLabel = dematerializeMutation.isPending
    ? t('fiche.dematerialize.submitting')
    : materializeMutation.isPending
      ? t('fiche.materialize.submitting')
      : t('fiche.operational.syncing');

  const ratioLabel = t('fiche.operational.production.ratioLabel', {
    count: gaugeRatio?.filled ?? 0,
  });

  const toolbar = showViewMatchesLink ? (
    <TextLink to={`/stages/${stage.stageId}/matches`}>
      {t('fiche.materialize.viewPhaseMatches')}
    </TextLink>
  ) : null;

  const canShowMetric =
    gaugeRatio != null && (materialize != null || dematerialize != null);

  const matchesContent = canShowMetric ? (
    <>
      <MetricRow
        aside={
          <RatioAside
            current={gaugeRatio!.filled}
            capacity={gaugeRatio!.capacity}
            label={ratioLabel}
            poolTone={productionRatioTone}
          />
        }
        meter={
          <MetricGaugeBlock
            message={matchMeter.text}
            messageKind={matchMeter.kind}
            gauge={
              <StructureOperationalRatioGauge
                current={gaugeRatio!.filled}
                capacity={gaugeRatio!.capacity}
                caption={t('fiche.operational.production.meterCaptionMatches')}
                ariaLabel={t('fiche.operational.production.meterAria')}
                poolTone={productionRatioTone}
                layout="trackOnly"
              />
            }
          />
        }
      />
      {materializeMutation.isError ? (
        <MutationError error={materializeMutation.error} />
      ) : null}
      {dematerializeMutation.isError ? (
        <MutationError error={dematerializeMutation.error} />
      ) : null}
      <div className="structure-operational-strip__section-actions">
        {showMaterializePrimary && materialize ? (
          <button
            type="button"
            className={primaryBtn}
            disabled={materializeMutation.isPending}
            onClick={() => materializeMutation.mutate(undefined)}
          >
            <>
              <CreateMatchesIcon size="sm" />
              {materializeActionLabel(materialize, tActions)}
            </>
          </button>
        ) : null}
        {dematerialize?.enabled ? (
          <Tooltip
            content={
              !dematerialize.enabled && dematerializeCaption
                ? dematerializeCaption
                : dematerializeTitle
            }
          >
            <span className="structure-operational-tile__action-wrap">
              <button
                type="button"
                className="ds-btn ds-btn--destructive ds-btn--sm"
                disabled={!dematerialize.enabled || dematerializeMutation.isPending}
                onClick={() => setDematerializeConfirmOpen(true)}
              >
                <>
                  <TrashIcon size="sm" />
                  {dematerializeTitle}
                </>
              </button>
            </span>
          </Tooltip>
        ) : null}
      </div>
    </>
  ) : (
    <div className="structure-operational-strip__empty">
      <EmptyState
        variant="idle"
        icon={<MatchesStatIcon size="lg" aria-hidden="true" />}
      >
        {matchMeter.text}
      </EmptyState>
    </div>
  );

  return (
    <StripSection
      index={index}
      title={t('fiche.operational.cluster.matches')}
      toolbar={toolbar}
    >
      <SectionUpdatingBody updating={matchesUpdating} label={matchesUpdatingLabel}>
        {matchesContent}
      </SectionUpdatingBody>
    </StripSection>
  );
}

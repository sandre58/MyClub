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
import { RatioMessageGauge } from '../../design-system/components/RatioMessageGauge';
import { TextLink } from '../../design-system/components/TextLink';
import { Tooltip } from '../../design-system/components/Tooltip';
import {
  CreateMatchesIcon,
  DrawPendingIcon,
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
import { StructureDrawReadinessChecklist } from './StructureDrawReadinessChecklist';
import { StructureOperationalRatioGauge } from './StructureOperationalGauge';
import type { DrawCtaPoolTone } from './structureDrawCtaPool';
import type { StructurePlacementOperationalSectionProps } from './structurePlacementOperationalProps';
import {
  materializeDisabledCaption,
  materializeRatioTone,
  showViewMaterializedMatchesLink,
  type MaterializePresentation,
} from './structureMaterializeCopy';
import {
  productionRatio,
  drawPoolToneToRatioGaugeTone,
  operationalStripPoolTone,
  resolvePlaceOccupancyTone,
  resolveResolvedPoolTone,
} from './structureOperationalCopy';
import {
  isDraftLikeStageStatus,
  isSuspendedStageStatus,
} from './structureOperationalMeterPresentation';
import { resolveDrawCtaPoolTone } from './structureDrawCtaPool';
import {
  scheduleQueryRefresh,
  scheduleStructureRefresh,
} from './structureInvalidation';
import { StructureMatchPhaseProgressGauge } from './StructureMatchPhaseProgressGauge';
import { resolveSwissMatchsGuidance } from './swissMatchsGuidance';
import {
  StructurePhaseDraftMatchsPrepare,
  StructurePhaseMatchsLifecycleBlock,
} from './StructurePhaseMatchsLifecycleBlock';
import {
  shouldShowMatchExploitationProgress,
  shouldShowMatchsSection,
  shouldShowProductionInMatchsSection,
} from './structureMatchsSectionUi';
import {
  joinCompositionPlaceAssignmentSections,
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
import {Link} from "react-router-dom";

const primaryBtn = 'ds-btn ds-btn--primary ds-btn--sm';
const secondaryBtn = 'ds-btn ds-btn--secondary ds-btn--sm';

type MaterializeMutationResult = UseMutationResult<
  {
    createdCount: number;
    attachedMatchIds: string[];
    alreadyComplete: boolean;
  },
  Error,
  void
>;

type DematerializeMutationResult = UseMutationResult<
  {
    removedMatchCount: number;
    removedRoundNumber: number | null;
  },
  Error,
  void
>;

type StripSectionKind = 'places' | 'draw' | 'matches';

export type StructureOperationalStripLayoutVariant = 'composition' | 'aside-matchs';

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
  poolTone: DrawCtaPoolTone;
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
  structureStages,
  presentation,
  structureHubBackgroundRefresh = false,
  drawApplied,
  layoutVariant = 'composition',
}: {
  placement: StructurePlacementOperationalSectionProps | null;
  showPlaces: boolean;
  showDraw: boolean;
  competitionId: string;
  competitionStatus: CompetitionStatus;
  stage: StructureStageHubSummary;
  /** Hub phases for structure-aware mutation copy (D1 consumer names). */
  structureStages: readonly StructureStageHubSummary[];
  presentation: MaterializePresentation | null;
  structureHubBackgroundRefresh?: boolean;
  drawApplied: boolean;
  layoutVariant?: StructureOperationalStripLayoutVariant;
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
    onSuccess: (result) => {
      notify.success(
        result.alreadyComplete
          ? t('fiche.materialize.successAlreadyComplete')
          : t('fiche.materialize.successCreated', { count: result.createdCount }),
      );
      scheduleStructureRefresh(queryClient, competitionId, ['hubStageShell'], {
        stageId: stage.stageId,
      });
      scheduleQueryRefresh(
        queryClient,
        queryKeys.matches.byStage(stage.stageId),
      );
      scheduleQueryRefresh(
        queryClient,
        queryKeys.competitions.matchHub(competitionId),
      );
    },
  });

  const dematerializeMutation = useMutation({
    mutationFn: () => dematerializePhase(stage.stageId),
    onSuccess: (result) => {
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
      setDematerializeConfirmOpen(false);
      scheduleStructureRefresh(queryClient, competitionId, ['hubStageShell'], {
        stageId: stage.stageId,
      });
      scheduleQueryRefresh(
        queryClient,
        queryKeys.matches.byStage(stage.stageId),
      );
      scheduleQueryRefresh(
        queryClient,
        queryKeys.competitions.matchHub(competitionId),
      );
    },
  });

  const visibleSections: StripSectionKind[] =
    layoutVariant === 'composition'
      ? [
          ...(showPlaces ? (['places'] as const) : []),
          ...(showDraw ? (['draw'] as const) : []),
        ]
      : shouldShowMatchsSection(stage)
        ? (['matches'] as const)
        : [];

  if (visibleSections.length === 0) {
    return null;
  }

  const compositionStack = layoutVariant === 'composition';
  const stripSectionTitlePresentation = compositionStack ? 'subtitle' : 'chip';
  const placesTitlePresentation = compositionStack ? 'hidden' : 'chip';
  const drawTitlePresentation = compositionStack
    ? 'subtitle'
    : stripSectionTitlePresentation;

  const sectionIndex = (kind: StripSectionKind) =>
    visibleSections.indexOf(kind) + 1;

  const placesColumn = showPlaces && placement ? (
    <PlacesColumn
      index={sectionIndex('places')}
      titleChipRefreshing={structureHubBackgroundRefresh}
      titlePresentation={placesTitlePresentation}
      structureHubBackgroundRefresh={structureHubBackgroundRefresh}
      {...placement}
    />
  ) : null;

  const drawColumn =
    showDraw && placement ? (
      <DrawColumn
        index={sectionIndex('draw')}
        titleChipRefreshing={structureHubBackgroundRefresh}
        titlePresentation={drawTitlePresentation}
        compositionLayout={compositionStack}
        structureHubBackgroundRefresh={structureHubBackgroundRefresh}
        {...placement}
        drawApplied={drawApplied}
      />
    ) : null;

  const asideMatchsLayout = layoutVariant === 'aside-matchs';

  const matchesColumn = (
    <MatchesColumn
      index={sectionIndex('matches')}
      titleChipRefreshing={structureHubBackgroundRefresh}
      competitionId={competitionId}
      competitionStatus={competitionStatus}
      stage={stage}
      structureStages={structureStages}
      presentation={presentation}
      materializeMutation={materializeMutation}
      dematerializeMutation={dematerializeMutation}
      dematerializeConfirmOpen={dematerializeConfirmOpen}
      setDematerializeConfirmOpen={setDematerializeConfirmOpen}
      omitLifecycle={asideMatchsLayout}
      hideSectionHead={asideMatchsLayout}
    />
  );

  const stripSections =
    layoutVariant === 'aside-matchs'
      ? [matchesColumn]
      : compositionStack
        ? joinCompositionPlaceAssignmentSections(placesColumn, drawColumn, {
            showPlaces,
            showDraw,
          })
        : [];

  return (
    <>
      {structureHubBackgroundRefresh ? (
        <span className="ds-visually-hidden" role="status">
          {t('fiche.operational.syncing')}
        </span>
      ) : null}
      <div
        className="structure-operational-strip"
        data-layout-variant={layoutVariant}
        data-section-count={visibleSections.length}
        aria-busy={structureHubBackgroundRefresh || undefined}
        style={
          compositionStack
            ? undefined
            : {
                gridTemplateColumns: operationalStripGridColumns(
                  visibleSections.length,
                ),
              }
        }
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
              <MutationError
                error={dematerializeMutation.error}
                structureStages={structureStages}
              />
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
    titleChipRefreshing?: boolean;
    titlePresentation?: 'chip' | 'subtitle' | 'hidden';
    structureHubBackgroundRefresh?: boolean;
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
    stageStatus: props.stageStatus,
    slotFeedPresentationContext: props.slotFeedPresentationContext,
  });

  const softenPlacePoolTone =
    placeMeter.kind !== 'danger' &&
    (isDraftLikeStageStatus(props.stageStatus) ||
      isSuspendedStageStatus(props.stageStatus));
  const occupancyToneForPool = softenPlacePoolTone
    ? operationalStripPoolTone(
        placeMeter.kind,
        placeMeter.kind === 'attention' ? 'attention' : 'neutral',
      )
    : operationalStripPoolTone(placeMeter.kind, occupancyTone);

  const placesPoolTone = operationalStripPoolTone(
    placeMeter.kind,
    occupancyToneForPool,
  );

  const placesInitialLoading = !props.occupancyReady;
  const placesUpdatingLabel = t(
    'fiche.operational.placement.stateMessage.loading',
  );
  const showOccupancyRatio =
    !unknownCapacity &&
    props.occupancyReady &&
    props.occupiedPlacesCount != null;

  return (
    <StripSection
      index={props.index}
      title={t('fiche.operational.cluster.places')}
      titlePresentation={props.titlePresentation}
      titleChipRefreshing={props.titleChipRefreshing}
    >
      <SectionUpdatingBody
        updating={
          placesInitialLoading &&
          !(props.structureHubBackgroundRefresh && props.occupancyReady)
        }
        label={placesUpdatingLabel}
      >
        <>
          {props.placesDiagnostics ? (
            <div className="structure-operational-strip__message-stack">
              {props.placesDiagnostics}
            </div>
          ) : null}
          <RatioMessageGauge
            size="default"
            current={showOccupancyRatio ? props.occupiedPlacesCount : null}
            capacity={showOccupancyRatio ? capacity : null}
            ratioCaption={ratioLabel}
            message={placeMeter.text}
            messageKind={placeMeter.kind}
            ratioTone={drawPoolToneToRatioGaugeTone(placesPoolTone)}
            ariaLabel={t('fiche.operational.placement.meterAria')}
            loading={placesInitialLoading && !unknownCapacity}
            loadingMessage={placesUpdatingLabel}
            className="structure-operational-strip__ratio-gauge"
          />
          {props.canManualPlace ? (
            <div className="structure-operational-strip__section-actions">
              <button type="button" className={secondaryBtn} onClick={props.onManualPlace}>
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
    titleChipRefreshing?: boolean;
    titlePresentation?: 'chip' | 'subtitle' | 'hidden';
    /** Composition band: resolved pool meter lives on Qualifiés. */
    compositionLayout?: boolean;
    structureHubBackgroundRefresh?: boolean;
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
              {props.deactivateDrawPending ? (
                <span className="ds-spinner ds-spinner--sm" aria-hidden="true" />
              ) : (
                <TrashIcon size="sm" />
              )}
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

  const drawInitialLoading = props.showDrawCta && props.drawOperationalReady === false;
  const drawUpdatingLabel = t('fiche.operational.draw.loading');

  const compositionDrawDeactivate = (
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
          className={[
            compactDangerIcon,
            props.compositionLayout
              ? 'structure-operational-strip__composition-draw-deactivate'
              : null,
          ]
            .filter(Boolean)
            .join(' ')}
          disabled={deactivateBlocked}
          aria-label={t('fiche.deactivateDraw')}
          onClick={props.onDeactivateDraw}
        >
          {props.deactivateDrawPending ? (
            <span className="ds-spinner ds-spinner--sm" aria-hidden="true" />
          ) : (
            <TrashIcon size="sm" />
          )}
        </button>
      </span>
    </Tooltip>
  );

  const compositionLaunchButton = (
    label: string,
    onClick: () => void,
    options?: {
      disabled?: boolean;
      disabledTitle?: string;
      variant?: 'primary' | 'secondary';
    },
  ) => {
    const btnClass =
      options?.variant === 'secondary' ? secondaryBtn : primaryBtn;
    const disabled = options?.disabled ?? false;
    const disabledTitle = options?.disabledTitle;
    return disabled ? (
      <Tooltip content={disabledTitle ?? label}>
        <span className="structure-operational-tile__action-wrap structure-operational-strip__composition-draw-cta-wrap">
          <button type="button" className={btnClass} disabled>
            <DrawPendingIcon size="sm" />
            {label}
          </button>
        </span>
      </Tooltip>
    ) : (
      <button type="button" className={btnClass} onClick={onClick}>
        <DrawPendingIcon size="sm" />
        {label}
      </button>
    );
  };

  let body: ReactNode;

  const readOnlyCompletedFinal = props.stageStatus === 'Completed';

  const drawSetupEditableForChecklist =
    props.canEditDraw && !props.drawApplied;
  const showDrawReadinessChecklist =
    props.compositionLayout &&
    drawEngaged &&
    drawSetupEditableForChecklist &&
    props.drawReadinessChecklist != null &&
    props.drawReadinessChecklist.length > 0;

  const drawReadinessList = showDrawReadinessChecklist ? (
    <StructureDrawReadinessChecklist items={props.drawReadinessChecklist!} />
  ) : null;

  if (readOnlyCompletedFinal && drawEngaged) {
    if (props.drawApplied) {
      body = props.compositionLayout ? (
        <div className="structure-operational-strip__composition-draw">
          <StripOperationalMessage kind="success">
            {t('fiche.operational.draw.appliedMessage')}
          </StripOperationalMessage>
          {compositionLaunchButton(
            t('fiche.viewDrawWorkflow'),
            props.onOpenDrawWorkflow,
            { variant: 'secondary' },
          )}
        </div>
      ) : (
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
    } else {
      body = (
        <StripOperationalMessage kind="info">
          {t('fiche.operational.draw.unavailable')}
        </StripOperationalMessage>
      );
    }
  } else if (props.compositionLayout && showActivate) {
    body = (
      <div className="structure-operational-strip__composition-draw">
        {compositionLaunchButton(t('fiche.activateDraw'), props.onActivateDraw)}
      </div>
    );
  } else if (props.compositionLayout && drawEngaged) {
    const launchLabel = props.drawApplied
      ? t('fiche.viewDrawWorkflow')
      : t('fiche.performDraw');
    const drawSetupEditable = props.canEditDraw && !props.drawApplied;
    body = (
      <div className="structure-operational-strip__composition-draw">
        {!showDrawReadinessChecklist && drawBlockedNote ? (
          <div className="structure-operational-strip__message-stack">
            {drawBlockedNote}
          </div>
        ) : null}
        {props.drawApplied ? (
          <StripOperationalMessage kind="success">
            {t('fiche.operational.draw.appliedMessage')}
          </StripOperationalMessage>
        ) : null}
        <div className="structure-operational-strip__composition-draw-stack">
          {props.showDrawCta
            ? compositionLaunchButton(launchLabel, props.onOpenDrawWorkflow, {
                disabled: props.blockPerformDrawCta,
                disabledTitle: props.createBlockedShort ?? launchLabel,
                variant: props.drawApplied ? 'secondary' : 'primary',
              })
            : null}
          {drawSetupEditable ? (
            <div className="structure-operational-strip__composition-draw-toolbar">
              <button
                type="button"
                className={`${secondaryBtn} structure-operational-strip__composition-draw-params`}
                onClick={props.onEditDrawParams}
              >
                <SettingsNavIcon size="sm" />
                {t('fiche.drawParamsAction')}
              </button>
              {compositionDrawDeactivate}
            </div>
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
        {drawReadinessList}
      </div>
    );
  } else if (showActivate) {
    body = (
      <div className="structure-operational-strip__empty">
        <EmptyState
          variant="idle"
          action={
            <button
              type="button"
              className={primaryBtn}
              onClick={props.onActivateDraw}
            >
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
    const showDrawPoolGauge =
      props.showResolvedPoolGauge &&
      !unknownCapacity &&
      !props.compositionLayout;

    body = (
      <>
        {showDrawPoolGauge ? (
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
            {!props.compositionLayout ? (
              <StripOperationalMessage kind={poolMeter.kind}>
                {poolMeter.text}
              </StripOperationalMessage>
            ) : null}
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
      titlePresentation={props.titlePresentation}
      titleIcon={
        props.titlePresentation === 'subtitle' ? (
          <DrawPendingIcon size="sm" />
        ) : undefined
      }
      titleChipRefreshing={props.titleChipRefreshing}
      toolbar={props.compositionLayout ? null : toolbar}
    >
      <SectionUpdatingBody
        updating={
          drawInitialLoading &&
          !(
            props.structureHubBackgroundRefresh &&
            props.drawOperationalReady === true
          )
        }
        label={drawUpdatingLabel}
      >
        {body}
      </SectionUpdatingBody>
    </StripSection>
  );
}

function MatchesColumn({
  index,
  titleChipRefreshing,
  competitionId,
  stage,
  structureStages,
  presentation,
  materializeMutation,
  dematerializeMutation,
  dematerializeConfirmOpen: _dematerializeConfirmOpen,
  setDematerializeConfirmOpen,
  omitLifecycle = false,
  hideSectionHead = false,
}: {
  index: number;
  titleChipRefreshing?: boolean;
  competitionId: string;
  competitionStatus: CompetitionStatus;
  stage: StructureStageHubSummary;
  structureStages: readonly StructureStageHubSummary[];
  presentation: MaterializePresentation | null;
  materializeMutation: MaterializeMutationResult;
  dematerializeMutation: DematerializeMutationResult;
  dematerializeConfirmOpen: boolean;
  setDematerializeConfirmOpen: (open: boolean) => void;
  omitLifecycle?: boolean;
  hideSectionHead?: boolean;
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

  const productionBusy =
    materializeMutation.isPending || dematerializeMutation.isPending;

  const ratioLabel = t('fiche.operational.production.ratioLabel', {
    count: gaugeRatio?.filled ?? 0,
  });

  const toolbar = showViewMatchesLink ? (
    <TextLink to={`/stages/${stage.stageId}/matches`}>
      {t('fiche.materialize.viewPhaseMatches')}
    </TextLink>
  ) : null;

  const showProduction = shouldShowProductionInMatchsSection(
    stage,
    presentation,
  );
  const canShowMetric =
    showProduction &&
    gaugeRatio != null &&
    (materialize != null || dematerialize != null);

  const materializeBtnClass = primaryBtn;

  const productionGauge = canShowMetric ? (
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
  ) : showProduction && (materialize != null || dematerialize != null) ? (
    <div className="structure-operational-strip__empty">
      <EmptyState variant="idle">
        {presentation === 'blocked' && materialize
          ? materializeDisabledCaption(materialize, t)
          : matchMeter.text}
      </EmptyState>
    </div>
  ) : null;

  const productionLeadingActions =
    showProduction && (showMaterializePrimary || dematerialize?.enabled) ? (
      <>
        {showMaterializePrimary && materialize ? (
          <button
            type="button"
            className={materializeBtnClass}
            disabled={materializeMutation.isPending}
            onClick={() => materializeMutation.mutate(undefined)}
          >
            {materializeMutation.isPending ? (
              <PendingLabel>
                {materializeActionLabel(materialize, tActions)}
              </PendingLabel>
            ) : (
              <>
                <CreateMatchesIcon size="sm" />
                {materializeActionLabel(materialize, tActions)}
              </>
            )}
          </button>
        ) : null}
        {dematerialize?.enabled ? (
          <Tooltip content={dematerializeTitle}>
            <span className="structure-operational-tile__action-wrap">
              <button
                type="button"
                className="ds-btn ds-btn--destructive ds-btn--sm ds-icon-button"
                disabled={dematerializeMutation.isPending}
                aria-label={dematerializeTitle}
                onClick={() => setDematerializeConfirmOpen(true)}
              >
                {dematerializeMutation.isPending ? (
                  <span className="ds-spinner ds-spinner--sm" aria-hidden="true" />
                ) : (
                  <TrashIcon size="sm" aria-hidden="true" />
                )}
              </button>
            </span>
          </Tooltip>
        ) : null}
      </>
    ) : null;

  const productionErrors =
    showProduction &&
    (materializeMutation.isError || dematerializeMutation.isError) ? (
      <>
        {materializeMutation.isError ? (
          <MutationError
            error={materializeMutation.error}
            structureStages={structureStages}
          />
        ) : null}
        {dematerializeMutation.isError ? (
          <MutationError
            error={dematerializeMutation.error}
            structureStages={structureStages}
          />
        ) : null}
      </>
    ) : null;

  let matchesContent: ReactNode;

  if (stage.status === 'Draft') {
    matchesContent = omitLifecycle
      ? null
      : (
        <StructurePhaseDraftMatchsPrepare
          competitionId={competitionId}
          stage={stage}
        />
      );
  } else if (stage.status === 'Completed') {
    const progress = stage.lifecycle?.matchProgress;
    matchesContent =
      shouldShowMatchExploitationProgress(stage) && progress ? (
        <StructureMatchPhaseProgressGauge
          finished={progress.finished}
          total={progress.total}
          open={progress.open}
          cancelled={progress.cancelled}
          layout="finishedTotal"
          phaseStatus={stage.status}
        />
      ) : null;
  } else {
    const progress = stage.lifecycle?.matchProgress;
    const swissProgressCaption =
      stage.formatKind === 'Swiss' ? resolveSwissMatchsGuidance(stage) : null;
    const exploitationGauge =
      shouldShowMatchExploitationProgress(stage) && progress ? (
        <StructureMatchPhaseProgressGauge
          finished={progress.finished}
          total={progress.total}
          open={progress.open}
          cancelled={progress.cancelled}
          layout="finishedTotal"
          phaseStatus={stage.status}
          swissProgressCaption={swissProgressCaption}
        />
      ) : null;

    matchesContent = (
      <>
        {productionGauge}
        {productionErrors}
        {exploitationGauge}
        {omitLifecycle && productionLeadingActions ? (
          <div
            className="structure-operational-strip__production-actions structure-operational-strip__production-actions--tile"
          >
            {productionLeadingActions}
          </div>
        ) : null}
        {omitLifecycle ? null : (
          <StructurePhaseMatchsLifecycleBlock
            competitionId={competitionId}
            stage={stage}
            productionBusy={productionBusy}
            leadingActions={productionLeadingActions}
          />
        )}
      </>
    );
  }

  const effectiveToolbar =
    hideSectionHead
      ? null
      : stage.status === 'Completed' &&
          showViewMaterializedMatchesLink(stage, materialize ?? undefined)
        ? (
          <TextLink to={`/stages/${stage.stageId}/matches`}>
            {t('fiche.materialize.viewPhaseMatches')}
          </TextLink>
        )
        : toolbar;

  return (
    <StripSection
      index={index}
      sectionId="operational-matchs"
      title={t('fiche.operational.cluster.matches')}
      titleChipRefreshing={titleChipRefreshing}
      titlePresentation={hideSectionHead ? 'hidden' : 'chip'}
      toolbar={effectiveToolbar}
    >
      {matchesContent}
    </StripSection>
  );
}

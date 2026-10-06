import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

import { Tooltip } from '../../design-system/components/Tooltip';
import {
  DrawPendingIcon,
  LayersIcon,
  OverviewAttentionIcon,
  PersonIcon,
  TrashIcon,
  UnlockIcon,
} from '../../design-system/icons/contentIcons';
import { SettingsNavIcon } from '../../design-system/icons/shellIcons';
import { PendingLabel } from '../../ui';
import { StructureOperationalActionGroup } from './StructureOperationalActionGroup';
import { StructureOperationalRatioGauge } from './StructureOperationalGauge';
import { StructureOperationalTile } from './StructureOperationalTile';
import {
  resolvePlaceOccupancyTone,
  resolveResolvedPoolTone,
} from './structureOperationalCopy';

const compactDangerIcon =
  'ds-btn ds-btn--destructive ds-icon-button ds-icon-button--compact';
const primaryBtn = 'ds-btn ds-btn--primary ds-btn--sm';
const secondaryBtn = 'ds-btn ds-btn--secondary ds-btn--sm';

export type StructurePlacementOperationalSectionProps = {
  competitionId: string;
  placesN: number | null;
  poolFilled: number;
  occupiedPlacesCount: number | null;
  occupancyReady: boolean;
  showDrawCta: boolean;
  showActivateDrawCta: boolean;
  showResolvedPoolGauge: boolean;
  canManualPlace: boolean;
  canEditDraw: boolean;
  showReleaseCta: boolean;
  hasNonCancelledDraw: boolean;
  activeDraw: boolean;
  blockPerformDrawCta: boolean;
  createBlockedShort: string | null;
  createBlockedReason: string | null;
  showCreateBlockedCaption: boolean;
  showPoolHint: boolean;
  releasableAlignedCount: number;
  releasableTotal: number;
  releasePending: boolean;
  deactivateDrawPending: boolean;
  placesDiagnostics?: ReactNode;
  onOpenDrawWorkflow: () => void;
  onActivateDraw: () => void;
  onEditDrawParams: () => void;
  onManualPlace: () => void;
  onReleasePlacements: () => void;
  onDeactivateDraw: () => void;
  structureHubRefreshing?: boolean;
  embeddedInSection?: boolean;
};

export function StructurePlacementOperationalSection(props: StructurePlacementOperationalSectionProps) {
  const { t } = useTranslation('structure');
  const { t: tDraw } = useTranslation('draw');

  const capacity = props.placesN;
  const unknownCapacity = capacity == null || capacity <= 0;
  const poolTone =
    !unknownCapacity && capacity != null
      ? resolveResolvedPoolTone(props.poolFilled, capacity)
      : 'neutral';
  const occupancyTone =
    !unknownCapacity &&
    capacity != null &&
    props.occupiedPlacesCount != null
      ? resolvePlaceOccupancyTone(props.occupiedPlacesCount, capacity)
      : 'neutral';

  const showDrawOpen = props.showDrawCta;
  const showDrawActivate = props.showActivateDrawCta;
  const deactivateBlocked = props.hasNonCancelledDraw || props.deactivateDrawPending;

  const drawPrimary = showDrawOpen || showDrawActivate;
  const manualPrimary = props.canManualPlace && !drawPrimary;
  const drawBtnClass = drawPrimary ? primaryBtn : secondaryBtn;
  const manualBtnClass = manualPrimary ? primaryBtn : secondaryBtn;

  const drawPrimaryLabel = props.activeDraw
    ? t('fiche.openDrawWorkflow')
    : t('fiche.performDraw');

  const drawBlockedNote =
    props.showCreateBlockedCaption && props.createBlockedShort ? (
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
            <span>
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
            </span>
          </li>
        </ul>
      </div>
    ) : null;

  const releaseNote = props.releasePending ? (
    <p className="structure-operational-tile__note" role="status">
      <PendingLabel>{tDraw('releasingPlacements')}</PendingLabel>
    </p>
  ) : null;

  const deactivateNote = props.deactivateDrawPending ? (
    <p className="structure-operational-tile__note" role="status">
      <PendingLabel>{t('fiche.operational.placement.deactivatingDraw')}</PendingLabel>
    </p>
  ) : null;

  const showPoolState = !unknownCapacity && props.showResolvedPoolGauge;
  const showOccupancyGauge =
    !unknownCapacity &&
    (props.occupancyReady
      ? props.occupiedPlacesCount != null
      : true);

  const occupancyGauge =
    showOccupancyGauge && props.occupancyReady && props.occupiedPlacesCount != null ? (
      <StructureOperationalRatioGauge
        current={props.occupiedPlacesCount}
        capacity={capacity}
        caption={t('fiche.operational.placement.meterCaptionOccupied')}
        ariaLabel={t('fiche.operational.placement.meterAria')}
        poolTone={occupancyTone}
      />
    ) : showOccupancyGauge && !props.occupancyReady ? (
      <p className="structure-operational-tile__state-loading" role="status">
        <PendingLabel>{t('fiche.operational.placement.occupancyLoading')}</PendingLabel>
      </p>
    ) : null;

  const occupancySyncNote =
    props.structureHubRefreshing &&
    props.occupancyReady &&
    props.occupiedPlacesCount != null ? (
      <p className="structure-operational-tile__sync-status" role="status">
        <PendingLabel>{t('fiche.operational.syncing')}</PendingLabel>
      </p>
    ) : null;

  const drawPoolGauge = showPoolState ? (
    <StructureOperationalRatioGauge
      current={props.poolFilled}
      capacity={capacity}
      caption={t('fiche.drawCtaTeamsCaption')}
      ariaLabel={t('fiche.operational.placement.poolMeterAria')}
      poolTone={poolTone}
    />
  ) : null;

  const drawPrimaryControl = showDrawOpen ? (
    props.blockPerformDrawCta ? (
      <Tooltip content={props.createBlockedShort ?? drawPrimaryLabel}>
        <span className="structure-operational-tile__action-wrap">
          <button type="button" className={drawBtnClass} disabled>
            {props.activeDraw ? (
              <>
                <DrawPendingIcon size="sm" />
                {drawPrimaryLabel}
              </>
            ) : (
              drawPrimaryLabel
            )}
          </button>
        </span>
      </Tooltip>
    ) : (
      <button
        type="button"
        className={drawBtnClass}
        onClick={props.onOpenDrawWorkflow}
      >
        {props.activeDraw ? (
          <>
            <DrawPendingIcon size="sm" />
            {drawPrimaryLabel}
          </>
        ) : (
          drawPrimaryLabel
        )}
      </button>
    )
  ) : showDrawActivate ? (
    <button
      type="button"
      className={drawBtnClass}
      onClick={props.onActivateDraw}
    >
      <DrawPendingIcon size="sm" />
      {t('fiche.activateDraw')}
    </button>
  ) : null;

  const releaseControl = props.showReleaseCta ? (
    <Tooltip content={tDraw('releasePlacementsHint')}>
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
    </Tooltip>
  ) : null;

  const settingsControl = props.canEditDraw ? (
    <Tooltip content={t('fiche.drawParamsAction')}>
      <button
        type="button"
        className="ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact"
        onClick={props.onEditDrawParams}
        aria-label={t('fiche.drawParamsAction')}
      >
        <SettingsNavIcon size="sm" />
      </button>
    </Tooltip>
  ) : null;

  const deactivateTooltip = props.hasNonCancelledDraw
    ? t('regulation.deactivateDrawBlockedHint')
    : t('fiche.deactivateDraw');

  const deactivateControl = props.canEditDraw && props.showDrawCta ? (
    <Tooltip content={deactivateTooltip}>
      <span className="structure-operational-tile__action-wrap">
        <button
          type="button"
          className={compactDangerIcon}
          disabled={deactivateBlocked}
          onClick={props.onDeactivateDraw}
          aria-label={t('fiche.deactivateDraw')}
        >
          <TrashIcon size="sm" />
        </button>
      </span>
    </Tooltip>
  ) : null;

  const showDrawCluster =
    showPoolState ||
    drawPrimaryControl != null ||
    releaseControl != null ||
    settingsControl != null ||
    deactivateControl != null;

  const showOccupancyCluster = showOccupancyGauge || props.canManualPlace;

  const manualControl = props.canManualPlace ? (
    <button type="button" className={manualBtnClass} onClick={props.onManualPlace}>
      <PersonIcon size="sm" />
      {t('fiche.manualPlacementCta')}
    </button>
  ) : null;

  const stateGrid =
    showOccupancyCluster || showDrawCluster ? (
      <div
        className={[
          'structure-operational-state',
          showOccupancyCluster && showDrawCluster
            ? 'structure-operational-state--dual'
            : null,
        ]
          .filter(Boolean)
          .join(' ')}
      >
        {showOccupancyCluster ? (
          <div className="structure-operational-state__cluster">
            <StructureOperationalActionGroup
              label={t('fiche.operational.cluster.places')}
              labelId="operational-placement-places-label"
            >
              <>
                {showOccupancyGauge ? (
                  <div className="structure-operational-state__gauge-block">
                    {occupancyGauge}
                    {occupancySyncNote}
                  </div>
                ) : null}
                {manualControl ? (
                  <div className="structure-operational-state__cluster-actions">
                    {manualControl}
                  </div>
                ) : null}
              </>
            </StructureOperationalActionGroup>
          </div>
        ) : null}
        {showDrawCluster ? (
          <div className="structure-operational-state__cluster">
            <StructureOperationalActionGroup
              label={t('fiche.operational.cluster.draw')}
              labelId="operational-placement-draw-label"
            >
              <>
                {showPoolState ? (
                  <div className="structure-operational-state__gauge-block">
                    {drawPoolGauge}
                  </div>
                ) : null}
                {drawBlockedNote}
                <div className="structure-operational-state__cluster-actions structure-operational-state__cluster-actions--draw">
                  {drawPrimaryControl}
                  {releaseControl}
                  {settingsControl}
                  {deactivateControl}
                </div>
              </>
            </StructureOperationalActionGroup>
          </div>
        ) : null}
      </div>
    ) : (
      <span className="structure-operational-tile__state-empty" />
    );

  const hasNotes = releaseNote || deactivateNote;

  if (props.embeddedInSection) {
    return (
      <>
        {stateGrid}
        {hasNotes ? (
          <div className="structure-operational-tile__notes">
            {releaseNote}
            {deactivateNote}
          </div>
        ) : null}
        {props.placesDiagnostics ? (
          <div className="structure-operational-tile__trail">
            {props.placesDiagnostics}
          </div>
        ) : null}
      </>
    );
  }

  return (
    <StructureOperationalTile
      id="operational-placement"
      title={t('fiche.operational.placement.title')}
      icon={<LayersIcon size="sm" />}
      showHead
      gauge={stateGrid}
      notes={
        hasNotes ? (
          <>
            {releaseNote}
            {deactivateNote}
          </>
        ) : undefined
      }
      trail={props.placesDiagnostics}
    />
  );
}

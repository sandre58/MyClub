import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

import { Tooltip } from '../../design-system/components/Tooltip';
import {
  DrawPendingIcon,
  LayersIcon,
  PersonIcon,
  TrashIcon,
  UnlockIcon,
} from '../../design-system/icons/contentIcons';
import { SettingsNavIcon } from '../../design-system/icons/shellIcons';
import { PendingLabel } from '../../ui';
import { StructureOperationalActionGroup } from './StructureOperationalActionGroup';
import { StructureOperationalRatioGauge } from './StructureOperationalGauge';
import { StructureOperationalTile } from './StructureOperationalTile';
import { StructureOperationalOverflowMenu } from './StructureOperationalOverflowMenu';
import { resolvePlacementPoolTone } from './structureOperationalCopy';

const compactDangerIcon =
  'ds-btn ds-btn--destructive ds-icon-button ds-icon-button--compact';

export type StructurePlacementOperationalSectionProps = {
  competitionId: string;
  placesN: number | null;
  poolFilled: number;
  occupiedPlacesCount: number | null;
  occupancyReady: boolean;
  showDrawCta: boolean;
  showActivateDrawCta: boolean;
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
};

export function StructurePlacementOperationalSection(props: StructurePlacementOperationalSectionProps) {
  const { t } = useTranslation('structure');
  const { t: tDraw } = useTranslation('draw');

  const capacity = props.placesN;
  const unknownCapacity = capacity == null || capacity <= 0;
  const poolTone =
    !unknownCapacity && capacity != null
      ? resolvePlacementPoolTone(props.poolFilled, capacity)
      : 'neutral';
  const occupancyTone =
    !unknownCapacity &&
    capacity != null &&
    props.occupiedPlacesCount != null
      ? resolvePlacementPoolTone(props.occupiedPlacesCount, capacity)
      : 'neutral';

  const overflowItems = [];
  if (props.showReleaseCta) {
    overflowItems.push({
      id: 'release',
      label: tDraw('releasePlacementsCount', {
        aligned: props.releasableAlignedCount,
        total: props.releasableTotal,
      }),
      disabled: props.releasePending,
      leading: <UnlockIcon size="sm" />,
      onSelect: props.onReleasePlacements,
    });
  }

  const showDrawOpen = props.showDrawCta;
  const showDrawActivate = props.showActivateDrawCta;
  const showDrawActions = showDrawOpen || showDrawActivate;
  const deactivateBlocked = props.hasNonCancelledDraw || props.deactivateDrawPending;
  const primaryDrawClass = 'ds-btn ds-btn--primary ds-btn--sm';
  const primaryManualClass = 'ds-btn ds-btn--primary ds-btn--sm';

  const blockedNotes =
    props.showCreateBlockedCaption && props.createBlockedShort ? (
      <p className="structure-operational-tile__note" role="status">
        {props.createBlockedShort}
        {props.createBlockedReason === 'belowMinimumTeams' ? (
          <>
            {' '}
            <Link className="ds-text-link" to={`/competitions/${props.competitionId}/teams`}>
              {t('fiche.drawWorkflow.createBlocked.echoTeams')}
            </Link>
          </>
        ) : null}
        {props.createBlockedReason === 'missingPots' && props.canEditDraw ? (
          <>
            {' '}
            <button
              type="button"
              className="ds-text-link"
              onClick={props.onEditDrawParams}
            >
              {t('fiche.drawWorkflow.createBlocked.echoDrawParams')}
            </button>
          </>
        ) : null}
      </p>
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

  const showPoolState = showDrawActions && !unknownCapacity;
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

  const hasStateContent = showOccupancyGauge;

  const drawPoolGauge = showPoolState ? (
    <StructureOperationalRatioGauge
      current={props.poolFilled}
      capacity={capacity}
      caption={t('fiche.drawCtaTeamsCaption')}
      ariaLabel={t('fiche.operational.placement.poolMeterAria')}
      poolTone={poolTone}
      dense
    />
  ) : null;

  const drawPrimaryLabel = props.activeDraw
    ? t('fiche.openDrawWorkflow')
    : t('fiche.performDraw');

  const drawPrimaryControl = showDrawOpen ? (
    props.blockPerformDrawCta ? (
      <Tooltip content={props.createBlockedShort ?? ''}>
        <span className="structure-operational-tile__action-wrap">
          <button type="button" className={primaryDrawClass} disabled>
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
        className={primaryDrawClass}
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
      className={primaryDrawClass}
      onClick={props.onActivateDraw}
    >
      <DrawPendingIcon size="sm" />
      {t('fiche.activateDraw')}
    </button>
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

  const deactivateControl = props.canEditDraw ? (
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

  const showTirageColumn =
    showPoolState ||
    drawPrimaryControl != null ||
    settingsControl != null ||
    deactivateControl != null ||
    overflowItems.length > 0;
  const showPlacerColumn = props.canManualPlace;

  const footer =
    showTirageColumn || showPlacerColumn ? (
      <div className="structure-operational-tile__columns">
        {showTirageColumn ? (
          <StructureOperationalActionGroup
            label={t('fiche.operational.placement.groupDraw')}
            labelId="operational-placement-draw-label"
            suffix={drawPoolGauge}
            rowClassName="structure-operational-tile__group-row--tirage"
          >
            <div className="structure-operational-tile__tirage-primary">
              {drawPrimaryControl}
            </div>
            <div className="ds-icon-toolbar structure-operational-tile__inline-tools">
              {settingsControl}
              {deactivateControl}
              {overflowItems.length > 0 ? (
                <StructureOperationalOverflowMenu
                  items={overflowItems}
                  ariaLabel={t('fiche.operational.placementOverflowAria')}
                />
              ) : null}
            </div>
          </StructureOperationalActionGroup>
        ) : null}
        {showPlacerColumn ? (
          <StructureOperationalActionGroup
            label={t('fiche.operational.placement.groupPlacer')}
            labelId="operational-placement-placer-label"
          >
            <button
              type="button"
              className={primaryManualClass}
              onClick={props.onManualPlace}
            >
              <PersonIcon size="sm" />
              {t('fiche.manualPlacementCta')}
            </button>
          </StructureOperationalActionGroup>
        ) : null}
      </div>
    ) : null;

  const hasNotes = blockedNotes || releaseNote || deactivateNote;

  return (
    <StructureOperationalTile
      id="operational-placement"
      title={t('fiche.operational.placement.title')}
      icon={<LayersIcon size="sm" />}
      gauge={
        hasStateContent ? (
          <>
            {occupancyGauge}
            {occupancySyncNote}
          </>
        ) : (
          <span className="structure-operational-tile__state-empty" />
        )
      }
      notes={
        hasNotes ? (
          <>
            {blockedNotes}
            {releaseNote}
            {deactivateNote}
          </>
        ) : undefined
      }
      actions={footer ?? undefined}
      trail={props.placesDiagnostics}
    />
  );
}

import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';

import { Tooltip } from '../../../design-system/components/Tooltip';
import {
  HourglassIcon,
  LocateIcon,
  WithdrawIcon,
} from '../../../design-system/icons/contentIcons';
import { TeamCrest } from '../../../design-system/TeamCrest';
import type { StructureResolutionOccupant } from '../../../types';
import { StructureResolutionFreshnessBadge } from '../resolution/StructureResolutionFreshnessBadge';
import { resolutionOccupantNeedsBadge } from '../resolution/structureResolutionOccupants';

export type StructurePhaseTeamLineProps = {
  displayName: string;
  logoMediaId?: string | null;
  primaryColor?: string | null;
  withdrawn?: boolean;
  ineligible?: boolean;
  pending?: boolean;
  pendingLabel?: string;
  placed?: boolean;
  placedPlaceLabel?: string | null;
  placedTooltip?: string | null;
  provenanceMeta?: string | null;
  provenanceTitle?: string | null;
  resolutionOccupant?: StructureResolutionOccupant | null;
  multiplePlacements?: boolean;
  entryName?: string | null;
  onReplace?: (occupant: StructureResolutionOccupant) => void;
  onRelease?: (occupant: StructureResolutionOccupant) => void;
};

function freshnessStateMetaLabel(
  occupant: StructureResolutionOccupant,
  translate: (key: string) => string,
): string | null {
  if (occupant.state === 'Perime' || occupant.state === 'Indetermine') {
    return translate(`resolution.freshness.state.${occupant.state}`);
  }
  return null;
}

export function StructurePhaseTeamLine({
  displayName,
  logoMediaId,
  primaryColor,
  withdrawn = false,
  ineligible = false,
  pending = false,
  pendingLabel,
  placed = false,
  placedPlaceLabel,
  placedTooltip,
  provenanceMeta,
  provenanceTitle,
  resolutionOccupant,
  multiplePlacements = false,
  entryName,
  onReplace,
  onRelease,
}: StructurePhaseTeamLineProps) {
  const { t } = useTranslation(['structure', 'teams']);

  const freshnessMeta =
    resolutionOccupant && resolutionOccupantNeedsBadge(resolutionOccupant)
      ? freshnessStateMetaLabel(resolutionOccupant, t)
      : null;

  const metaParts: ReactNode[] = [];
  if (ineligible && !withdrawn) {
    metaParts.push(
      <span key="ineligible" className="structure-entries__freshness-state">
        {t('composition.noLongerEligible')}
      </span>,
    );
  }
  if (provenanceMeta) {
    metaParts.push(
      <span
        key="provenance"
        className="structure-entries__provenance"
        title={provenanceTitle ?? provenanceMeta}
      >
        {provenanceMeta}
      </span>,
    );
  }
  if (placed) {
    const placeText =
      placedPlaceLabel?.trim() || t('entries.entryPlacedStatus');
    const tip =
      placedTooltip ??
      (placedPlaceLabel
        ? t('entries.entryPlacedTooltip', { place: placedPlaceLabel })
        : t('entries.entryPlacedStatus'));
    metaParts.push(
      <span key="placed" className="structure-entries__placed-hint" title={tip}>
        <LocateIcon size="sm" aria-hidden="true" />
        <span className="ds-visually-hidden">
          {t('entries.entryPlacedAriaPrefix')}
        </span>
        {placeText}
      </span>,
    );
  }
  if (freshnessMeta) {
    metaParts.push(
      <span key="freshness" className="structure-entries__freshness-state">
        {freshnessMeta}
      </span>,
    );
  }

  const freshnessAction =
    resolutionOccupant && !pending ? (
      <StructureResolutionFreshnessBadge
        occupant={resolutionOccupant}
        multiplePlacements={multiplePlacements}
        entryName={entryName ?? displayName}
        onReplace={onReplace}
        onRelease={onRelease}
        actionOnly
      />
    ) : null;

  if (pending) {
    return (
      <>
        <span
          className="structure-entries__team-pending-icon"
          aria-hidden="true"
        >
          <HourglassIcon size="sm" />
        </span>
        <div className="structure-entries__identity">
          <div className="structure-entries__identity-head">
            <span className="structure-entries__team-name structure-entries__team-name--pending">
              {pendingLabel}
            </span>
          </div>
          {metaParts.length > 0 ? (
            <div className="structure-entries__meta">{metaParts}</div>
          ) : null}
        </div>
        <div
          className="structure-entries__action"
          aria-hidden={!freshnessAction}
        >
          {freshnessAction}
        </div>
      </>
    );
  }

  return (
    <>
      <TeamCrest
        name={displayName}
        logoMediaId={logoMediaId}
        primaryColor={primaryColor}
        size="sm"
      />
      <div className="structure-entries__identity">
        <div className="structure-entries__identity-head">
          <span
            className={[
              'structure-entries__team-name',
              ineligible ? 'structure-entries__team-name--ineligible' : null,
            ]
              .filter(Boolean)
              .join(' ')}
          >
            {displayName}
          </span>
          {withdrawn ? (
            <Tooltip content={t('teams:withdrawnBadge')}>
              <span
                className="structure-entries__team-withdrawn-inline"
                role="img"
                aria-label={t('teams:withdrawnBadge')}
              >
                <WithdrawIcon size="sm" aria-hidden="true" />
              </span>
            </Tooltip>
          ) : null}
        </div>
        {metaParts.length > 0 ? (
          <div className="structure-entries__meta">{metaParts}</div>
        ) : null}
      </div>
      <div
        className="structure-entries__action"
        aria-hidden={freshnessAction == null}
      >
        {freshnessAction}
      </div>
    </>
  );
}

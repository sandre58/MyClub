import type { ReactNode } from 'react';
import type { TFunction } from 'i18next';
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
  /** Qualifiers panel — compact hint row under the rule label (option B). */
  layout?: 'default' | 'qualifierHint';
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

function getTeamLineMeta(props: StructurePhaseTeamLineProps, t: TFunction) {
  const {
    ineligible,
    withdrawn,
    provenanceMeta,
    provenanceTitle,
    placed,
    placedPlaceLabel,
    placedTooltip,
    resolutionOccupant,
    layout,
  } = props;

  const freshnessMeta =
    resolutionOccupant && resolutionOccupantNeedsBadge(resolutionOccupant)
      ? freshnessStateMetaLabel(resolutionOccupant, t)
      : null;

  const metaParts: ReactNode[] = [];
  if (ineligible && !withdrawn) {
    metaParts.push(
      <span key="ineligible" className="structure-entries__eligibility-meta">
        {t('composition.noLongerEligible')}
      </span>,
    );
  }
  if (provenanceMeta && layout !== 'qualifierHint') {
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
    const placeLabel = placedPlaceLabel?.trim() ?? '';
    const tip =
      placedTooltip ??
      (placeLabel
        ? t('entries.entryPlacedTooltip', { place: placeLabel })
        : t('entries.entryPlacedStatus'));
    metaParts.push(
      <span key="placed" className="structure-entries__placed-hint" title={tip}>
        <LocateIcon size="sm" aria-hidden="true" />
        {placeLabel ? (
          <>
            <span className="ds-visually-hidden">
              {t('entries.entryPlacedAriaPrefix')}
            </span>
            {placeLabel}
          </>
        ) : (
          <span className="ds-visually-hidden">
            {t('entries.entryPlacedStatus')}
          </span>
        )}
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

  return metaParts;
}

export function StructurePhaseTeamLine(props: StructurePhaseTeamLineProps) {
  const {
    displayName,
    logoMediaId,
    primaryColor,
    withdrawn = false,
    ineligible = false,
    pending = false,
    pendingLabel,
    resolutionOccupant,
    multiplePlacements = false,
    entryName,
    onReplace,
    onRelease,
    layout = 'default',
  } = props;
  const { t } = useTranslation(['structure', 'teams']);
  const metaParts = getTeamLineMeta(props, t);

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

  if (layout === 'qualifierHint') {
    if (pending) {
      return (
        <div className="structure-composition-qualifiers__outcome">
          <span className="structure-composition-qualifiers__hint structure-composition-qualifiers__hint--pending">
            <span
              className="structure-entries__team-pending-icon"
              aria-hidden="true"
            >
              <HourglassIcon size="sm" />
            </span>
            <span className="structure-entries__team-name structure-entries__team-name--pending">
              {pendingLabel}
            </span>
          </span>
          <div
            className="structure-entries__action"
            aria-hidden={!freshnessAction}
          >
            {freshnessAction}
          </div>
        </div>
      );
    }

    return (
      <div className="structure-composition-qualifiers__outcome">
        <span className="structure-composition-qualifiers__hint">
          <TeamCrest
            name={displayName}
            logoMediaId={logoMediaId}
            primaryColor={primaryColor}
            size="xs"
            className="structure-composition-qualifiers__hint-crest"
          />
          <span
            className={[
              'structure-composition-qualifiers__hint-name',
              ineligible
                ? 'structure-composition-qualifiers__hint-name--ineligible'
                : null,
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
          {metaParts.length > 0 ? (
            <span className="structure-composition-qualifiers__hint-meta">
              {metaParts}
            </span>
          ) : null}
        </span>
        <div
          className="structure-entries__action"
          aria-hidden={freshnessAction == null}
        >
          {freshnessAction}
        </div>
      </div>
    );
  }

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
        <div
          className={[
            'structure-entries__identity-head',
            metaParts.length === 0
              ? 'structure-entries__identity-head--solo'
              : null,
          ]
            .filter(Boolean)
            .join(' ')}
        >
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

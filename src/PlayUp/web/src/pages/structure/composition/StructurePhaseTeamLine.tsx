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

function placedHintNode(
  props: Pick<
    StructurePhaseTeamLineProps,
    'placed' | 'placedPlaceLabel' | 'placedTooltip'
  >,
  t: TFunction,
) {
  if (!props.placed) {
    return null;
  }
  const placeLabel = props.placedPlaceLabel?.trim() ?? '';
  const tip =
    props.placedTooltip ??
    (placeLabel
      ? t('entries.entryPlacedTooltip', { place: placeLabel })
      : t('entries.entryPlacedStatus'));
  return (
    <span className="structure-entries__placed-hint" title={tip}>
      <LocateIcon size="xs" aria-hidden="true" />
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
    </span>
  );
}

function qualifierHintTrailingUnits(
  props: StructurePhaseTeamLineProps,
  t: TFunction,
): ReactNode[] {
  const { resolutionOccupant, placed } = props;
  const freshnessMeta =
    resolutionOccupant && resolutionOccupantNeedsBadge(resolutionOccupant)
      ? freshnessStateMetaLabel(resolutionOccupant, t)
      : null;

  const units: ReactNode[] = [];
  if (placed) {
    units.push(
      <span key="place" className="structure-composition-qualifiers__hint-unit">
        {placedHintNode(props, t)}
      </span>,
    );
  }
  if (freshnessMeta) {
    units.push(
      <span
        key="freshness"
        className="structure-composition-qualifiers__hint-unit"
      >
        <span className="structure-entries__freshness-state">
          {freshnessMeta}
        </span>
      </span>,
    );
  }
  return units;
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
      const trailingUnits = qualifierHintTrailingUnits(props, t);
      return (
        <div className="structure-composition-qualifiers__outcome">
          <span
            className={[
              'structure-composition-qualifiers__hint',
              'structure-composition-qualifiers__hint--pending',
            ].join(' ')}
          >
            <span className="structure-composition-qualifiers__hint-unit structure-composition-qualifiers__hint-unit--identity">
              <span
                className="structure-entries__team-pending-icon structure-entries__team-pending-icon--caption"
                aria-hidden="true"
              >
                <HourglassIcon size="xs" />
              </span>
              <span className="structure-composition-qualifiers__hint-pending-label">
                {pendingLabel}
              </span>
            </span>
            {trailingUnits}
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

    const trailingUnits = qualifierHintTrailingUnits(props, t);

    return (
      <div className="structure-composition-qualifiers__outcome">
        <span className="structure-composition-qualifiers__hint">
          <span className="structure-composition-qualifiers__hint-unit structure-composition-qualifiers__hint-unit--identity">
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
                  <WithdrawIcon size="xs" aria-hidden="true" />
                </span>
              </Tooltip>
            ) : null}
            {ineligible && !withdrawn ? (
              <span className="structure-entries__eligibility-meta">
                {t('composition.noLongerEligible')}
              </span>
            ) : null}
          </span>
          {trailingUnits}
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
          <HourglassIcon size="xs" />
        </span>
        <div className="structure-entries__identity">
          <div className="structure-entries__identity-head">
            <span className="structure-entries__team-name structure-entries__team-name--pending">
              {pendingLabel}
            </span>
          </div>
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
            'structure-entries__identity-head--solo',
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

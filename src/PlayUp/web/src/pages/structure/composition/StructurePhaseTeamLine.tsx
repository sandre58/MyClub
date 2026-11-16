import type { ReactNode } from 'react';
import type { TFunction } from 'i18next';
import { useTranslation } from 'react-i18next';

import { Tooltip } from '../../../design-system/components/Tooltip';
import {
  HourglassIcon,
  ListMinusIcon,
  LocateIcon,
  PendingCircleIcon,
  StructureIssueIcon,
  UnconfirmableIcon,
  WithdrawIcon,
} from '../../../design-system/icons/contentIcons';
import { TeamCrest } from '../../../design-system/TeamCrest';
import type { StructureResolutionOccupant } from '../../../types';
import {
  isDefinitivelyUnconfirmed,
  resolutionOccupantNeedsBadge,
} from '../resolution/structureResolutionOccupants';
import { structurePhaseTeamFreshnessAction } from './structurePhaseTeamFreshnessAction';
import type { StructurePhaseTeamLineProps } from './structurePhaseTeamLineTypes';

export type { StructurePhaseTeamLineProps } from './structurePhaseTeamLineTypes';

function freshnessStateMetaLabel(
  occupant: StructureResolutionOccupant,
  translate: (key: string) => string,
): string | null {
  if (occupant.state === 'Perime' || occupant.state === 'Indetermine') {
    return translate(`resolution.freshness.state.${occupant.state}`);
  }
  return null;
}

function eligibilityMetaNode(label: string) {
  return (
    <span className="structure-entries__eligibility-meta">
      <ListMinusIcon size="xs" aria-hidden="true" />
      {label}
    </span>
  );
}

function freshnessStateMetaNode(
  occupant: StructureResolutionOccupant,
  translate: (key: string) => string,
) {
  const label = freshnessStateMetaLabel(occupant, translate);
  if (!label) {
    return null;
  }
  const StateIcon =
    occupant.state === 'Perime'
      ? StructureIssueIcon
      : isDefinitivelyUnconfirmed(occupant)
        ? UnconfirmableIcon
        : PendingCircleIcon;
  return (
    <span className="structure-entries__freshness-state">
      <StateIcon size="xs" aria-hidden="true" />
      {label}
    </span>
  );
}

function placedTooltipBody(model: {
  intro: string;
  whereLines: readonly string[];
}): ReactNode {
  return (
    <span className="structure-entries__placed-tooltip-body">
      <span className="structure-entries__placed-tooltip-intro">
        {model.intro}
      </span>
      {model.whereLines.map((line, index) => (
        <span
          key={`${index}-${line}`}
          className="structure-entries__placed-tooltip-line"
        >
          {line}
        </span>
      ))}
    </span>
  );
}

function placedHintNode(
  props: Pick<
    StructurePhaseTeamLineProps,
    'placed' | 'placedPlaceLabel' | 'placedTooltipModel' | 'placedTooltip'
  >,
  t: TFunction,
) {
  if (!props.placed) {
    return null;
  }
  const placeLabel = props.placedPlaceLabel?.trim() ?? '';
  const model = props.placedTooltipModel;
  const tooltipContent =
    model != null
      ? placedTooltipBody(model)
      : (props.placedTooltip ?? t('entries.entryPlacedStatus'));
  const shortPlace = (model?.shortPlace?.trim() || placeLabel).trim();
  const accessibleName = shortPlace
    ? t('entries.placedHintShort', { place: shortPlace })
    : t('entries.entryPlacedStatus');
  return (
    <Tooltip content={tooltipContent}>
      <button
        type="button"
        className="structure-entries__placed-hint"
        aria-label={accessibleName}
      >
        <LocateIcon size="xs" aria-hidden="true" />
        {placeLabel ? (
          <span aria-hidden="true">{placeLabel}</span>
        ) : (
          <span className="ds-visually-hidden">
            {t('entries.entryPlacedStatus')}
          </span>
        )}
      </button>
    </Tooltip>
  );
}

function directTeamCaptionUnits(
  props: StructurePhaseTeamLineProps,
  t: TFunction,
): ReactNode[] {
  const {
    ineligible,
    withdrawn,
    provenanceMeta,
    provenanceTitle,
    placed,
    resolutionOccupant,
  } = props;

  const units: ReactNode[] = [];
  if (ineligible && !withdrawn) {
    units.push(
      <span key="ineligible">
        {eligibilityMetaNode(t('composition.noLongerEligible'))}
      </span>,
    );
  }
  if (provenanceMeta) {
    units.push(
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
    units.push(<span key="place">{placedHintNode(props, t)}</span>);
  }
  if (resolutionOccupant && resolutionOccupantNeedsBadge(resolutionOccupant)) {
    units.push(
      <span key="freshness">
        {freshnessStateMetaNode(resolutionOccupant, t)}
      </span>,
    );
  }
  return units;
}

function qualifierHintTrailingUnits(
  props: StructurePhaseTeamLineProps,
  t: TFunction,
): ReactNode[] {
  const { resolutionOccupant, placed, ineligible, withdrawn } = props;

  const units: ReactNode[] = [];
  if (ineligible && !withdrawn) {
    units.push(
      <span key="ineligible">
        {eligibilityMetaNode(t('composition.noLongerEligible'))}
      </span>,
    );
  }
  if (placed) {
    units.push(<span key="place">{placedHintNode(props, t)}</span>);
  }
  if (resolutionOccupant && resolutionOccupantNeedsBadge(resolutionOccupant)) {
    units.push(
      <span key="freshness">
        {freshnessStateMetaNode(resolutionOccupant, t)}
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
    omitFreshnessAction = false,
  } = props;
  const { t } = useTranslation(['structure', 'teams']);
  const captionUnits =
    layout === 'default' ? directTeamCaptionUnits(props, t) : [];

  const freshnessAction = structurePhaseTeamFreshnessAction({
    displayName,
    resolutionOccupant,
    multiplePlacements,
    entryName,
    onReplace,
    onRelease,
    pending,
  });

  const hintFreshnessAction =
    layout === 'qualifierHint' && !omitFreshnessAction ? freshnessAction : null;

  if (layout === 'qualifierHint') {
    if (pending) {
      const trailingUnits = qualifierHintTrailingUnits(props, t);
      return (
        <div className="structure-composition-qualifiers__outcome">
          <div
            className={[
              'structure-composition-qualifiers__hint',
              'structure-composition-qualifiers__hint--as-team-line',
              'structure-composition-qualifiers__hint--pending',
            ].join(' ')}
          >
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
              {trailingUnits.length > 0 ? (
                <div className="structure-entries__meta structure-entries__meta--caption">
                  {trailingUnits.map((unit, index) => (
                    <span key={index} className="structure-entries__meta-unit">
                      {unit}
                    </span>
                  ))}
                </div>
              ) : null}
            </div>
          </div>
          {hintFreshnessAction != null ? (
            <div className="structure-entries__action">
              {hintFreshnessAction}
            </div>
          ) : null}
        </div>
      );
    }

    const trailingUnits = qualifierHintTrailingUnits(props, t);

    return (
      <div className="structure-composition-qualifiers__outcome">
        <div
          className={[
            'structure-composition-qualifiers__hint',
            'structure-composition-qualifiers__hint--as-team-line',
          ].join(' ')}
        >
          <TeamCrest
            name={displayName}
            logoMediaId={logoMediaId}
            primaryColor={primaryColor}
            size="sm"
            className="structure-composition-qualifiers__hint-crest"
          />
          <div className="structure-entries__identity">
            <div className="structure-entries__identity-head">
              <span
                className={[
                  'structure-entries__team-name',
                  ineligible
                    ? 'structure-entries__team-name--ineligible'
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
            </div>
            {trailingUnits.length > 0 ? (
              <div className="structure-entries__meta structure-entries__meta--caption">
                {trailingUnits.map((unit, index) => (
                  <span key={index} className="structure-entries__meta-unit">
                    {unit}
                  </span>
                ))}
              </div>
            ) : null}
          </div>
        </div>
        {hintFreshnessAction != null ? (
          <div className="structure-entries__action">{hintFreshnessAction}</div>
        ) : null}
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
            captionUnits.length === 0
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
        {captionUnits.length > 0 ? (
          <div className="structure-entries__meta structure-entries__meta--caption">
            {captionUnits.map((unit, index) => (
              <span key={index} className="structure-entries__meta-unit">
                {unit}
              </span>
            ))}
          </div>
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

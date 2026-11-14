import type { ReactNode, SyntheticEvent } from 'react';
import { Link, useInRouterContext, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

import { Tooltip } from '../../../design-system/components/Tooltip';
import {
  HourglassIcon,
  RefreshIcon,
  UnconfirmableIcon,
  UnlockIcon,
} from '../../../design-system/icons/contentIcons';
import { ChevronRightIcon } from '../../../design-system/icons/shellIcons';
import type { StructureResolutionOccupant } from '../../../types';
import {
  freshnessAction,
  indeterminacyExplanation,
  type FreshnessAction,
} from './freshnessGuidance';
import {
  isDefinitivelyUnconfirmed,
  resolutionOccupantNeedsBadge,
} from './structureResolutionOccupants';

const actionButtonClass =
  'ds-btn ds-btn--ghost ds-icon-button ds-icon-button--xs structure-freshness-badge__action';

type FreshnessControl = 'replace' | 'release' | 'navigate';

type StructureResolutionFreshnessBadgeProps = {
  occupant: StructureResolutionOccupant | null | undefined;
  /** Team listed on several Qual/Prog places — informational only. */
  multiplePlacements?: boolean;
  /** Opens the cascade preview. Ignored unless the occupant is replayable. */
  onReplace?: (occupant: StructureResolutionOccupant) => void;
  /** Opens the release preview. Ignored unless the occupant is releasable. */
  onRelease?: (occupant: StructureResolutionOccupant) => void;
  /** Stored team, used to name who is no longer confirmed. */
  entryName?: string | null;
};

function stateIcon(
  occupant: StructureResolutionOccupant,
  className: string,
): ReactNode {
  if (occupant.state === 'Perime') {
    return <RefreshIcon size="xs" className={className} aria-hidden="true" />;
  }
  if (isDefinitivelyUnconfirmed(occupant)) {
    return (
      <UnconfirmableIcon size="xs" className={className} aria-hidden="true" />
    );
  }
  return <HourglassIcon size="xs" className={className} aria-hidden="true" />;
}

function controlIcon(
  occupant: StructureResolutionOccupant,
  control: FreshnessControl,
  className: string,
): ReactNode {
  if (control === 'release') {
    return <UnlockIcon size="xs" className={className} aria-hidden="true" />;
  }
  if (control === 'replace') {
    return <RefreshIcon size="xs" className={className} aria-hidden="true" />;
  }
  if (control === 'navigate') {
    return (
      <ChevronRightIcon size="xs" className={className} aria-hidden="true" />
    );
  }
  return stateIcon(occupant, className);
}

function stopSlotActivation(event: SyntheticEvent) {
  event.stopPropagation();
}

/** Stops schematic slot hit / case tooltip from seeing the event; sits above Tooltip. */
function BadgeActivationGuard({ children }: { children: ReactNode }) {
  return (
    <span
      className="structure-freshness-badge"
      onPointerDown={stopSlotActivation}
      onTouchStart={stopSlotActivation}
    >
      {children}
    </span>
  );
}

export function StructureResolutionFreshnessBadge(
  props: StructureResolutionFreshnessBadgeProps,
) {
  const inRouter = useInRouterContext();
  if (!inRouter) {
    return (
      <FreshnessBadgeBody {...props} competitionId={null} linked={false} />
    );
  }
  return <FreshnessBadgeInRouter {...props} />;
}

function FreshnessBadgeInRouter(props: StructureResolutionFreshnessBadgeProps) {
  const { competitionId } = useParams();
  return (
    <FreshnessBadgeBody
      {...props}
      competitionId={competitionId ?? null}
      linked
    />
  );
}

function FreshnessBadgeBody({
  occupant,
  multiplePlacements = false,
  onReplace,
  onRelease,
  entryName,
  competitionId,
  linked,
}: StructureResolutionFreshnessBadgeProps & {
  competitionId: string | null;
  linked: boolean;
}) {
  const { t } = useTranslation('structure');

  if (!resolutionOccupantNeedsBadge(occupant)) {
    return null;
  }

  const action = freshnessAction(occupant, competitionId, {
    canReplace: !!onReplace,
    canRelease: !!onRelease,
  });
  const control = multiplePlacements
    ? null
    : action?.kind === 'replace'
      ? 'replace'
      : action?.kind === 'release'
        ? 'release'
        : action?.kind === 'navigate' && linked
          ? 'navigate'
          : null;
  const labelKey = `resolution.freshness.state.${occupant.state}` as const;
  const label = t(labelKey);
  const causeKey =
    occupant.state === 'Perime' && occupant.stalenessCause
      ? (`resolution.freshness.cause.${occupant.stalenessCause}` as const)
      : occupant.state === 'Indetermine' && occupant.indeterminacyCause
        ? (`resolution.freshness.indeterminacy.${occupant.indeterminacyCause}.cause` as const)
        : null;
  const causeLabel = causeKey ? t(causeKey) : null;
  const explanation = indeterminacyExplanation(occupant, entryName);
  const sentence = explanation
    ? t('resolution.freshness.unconfirmedSentence', {
        team: explanation.team,
        detail: t(explanation.detailKey, explanation.detailParams),
      })
    : null;
  const explained = sentence ?? causeLabel;
  const definitiveLabel = isDefinitivelyUnconfirmed(occupant)
    ? t('resolution.freshness.definitive')
    : null;
  const multiplePlacementsNote = multiplePlacements
    ? t('resolution.freshness.multiplePlacements')
    : null;
  const cascadeBlockedNote =
    occupant.state === 'Perime' &&
    !action &&
    !multiplePlacements &&
    !occupant.cascadeReplaceable &&
    (occupant.stalenessCause === 'ResultatAmont' ||
      occupant.stalenessCause === 'RegleQualification')
      ? t('resolution.freshness.cascadeNotReplaceable')
      : null;
  const causeText = [
    explained,
    definitiveLabel,
    cascadeBlockedNote,
    multiplePlacementsNote,
  ]
    .filter(Boolean)
    .join(' — ');
  const actionLabel =
    multiplePlacements || !action ? null : actionText(t, action);

  const ariaLabel =
    control && actionLabel
      ? causeText
        ? t('resolution.freshness.ariaAction', {
            label,
            cause: causeText,
            action: actionLabel,
          })
        : t('resolution.freshness.ariaActionNoCause', {
            label,
            action: actionLabel,
          })
      : causeText
        ? t('resolution.freshness.ariaWithCause', {
            label,
            cause: causeText,
          })
        : label;

  const icon =
    control === 'replace' || control === 'release' || control === 'navigate'
      ? controlIcon(occupant, control, 'structure-freshness-badge__icon')
      : stateIcon(occupant, 'structure-freshness-badge__icon');

  const tip = (
    <span className="structure-freshness-replace__tip">
      <span className="structure-freshness-replace__state">{label}</span>
      {explained ? (
        <span className="structure-freshness-replace__cause">{explained}</span>
      ) : null}
      {definitiveLabel ? (
        <span className="structure-freshness-replace__cause">
          {definitiveLabel}
        </span>
      ) : null}
      {cascadeBlockedNote ? (
        <span className="structure-freshness-replace__cause">
          {cascadeBlockedNote}
        </span>
      ) : null}
      {multiplePlacementsNote ? (
        <span className="structure-freshness-replace__cause">
          {multiplePlacementsNote}
        </span>
      ) : null}
      {actionLabel ? (
        <span className="structure-freshness-replace__action">
          {actionLabel}
        </span>
      ) : null}
    </span>
  );

  if (control === 'replace' || control === 'release') {
    return (
      <BadgeActivationGuard>
        <Tooltip content={tip}>
          <button
            type="button"
            className={actionButtonClass}
            aria-label={ariaLabel}
            onClick={(event) => {
              stopSlotActivation(event);
              if (control === 'release') {
                onRelease?.(occupant);
                return;
              }
              onReplace?.(occupant);
            }}
          >
            {icon}
          </button>
        </Tooltip>
      </BadgeActivationGuard>
    );
  }

  if (control === 'navigate' && action?.kind === 'navigate') {
    return (
      <BadgeActivationGuard>
        <Tooltip content={tip}>
          <Link
            to={action.href}
            className={actionButtonClass}
            aria-label={ariaLabel}
            onClick={stopSlotActivation}
          >
            {icon}
          </Link>
        </Tooltip>
      </BadgeActivationGuard>
    );
  }

  return (
    <BadgeActivationGuard>
      <Tooltip content={tip}>
        <span
          className="structure-freshness-badge__glyph"
          role="img"
          tabIndex={0}
          aria-label={ariaLabel}
        >
          {icon}
        </span>
      </Tooltip>
    </BadgeActivationGuard>
  );
}

function actionText(
  t: (key: string, params?: Record<string, string>) => string,
  action: FreshnessAction | null,
): string | null {
  if (action?.kind === 'replace') {
    return t('resolution.cascade.replaceHint');
  }
  if (action?.kind === 'release') {
    return t('resolution.release.action');
  }
  if (action?.kind === 'navigate') {
    return t(action.labelKey, action.labelParams);
  }
  return null;
}

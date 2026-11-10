import { type KeyboardEvent, type MouseEvent, type ReactNode } from 'react';
import { useInRouterContext, useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

import { Tooltip } from '../../../design-system/components/Tooltip';
import { Status } from '../../../design-system/components/Status';
import {
  HourglassIcon,
  InfoIcon,
  RefreshIcon,
} from '../../../design-system/icons/contentIcons';
import { ChevronRightIcon } from '../../../design-system/icons/shellIcons';
import type { StructureResolutionOccupant } from '../../../types';
import { freshnessGuidance } from './freshnessGuidance';
import {
  isReplayableStale,
  resolutionOccupantNeedsBadge,
} from './structureResolutionOccupants';

type FreshnessBadgeVariant = 'full' | 'icon';

type StructureResolutionFreshnessBadgeProps = {
  occupant: StructureResolutionOccupant | null | undefined;
  density?: 'context' | 'compact';
  /**
   * `icon` hides the state word (schematic slots). The word moves into the tooltip
   * when the badge is not directly clickable.
   * Lists and details keep the default `full` pill.
   */
  variant?: FreshnessBadgeVariant;
  /** Opens the cascade preview. Ignored unless the occupant is stale Qual/Prog. */
  onReplace?: (occupant: StructureResolutionOccupant) => void;
  /**
   * Manual placement slots use a single outer button. Nested action controls are
   * invalid HTML — keep the tooltip and skip direct navigation/replace here.
   */
  directActionDisabled?: boolean;
};

function badgeTone(
  state: StructureResolutionOccupant['state'],
): 'attention' | 'error' | 'neutral' {
  switch (state) {
    case 'Perime':
      return 'attention';
    case 'Indetermine':
      return 'attention';
    case 'NonVerifiee':
      return 'neutral';
    default:
      return 'neutral';
  }
}

function stateIcon(state: StructureResolutionOccupant['state']): ReactNode {
  const className = 'structure-freshness-badge__icon';
  switch (state) {
    case 'Perime':
      return <RefreshIcon size="sm" className={className} aria-hidden="true" />;
    case 'Indetermine':
      return (
        <HourglassIcon size="sm" className={className} aria-hidden="true" />
      );
    default:
      return <InfoIcon size="sm" className={className} aria-hidden="true" />;
  }
}

export function StructureResolutionFreshnessBadge(
  props: StructureResolutionFreshnessBadgeProps,
) {
  const inRouter = useInRouterContext();
  if (!inRouter) {
    return (
      <FreshnessBadgeBody {...props} competitionId={null} onNavigate={null} />
    );
  }
  return <FreshnessBadgeRouted {...props} />;
}

function FreshnessBadgeRouted(props: StructureResolutionFreshnessBadgeProps) {
  const { competitionId } = useParams();
  const navigate = useNavigate();
  return (
    <FreshnessBadgeBody
      {...props}
      competitionId={competitionId ?? null}
      onNavigate={navigate}
    />
  );
}

function FreshnessBadgeBody({
  occupant,
  density = 'compact',
  variant = 'full',
  onReplace,
  competitionId,
  directActionDisabled = false,
  onNavigate,
}: StructureResolutionFreshnessBadgeProps & {
  competitionId: string | null;
  onNavigate: ((href: string) => void) | null;
}) {
  const { t } = useTranslation('structure');

  if (!resolutionOccupantNeedsBadge(occupant)) {
    return null;
  }

  const guidance = freshnessGuidance(occupant, competitionId);
  const labelKey = `resolution.freshness.state.${occupant.state}` as const;
  const label = t(labelKey);
  const causeKey =
    occupant.state === 'Perime' && occupant.stalenessCause
      ? (`resolution.freshness.cause.${occupant.stalenessCause}` as const)
      : occupant.state === 'Indetermine' && occupant.indeterminacyCause
        ? (`resolution.freshness.indeterminacy.${occupant.indeterminacyCause}.cause` as const)
        : null;
  const causeLabel = causeKey ? t(causeKey) : null;
  const replaceable = !!onReplace && isReplayableStale(occupant);
  const replaceHint = t('resolution.cascade.replaceHint');
  const navigateHref = guidance?.href ?? null;
  const directAction =
    !directActionDisabled &&
    (replaceable || (navigateHref != null && onNavigate != null));

  const guidanceActionLabel =
    guidance?.kind === 'upstream'
      ? t('resolution.freshness.upstreamLink')
      : guidance?.kind === 'indeterminate'
        ? guidance.cause === 'MatchNonJoue' && guidance.matchLabel
          ? t('resolution.freshness.indeterminacy.MatchNonJoue.actionNamed', {
              match: guidance.matchLabel,
            })
          : t(`resolution.freshness.indeterminacy.${guidance.cause}.action`)
        : null;

  const tooltipAction = replaceable
    ? replaceHint
    : (guidanceActionLabel ?? null);

  const ariaLabel =
    tooltipAction != null && causeLabel != null
      ? `${causeLabel}. ${tooltipAction}`
      : causeLabel != null
        ? t('resolution.freshness.ariaWithCause', { label, cause: causeLabel })
        : label;

  const iconOnly = variant === 'icon';
  const showStateIcon =
    iconOnly ||
    occupant.state === 'Indetermine' ||
    (replaceable && occupant.state === 'Perime');

  const badge = (
    <Status
      density={density}
      tone={badgeTone(occupant.state)}
      variant="soft"
      shape="pill"
      className={iconOnly ? 'structure-freshness-badge--icon' : undefined}
      aria-label={directAction ? undefined : ariaLabel}
    >
      {showStateIcon ? stateIcon(occupant.state) : null}
      {iconOnly ? null : label}
      {directAction ? (
        <ChevronRightIcon
          size="sm"
          className="structure-freshness-badge__chevron"
          aria-hidden="true"
        />
      ) : null}
    </Status>
  );

  const tip = (
    <span className="structure-freshness-replace__tip">
      {iconOnly ? (
        <span className="structure-freshness-replace__state">{label}</span>
      ) : null}
      {causeLabel ? (
        <span className="structure-freshness-replace__cause">{causeLabel}</span>
      ) : null}
      {tooltipAction ? (
        <span className="structure-freshness-replace__action">
          {tooltipAction}
        </span>
      ) : null}
    </span>
  );
  const hasTip = iconOnly || causeLabel != null || tooltipAction != null;

  const runAction = (event: MouseEvent | KeyboardEvent) => {
    event.stopPropagation();
    event.preventDefault();
    if (replaceable) {
      onReplace?.(occupant);
      return;
    }
    if (navigateHref && onNavigate) {
      onNavigate(navigateHref);
    }
  };

  if (directAction) {
    return (
      <Tooltip content={tip}>
        <button
          type="button"
          className="structure-freshness-replace"
          aria-label={ariaLabel}
          onClick={runAction}
        >
          {badge}
        </button>
      </Tooltip>
    );
  }

  if (!hasTip) {
    return badge;
  }

  return <Tooltip content={tip}>{badge}</Tooltip>;
}

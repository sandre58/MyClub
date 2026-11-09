import type { KeyboardEvent, MouseEvent, ReactNode } from 'react';
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
import {
  isReplayableStale,
  resolutionOccupantNeedsBadge,
} from './structureResolutionOccupants';

type FreshnessBadgeVariant = 'full' | 'icon';

type StructureResolutionFreshnessBadgeProps = {
  occupant: StructureResolutionOccupant | null | undefined;
  density?: 'context' | 'compact';
  /**
   * `icon` hides the state word (schematic slots). The word moves into the tooltip.
   * Lists and details keep the default `full` pill.
   */
  variant?: FreshnessBadgeVariant;
  /** Opens the cascade preview. Ignored unless the occupant is stale Qual/Prog. */
  onReplace?: (occupant: StructureResolutionOccupant) => void;
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

export function StructureResolutionFreshnessBadge({
  occupant,
  density = 'compact',
  variant = 'full',
  onReplace,
}: StructureResolutionFreshnessBadgeProps) {
  const { t } = useTranslation('structure');

  if (!resolutionOccupantNeedsBadge(occupant)) {
    return null;
  }

  const labelKey = `resolution.freshness.state.${occupant.state}` as const;
  const label = t(labelKey);
  const causeKey =
    occupant.state === 'Perime' && occupant.stalenessCause
      ? (`resolution.freshness.cause.${occupant.stalenessCause}` as const)
      : null;
  const causeLabel = causeKey ? t(causeKey) : null;
  const ariaLabel =
    causeLabel != null
      ? t('resolution.freshness.ariaWithCause', { label, cause: causeLabel })
      : label;
  const replaceable = !!onReplace && isReplayableStale(occupant);
  const replaceHint = t('resolution.cascade.replaceHint');
  const replaceDetail =
    causeLabel != null
      ? t('resolution.cascade.replaceHintWithCause', {
          cause: causeLabel,
          action: replaceHint,
        })
      : replaceHint;
  const replaceName =
    variant === 'icon' ? `${label}. ${replaceDetail}` : replaceDetail;
  const iconOnly = variant === 'icon';

  const badge = (
    <Status
      density={density}
      tone={badgeTone(occupant.state)}
      variant="soft"
      shape="pill"
      className={iconOnly ? 'structure-freshness-badge--icon' : undefined}
      aria-label={replaceable ? undefined : ariaLabel}
    >
      {replaceable ? (
        <RefreshIcon
          size="sm"
          className="structure-freshness-badge__icon"
          aria-hidden="true"
        />
      ) : iconOnly ? (
        stateIcon(occupant.state)
      ) : null}
      {iconOnly ? null : label}
      {replaceable ? (
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
      {replaceable ? (
        <span className="structure-freshness-replace__action">
          {replaceHint}
        </span>
      ) : null}
    </span>
  );
  const hasTip = iconOnly || causeLabel != null || replaceable;

  if (replaceable) {
    const open = (event: MouseEvent | KeyboardEvent) => {
      event.stopPropagation();
      event.preventDefault();
      onReplace(occupant);
    };
    return (
      <Tooltip content={tip}>
        <button
          type="button"
          className="structure-freshness-replace"
          aria-label={replaceName}
          onClick={open}
        >
          {badge}
        </button>
      </Tooltip>
    );
  }

  if (!hasTip || (causeLabel == null && !iconOnly)) {
    return badge;
  }

  return <Tooltip content={tip}>{badge}</Tooltip>;
}

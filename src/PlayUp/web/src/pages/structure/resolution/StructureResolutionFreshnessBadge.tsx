import type { KeyboardEvent, MouseEvent } from 'react';
import { useTranslation } from 'react-i18next';

import { Tooltip } from '../../../design-system/components/Tooltip';
import { Status } from '../../../design-system/components/Status';
import { ReplaceOccupantIcon } from '../../../design-system/icons/contentIcons';
import type { StructureResolutionOccupant } from '../../../types';
import {
  isReplayableStale,
  resolutionOccupantNeedsBadge,
} from './structureResolutionOccupants';

type StructureResolutionFreshnessBadgeProps = {
  occupant: StructureResolutionOccupant | null | undefined;
  density?: 'context' | 'compact';
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

export function StructureResolutionFreshnessBadge({
  occupant,
  density = 'compact',
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
  const replaceName =
    causeLabel != null
      ? t('resolution.cascade.replaceHintWithCause', {
          cause: causeLabel,
          action: replaceHint,
        })
      : replaceHint;

  const badge = (
    <Status
      density={density}
      tone={badgeTone(occupant.state)}
      variant="soft"
      shape="pill"
      aria-label={replaceable ? undefined : ariaLabel}
    >
      {replaceable ? (
        <ReplaceOccupantIcon
          size="sm"
          className="structure-freshness-replace__icon"
          aria-hidden="true"
        />
      ) : null}
      {label}
    </Status>
  );

  if (replaceable) {
    const open = (event: MouseEvent | KeyboardEvent) => {
      event.stopPropagation();
      event.preventDefault();
      onReplace(occupant);
    };
    return (
      <Tooltip
        content={
          <span className="structure-freshness-replace__tip">
            {causeLabel ? (
              <span className="structure-freshness-replace__cause">
                {causeLabel}
              </span>
            ) : null}
            <span className="structure-freshness-replace__action">
              {replaceHint}
            </span>
          </span>
        }
      >
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

  if (causeLabel == null) {
    return badge;
  }

  return <Tooltip content={causeLabel}>{badge}</Tooltip>;
}

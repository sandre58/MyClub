import { useTranslation } from 'react-i18next';

import { Tooltip } from '../../../design-system/components/Tooltip';
import { Status } from '../../../design-system/components/Status';
import type { StructureResolutionOccupant } from '../../../types';
import { resolutionOccupantNeedsBadge } from './structureResolutionOccupants';

type StructureResolutionFreshnessBadgeProps = {
  occupant: StructureResolutionOccupant | null | undefined;
  density?: 'context' | 'compact';
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

  const badge = (
    <Status
      density={density}
      tone={badgeTone(occupant.state)}
      variant="soft"
      shape="pill"
      aria-label={ariaLabel}
    >
      {label}
    </Status>
  );

  if (causeLabel == null) {
    return badge;
  }

  return <Tooltip content={causeLabel}>{badge}</Tooltip>;
}

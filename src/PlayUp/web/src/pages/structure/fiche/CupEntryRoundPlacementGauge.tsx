import { useTranslation } from 'react-i18next';

import { RatioMessageGauge } from '../../../design-system/components/RatioMessageGauge';
import { resolveDrawCtaPoolTone } from '../draw/structureDrawCtaPool';
import {
  drawPoolToneToRatioGaugeTone,
  operationalStripPoolTone,
} from './structureOperationalCopy';
import type { StripOperationalMessageKind } from './structureOperationalStripUi';
import { resolveEntryRoundPlacementMeterCopy } from './structureOperationalStripMessages';

/** Cup canonical entry-round pairs — compact footer gauge (same pattern as composition Entrées). */
export function CupEntryRoundPlacementGauge({
  filled,
  capacity,
  blocksPhaseStart = false,
  presentationKind,
  className,
}: {
  filled: number;
  capacity: number;
  blocksPhaseStart?: boolean;
  presentationKind?: StripOperationalMessageKind;
  className?: string;
}) {
  const { t } = useTranslation('structure');
  const meter = resolveEntryRoundPlacementMeterCopy({
    t,
    gaugeRatio: { filled, capacity },
    blocksPhaseStart,
    presentationKind,
  });
  if (meter == null) {
    return null;
  }

  const complete = filled >= capacity;
  const poolTone = operationalStripPoolTone(
    meter.kind,
    complete ? 'complete' : resolveDrawCtaPoolTone(filled, capacity),
  );

  const rootClass = ['structure-phase-match-tile__entry-round-gauge', className]
    .filter(Boolean)
    .join(' ');

  return (
    <div className={rootClass}>
      <RatioMessageGauge
        size="compact"
        current={filled}
        capacity={capacity}
        ratioCaption={t('fiche.operational.production.entryRoundRatioCaption')}
        message={meter.text}
        messageKind={meter.kind}
        ratioTone={drawPoolToneToRatioGaugeTone(poolTone)}
        ariaLabel={t('fiche.operational.production.meterAriaEntryPairs')}
      />
    </div>
  );
}

import { useTranslation } from 'react-i18next';

import { RatioMessageGauge } from '../../design-system/components/RatioMessageGauge';
import {
  drawPoolToneToRatioGaugeTone,
  operationalStripPoolTone,
  resolvePlacementPoolTone,
} from './structureOperationalCopy';
import { resolveCompositionPopulationMeterCopy } from './structureOperationalStripMessages';

/** Phase entries vs Places N — composition band (under Qualifiés / Équipes). */
export function CompositionPopulationGauge({
  current,
  capacity,
  className,
  populationShortfallMessageKind,
}: {
  current: number;
  capacity: number;
  className?: string;
  /** When U1 shortfall is presented as danger/attention (D5), align gauge chrome. */
  populationShortfallMessageKind?: 'danger' | 'attention' | null;
}) {
  const { t } = useTranslation('structure');
  const meter = resolveCompositionPopulationMeterCopy({
    t,
    current,
    capacity,
  });
  const ratioTone = resolvePlacementPoolTone(current, capacity);
  const messageKind =
    populationShortfallMessageKind ?? meter.kind;
  const poolTone = operationalStripPoolTone(messageKind, ratioTone);

  const rootClass = [
    'structure-phase-composition__population-gauge',
    className,
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <div className={rootClass}>
      <RatioMessageGauge
        size="compact"
        current={current}
        capacity={capacity}
        ratioCaption={t('composition.gauge.entriesPlacesCaption')}
        message={meter.text}
        messageKind={messageKind}
        ratioTone={drawPoolToneToRatioGaugeTone(poolTone)}
        ariaLabel={t('composition.gauge.populationAria', {
          current,
          capacity,
        })}
      />
    </div>
  );
}

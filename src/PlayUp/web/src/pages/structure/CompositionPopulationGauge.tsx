import { useTranslation } from 'react-i18next';

import { RatioMessageGauge } from '../../design-system/components/RatioMessageGauge';
import type { StructureDiagnosticPresentationContext } from './structureDiagnosticPresentationPolicy';
import {
  drawPoolToneToRatioGaugeTone,
  operationalStripPoolTone,
  resolvePlacementPoolTone,
} from './structureOperationalCopy';
import { isDraftLikeStageStatus } from './structureOperationalMeterPresentation';
import { resolveCompositionPopulationMeterCopy } from './structureOperationalStripMessages';

/** Phase entries vs Places N — composition band (under Qualifiés / Équipes). */
export function CompositionPopulationGauge({
  current,
  capacity,
  className,
  presentationContext,
}: {
  current: number;
  capacity: number;
  className?: string;
  presentationContext?: StructureDiagnosticPresentationContext;
}) {
  const { t } = useTranslation('structure');
  const meter = resolveCompositionPopulationMeterCopy({
    t,
    current,
    capacity,
    presentationContext,
  });
  const ratioTone = resolvePlacementPoolTone(current, capacity);
  const ratioToneForPool =
    isDraftLikeStageStatus(presentationContext?.stageStatus) &&
    meter.kind !== 'danger'
      ? meter.kind === 'attention'
        ? 'attention'
        : 'neutral'
      : ratioTone;
  const poolTone = operationalStripPoolTone(meter.kind, ratioToneForPool);

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
        messageKind={meter.kind}
        ratioTone={drawPoolToneToRatioGaugeTone(poolTone)}
        ariaLabel={t('composition.gauge.populationAria', {
          current,
          capacity,
        })}
      />
    </div>
  );
}

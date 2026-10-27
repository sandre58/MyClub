import { useTranslation } from 'react-i18next';

import { RatioMessageGauge } from '../../../design-system/components/RatioMessageGauge';
import type { CompositionTileMode } from './compositionTilePresentation';
import type { RatioMessageGaugeRatioTone } from '../../../design-system/components/RatioMessageGauge';

export function CompositionInboundResolvedGauge({
  resolved,
  total,
  mode,
  className,
}: {
  resolved: number;
  total: number;
  mode: CompositionTileMode;
  className?: string;
}) {
  const { t } = useTranslation('structure');
  const unresolved = Math.max(0, total - resolved);
  const complete = unresolved === 0;

  const message = complete
    ? t('composition.gauge.inboundAllResolved')
    : t('composition.gauge.inboundUnresolved', { count: unresolved });

  const messageKind = complete
    ? 'success'
    : mode === 'historical'
      ? 'info'
      : 'info';
  const ratioTone: RatioMessageGaugeRatioTone = complete ? 'complete' : 'brand';

  const rootClass = ['structure-phase-composition__population-gauge', className]
    .filter(Boolean)
    .join(' ');

  return (
    <div className={rootClass}>
      <RatioMessageGauge
        size="compact"
        current={resolved}
        capacity={total}
        ratioCaption={t('composition.gauge.resolvedQualifiedCaption')}
        message={message}
        messageKind={messageKind}
        ratioTone={ratioTone}
        ariaLabel={t('composition.gauge.inboundResolvedAria', {
          resolved,
          total,
        })}
      />
    </div>
  );
}

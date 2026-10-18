import { useTranslation } from 'react-i18next';

import { CompositionGaugeRatioLine } from './CompositionGaugeRatioLine';

/** Qualif/Prog inbound seats resolved vs configured — never the Équipes eligibility counter. */
export function CompositionQualifiersResolvedMeta({
  resolved,
  configured,
  ready,
}: {
  resolved: number;
  configured: number;
  ready: boolean;
}) {
  const { t } = useTranslation('structure');

  if (!ready) {
    return <span className="structure-phase-composition__qual-resolved-meta">…</span>;
  }

  const resolutionLabel = t('composition.gauge.resolvedQualifiedCaption');
  const capacity = configured > 0 ? configured : Math.max(resolved, 1);

  return (
    <CompositionGaugeRatioLine
      current={resolved}
      capacity={capacity}
      label={resolutionLabel}
    />
  );
}

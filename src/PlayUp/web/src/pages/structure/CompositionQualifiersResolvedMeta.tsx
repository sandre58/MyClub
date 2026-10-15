import { useTranslation } from 'react-i18next';

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

  const label = t('composition.gauge.resolvedQualifiedCaption');

  if (configured <= 0) {
    return (
      <span className="structure-phase-composition__qual-resolved-meta">
        <span
          className="structure-phase-composition__qual-resolved-meta__filled structure-phase-composition__qual-resolved-meta__filled--neutral"
        >
          {resolved}
        </span>
        <span className="structure-phase-composition__qual-resolved-meta__label">
          {label}
        </span>
      </span>
    );
  }

  const filledTone =
    resolved >= configured
      ? 'complete'
      : resolved > configured
        ? 'over'
        : 'partial';

  return (
    <span className="structure-phase-composition__qual-resolved-meta">
      <span
        className={`structure-phase-composition__qual-resolved-meta__filled structure-phase-composition__qual-resolved-meta__filled--${filledTone}`}
      >
        {resolved}
      </span>
      <span className="structure-phase-composition__qual-resolved-meta__sep">/</span>
      <span className="structure-phase-composition__qual-resolved-meta__cap">
        {configured}
      </span>
      <span className="structure-phase-composition__qual-resolved-meta__label">
        {label}
      </span>
    </span>
  );
}

import { compositionGaugeFilledTone } from './compositionGaugeFilledTone';

export function CompositionGaugeRatioLine({
  current,
  capacity,
  label,
  className = 'structure-phase-composition__qual-resolved-meta',
}: {
  current: number;
  capacity: number;
  label: string;
  className?: string;
}) {
  const filledTone = compositionGaugeFilledTone(current, capacity);

  return (
    <span className={className}>
      <span
        className={`structure-phase-composition__qual-resolved-meta__filled structure-phase-composition__qual-resolved-meta__filled--${filledTone}`}
      >
        {current}
      </span>
      <span className="structure-phase-composition__qual-resolved-meta__sep">
        /
      </span>
      <span className="structure-phase-composition__qual-resolved-meta__cap">
        {capacity}
      </span>
      <span className="structure-phase-composition__qual-resolved-meta__label">
        {label}
      </span>
    </span>
  );
}

import type { ReactNode } from 'react';

export type OutcomeTone = 'win' | 'draw' | 'loss';

export type OutcomePointsCardProps = {
  tone: OutcomeTone;
  label: string;
  /** Points copy or nested control (e.g. InputNumber). */
  value: ReactNode;
  icon?: ReactNode;
};

/**
 * Semantic points card — Win / Draw / Loss points scale display or edit slot.
 */
export function OutcomePointsCard({
  tone,
  label,
  value,
  icon,
}: OutcomePointsCardProps) {
  return (
    <div className="ds-outcome-points__card" data-tone={tone}>
      <div className="ds-outcome-points__meta">
        {icon ? (
          <span className="ds-outcome-points__icon" aria-hidden="true">
            {icon}
          </span>
        ) : null}
        <span className="ds-outcome-points__label">{label}</span>
      </div>
      <div className="ds-outcome-points__value">{value}</div>
    </div>
  );
}

export type OutcomePointsProps = {
  children: ReactNode;
  'aria-label'?: string;
};

/** Horizontal row of OutcomePointsCard. */
export function OutcomePoints({
  children,
  'aria-label': ariaLabel,
}: OutcomePointsProps) {
  return (
    <div className="ds-outcome-points" role="group" aria-label={ariaLabel}>
      {children}
    </div>
  );
}

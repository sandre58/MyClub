import type { ReactNode } from 'react';

export type MeterTone =
  'brand' | 'success' | 'attention' | 'error' | 'info' | 'neutral';

export type MeterProps = {
  /** Fill ratio in [0, 1]. */
  ratio: number;
  tone?: MeterTone;
  size?: 'md' | 'lg';
  /** Clip overflow (hide marker spill). Default false so markers can sit outside. */
  clip?: boolean;
  marker?: {
    ratio: number;
    label?: ReactNode;
  };
  className?: string;
  'aria-valuemin'?: number;
  'aria-valuemax'?: number;
  'aria-valuenow'?: number;
  'aria-valuetext'?: string;
  'aria-label'?: string;
  'aria-hidden'?: boolean | 'true' | 'false';
};

function clamp01(n: number): number {
  if (Number.isNaN(n)) return 0;
  return Math.min(1, Math.max(0, n));
}

/**
 * Capsule progress meter — shared by Teams plateau and Règlement point gauges.
 */
export function Meter({
  ratio,
  tone = 'brand',
  size = 'md',
  clip = false,
  marker,
  className,
  ...aria
}: MeterProps) {
  const fill = clamp01(ratio);
  const classes = [
    'ds-meter',
    size === 'lg' ? 'ds-meter--lg' : null,
    clip ? 'ds-meter--clip' : null,
    className,
  ]
    .filter(Boolean)
    .join(' ');

  const progressRole =
    aria['aria-valuenow'] != null || aria['aria-label'] != null
      ? 'progressbar'
      : undefined;

  return (
    <div className={classes} data-tone={tone} role={progressRole} {...aria}>
      <span
        className="ds-meter__fill"
        style={{ width: `${fill * 100}%` }}
        aria-hidden="true"
      />
      {marker ? (
        <span
          className="ds-meter__marker"
          style={{ left: `${clamp01(marker.ratio) * 100}%` }}
          aria-hidden="true"
        >
          {marker.label != null ? (
            <span className="ds-meter__marker-label">{marker.label}</span>
          ) : null}
        </span>
      ) : null}
    </div>
  );
}

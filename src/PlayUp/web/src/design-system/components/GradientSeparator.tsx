import type { HTMLAttributes } from 'react';

export type GradientSeparatorOrientation =
  | 'vertical'
  | 'horizontal'
  | 'responsive';

/**
 * Hairline gradient divider — vertical between columns, horizontal when stacked.
 * `responsive` follows the phase-composition container (Structure band-3).
 */
export function GradientSeparator({
  orientation = 'vertical',
  className,
  ...rest
}: HTMLAttributes<HTMLDivElement> & {
  orientation?: GradientSeparatorOrientation;
}) {
  const orientationClass =
    orientation === 'horizontal'
      ? 'ds-gradient-separator--horizontal'
      : orientation === 'responsive'
        ? 'ds-gradient-separator--responsive'
        : 'ds-gradient-separator--vertical';

  return (
    <div
      className={['ds-gradient-separator', orientationClass, className]
        .filter(Boolean)
        .join(' ')}
      aria-hidden="true"
      {...rest}
    />
  );
}

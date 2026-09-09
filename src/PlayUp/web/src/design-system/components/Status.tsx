import type { ReactNode } from 'react';

export type StatusDensity = 'context' | 'compact' | 'dense';
export type StatusVariant = 'soft' | 'outline' | 'plain';
export type StatusShape = 'rounded' | 'pill';
export type StatusTone =
  'neutral' | 'info' | 'success' | 'live' | 'done' | 'attention' | 'error';

/**
 * Status primitive — presentation only. Read supplies labels and meaning.
 */
export function Status({
  density,
  tone = 'info',
  variant = 'soft',
  shape = 'rounded',
  children,
  className,
  title,
}: {
  density: StatusDensity;
  tone?: StatusTone;
  variant?: StatusVariant;
  shape?: StatusShape;
  children: ReactNode;
  className?: string;
  title?: string;
}) {
  const isChip = density !== 'dense';
  const classes = [
    'ds-status',
    `ds-status--${density}`,
    isChip && `ds-status--${variant}`,
    isChip && `ds-status--tone-${tone}`,
    isChip && `ds-status--${shape}`,
    className,
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <span className={classes} title={title}>
      {children}
    </span>
  );
}

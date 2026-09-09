import type { HTMLAttributes, ReactNode } from 'react';

export type StatusDensity = 'context' | 'compact' | 'dense';
export type StatusVariant = 'soft' | 'outline' | 'plain';
export type StatusShape = 'rounded' | 'pill';
export type StatusTone =
  | 'neutral'
  | 'info'
  | 'success'
  | 'live'
  | 'done'
  | 'attention'
  | 'error';

type StatusProps = {
  density: StatusDensity;
  tone?: StatusTone;
  variant?: StatusVariant;
  shape?: StatusShape;
  children: ReactNode;
  className?: string;
  /** @deprecated Prefer wrapping with Tooltip for Hint content. */
  title?: string;
} & Omit<HTMLAttributes<HTMLSpanElement>, 'children' | 'title' | 'className'>;

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
  ...rest
}: StatusProps) {
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
    <span className={classes} title={title} {...rest}>
      {children}
    </span>
  );
}

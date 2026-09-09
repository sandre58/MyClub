import type { HTMLAttributes, ReactNode } from 'react';

export type ChipTone = 'neutral' | 'soft' | 'accent' | 'win' | 'draw' | 'loss';

type ChipProps = {
  tone?: ChipTone;
  children: ReactNode;
  className?: string;
} & Omit<HTMLAttributes<HTMLSpanElement>, 'children' | 'title' | 'className'>;

/**
 * Lightweight fact chip — rule tokens, meta labels. Not a lifecycle Status.
 * Hint copy: wrap with Tooltip (do not pass native `title`).
 */
export function Chip({
  tone = 'neutral',
  children,
  className,
  ...rest
}: ChipProps) {
  const classes = ['ds-chip', `ds-chip--${tone}`, className]
    .filter(Boolean)
    .join(' ');
  return (
    <span className={classes} {...rest}>
      {children}
    </span>
  );
}

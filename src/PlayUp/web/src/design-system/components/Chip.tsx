import type { HTMLAttributes, ReactNode } from 'react';

export type ChipTone = 'neutral' | 'soft' | 'accent' | 'win' | 'draw' | 'loss';

type ChipProps = {
  tone?: ChipTone;
  /** @deprecated Prefer wrapping with Tooltip for Hint content. */
  title?: string;
  children: ReactNode;
  className?: string;
} & Omit<HTMLAttributes<HTMLSpanElement>, 'children' | 'title' | 'className'>;

/**
 * Lightweight fact chip — rule tokens, meta labels. Not a lifecycle Status.
 * Hint copy: wrap with Tooltip (do not rely on native `title` for V1 tips).
 */
export function Chip({
  tone = 'neutral',
  title,
  children,
  className,
  ...rest
}: ChipProps) {
  const classes = ['ds-chip', `ds-chip--${tone}`, className]
    .filter(Boolean)
    .join(' ');
  return (
    <span className={classes} title={title} {...rest}>
      {children}
    </span>
  );
}

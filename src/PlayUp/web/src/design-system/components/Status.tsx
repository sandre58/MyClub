import type { HTMLAttributes, ReactNode } from 'react';
import { PlacedIcon } from '../icons/contentIcons';
import { Tooltip } from './Tooltip';

export type StatusDensity = 'context' | 'compact' | 'dense';
export type StatusVariant = 'soft' | 'outline' | 'plain';
export type StatusShape = 'rounded' | 'pill';
export type StatusTone =
  'neutral' | 'info' | 'success' | 'live' | 'done' | 'attention' | 'error';

type StatusProps = {
  density: StatusDensity;
  tone?: StatusTone;
  variant?: StatusVariant;
  shape?: StatusShape;
  /** Wrapped label (e.g. PageHead). Icon row uses `.ds-status-inline`. */
  multiline?: boolean;
  /** Context density only — use `--text-body` instead of label size. */
  textBody?: boolean;
  children: ReactNode;
  className?: string;
} & Omit<HTMLAttributes<HTMLSpanElement>, 'children' | 'title' | 'className'>;

/**
 * Status primitive — presentation only. Read supplies labels and meaning.
 * Hint copy: wrap with Tooltip (do not pass native `title`).
 */
export function Status({
  density,
  tone = 'info',
  variant = 'soft',
  shape = 'rounded',
  multiline = false,
  textBody = false,
  children,
  className,
  ...rest
}: StatusProps) {
  const isChip = density !== 'dense';
  const classes = [
    'ds-status',
    `ds-status--${density}`,
    isChip && `ds-status--${variant}`,
    isChip && `ds-status--tone-${tone}`,
    isChip && `ds-status--${shape}`,
    multiline && 'ds-status--multiline',
    textBody && density === 'context' && 'ds-status--text-body',
    className,
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <span className={classes} {...rest}>
      {children}
    </span>
  );
}

/**
 * « Déjà placée » — compact neutral outline pin. Label lives in the tooltip only.
 * Product rows style the team name separately (see draw confrontation grid).
 * Ink/contrast budget: Foundations specimen + AlreadyPlacedStatus.test.ts.
 */
export function AlreadyPlacedStatus({ label }: { label: ReactNode }) {
  return (
    <Tooltip content={label}>
      <Status
        density="compact"
        tone="neutral"
        variant="outline"
        aria-label={typeof label === 'string' ? label : undefined}
      >
        <span className="ds-status-inline">
          <span className="ds-status-inline__icon" aria-hidden="true">
            <PlacedIcon size="sm" />
          </span>
        </span>
      </Status>
    </Tooltip>
  );
}

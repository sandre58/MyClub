import type { ReactNode } from 'react';
import { DrawPendingIcon } from '../design-system/icons/contentIcons';
import { ChevronRightIcon } from '../design-system/icons/shellIcons';

/** Ghost = Activate (subdued). Emphasis = Launch/Open (outlined). Never brand primary. */
export type StructureDrawCtaTone = 'ghost' | 'emphasis';

/** Ratio color = pool resolution only — never Draw readiness. */
export type DrawCtaPoolTone = 'neutral' | 'partial' | 'complete';

export function resolveDrawCtaPoolTone(
  filled: number,
  capacity: number,
): DrawCtaPoolTone {
  if (capacity <= 0 || filled <= 0) return 'neutral';
  if (filled < capacity) return 'partial';
  return 'complete';
}

/**
 * Draw stage-card CTA — gesture + minimal context.
 * Chevron aligned across the whole card; title aligned to the leading.
 */
export function StructureDrawCta({
  title,
  body,
  tone,
  onClick,
  leading,
  disabled = false,
}: {
  title: string;
  body: ReactNode;
  tone: StructureDrawCtaTone;
  onClick: () => void;
  leading?: ReactNode;
  disabled?: boolean;
}) {
  return (
    <button
      type="button"
      className="structure-draw-cta"
      data-tone={tone}
      disabled={disabled}
      aria-disabled={disabled || undefined}
      onClick={onClick}
    >
      <span className="structure-draw-cta__main">
        <span className="structure-draw-cta__head">
          <span className="structure-draw-cta__leading" aria-hidden="true">
            {leading ?? <DrawPendingIcon size="sm" />}
          </span>
          <span className="structure-draw-cta__title">{title}</span>
        </span>
        {body != null && body !== '' ? (
          <span className="structure-draw-cta__body">{body}</span>
        ) : null}
      </span>
      <span className="structure-draw-cta__chevron" aria-hidden="true">
        <ChevronRightIcon size="md" />
      </span>
    </button>
  );
}

/**
 * Launch/Open — large coloured ratio (neutral / attention / success).
 * Caption always secondary. Colour ≠ Draw readiness. No mode on the CTA.
 */
export function DrawCtaActionBody({
  filled,
  capacity,
  teamsCaption,
  poolTone = 'neutral',
}: {
  filled: number;
  capacity: number;
  /** e.g. "teams resolved" */
  teamsCaption: string;
  poolTone?: DrawCtaPoolTone;
}) {
  return (
    <span className="structure-draw-cta__context">
      <span
        className="structure-draw-cta__readiness"
        data-pool-tone={poolTone}
      >
        <span className="structure-draw-cta__ratio">
          <span className="structure-draw-cta__ratio-filled">{filled}</span>
          <span className="structure-draw-cta__ratio-sep">/</span>
          <span className="structure-draw-cta__ratio-cap">{capacity}</span>
        </span>
        <span className="structure-draw-cta__caption">{teamsCaption}</span>
      </span>
    </span>
  );
}

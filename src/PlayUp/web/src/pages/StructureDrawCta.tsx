import type { ReactNode } from 'react';
import {
  DrawPendingIcon,
  RandomIcon,
} from '../design-system/icons/contentIcons';
import { ChevronRightIcon } from '../design-system/icons/shellIcons';

/** Ghost = Activer (subdued). Emphasis = Lancer/Ouvrir (outlined). Never brand primary. */
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
 * Fiche Tirage CTA — geste + contexte minimal.
 * Chevron aligné sur toute la carte ; titre aligné au leading.
 */
export function StructureDrawCta({
  title,
  body,
  tone,
  onClick,
  leading,
}: {
  title: string;
  body: ReactNode;
  tone: StructureDrawCtaTone;
  onClick: () => void;
  leading?: ReactNode;
}) {
  return (
    <button
      type="button"
      className="structure-draw-cta"
      data-tone={tone}
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
 * Lancer/Ouvrir — gros ratio coloré (neutre / attention / success) + mode.
 * Légende toujours secondary. Couleur ≠ readiness Draw.
 */
export function DrawCtaActionBody({
  filled,
  capacity,
  teamsCaption,
  poolTone = 'neutral',
  modeLabel,
}: {
  filled: number;
  capacity: number;
  /** e.g. « équipes résolues » */
  teamsCaption: string;
  poolTone?: DrawCtaPoolTone;
  modeLabel: string;
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
      <span className="structure-draw-cta__mode-row">
        <span className="structure-draw-cta__mode-icon" aria-hidden="true">
          <RandomIcon size="sm" />
        </span>
        <span className="structure-draw-cta__mode-label">{modeLabel}</span>
      </span>
    </span>
  );
}

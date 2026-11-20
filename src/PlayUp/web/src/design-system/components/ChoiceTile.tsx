import type { ReactNode } from 'react';
import { CheckIcon } from '../icons/contentIcons';

export type ChoiceTileProps = {
  label: string;
  /** Secondary line under the label (e.g. short scope hint or compact meter). */
  description?: ReactNode;
  selected: boolean;
  onChange: (selected: boolean) => void;
  /** Leading visual — swatch, icon, crest fragment (aligned with the title). */
  leading?: ReactNode;
  /**
   * `start` = leading beside the title (default).
   * `above` = leading centered above the title (e.g. team crest pickers).
   */
  leadingPlacement?: 'start' | 'above';
  disabled?: boolean;
  /** `radio` for single-select groups; default `checkbox`. */
  role?: 'checkbox' | 'radio';
  /** Locked at capacity but still keyboard-reachable (`aria-disabled`). */
  unavailable?: boolean;
  /** Accessible name; defaults to label. */
  'aria-label'?: string;
  /** Why the option is unavailable, or other supplementary text. */
  'aria-describedby'?: string;
  /**
   * `stacked` (default) aligns the check to the first line — title plus a
   * description or other multi-line content. `inline` centers the check for
   * a single line (crest + name, swatch + label).
   */
  layout?: 'stacked' | 'inline';
  /** Keeps the check column width when unselected (avoids layout shift). */
  reserveSelectionMark?: boolean;
};

/**
 * Selectable option tile — checkbox semantics with a visual leading mark.
 * Multi-select is owned by the parent (map of selected values).
 */
export function ChoiceTile({
  label,
  description,
  selected,
  onChange,
  leading,
  leadingPlacement = 'start',
  disabled = false,
  role = 'checkbox',
  unavailable = false,
  'aria-label': ariaLabel,
  'aria-describedby': ariaDescribedBy,
  layout = 'stacked',
  reserveSelectionMark = false,
}: ChoiceTileProps) {
  const inactive = disabled || unavailable;
  const inlineLeading = layout === 'inline' && leading != null;
  const classes = [
    'ds-choice-tile',
    layout === 'inline' ? 'ds-choice-tile--inline' : null,
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <button
      type="button"
      role={role}
      aria-checked={selected}
      aria-label={ariaLabel ?? label}
      aria-describedby={ariaDescribedBy}
      disabled={disabled}
      aria-disabled={unavailable ? true : undefined}
      className={classes}
      data-selected={selected ? 'true' : 'false'}
      data-disabled={inactive ? 'true' : 'false'}
      data-leading={leading ? leadingPlacement : undefined}
      onClick={(event) => {
        if (!inactive) {
          onChange(role === 'radio' ? true : !selected);
          event.currentTarget.focus({ preventScroll: true });
        }
      }}
    >
      <span className="ds-choice-tile__label">
        {inlineLeading ? (
          <span className="ds-choice-tile__title-row">
            <span className="ds-choice-tile__leading" aria-hidden="true">
              {leading}
            </span>
            <span className="ds-choice-tile__text">
              <span className="ds-choice-tile__title">{label}</span>
              {description ? (
                <span className="ds-choice-tile__description">
                  {description}
                </span>
              ) : null}
            </span>
          </span>
        ) : (
          <>
            <span className="ds-choice-tile__title-row">
              {leading ? (
                <span className="ds-choice-tile__leading" aria-hidden="true">
                  {leading}
                </span>
              ) : null}
              <span className="ds-choice-tile__title">{label}</span>
            </span>
            {description ? (
              <span className="ds-choice-tile__description">{description}</span>
            ) : null}
          </>
        )}
      </span>
      {selected || reserveSelectionMark ? (
        <span
          className="ds-choice-tile__check"
          data-reserved={selected ? undefined : 'true'}
          aria-hidden="true"
        >
          {selected ? <CheckIcon size="sm" /> : null}
        </span>
      ) : null}
    </button>
  );
}

export type ChoiceSwatchProps = {
  color: string;
};

/** Color swatch for ChoiceTile leading (discipline tokens, kits, …). */
export function ChoiceSwatch({ color }: ChoiceSwatchProps) {
  return (
    <span
      className="ds-choice-swatch"
      style={{ backgroundColor: color }}
      aria-hidden="true"
    />
  );
}

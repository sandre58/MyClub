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
  'aria-label': ariaLabel,
  'aria-describedby': ariaDescribedBy,
  layout = 'stacked',
}: ChoiceTileProps) {
  const classes = [
    'ds-choice-tile',
    layout === 'inline' ? 'ds-choice-tile--inline' : null,
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <button
      type="button"
      role="checkbox"
      aria-checked={selected}
      aria-label={ariaLabel ?? label}
      aria-describedby={ariaDescribedBy}
      disabled={disabled}
      className={classes}
      data-selected={selected ? 'true' : 'false'}
      data-disabled={disabled ? 'true' : 'false'}
      data-leading={leading ? leadingPlacement : undefined}
      onClick={() => {
        if (!disabled) {
          onChange(!selected);
        }
      }}
    >
      <span className="ds-choice-tile__label">
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
      </span>
      {selected ? (
        <span className="ds-choice-tile__check" aria-hidden="true">
          <CheckIcon size="sm" />
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

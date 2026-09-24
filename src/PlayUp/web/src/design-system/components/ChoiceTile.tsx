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
}: ChoiceTileProps) {
  return (
    <button
      type="button"
      role="checkbox"
      aria-checked={selected}
      aria-label={ariaLabel ?? label}
      disabled={disabled}
      className="ds-choice-tile"
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
  /** @deprecated Unused — tile label lives on ChoiceTile. */
  label?: string;
};

/** Color swatch for ChoiceTile leading (cards, kits, …). */
export function ChoiceSwatch({ color }: ChoiceSwatchProps) {
  return (
    <span
      className="ds-choice-swatch"
      style={{ backgroundColor: color }}
    />
  );
}

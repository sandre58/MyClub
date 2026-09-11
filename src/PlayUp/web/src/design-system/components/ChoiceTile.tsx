import type { ReactNode } from 'react';
import { CheckIcon } from '../icons/contentIcons';

export type ChoiceTileProps = {
  label: string;
  selected: boolean;
  onChange: (selected: boolean) => void;
  /** Leading visual — swatch, icon, crest fragment. */
  leading?: ReactNode;
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
  selected,
  onChange,
  leading,
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
      onClick={() => {
        if (!disabled) {
          onChange(!selected);
        }
      }}
    >
      {leading ? (
        <span className="ds-choice-tile__leading" aria-hidden="true">
          {leading}
        </span>
      ) : null}
      <span className="ds-choice-tile__label">{label}</span>
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

import type { ReactNode } from 'react';

export type ToggleButtonOption<T extends string = string> = {
  value: T;
  label: string;
  /** Optional icon or mark before the label. */
  leading?: ReactNode;
  disabled?: boolean;
  /** Accessible name; defaults to label. */
  'aria-label'?: string;
};

export type ToggleButtonGroupProps<T extends string = string> = {
  value: T;
  onChange: (value: T) => void;
  options: readonly ToggleButtonOption<T>[];
  /** Accessible name for the radiogroup. */
  'aria-label'?: string;
  disabled?: boolean;
  className?: string;
};

/**
 * Exclusive segmented control — role="radiogroup".
 * Presentation only; caller owns the selected value.
 */
export function ToggleButtonGroup<T extends string>({
  value,
  onChange,
  options,
  'aria-label': ariaLabel,
  disabled = false,
  className,
}: ToggleButtonGroupProps<T>) {
  return (
    <div
      className={['ds-toggle-button-group', className]
        .filter(Boolean)
        .join(' ')}
      role="radiogroup"
      aria-label={ariaLabel}
      data-disabled={disabled ? 'true' : 'false'}
    >
      {options.map((option) => {
        const selected = value === option.value;
        const optionDisabled = disabled || Boolean(option.disabled);
        return (
          <button
            key={option.value}
            type="button"
            role="radio"
            aria-checked={selected}
            aria-label={option['aria-label'] ?? option.label}
            disabled={optionDisabled}
            className="ds-toggle-button-group__option"
            data-selected={selected ? 'true' : 'false'}
            data-disabled={optionDisabled ? 'true' : 'false'}
            onClick={() => {
              if (!optionDisabled) {
                onChange(option.value);
              }
            }}
          >
            {option.leading ? (
              <span
                className="ds-toggle-button-group__leading"
                aria-hidden="true"
              >
                {option.leading}
              </span>
            ) : null}
            <span className="ds-toggle-button-group__label">
              {option.label}
            </span>
          </button>
        );
      })}
    </div>
  );
}

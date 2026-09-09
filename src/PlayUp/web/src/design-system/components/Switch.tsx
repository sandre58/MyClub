import { useId, type ButtonHTMLAttributes } from 'react';

export type SwitchProps = Omit<
  ButtonHTMLAttributes<HTMLButtonElement>,
  'role' | 'children' | 'onChange' | 'type'
> & {
  checked: boolean;
  onChange: (checked: boolean) => void;
  /** Accessible name when no visible label is associated. */
  label?: string;
};

/**
 * Binary switch — role="switch". Presentation only; caller owns state.
 */
export function Switch({
  checked,
  onChange,
  label,
  disabled,
  id,
  className,
  onClick,
  ...props
}: SwitchProps) {
  const autoId = useId();
  const switchId = id ?? autoId;

  return (
    <button
      {...props}
      id={switchId}
      type="button"
      role="switch"
      aria-checked={checked}
      aria-label={label}
      disabled={disabled}
      className={['ds-switch', className].filter(Boolean).join(' ')}
      data-checked={checked ? 'true' : 'false'}
      data-disabled={disabled ? 'true' : 'false'}
      onClick={(event) => {
        onClick?.(event);
        if (!event.defaultPrevented && !disabled) {
          onChange(!checked);
        }
      }}
    >
      <span className="ds-switch__track" aria-hidden="true">
        <span className="ds-switch__thumb" />
      </span>
    </button>
  );
}

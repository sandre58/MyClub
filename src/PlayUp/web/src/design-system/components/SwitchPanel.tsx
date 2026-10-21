import type { ReactNode } from 'react';
import { Switch } from './Switch';

export type SwitchPanelProps = {
  title: string;
  description?: string;
  checked: boolean;
  onChange: (checked: boolean) => void;
  disabled?: boolean;
  switchLabel?: string;
  children?: ReactNode;
};

/**
 * Sub-section gated by a Switch — body is inert/dimmed when off.
 * Use for optional rule blocks (extra time, penalties, …).
 */
export function SwitchPanel({
  title,
  description,
  checked,
  onChange,
  disabled = false,
  switchLabel,
  children,
}: SwitchPanelProps) {
  const hasBody = children != null;

  return (
    <section
      className="ds-switch-panel"
      data-enabled={checked ? 'true' : 'false'}
      data-disabled={disabled ? 'true' : 'false'}
    >
      <header className="ds-switch-panel__head">
        <div className="ds-switch-panel__copy">
          <h4 className="ds-switch-panel__title">{title}</h4>
          {description ? (
            <p className="ds-switch-panel__description">{description}</p>
          ) : null}
        </div>
        <Switch
          checked={checked}
          onChange={onChange}
          disabled={disabled}
          label={switchLabel ?? title}
        />
      </header>
      {hasBody ? (
        <div
          className="ds-switch-panel__body"
          {...(checked ? {} : { inert: true })}
          aria-hidden={checked ? undefined : true}
        >
          {children}
        </div>
      ) : null}
    </section>
  );
}

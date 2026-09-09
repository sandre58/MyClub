import type { ReactNode } from 'react';
import { ToastToneIcon } from '../icons/toastIcons';

export type AlertTone = 'danger' | 'warning' | 'info' | 'success';

export type AlertProps = {
  tone?: AlertTone;
  children: ReactNode;
  role?: 'alert' | 'status';
};

/**
 * Inline alert — soft fill + tone icon (Ant Alert–inspired).
 * Prefer over bare `.ds-notice` when an icon helps scan.
 */
export function Alert({ tone = 'info', children, role = 'alert' }: AlertProps) {
  const toastTone =
    tone === 'danger' ? 'error' : tone === 'warning' ? 'attention' : tone;

  return (
    <div className={`ds-alert ds-alert--${tone}`} role={role}>
      <span className="ds-alert__icon" aria-hidden="true">
        <ToastToneIcon tone={toastTone} size="md" />
      </span>
      <div className="ds-alert__body">{children}</div>
    </div>
  );
}

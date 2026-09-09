import type { ReactNode } from 'react';

export type FieldMessageTone = 'hint' | 'warning' | 'error';

export type FieldWidth = 'full' | 'sm';

export type FieldProps = {
  label: string;
  htmlFor?: string;
  required?: boolean;
  counter?: string;
  message?: string;
  className?: string;
  messageTone?: FieldMessageTone;
  /** Constrain control width (e.g. short name). Default full. */
  width?: FieldWidth;
  children: ReactNode;
};

/**
 * Form field chrome — label row (optional counter) + control + message.
 * Does not own input state.
 */
export function Field({
  label,
  htmlFor,
  required = false,
  counter,
  message,
  className = '',
  messageTone = 'hint',
  width = 'full',
  children,
}: FieldProps) {
  const hasMessage = Boolean(message);
  const invalid = messageTone === 'error' && hasMessage;
  const classes = ['ds-field', className].filter(Boolean).join(' ');

  return (
    <div
      className={classes}
      data-invalid={invalid ? 'true' : 'false'}
      data-tone={hasMessage ? messageTone : undefined}
      data-width={width === 'full' ? undefined : width}
    >
      <div className="ds-field__label-row">
        <label className="ds-field__label" htmlFor={htmlFor}>
          {label}
          {required ? (
            <span className="ds-field__required" aria-hidden="true">
              {' '}
              *
            </span>
          ) : null}
        </label>
        {counter ? (
          <span className="ds-field__counter" aria-hidden="true">
            {counter}
          </span>
        ) : null}
      </div>
      {children}
      {message ? (
        <p
          className={`ds-field__message ds-field__message--${messageTone}`}
          role={messageTone === 'error' ? 'alert' : 'status'}
        >
          {message}
        </p>
      ) : null}
    </div>
  );
}

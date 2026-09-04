import type { ReactNode } from 'react'

export type FieldMessageTone = 'hint' | 'warning' | 'error'

export type FieldProps = {
  label: string
  htmlFor?: string
  required?: boolean
  counter?: string
  message?: string
  messageTone?: FieldMessageTone
  children: ReactNode
}

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
  messageTone = 'hint',
  children,
}: FieldProps) {
  const invalid = messageTone === 'error' && Boolean(message)

  return (
    <div className="ds-field" data-invalid={invalid ? 'true' : 'false'}>
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
  )
}

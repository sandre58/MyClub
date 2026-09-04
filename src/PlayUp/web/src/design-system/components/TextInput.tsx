import {
  useEffect,
  useId,
  useRef,
  useState,
  type ChangeEvent,
  type InputHTMLAttributes,
  type ReactNode,
} from 'react'
import { CloseIcon } from '../icons/shellIcons'
import { CheckIcon, CopyIcon } from '../icons/overviewIcons'

export type TextInputProps = Omit<
  InputHTMLAttributes<HTMLInputElement>,
  'className' | 'size'
> & {
  leadingIcon?: ReactNode
  invalid?: boolean
  allowClear?: boolean
  clearLabel?: string
  allowCopy?: boolean
  copyLabel?: string
  copiedLabel?: string
}

/**
 * Text input shell — optional leading icon, clear, copy (+ copied feedback).
 */
export function TextInput({
  leadingIcon,
  invalid = false,
  allowClear = false,
  allowCopy = false,
  clearLabel = 'Vider',
  copyLabel = 'Copier dans le presse-papiers',
  copiedLabel = 'Copié',
  disabled,
  id,
  value,
  defaultValue,
  onChange,
  ...props
}: TextInputProps) {
  const autoId = useId()
  const inputId = id ?? autoId
  const controlled = value !== undefined
  const [uncontrolled, setUncontrolled] = useState(
    String(defaultValue ?? ''),
  )
  const [copied, setCopied] = useState(false)
  const copiedTimer = useRef<number | null>(null)
  const current = controlled ? String(value ?? '') : uncontrolled
  const showClear = allowClear && current.length > 0 && !disabled

  useEffect(() => {
    return () => {
      if (copiedTimer.current != null) {
        window.clearTimeout(copiedTimer.current)
      }
    }
  }, [])

  function handleChange(event: ChangeEvent<HTMLInputElement>) {
    if (!controlled) {
      setUncontrolled(event.target.value)
    }
    onChange?.(event)
  }

  function emitValue(next: string) {
    if (!controlled) {
      setUncontrolled(next)
    }
    onChange?.({
      target: { value: next },
      currentTarget: { value: next },
    } as ChangeEvent<HTMLInputElement>)
  }

  async function copyValue() {
    if (
      current.length === 0 ||
      typeof navigator.clipboard?.writeText !== 'function'
    ) {
      return
    }
    try {
      await navigator.clipboard.writeText(current)
      setCopied(true)
      if (copiedTimer.current != null) {
        window.clearTimeout(copiedTimer.current)
      }
      copiedTimer.current = window.setTimeout(() => {
        setCopied(false)
        copiedTimer.current = null
      }, 1600)
    } catch {
      /* clipboard may be denied */
    }
  }

  return (
    <div
      className="ds-input"
      data-disabled={disabled ? 'true' : 'false'}
      data-invalid={invalid ? 'true' : 'false'}
    >
      {leadingIcon ? (
        <span className="ds-input__leading" aria-hidden="true">
          {leadingIcon}
        </span>
      ) : null}
      <input
        {...props}
        id={inputId}
        disabled={disabled}
        className="ds-input__control"
        aria-invalid={invalid || undefined}
        value={current}
        onChange={handleChange}
      />
      {allowCopy ? (
        <button
          type="button"
          className="ds-input__affix"
          data-copied={copied ? 'true' : 'false'}
          aria-label={copied ? copiedLabel : copyLabel}
          title={copied ? copiedLabel : copyLabel}
          disabled={disabled || current.length === 0}
          tabIndex={-1}
          onClick={() => {
            void copyValue()
          }}
        >
          {copied ? (
            <CheckIcon size="sm" aria-hidden="true" />
          ) : (
            <CopyIcon size="sm" aria-hidden="true" />
          )}
          {copied ? (
            <span className="ds-input__affix-tip" role="status">
              {copiedLabel}
            </span>
          ) : null}
        </button>
      ) : null}
      {showClear ? (
        <button
          type="button"
          className="ds-input__affix"
          aria-label={clearLabel}
          title={clearLabel}
          tabIndex={-1}
          onClick={() => emitValue('')}
        >
          <CloseIcon size="sm" aria-hidden="true" />
        </button>
      ) : null}
    </div>
  )
}

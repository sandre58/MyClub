import {
  useEffect,
  useId,
  useState,
  type ChangeEvent,
  type FocusEvent,
  type InputHTMLAttributes,
  type KeyboardEvent,
  type ReactNode,
} from 'react'
import { CloseIcon, ChevronDownIcon, ChevronUpIcon } from '../icons/shellIcons'
import { MinusIcon, PlusIcon } from '../icons/overviewIcons'

export type InputNumberControlsLayout = 'end' | 'split'

export type InputNumberProps = Omit<
  InputHTMLAttributes<HTMLInputElement>,
  'className' | 'size' | 'type' | 'value' | 'defaultValue' | 'onChange'
> & {
  value?: number | null
  defaultValue?: number | null
  onChange?: (value: number | null) => void
  min?: number
  max?: number
  step?: number
  precision?: number
  controls?: boolean
  /**
   * `end` — Ant-like vertical chevrons (default).
   * `split` — horizontal − value + (édition dense / mockup règlement).
   */
  controlsLayout?: InputNumberControlsLayout
  leadingIcon?: ReactNode
  /** Static trailing unit (e.g. « min ») — not an action affix. */
  suffix?: ReactNode
  invalid?: boolean
  allowClear?: boolean
  clearLabel?: string
}

function clamp(n: number, min?: number, max?: number): number {
  let next = n
  if (min != null && next < min) {
    next = min
  }
  if (max != null && next > max) {
    next = max
  }
  return next
}

function formatNumber(n: number, precision?: number): string {
  if (precision == null) {
    return String(n)
  }
  return n.toFixed(precision)
}

function parseInput(raw: string): number | null {
  const trimmed = raw.trim()
  if (
    trimmed.length === 0 ||
    trimmed === '-' ||
    trimmed === '.' ||
    trimmed === '-.'
  ) {
    return null
  }
  const n = Number(trimmed)
  return Number.isFinite(n) ? n : null
}

/**
 * Numeric input — TextInput shell + Ant-like steppers.
 * Optional leading icon and clear affix.
 */
export function InputNumber({
  value,
  defaultValue = null,
  onChange,
  min,
  max,
  step = 1,
  precision,
  controls = true,
  controlsLayout = 'end',
  leadingIcon,
  suffix,
  invalid = false,
  allowClear = false,
  clearLabel = 'Vider',
  disabled,
  id,
  onBlur,
  onFocus,
  onKeyDown,
  ...props
}: InputNumberProps) {
  const autoId = useId()
  const inputId = id ?? autoId
  const controlled = value !== undefined
  const [uncontrolled, setUncontrolled] = useState<number | null>(
    defaultValue ?? null,
  )
  const current = controlled ? (value ?? null) : uncontrolled
  const [draft, setDraft] = useState(() =>
    current == null ? '' : formatNumber(current, precision),
  )
  const [focused, setFocused] = useState(false)

  useEffect(() => {
    if (!focused) {
      setDraft(current == null ? '' : formatNumber(current, precision))
    }
  }, [current, precision, focused])

  const showClear = allowClear && current != null && !disabled
  const split = controls && controlsLayout === 'split'
  const decDisabled =
    disabled || (min != null && current != null && current <= min)
  const incDisabled =
    disabled || (max != null && current != null && current >= max)

  function emit(next: number | null) {
    let committed = next
    if (committed != null) {
      committed = clamp(committed, min, max)
      if (precision != null) {
        const factor = 10 ** precision
        committed = Math.round(committed * factor) / factor
      }
    }
    if (!controlled) {
      setUncontrolled(committed)
    }
    setDraft(committed == null ? '' : formatNumber(committed, precision))
    onChange?.(committed)
  }

  function stepBy(direction: 1 | -1) {
    if (disabled) {
      return
    }
    const base = current ?? min ?? 0
    emit(base + direction * step)
  }

  function handleChange(event: ChangeEvent<HTMLInputElement>) {
    const raw = event.target.value
    setDraft(raw)
    const parsed = parseInput(raw)
    if (parsed == null) {
      if (raw.trim().length === 0) {
        if (!controlled) {
          setUncontrolled(null)
        }
        onChange?.(null)
      }
      return
    }
    const committed = clamp(parsed, min, max)
    if (!controlled) {
      setUncontrolled(committed)
    }
    onChange?.(committed)
  }

  function handleBlur(event: FocusEvent<HTMLInputElement>) {
    setFocused(false)
    emit(parseInput(draft))
    onBlur?.(event)
  }

  function handleKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === 'ArrowUp') {
      event.preventDefault()
      stepBy(1)
    } else if (event.key === 'ArrowDown') {
      event.preventDefault()
      stepBy(-1)
    }
    onKeyDown?.(event)
  }

  const input = (
    <input
      {...props}
      id={inputId}
      type="text"
      inputMode="decimal"
      disabled={disabled}
      className="ds-input__control"
      aria-invalid={invalid || undefined}
      value={draft}
      onChange={handleChange}
      onFocus={(event) => {
        setFocused(true)
        onFocus?.(event)
      }}
      onBlur={handleBlur}
      onKeyDown={handleKeyDown}
    />
  )

  if (split) {
    return (
      <div
        className="ds-input ds-input-number ds-input-number--split"
        data-disabled={disabled ? 'true' : 'false'}
        data-invalid={invalid ? 'true' : 'false'}
        data-controls="true"
        data-controls-layout="split"
      >
        {leadingIcon ? (
          <span className="ds-input__leading" aria-hidden="true">
            {leadingIcon}
          </span>
        ) : null}
        <button
          type="button"
          className="ds-input__affix ds-input-number__step"
          tabIndex={-1}
          disabled={decDisabled}
          aria-label="Diminuer"
          onClick={() => stepBy(-1)}
        >
          <MinusIcon size="sm" aria-hidden="true" />
        </button>
        {input}
        {suffix ? (
          <span className="ds-input-number__suffix" aria-hidden="true">
            {suffix}
          </span>
        ) : null}
        <button
          type="button"
          className="ds-input__affix ds-input-number__step"
          tabIndex={-1}
          disabled={incDisabled}
          aria-label="Augmenter"
          onClick={() => stepBy(1)}
        >
          <PlusIcon size="sm" aria-hidden="true" />
        </button>
      </div>
    )
  }

  return (
    <div
      className="ds-input ds-input-number"
      data-disabled={disabled ? 'true' : 'false'}
      data-invalid={invalid ? 'true' : 'false'}
      data-controls={controls ? 'true' : 'false'}
      data-controls-layout="end"
    >
      {leadingIcon ? (
        <span className="ds-input__leading" aria-hidden="true">
          {leadingIcon}
        </span>
      ) : null}
      {input}
      {suffix ? (
        <span className="ds-input-number__suffix" aria-hidden="true">
          {suffix}
        </span>
      ) : null}
      {showClear ? (
        <button
          type="button"
          className="ds-input__affix"
          aria-label={clearLabel}
          title={clearLabel}
          tabIndex={-1}
          onClick={() => emit(null)}
        >
          <CloseIcon size="sm" aria-hidden="true" />
        </button>
      ) : null}
      {controls ? (
        <div className="ds-input-number__controls">
          <button
            type="button"
            className="ds-input__affix ds-input-number__step"
            tabIndex={-1}
            disabled={incDisabled}
            aria-label="Augmenter"
            onClick={() => stepBy(1)}
          >
            <ChevronUpIcon size="sm" aria-hidden="true" />
          </button>
          <button
            type="button"
            className="ds-input__affix ds-input-number__step"
            tabIndex={-1}
            disabled={decDisabled}
            aria-label="Diminuer"
            onClick={() => stepBy(-1)}
          >
            <ChevronDownIcon size="sm" aria-hidden="true" />
          </button>
        </div>
      ) : null}
    </div>
  )
}

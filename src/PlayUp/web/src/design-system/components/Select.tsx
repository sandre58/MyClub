import {
  useEffect,
  useId,
  useRef,
  useState,
  type KeyboardEvent,
  type ReactNode,
} from 'react'
import { CloseIcon, ChevronDownIcon } from '../icons/shellIcons'

export type SelectOption = {
  value: string
  label: string
  disabled?: boolean
}

export type SelectProps = {
  options: SelectOption[]
  value?: string | null
  defaultValue?: string | null
  onChange?: (value: string | null) => void
  placeholder?: string
  disabled?: boolean
  invalid?: boolean
  leadingIcon?: ReactNode
  allowClear?: boolean
  clearLabel?: string
  id?: string
  'aria-label'?: string
}

/**
 * Select — TextInput shell + Ant-like dropdown list.
 * Optional leading icon and clear affix. Whole shell opens (except clear).
 */
export function Select({
  options,
  value,
  defaultValue = null,
  onChange,
  placeholder = 'Sélectionner…',
  disabled = false,
  invalid = false,
  leadingIcon,
  allowClear = false,
  clearLabel = 'Vider',
  id,
  'aria-label': ariaLabel,
}: SelectProps) {
  const autoId = useId()
  const triggerId = id ?? autoId
  const listId = `${triggerId}-list`
  const rootRef = useRef<HTMLDivElement>(null)
  const controlled = value !== undefined
  const [uncontrolled, setUncontrolled] = useState<string | null>(
    defaultValue ?? null,
  )
  const [open, setOpen] = useState(false)
  const current = controlled ? (value ?? null) : uncontrolled
  const selected = options.find((option) => option.value === current) ?? null
  const showClear = allowClear && current != null && !disabled

  useEffect(() => {
    if (!open) {
      return
    }

    function onPointerDown(event: MouseEvent) {
      if (!rootRef.current?.contains(event.target as Node)) {
        setOpen(false)
      }
    }

    function onKeyDown(event: globalThis.KeyboardEvent) {
      if (event.key === 'Escape') {
        setOpen(false)
      }
    }

    document.addEventListener('mousedown', onPointerDown)
    document.addEventListener('keydown', onKeyDown)
    return () => {
      document.removeEventListener('mousedown', onPointerDown)
      document.removeEventListener('keydown', onKeyDown)
    }
  }, [open])

  function emit(next: string | null) {
    if (!controlled) {
      setUncontrolled(next)
    }
    onChange?.(next)
  }

  function selectOption(next: string) {
    emit(next)
    setOpen(false)
  }

  function toggleOpen() {
    if (!disabled) {
      setOpen((currentOpen) => !currentOpen)
    }
  }

  function onShellKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (disabled) {
      return
    }
    if (
      event.key === 'ArrowDown' ||
      event.key === 'Enter' ||
      event.key === ' '
    ) {
      event.preventDefault()
      setOpen(true)
    }
  }

  return (
    <div className="ds-select" ref={rootRef}>
      <div
        id={triggerId}
        className="ds-input ds-select__shell"
        role="combobox"
        tabIndex={disabled ? -1 : 0}
        data-disabled={disabled ? 'true' : 'false'}
        data-invalid={invalid ? 'true' : 'false'}
        data-open={open ? 'true' : 'false'}
        aria-label={ariaLabel}
        aria-expanded={open}
        aria-haspopup="listbox"
        aria-controls={open ? listId : undefined}
        aria-invalid={invalid || undefined}
        aria-disabled={disabled || undefined}
        onClick={toggleOpen}
        onKeyDown={onShellKeyDown}
      >
        {leadingIcon ? (
          <span className="ds-input__leading" aria-hidden="true">
            {leadingIcon}
          </span>
        ) : null}
        <span
          className="ds-select__value"
          data-empty={selected ? 'false' : 'true'}
        >
          {selected ? selected.label : placeholder}
        </span>
        {showClear ? (
          <button
            type="button"
            className="ds-input__affix"
            aria-label={clearLabel}
            title={clearLabel}
            tabIndex={-1}
            onClick={(event) => {
              event.stopPropagation()
              emit(null)
              setOpen(false)
            }}
          >
            <CloseIcon size="sm" aria-hidden="true" />
          </button>
        ) : null}
        <span className="ds-select__arrow" aria-hidden="true">
          <ChevronDownIcon size="sm" />
        </span>
      </div>

      {open ? (
        <ul
          id={listId}
          className="ds-select__dropdown"
          role="listbox"
          aria-labelledby={triggerId}
        >
          {options.map((option) => {
            const isSelected = option.value === current
            return (
              <li key={option.value} role="presentation">
                <button
                  type="button"
                  className="ds-select__option"
                  role="option"
                  aria-selected={isSelected}
                  disabled={option.disabled || disabled}
                  data-selected={isSelected ? 'true' : 'false'}
                  onClick={() => selectOption(option.value)}
                >
                  {option.label}
                </button>
              </li>
            )
          })}
        </ul>
      ) : null}
    </div>
  )
}

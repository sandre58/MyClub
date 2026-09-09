import {
  useEffect,
  useId,
  useRef,
  useState,
  type KeyboardEvent,
  type ReactNode,
} from 'react'
import { CloseIcon, ChevronDownIcon } from '../icons/shellIcons'
import { useDismissLayer } from '../useDismissLayer'

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
  className?: string
}

function isEnabled(option: SelectOption) {
  return !option.disabled
}

function firstEnabledIndex(options: SelectOption[]) {
  return options.findIndex(isEnabled)
}

function lastEnabledIndex(options: SelectOption[]) {
  for (let index = options.length - 1; index >= 0; index -= 1) {
    if (isEnabled(options[index])) {
      return index
    }
  }
  return -1
}

function nextEnabledIndex(
  options: SelectOption[],
  from: number,
  direction: 1 | -1,
) {
  if (options.length === 0) {
    return -1
  }

  let index = from
  for (let step = 0; step < options.length; step += 1) {
    index += direction
    if (index < 0 || index >= options.length) {
      return from
    }
    if (isEnabled(options[index])) {
      return index
    }
  }
  return from
}

function initialActiveIndex(
  options: SelectOption[],
  current: string | null,
) {
  if (current != null) {
    const selected = options.findIndex(
      (option) => option.value === current && isEnabled(option),
    )
    if (selected >= 0) {
      return selected
    }
  }
  return firstEnabledIndex(options)
}

/**
 * Select — TextInput shell + Ant-like dropdown list.
 * Optional leading icon and clear affix. Whole shell opens (except clear).
 * Escape dismiss is coordinated via the shared dismiss stack (LIFO).
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
  className = '',
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
  const [activeIndex, setActiveIndex] = useState(-1)
  const current = controlled ? (value ?? null) : uncontrolled
  const selected = options.find((option) => option.value === current) ?? null
  const showClear = allowClear && current != null && !disabled
  const activeOption =
    activeIndex >= 0 && activeIndex < options.length
      ? options[activeIndex]
      : null
  const activeOptionId =
    open && activeOption ? `${listId}-opt-${activeOption.value}` : undefined
    const classes = ['ds-select', className].filter(Boolean).join(' ')

  useDismissLayer(open && !disabled, () => {
    setOpen(false)
  })

  useEffect(() => {
    if (!open) {
      return
    }

    function onPointerDown(event: MouseEvent) {
      if (!rootRef.current?.contains(event.target as Node)) {
        setOpen(false)
      }
    }

    document.addEventListener('mousedown', onPointerDown)
    return () => {
      document.removeEventListener('mousedown', onPointerDown)
    }
  }, [open])

  useEffect(() => {
    if (!open) {
      setActiveIndex(-1)
      return
    }
    setActiveIndex(initialActiveIndex(options, current))
    // Only seed highlight when the list opens — not on every options identity change.
    // eslint-disable-next-line react-hooks/exhaustive-deps -- open transition only
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

  function openList() {
    if (!disabled) {
      setOpen(true)
    }
  }

  function toggleOpen() {
    if (disabled) {
      return
    }
    setOpen((currentOpen) => !currentOpen)
  }

  function onShellKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (disabled) {
      return
    }

    if (!open) {
      if (
        event.key === 'ArrowDown' ||
        event.key === 'ArrowUp' ||
        event.key === 'Enter' ||
        event.key === ' '
      ) {
        event.preventDefault()
        openList()
      }
      return
    }

    if (event.key === 'ArrowDown') {
      event.preventDefault()
      setActiveIndex((index) => {
        if (index < 0) {
          return firstEnabledIndex(options)
        }
        return nextEnabledIndex(options, index, 1)
      })
      return
    }

    if (event.key === 'ArrowUp') {
      event.preventDefault()
      setActiveIndex((index) => {
        if (index < 0) {
          return lastEnabledIndex(options)
        }
        return nextEnabledIndex(options, index, -1)
      })
      return
    }

    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault()
      if (activeOption && isEnabled(activeOption)) {
        selectOption(activeOption.value)
      }
    }
  }

  return (
    <div className={classes} ref={rootRef}>
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
        aria-activedescendant={activeOptionId}
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
          {options.map((option, index) => {
            const isSelected = option.value === current
            const isActive = index === activeIndex
            return (
              <li key={option.value} role="presentation">
                <button
                  type="button"
                  id={`${listId}-opt-${option.value}`}
                  className="ds-select__option"
                  role="option"
                  tabIndex={-1}
                  aria-selected={isSelected}
                  disabled={option.disabled || disabled}
                  data-selected={isSelected ? 'true' : 'false'}
                  data-active={isActive ? 'true' : 'false'}
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

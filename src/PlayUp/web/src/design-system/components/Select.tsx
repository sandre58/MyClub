import {
  useEffect,
  useId,
  useLayoutEffect,
  useRef,
  useState,
  type CSSProperties,
  type KeyboardEvent,
  type ReactNode,
} from 'react';
import { createPortal } from 'react-dom';
import { CloseIcon, ChevronDownIcon } from '../icons/shellIcons';
import { useDismissLayer } from '../useDismissLayer';
import { usePresence } from '../usePresence';
import { Tooltip } from './Tooltip';

export type SelectOption = {
  value: string;
  label: string;
  disabled?: boolean;
  /** Optional leading mark (e.g. locale flag) — shown in list and on closed trigger when `leadingIcon` is omitted. */
  leading?: ReactNode;
};

export type SelectProps = {
  options: SelectOption[];
  value?: string | null;
  defaultValue?: string | null;
  onChange?: (value: string | null) => void;
  placeholder?: string;
  disabled?: boolean;
  invalid?: boolean;
  leadingIcon?: ReactNode;
  allowClear?: boolean;
  clearLabel?: string;
  id?: string;
  'aria-label'?: string;
  className?: string;
};

function isEnabled(option: SelectOption) {
  return !option.disabled;
}

function firstEnabledIndex(options: SelectOption[]) {
  return options.findIndex(isEnabled);
}

function lastEnabledIndex(options: SelectOption[]) {
  for (let index = options.length - 1; index >= 0; index -= 1) {
    if (isEnabled(options[index])) {
      return index;
    }
  }
  return -1;
}

function nextEnabledIndex(
  options: SelectOption[],
  from: number,
  direction: 1 | -1,
) {
  if (options.length === 0) {
    return -1;
  }

  let index = from;
  for (let step = 0; step < options.length; step += 1) {
    index += direction;
    if (index < 0 || index >= options.length) {
      return from;
    }
    if (isEnabled(options[index])) {
      return index;
    }
  }
  return from;
}

function initialActiveIndex(options: SelectOption[], current: string | null) {
  if (current != null) {
    const selected = options.findIndex(
      (option) => option.value === current && isEnabled(option),
    );
    if (selected >= 0) {
      return selected;
    }
  }
  return firstEnabledIndex(options);
}

const EXIT_MS = 200;
/** Match `--space-4` gap under the shell. */
const GAP_PX = 4;
const VIEWPORT_PAD_PX = 16;
/** Prefer below when at least this many px remain (otherwise flip above). */
const FLIP_THRESHOLD_PX = 160;
/** Above Popover (50); below Tooltip (60). Nested Select-in-Popover must float over its host. */
const Z_INDEX = 55;

type SelectSide = 'below' | 'above';

type Placement = {
  style: CSSProperties;
  side: SelectSide;
};

/**
 * Select — TextInput shell + Ant-like dropdown list.
 * Optional leading icon and clear affix. Whole shell opens (except clear).
 * Escape dismiss is coordinated via the shared dismiss stack (LIFO).
 * Listbox portals to `document.body` so overflow:hidden ancestors cannot clip it.
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
  const autoId = useId();
  const triggerId = id ?? autoId;
  const listId = `${triggerId}-list`;
  const shellRef = useRef<HTMLDivElement>(null);
  const listRef = useRef<HTMLUListElement>(null);
  const controlled = value !== undefined;
  const [uncontrolled, setUncontrolled] = useState<string | null>(
    defaultValue ?? null,
  );
  const [open, setOpen] = useState(false);
  const [activeIndex, setActiveIndex] = useState(-1);
  const [placement, setPlacement] = useState<Placement | undefined>();
  const { present, state } = usePresence(open, EXIT_MS);
  const current = controlled ? (value ?? null) : uncontrolled;
  const selected = options.find((option) => option.value === current) ?? null;
  const triggerLeading = leadingIcon ?? selected?.leading ?? null;
  const showClear = allowClear && current != null && !disabled;
  const activeOption =
    activeIndex >= 0 && activeIndex < options.length
      ? options[activeIndex]
      : null;
  const activeOptionId =
    open && activeOption ? `${listId}-opt-${activeOption.value}` : undefined;
  const classes = ['ds-select', className].filter(Boolean).join(' ');

  useDismissLayer(open && !disabled, () => {
    setOpen(false);
  });

  useLayoutEffect(() => {
    if (!present) {
      setPlacement(undefined);
      return;
    }

    function placeList() {
      const shell = shellRef.current;
      if (!shell) {
        return;
      }

      const rect = shell.getBoundingClientRect();
      const width = rect.width;

      let left = rect.left;
      if (left + width > window.innerWidth - VIEWPORT_PAD_PX) {
        left = Math.max(
          VIEWPORT_PAD_PX,
          window.innerWidth - VIEWPORT_PAD_PX - width,
        );
      }
      left = Math.max(VIEWPORT_PAD_PX, left);

      const spaceBelow = window.innerHeight - rect.bottom - GAP_PX;
      const spaceAbove = rect.top - GAP_PX;
      const preferBelow =
        spaceBelow >= FLIP_THRESHOLD_PX || spaceBelow >= spaceAbove;
      const side: SelectSide = preferBelow ? 'below' : 'above';
      const available = preferBelow ? spaceBelow : spaceAbove;
      const maxHeight = Math.max(0, available - VIEWPORT_PAD_PX);

      setPlacement({
        side,
        style: {
          position: 'fixed',
          top: preferBelow ? rect.bottom + GAP_PX : undefined,
          bottom: preferBelow
            ? undefined
            : window.innerHeight - rect.top + GAP_PX,
          left,
          width,
          maxHeight,
          zIndex: Z_INDEX,
        },
      });
    }

    placeList();
    window.addEventListener('resize', placeList);
    window.addEventListener('scroll', placeList, true);
    return () => {
      window.removeEventListener('resize', placeList);
      window.removeEventListener('scroll', placeList, true);
    };
  }, [present]);

  useEffect(() => {
    if (!open) {
      return;
    }

    function onPointerDown(event: MouseEvent) {
      const target = event.target as Node;
      if (
        shellRef.current?.contains(target) ||
        listRef.current?.contains(target)
      ) {
        return;
      }
      setOpen(false);
    }

    document.addEventListener('mousedown', onPointerDown);
    return () => {
      document.removeEventListener('mousedown', onPointerDown);
    };
  }, [open]);

  useEffect(() => {
    if (!open) {
      setActiveIndex(-1);
      return;
    }
    setActiveIndex(initialActiveIndex(options, current));
    // Only seed highlight when the list opens — not on every options identity change.
    // eslint-disable-next-line react-hooks/exhaustive-deps -- open transition only
  }, [open]);

  function emit(next: string | null) {
    if (!controlled) {
      setUncontrolled(next);
    }
    onChange?.(next);
  }

  function selectOption(next: string) {
    emit(next);
    setOpen(false);
  }

  function openList() {
    if (!disabled) {
      setOpen(true);
    }
  }

  function toggleOpen() {
    if (disabled) {
      return;
    }
    setOpen((currentOpen) => !currentOpen);
  }

  function onShellKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (disabled) {
      return;
    }

    if (!open) {
      if (
        event.key === 'ArrowDown' ||
        event.key === 'ArrowUp' ||
        event.key === 'Enter' ||
        event.key === ' '
      ) {
        event.preventDefault();
        openList();
      }
      return;
    }

    if (event.key === 'ArrowDown') {
      event.preventDefault();
      setActiveIndex((index) => {
        if (index < 0) {
          return firstEnabledIndex(options);
        }
        return nextEnabledIndex(options, index, 1);
      });
      return;
    }

    if (event.key === 'ArrowUp') {
      event.preventDefault();
      setActiveIndex((index) => {
        if (index < 0) {
          return lastEnabledIndex(options);
        }
        return nextEnabledIndex(options, index, -1);
      });
      return;
    }

    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      if (activeOption && isEnabled(activeOption)) {
        selectOption(activeOption.value);
      }
    }
  }

  const listbox =
    present && placement && typeof document !== 'undefined'
      ? createPortal(
          <ul
            ref={listRef}
            id={listId}
            className="ds-select__dropdown"
            role="listbox"
            aria-labelledby={triggerId}
            aria-hidden={open ? undefined : true}
            data-state={state}
            data-side={placement.side}
            style={placement.style}
          >
            {options.map((option, index) => {
              const isSelected = option.value === current;
              const isActive = index === activeIndex;
              return (
                <li key={option.value} role="presentation">
                  <button
                    type="button"
                    id={`${listId}-opt-${option.value}`}
                    className="ds-select__option"
                    role="option"
                    tabIndex={-1}
                    aria-selected={isSelected}
                    disabled={option.disabled || disabled || !open}
                    data-selected={isSelected ? 'true' : 'false'}
                    data-active={isActive ? 'true' : 'false'}
                    onClick={() => selectOption(option.value)}
                  >
                    {option.leading ? (
                      <span
                        className="ds-select__option-leading"
                        aria-hidden="true"
                      >
                        {option.leading}
                      </span>
                    ) : null}
                    <span className="ds-select__option-label">
                      {option.label}
                    </span>
                  </button>
                </li>
              );
            })}
          </ul>,
          document.body,
        )
      : null;

  return (
    <div className={classes} {...(disabled ? { inert: true } : {})}>
      <div
        ref={shellRef}
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
        aria-controls={present ? listId : undefined}
        aria-activedescendant={activeOptionId}
        aria-invalid={invalid || undefined}
        aria-disabled={disabled || undefined}
        onClick={toggleOpen}
        onKeyDown={onShellKeyDown}
      >
        {triggerLeading ? (
          <span className="ds-input__leading" aria-hidden="true">
            {triggerLeading}
          </span>
        ) : null}
        <span
          className="ds-select__value"
          data-empty={selected ? 'false' : 'true'}
        >
          {selected ? selected.label : placeholder}
        </span>
        {showClear ? (
          <Tooltip content={clearLabel}>
            <button
              type="button"
              className="ds-input__affix"
              aria-label={clearLabel}
              tabIndex={-1}
              onClick={(event) => {
                event.stopPropagation();
                emit(null);
                setOpen(false);
              }}
            >
              <CloseIcon size="sm" aria-hidden="true" />
            </button>
          </Tooltip>
        ) : null}
        <span className="ds-select__arrow" aria-hidden="true">
          <ChevronDownIcon size="sm" />
        </span>
      </div>

      {listbox}
    </div>
  );
}

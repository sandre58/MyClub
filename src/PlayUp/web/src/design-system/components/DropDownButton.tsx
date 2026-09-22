import {
  useId,
  useRef,
  useState,
  type KeyboardEvent,
  type ReactNode,
} from 'react';
import { ChevronDownIcon } from '../icons/shellIcons';
import { Popover } from './Popover';

export type DropDownButtonItem = {
  value: string;
  label: string;
  disabled?: boolean;
  leading?: ReactNode;
};

export type DropDownButtonVariant = 'primary' | 'secondary' | 'ghost';

export type DropDownButtonProps = {
  /** Visible label on the trigger button. */
  label: ReactNode;
  items: DropDownButtonItem[];
  onSelect: (value: string) => void;
  variant?: DropDownButtonVariant;
  disabled?: boolean;
  leadingIcon?: ReactNode;
  align?: 'start' | 'end';
  /** Shown in the menu when `items` is empty. */
  emptyLabel?: string;
  'aria-label'?: string;
  className?: string;
};

function firstEnabledIndex(items: DropDownButtonItem[]) {
  return items.findIndex((item) => !item.disabled);
}

function nextEnabledIndex(
  items: DropDownButtonItem[],
  from: number,
  direction: 1 | -1,
) {
  if (items.length === 0) return -1;
  let index = from;
  for (let step = 0; step < items.length; step += 1) {
    index += direction;
    if (index < 0 || index >= items.length) return from;
    if (!items[index]?.disabled) return index;
  }
  return from;
}

/**
 * Button that opens an action menu (pick → callback), not a value field.
 * Visual menu chrome aligns with Select options; trigger uses `.ds-btn`.
 */
export function DropDownButton({
  label,
  items,
  onSelect,
  variant = 'primary',
  disabled = false,
  leadingIcon,
  align = 'end',
  emptyLabel,
  'aria-label': ariaLabel,
  className = '',
}: DropDownButtonProps) {
  const [open, setOpen] = useState(false);
  const [activeIndex, setActiveIndex] = useState(-1);
  const rootRef = useRef<HTMLDivElement>(null);
  const menuId = useId();

  const enabled = !disabled;
  const rootClass = ['ds-dropdown-button', className].filter(Boolean).join(' ');
  const triggerClass = [
    'ds-btn',
    `ds-btn--${variant}`,
    'ds-dropdown-button__trigger',
  ].join(' ');

  function close() {
    setOpen(false);
    setActiveIndex(-1);
  }

  function toggle() {
    if (!enabled) return;
    setOpen((current) => {
      const next = !current;
      if (next) setActiveIndex(firstEnabledIndex(items));
      else setActiveIndex(-1);
      return next;
    });
  }

  function pick(value: string) {
    onSelect(value);
    close();
  }

  function onTriggerKeyDown(event: KeyboardEvent<HTMLButtonElement>) {
    if (!enabled) return;

    if (!open) {
      if (
        event.key === 'ArrowDown' ||
        event.key === 'ArrowUp' ||
        event.key === 'Enter' ||
        event.key === ' '
      ) {
        event.preventDefault();
        setOpen(true);
        setActiveIndex(firstEnabledIndex(items));
      }
      return;
    }

    if (event.key === 'ArrowDown') {
      event.preventDefault();
      setActiveIndex((index) =>
        index < 0
          ? firstEnabledIndex(items)
          : nextEnabledIndex(items, index, 1),
      );
      return;
    }

    if (event.key === 'ArrowUp') {
      event.preventDefault();
      setActiveIndex((index) =>
        index < 0
          ? firstEnabledIndex(items)
          : nextEnabledIndex(items, index, -1),
      );
      return;
    }

    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      const item = items[activeIndex];
      if (item && !item.disabled) pick(item.value);
    }
  }

  return (
    <div className={rootClass} ref={rootRef}>
      <button
        type="button"
        className={triggerClass}
        disabled={!enabled}
        aria-expanded={open}
        aria-haspopup="menu"
        aria-controls={open ? menuId : undefined}
        aria-label={ariaLabel}
        onClick={toggle}
        onKeyDown={onTriggerKeyDown}
      >
        {leadingIcon}
        <span className="ds-dropdown-button__label">{label}</span>
        <span
          className="ds-dropdown-button__chevron"
          data-open={open ? 'true' : 'false'}
          aria-hidden="true"
        >
          <ChevronDownIcon size="sm" />
        </span>
      </button>

      <Popover
        open={open}
        onOpenChange={(next) => {
          if (!next) close();
          else {
            setOpen(true);
            setActiveIndex(firstEnabledIndex(items));
          }
        }}
        anchorRef={rootRef}
        id={menuId}
        role="menu"
        aria-label={ariaLabel}
        align={align}
        className="ds-dropdown-button__menu"
        flipThreshold={160}
      >
        {items.length === 0 ? (
          <p className="ds-dropdown-button__empty">{emptyLabel}</p>
        ) : (
          <ul className="ds-dropdown-button__list" role="none">
            {items.map((item, index) => (
              <li key={item.value} role="none">
                <button
                  type="button"
                  role="menuitem"
                  className="ds-dropdown-button__item"
                  tabIndex={-1}
                  disabled={item.disabled || !open}
                  data-active={index === activeIndex ? 'true' : 'false'}
                  onClick={() => {
                    if (!item.disabled) pick(item.value);
                  }}
                  onMouseEnter={() => {
                    if (!item.disabled) setActiveIndex(index);
                  }}
                >
                  {item.leading ? (
                    <span
                      className="ds-dropdown-button__item-leading"
                      aria-hidden="true"
                    >
                      {item.leading}
                    </span>
                  ) : null}
                  <span className="ds-dropdown-button__item-label">
                    {item.label}
                  </span>
                </button>
              </li>
            ))}
          </ul>
        )}
      </Popover>
    </div>
  );
}

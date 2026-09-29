import {
  useEffect,
  useId,
  useRef,
  useState,
  type ChangeEvent,
  type FocusEvent,
  type InputHTMLAttributes,
  type KeyboardEvent,
  type PointerEvent as ReactPointerEvent,
  type ReactNode,
} from 'react';
import { CloseIcon, ChevronDownIcon, ChevronUpIcon } from '../icons/shellIcons';
import { MinusIcon, PlusIcon } from '../icons/contentIcons';
import { Tooltip } from './Tooltip';

export type InputNumberControlsLayout = 'end' | 'split';

export type InputNumberProps = Omit<
  InputHTMLAttributes<HTMLInputElement>,
  'className' | 'size' | 'type' | 'value' | 'defaultValue' | 'onChange'
> & {
  value?: number | null;
  defaultValue?: number | null;
  onChange?: (value: number | null) => void;
  min?: number;
  max?: number;
  step?: number;
  precision?: number;
  controls?: boolean;
  /**
   * `end` — Ant-like vertical chevrons (default).
   * `split` — horizontal − value + (dense edit / regulation mockup).
   */
  controlsLayout?: InputNumberControlsLayout;
  leadingIcon?: ReactNode;
  /** Static leading mark inside the value cluster (e.g. « V »). */
  prefix?: ReactNode;
  /** Static trailing unit (e.g. « min ») — not an action affix. */
  suffix?: ReactNode;
  /** Optional semantic tint (e.g. win / loss rank controls). */
  tone?: 'success' | 'danger';
  invalid?: boolean;
  allowClear?: boolean;
  clearLabel?: string;
};

const REPEAT_DELAY_MS = 400;
const REPEAT_INTERVAL_MS = 75;

function clamp(n: number, min?: number, max?: number): number {
  let next = n;
  if (min != null && next < min) {
    next = min;
  }
  if (max != null && next > max) {
    next = max;
  }
  return next;
}

function formatNumber(n: number, precision?: number): string {
  if (precision == null) {
    return String(n);
  }
  return n.toFixed(precision);
}

function parseInput(raw: string): number | null {
  const trimmed = raw.trim();
  if (
    trimmed.length === 0 ||
    trimmed === '-' ||
    trimmed === '.' ||
    trimmed === '-.'
  ) {
    return null;
  }
  const n = Number(trimmed);
  return Number.isFinite(n) ? n : null;
}

/**
 * Numeric input — TextInput shell + Ant-like steppers (press-and-hold repeat).
 * Optional leading icon, suffix, and clear affix.
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
  prefix,
  suffix,
  tone,
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
  const autoId = useId();
  const inputId = id ?? autoId;
  const controlled = value !== undefined;
  const [uncontrolled, setUncontrolled] = useState<number | null>(
    defaultValue ?? null,
  );
  const current = controlled ? (value ?? null) : uncontrolled;
  const [draft, setDraft] = useState(() =>
    current == null ? '' : formatNumber(current, precision),
  );
  const [focused, setFocused] = useState(false);

  const delayRef = useRef<number | null>(null);
  const intervalRef = useRef<number | null>(null);
  const stepByRef = useRef<(direction: 1 | -1) => void>(() => undefined);
  const currentRef = useRef(current);
  currentRef.current = current;

  useEffect(() => {
    if (!focused) {
      setDraft(current == null ? '' : formatNumber(current, precision));
    }
  }, [current, precision, focused]);

  useEffect(() => {
    return () => {
      stopRepeat();
    };
  }, []);

  const showClear = allowClear && current != null && !disabled;
  const split = controls && controlsLayout === 'split';
  const decDisabled =
    disabled || (min != null && current != null && current <= min);
  const incDisabled =
    disabled || (max != null && current != null && current >= max);

  function emit(next: number | null) {
    let committed = next;
    if (committed != null) {
      committed = clamp(committed, min, max);
      if (precision != null) {
        const factor = 10 ** precision;
        committed = Math.round(committed * factor) / factor;
      }
    }
    currentRef.current = committed;
    if (!controlled) {
      setUncontrolled(committed);
    }
    setDraft(committed == null ? '' : formatNumber(committed, precision));
    onChange?.(committed);
  }

  function stepBy(direction: 1 | -1) {
    if (disabled) {
      return;
    }
    const base = currentRef.current ?? min ?? 0;
    if (direction === 1 && max != null && base >= max) {
      stopRepeat();
      return;
    }
    if (direction === -1 && min != null && base <= min) {
      stopRepeat();
      return;
    }
    emit(base + direction * step);
  }

  stepByRef.current = stepBy;

  function stopRepeat() {
    if (delayRef.current != null) {
      window.clearTimeout(delayRef.current);
      delayRef.current = null;
    }
    if (intervalRef.current != null) {
      window.clearInterval(intervalRef.current);
      intervalRef.current = null;
    }
  }

  function startRepeat(direction: 1 | -1) {
    stopRepeat();
    stepByRef.current(direction);
    delayRef.current = window.setTimeout(() => {
      intervalRef.current = window.setInterval(() => {
        stepByRef.current(direction);
      }, REPEAT_INTERVAL_MS);
    }, REPEAT_DELAY_MS);
  }

  function handleStepPointerDown(
    direction: 1 | -1,
    event: ReactPointerEvent<HTMLButtonElement>,
  ) {
    if (disabled || event.button !== 0) {
      return;
    }
    event.preventDefault();
    startRepeat(direction);
  }

  function handleChange(event: ChangeEvent<HTMLInputElement>) {
    if (disabled) {
      return;
    }
    const raw = event.target.value;
    setDraft(raw);
    const parsed = parseInput(raw);
    if (parsed == null) {
      if (raw.trim().length === 0) {
        if (!controlled) {
          setUncontrolled(null);
        }
        onChange?.(null);
      }
      return;
    }
    const committed = clamp(parsed, min, max);
    if (!controlled) {
      setUncontrolled(committed);
    }
    onChange?.(committed);
  }

  function handleBlur(event: FocusEvent<HTMLInputElement>) {
    setFocused(false);
    if (!disabled) {
      emit(parseInput(draft));
    }
    onBlur?.(event);
  }

  function handleKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (disabled) {
      onKeyDown?.(event);
      return;
    }
    if (event.key === 'ArrowUp') {
      event.preventDefault();
      stepBy(1);
    } else if (event.key === 'ArrowDown') {
      event.preventDefault();
      stepBy(-1);
    }
    onKeyDown?.(event);
  }

  const stepPointerHandlers = {
    onPointerUp: stopRepeat,
    onPointerLeave: stopRepeat,
    onPointerCancel: stopRepeat,
  };

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
        setFocused(true);
        onFocus?.(event);
      }}
      onBlur={handleBlur}
      onKeyDown={handleKeyDown}
    />
  );

  if (split) {
    return (
      <div
        className="ds-input ds-input-number ds-input-number--split"
        data-disabled={disabled ? 'true' : 'false'}
        data-invalid={invalid ? 'true' : 'false'}
        data-tone={tone}
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
          onPointerDown={(event) => handleStepPointerDown(-1, event)}
          {...stepPointerHandlers}
        >
          <MinusIcon size="sm" aria-hidden="true" />
        </button>
        {prefix || suffix ? (
          <span className="ds-input-number__value">
            {prefix ? (
              <span className="ds-input-number__prefix" aria-hidden="true">
                {prefix}
              </span>
            ) : null}
            {input}
            {suffix ? (
              <span className="ds-input-number__suffix" aria-hidden="true">
                {suffix}
              </span>
            ) : null}
          </span>
        ) : (
          input
        )}
        <button
          type="button"
          className="ds-input__affix ds-input-number__step"
          tabIndex={-1}
          disabled={incDisabled}
          aria-label="Augmenter"
          onPointerDown={(event) => handleStepPointerDown(1, event)}
          {...stepPointerHandlers}
        >
          <PlusIcon size="sm" aria-hidden="true" />
        </button>
      </div>
    );
  }

  return (
    <div
      className="ds-input ds-input-number"
      data-disabled={disabled ? 'true' : 'false'}
      data-invalid={invalid ? 'true' : 'false'}
      data-tone={tone}
      data-controls={controls ? 'true' : 'false'}
      data-controls-layout="end"
    >
      {leadingIcon ? (
        <span className="ds-input__leading" aria-hidden="true">
          {leadingIcon}
        </span>
      ) : null}
      {prefix ? (
        <span className="ds-input-number__prefix" aria-hidden="true">
          {prefix}
        </span>
      ) : null}
      {input}
      {suffix ? (
        <span className="ds-input-number__suffix" aria-hidden="true">
          {suffix}
        </span>
      ) : null}
      {showClear ? (
        <Tooltip content={clearLabel}>
          <button
            type="button"
            className="ds-input__affix"
            aria-label={clearLabel}
            tabIndex={-1}
            onClick={() => emit(null)}
          >
            <CloseIcon size="sm" aria-hidden="true" />
          </button>
        </Tooltip>
      ) : null}
      {controls ? (
        <div className="ds-input-number__controls">
          <button
            type="button"
            className="ds-input__affix ds-input-number__step"
            tabIndex={-1}
            disabled={incDisabled}
            aria-label="Augmenter"
            onPointerDown={(event) => handleStepPointerDown(1, event)}
            {...stepPointerHandlers}
          >
            <ChevronUpIcon size="sm" aria-hidden="true" />
          </button>
          <button
            type="button"
            className="ds-input__affix ds-input-number__step"
            tabIndex={-1}
            disabled={decDisabled}
            aria-label="Diminuer"
            onPointerDown={(event) => handleStepPointerDown(-1, event)}
            {...stepPointerHandlers}
          >
            <ChevronDownIcon size="sm" aria-hidden="true" />
          </button>
        </div>
      ) : null}
    </div>
  );
}

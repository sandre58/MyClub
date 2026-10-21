import {
  Children,
  cloneElement,
  isValidElement,
  useCallback,
  useEffect,
  useId,
  useLayoutEffect,
  useRef,
  useState,
  type CSSProperties,
  type FocusEvent,
  type KeyboardEvent as ReactKeyboardEvent,
  type PointerEvent as ReactPointerEvent,
  type ReactElement,
  type ReactNode,
} from 'react';
import { createPortal } from 'react-dom';
import { usePresence } from '../usePresence';
import { claimTooltip, releaseTooltip } from '../tooltipStore';

export type TooltipSide = 'top' | 'bottom';
export type TooltipActivation = 'auto' | 'long-press' | 'tap';

/** Hover open delay (desktop fine pointer). */
export const TOOLTIP_DELAY_OPEN_MS = 400;
/** Leave close grace (desktop). */
export const TOOLTIP_DELAY_CLOSE_MS = 100;
/** Long-press to open on coarse + primary action. */
export const TOOLTIP_LONG_PRESS_MS = 500;
/** Auto-dismiss after show on coarse pointer. */
export const TOOLTIP_MOBILE_DISMISS_MS = 3000;
/** Match --motion-duration + small exit buffer (same as Popover). */
const EXIT_MS = 200;

const GAP_PX = 8;
const VIEWPORT_PAD_PX = 8;
const CARET_SIZE_PX = 6;
const CARET_EDGE_PAD_PX = 8;
const LONG_PRESS_MOVE_PX = 10;
const MAX_WIDTH_PX = 280;

export type TooltipProps = {
  /**
   * Tip body. Plain string or non-interactive structured markup.
   * Empty / whitespace string (or nullish) → render children only.
   * Interactive content → use Popover.
   */
  content: ReactNode;
  children: ReactElement;
  /** Preferred side; flips if not enough space. Default `top`. */
  side?: TooltipSide;
  delayOpen?: number;
  delayClose?: number;
  /**
   * Mobile activation. `auto` = long-press for primary-action triggers,
   * tap-toggle otherwise. Desktop always uses hover + focus.
   */
  activation?: TooltipActivation;
};

function isBlankTooltipContent(content: ReactNode): boolean {
  if (content == null || content === false || content === true) {
    return true;
  }
  if (typeof content === 'string') {
    return content.trim().length === 0;
  }
  return false;
}

type Placement = {
  style: CSSProperties;
  side: TooltipSide;
};

function isFinePointer(): boolean {
  if (
    typeof window === 'undefined' ||
    typeof window.matchMedia !== 'function'
  ) {
    return true;
  }
  return window.matchMedia('(hover: hover) and (pointer: fine)').matches;
}

function useFinePointer(): boolean {
  const [fine, setFine] = useState(isFinePointer);
  useEffect(() => {
    const mq = window.matchMedia('(hover: hover) and (pointer: fine)');
    const onChange = () => setFine(mq.matches);
    mq.addEventListener('change', onChange);
    return () => mq.removeEventListener('change', onChange);
  }, []);
  return fine;
}

function isDisabledTrigger(element: ReactElement): boolean {
  const props = element.props as {
    disabled?: boolean;
    'aria-disabled'?: boolean | 'true';
  };
  return (
    props.disabled === true ||
    props['aria-disabled'] === true ||
    props['aria-disabled'] === 'true'
  );
}

function isPrimaryActionElement(element: ReactElement): boolean {
  const type = element.type;
  if (type === 'button' || type === 'a') {
    return true;
  }
  const props = element.props as { role?: string; onClick?: unknown };
  if (props.role === 'button' || props.role === 'link') {
    return true;
  }
  return typeof props.onClick === 'function';
}

function resolveActivation(
  activation: TooltipActivation,
  child: ReactElement,
): 'long-press' | 'tap' {
  if (activation === 'long-press' || activation === 'tap') {
    return activation;
  }
  return isPrimaryActionElement(child) || isDisabledTrigger(child)
    ? 'long-press'
    : 'tap';
}

function clearTimer(ref: { current: number | null }) {
  if (ref.current != null) {
    window.clearTimeout(ref.current);
    ref.current = null;
  }
}

/**
 * Contextual tip for Hint / DisabledReason / short Labels (icon-only).
 * Desktop: hover (delayed) + focus. Mobile: long-press or tap-toggle.
 * Non-interactive structured markup allowed; interactive content → use Popover.
 *
 * Always wraps the child in `.ds-tooltip-trigger` so disabled controls and
 * non-forwardRef hosts (Chip, Status) still receive pointer / a11y wiring.
 */
export function Tooltip({
  content,
  children,
  side: preferredSide = 'top',
  delayOpen = TOOLTIP_DELAY_OPEN_MS,
  delayClose = TOOLTIP_DELAY_CLOSE_MS,
  activation = 'auto',
}: TooltipProps) {
  if (isBlankTooltipContent(content) || !isValidElement(children)) {
    return children;
  }

  return (
    <TooltipActive
      content={content}
      child={Children.only(children)}
      preferredSide={preferredSide}
      delayOpen={delayOpen}
      delayClose={delayClose}
      activation={activation}
    />
  );
}

function TooltipActive({
  content,
  child,
  preferredSide,
  delayOpen,
  delayClose,
  activation,
}: {
  content: ReactNode;
  child: ReactElement;
  preferredSide: TooltipSide;
  delayOpen: number;
  delayClose: number;
  activation: TooltipActivation;
}) {
  const tooltipId = useId();
  const triggerRef = useRef<HTMLSpanElement | null>(null);
  const panelRef = useRef<HTMLDivElement | null>(null);
  const closeFnRef = useRef<() => void>(() => undefined);
  const [open, setOpen] = useState(false);
  const { present, state } = usePresence(open, EXIT_MS);
  const [placement, setPlacement] = useState<Placement | undefined>();
  const fine = useFinePointer();
  const mobileMode = resolveActivation(activation, child);

  const openTimer = useRef<number | null>(null);
  const closeTimer = useRef<number | null>(null);
  const dismissTimer = useRef<number | null>(null);
  const longPressTimer = useRef<number | null>(null);
  const longPressOrigin = useRef<{ x: number; y: number } | null>(null);
  const openedByLongPress = useRef(false);
  const suppressClick = useRef(false);

  const clearAllTimers = useCallback(() => {
    clearTimer(openTimer);
    clearTimer(closeTimer);
    clearTimer(dismissTimer);
    clearTimer(longPressTimer);
    longPressOrigin.current = null;
  }, []);

  const close = useCallback(() => {
    clearAllTimers();
    setOpen(false);
    releaseTooltip(closeFnRef.current);
  }, [clearAllTimers]);

  closeFnRef.current = close;

  const openNow = useCallback(() => {
    clearAllTimers();
    claimTooltip(closeFnRef.current);
    setOpen(true);
    if (!isFinePointer()) {
      dismissTimer.current = window.setTimeout(() => {
        closeFnRef.current();
      }, TOOLTIP_MOBILE_DISMISS_MS);
    }
  }, [clearAllTimers]);

  useEffect(() => {
    return () => {
      clearAllTimers();
      releaseTooltip(closeFnRef.current);
    };
  }, [clearAllTimers]);

  useLayoutEffect(() => {
    if (!present) {
      setPlacement(undefined);
      return;
    }

    function place() {
      const anchor = triggerRef.current;
      if (!anchor) {
        return;
      }
      const rect = anchor.getBoundingClientRect();
      const panel = panelRef.current;
      const measuredWidth = panel?.offsetWidth ?? 0;
      const panelWidth = Math.min(
        MAX_WIDTH_PX,
        window.innerWidth - VIEWPORT_PAD_PX * 2,
        measuredWidth > 0 ? measuredWidth : MAX_WIDTH_PX,
      );
      const panelHeight = panel?.offsetHeight ?? 32;

      let side: TooltipSide = preferredSide;
      const spaceAbove = rect.top - GAP_PX - VIEWPORT_PAD_PX;
      const spaceBelow =
        window.innerHeight - rect.bottom - GAP_PX - VIEWPORT_PAD_PX;
      if (
        preferredSide === 'top' &&
        spaceAbove < panelHeight &&
        spaceBelow > spaceAbove
      ) {
        side = 'bottom';
      } else if (
        preferredSide === 'bottom' &&
        spaceBelow < panelHeight &&
        spaceAbove > spaceBelow
      ) {
        side = 'top';
      }

      let left = rect.left + rect.width / 2 - panelWidth / 2;
      left = Math.min(
        window.innerWidth - VIEWPORT_PAD_PX - panelWidth,
        Math.max(VIEWPORT_PAD_PX, left),
      );

      const anchorCenterX = rect.left + rect.width / 2;
      const caretInset = Math.min(
        panelWidth - CARET_EDGE_PAD_PX - CARET_SIZE_PX,
        Math.max(CARET_EDGE_PAD_PX, anchorCenterX - left - CARET_SIZE_PX / 2),
      );

      setPlacement({
        side,
        style: {
          position: 'fixed',
          top: side === 'bottom' ? rect.bottom + GAP_PX : undefined,
          bottom:
            side === 'top' ? window.innerHeight - rect.top + GAP_PX : undefined,
          left,
          maxWidth: MAX_WIDTH_PX,
          zIndex: 60,
          ['--ds-tooltip-caret-inset' as string]: `${caretInset}px`,
        },
      });
    }

    place();
    const raf = window.requestAnimationFrame(place);
    window.addEventListener('resize', place);
    window.addEventListener('scroll', place, true);
    return () => {
      window.cancelAnimationFrame(raf);
      window.removeEventListener('resize', place);
      window.removeEventListener('scroll', place, true);
    };
  }, [present, preferredSide, content]);

  useEffect(() => {
    if (!open) {
      return;
    }

    function onKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        close();
      }
    }

    function onPointerDown(event: PointerEvent) {
      const target = event.target as Node;
      if (
        triggerRef.current?.contains(target) ||
        panelRef.current?.contains(target)
      ) {
        return;
      }
      close();
    }

    document.addEventListener('keydown', onKeyDown);
    document.addEventListener('pointerdown', onPointerDown, true);
    return () => {
      document.removeEventListener('keydown', onKeyDown);
      document.removeEventListener('pointerdown', onPointerDown, true);
    };
  }, [open, close]);

  function scheduleOpen(ms: number) {
    clearTimer(closeTimer);
    clearTimer(openTimer);
    if (ms <= 0) {
      openNow();
      return;
    }
    openTimer.current = window.setTimeout(() => openNow(), ms);
  }

  function scheduleClose(ms: number) {
    clearTimer(openTimer);
    clearTimer(closeTimer);
    if (ms <= 0) {
      close();
      return;
    }
    closeTimer.current = window.setTimeout(() => close(), ms);
  }

  function onPointerEnter() {
    if (!fine) {
      return;
    }
    scheduleOpen(delayOpen);
  }

  function onPointerLeave() {
    if (!fine) {
      return;
    }
    clearTimer(longPressTimer);
    scheduleClose(delayClose);
  }

  function onFocus() {
    if (!fine) {
      return;
    }
    scheduleOpen(0);
  }

  function onBlur(event: FocusEvent) {
    const next = event.relatedTarget as Node | null;
    if (next && triggerRef.current?.contains(next)) {
      return;
    }
    scheduleClose(0);
  }

  function onPointerDownTrigger(event: ReactPointerEvent) {
    if (fine || event.button !== 0) {
      return;
    }
    if (mobileMode === 'long-press') {
      openedByLongPress.current = false;
      longPressOrigin.current = { x: event.clientX, y: event.clientY };
      clearTimer(longPressTimer);
      longPressTimer.current = window.setTimeout(() => {
        openedByLongPress.current = true;
        suppressClick.current = true;
        openNow();
      }, TOOLTIP_LONG_PRESS_MS);
    }
  }

  function onPointerMoveTrigger(event: ReactPointerEvent) {
    if (fine || mobileMode !== 'long-press' || !longPressOrigin.current) {
      return;
    }
    const dx = event.clientX - longPressOrigin.current.x;
    const dy = event.clientY - longPressOrigin.current.y;
    if (dx * dx + dy * dy > LONG_PRESS_MOVE_PX * LONG_PRESS_MOVE_PX) {
      clearTimer(longPressTimer);
      longPressOrigin.current = null;
    }
  }

  function onPointerUpOrCancel() {
    if (fine) {
      return;
    }
    clearTimer(longPressTimer);
    longPressOrigin.current = null;
  }

  function onClickTrigger(event: React.MouseEvent) {
    if (fine) {
      return;
    }
    if (suppressClick.current || openedByLongPress.current) {
      suppressClick.current = false;
      openedByLongPress.current = false;
      event.preventDefault();
      event.stopPropagation();
      return;
    }
    if (mobileMode === 'tap') {
      if (open) {
        close();
      } else {
        openNow();
      }
    }
  }

  function onKeyDownTrigger(event: ReactKeyboardEvent) {
    if (event.key === 'Escape' && open) {
      close();
    }
  }

  function onContextMenu(event: React.MouseEvent) {
    if (
      !fine &&
      mobileMode === 'long-press' &&
      (open || longPressTimer.current != null)
    ) {
      event.preventDefault();
    }
  }

  const describedBy = open || present ? tooltipId : undefined;
  const childDescribed = child.props as { 'aria-describedby'?: string };
  const mergedDescribedBy =
    [childDescribed['aria-describedby'], describedBy]
      .filter(Boolean)
      .join(' ') || undefined;

  return (
    <>
      <span
        className="ds-tooltip-trigger"
        ref={triggerRef}
        onPointerEnter={onPointerEnter}
        onPointerLeave={onPointerLeave}
        onFocus={onFocus}
        onBlur={onBlur}
        onPointerDown={onPointerDownTrigger}
        onPointerMove={onPointerMoveTrigger}
        onPointerUp={onPointerUpOrCancel}
        onPointerCancel={onPointerUpOrCancel}
        onClick={onClickTrigger}
        onKeyDown={onKeyDownTrigger}
        onContextMenu={onContextMenu}
      >
        {cloneElement(child, {
          'aria-describedby': mergedDescribedBy,
        } as Partial<typeof child.props>)}
      </span>
      {present && placement && typeof document !== 'undefined'
        ? createPortal(
            <div
              ref={panelRef}
              id={tooltipId}
              role="tooltip"
              className="ds-tooltip"
              data-side={placement.side}
              data-state={state}
              style={placement.style}
            >
              {content}
            </div>,
            document.body,
          )
        : null}
    </>
  );
}

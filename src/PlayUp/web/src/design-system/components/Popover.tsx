import {
  useEffect,
  useLayoutEffect,
  useRef,
  useState,
  type CSSProperties,
  type ReactNode,
  type RefObject,
} from 'react';
import { createPortal } from 'react-dom';
import { useDismissLayer } from '../useDismissLayer';

export type PopoverAlign = 'start' | 'end';
export type PopoverSide = 'below' | 'above';

const GAP_PX = 8;
const VIEWPORT_PAD_PX = 16;
const CARET_SIZE_PX = 8;
/** Keep caret inset away from rounded corners. */
const CARET_EDGE_PAD_PX = 12;

export type PopoverProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** Element used for placement and “inside” hit-testing with the panel. */
  anchorRef: RefObject<HTMLElement | null>;
  children: ReactNode;
  id?: string;
  className?: string;
  'aria-label'?: string;
  role?: 'dialog' | 'menu' | 'listbox';
  /** Horizontal alignment to the anchor. Default `start`. */
  align?: PopoverAlign;
  /** Preferred panel width in CSS pixels. */
  width?: number;
  /**
   * Prefer placing below the anchor when at least this many pixels remain
   * below (otherwise flip above). Default 280.
   */
  flipThreshold?: number;
  zIndex?: number;
  /**
   * When false, Escape / outside click do not close.
   * Called as a function when a dismiss is attempted (e.g. eyedropper).
   */
  dismissEnabled?: boolean | (() => boolean);
};

function isDismissEnabled(
  dismissEnabled: boolean | (() => boolean) | undefined,
): boolean {
  if (dismissEnabled === undefined) {
    return true;
  }
  return typeof dismissEnabled === 'function'
    ? dismissEnabled()
    : dismissEnabled;
}

type Placement = {
  style: CSSProperties;
  side: PopoverSide;
};

/**
 * Anchored surface panel — portal + fixed placement + Escape / outside dismiss.
 * Presentation chrome via `.ds-popover` (including caret notch).
 */
export function Popover({
  open,
  onOpenChange,
  anchorRef,
  children,
  id,
  className,
  'aria-label': ariaLabel,
  role = 'dialog',
  align = 'start',
  width,
  flipThreshold = 280,
  zIndex = 50,
  dismissEnabled = true,
}: PopoverProps) {
  const panelRef = useRef<HTMLDivElement>(null);
  const [placement, setPlacement] = useState<Placement | undefined>();

  useLayoutEffect(() => {
    if (!open) {
      setPlacement(undefined);
      return;
    }

    function placePanel() {
      const anchor = anchorRef.current;
      if (!anchor) {
        return;
      }

      const rect = anchor.getBoundingClientRect();
      const panelWidth = Math.min(
        window.innerWidth - VIEWPORT_PAD_PX * 2,
        width ?? Math.max(rect.width, 16 * 16),
      );

      let left = align === 'end' ? rect.right - panelWidth : rect.left;
      if (left + panelWidth > window.innerWidth - VIEWPORT_PAD_PX) {
        left = Math.max(
          VIEWPORT_PAD_PX,
          window.innerWidth - VIEWPORT_PAD_PX - panelWidth,
        );
      }
      left = Math.max(VIEWPORT_PAD_PX, left);

      const spaceBelow = window.innerHeight - rect.bottom - GAP_PX;
      const preferBelow =
        spaceBelow >= flipThreshold || spaceBelow >= rect.top;
      const side: PopoverSide = preferBelow ? 'below' : 'above';

      // Point caret at the anchor center; clamp inside rounded corners.
      const anchorCenterX = rect.left + rect.width / 2;
      const caretInset = Math.min(
        panelWidth - CARET_EDGE_PAD_PX - CARET_SIZE_PX,
        Math.max(CARET_EDGE_PAD_PX, anchorCenterX - left - CARET_SIZE_PX / 2),
      );

      setPlacement({
        side,
        style: {
          position: 'fixed',
          top: preferBelow ? rect.bottom + GAP_PX : undefined,
          bottom: preferBelow
            ? undefined
            : window.innerHeight - rect.top + GAP_PX,
          left,
          width: panelWidth,
          zIndex,
          ['--ds-popover-caret-inset' as string]: `${caretInset}px`,
        },
      });
    }

    placePanel();
    window.addEventListener('resize', placePanel);
    window.addEventListener('scroll', placePanel, true);
    return () => {
      window.removeEventListener('resize', placePanel);
      window.removeEventListener('scroll', placePanel, true);
    };
  }, [open, align, width, flipThreshold, zIndex, anchorRef]);

  useDismissLayer(open, () => {
    if (!isDismissEnabled(dismissEnabled)) {
      return;
    }
    onOpenChange(false);
  });

  useEffect(() => {
    if (!open) {
      return;
    }

    function onPointerDown(event: MouseEvent) {
      if (!isDismissEnabled(dismissEnabled)) {
        return;
      }
      const target = event.target as Node;
      if (
        anchorRef.current?.contains(target) ||
        panelRef.current?.contains(target)
      ) {
        return;
      }
      onOpenChange(false);
    }

    document.addEventListener('mousedown', onPointerDown);
    return () => {
      document.removeEventListener('mousedown', onPointerDown);
    };
  }, [open, onOpenChange, dismissEnabled, anchorRef]);

  if (!open || !placement || typeof document === 'undefined') {
    return null;
  }

  return createPortal(
    <div
      ref={panelRef}
      id={id}
      className={['ds-popover', className].filter(Boolean).join(' ')}
      role={role}
      aria-label={ariaLabel}
      style={placement.style}
      data-align={align}
      data-side={placement.side}
    >
      {children}
    </div>,
    document.body,
  );
}

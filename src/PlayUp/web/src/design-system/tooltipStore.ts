/** At most one Tooltip open app-wide (Hint / DisabledReason). */

type CloseFn = () => void;

let activeClose: CloseFn | null = null;

export function claimTooltip(close: CloseFn): void {
  if (activeClose && activeClose !== close) {
    activeClose();
  }
  activeClose = close;
}

export function releaseTooltip(close: CloseFn): void {
  if (activeClose === close) {
    activeClose = null;
  }
}

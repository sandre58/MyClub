import type { ReactNode } from 'react';

/** Live minute / match indicator — pulse dot + label. */
export function LiveStatus({ children }: { children: ReactNode }) {
  return (
    <span className="ds-status-live">
      <span className="ds-live-dot" aria-hidden="true" />
      {children}
    </span>
  );
}

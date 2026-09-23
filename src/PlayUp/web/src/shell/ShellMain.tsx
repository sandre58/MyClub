import type { ReactNode } from 'react';
import { Toaster } from '../design-system/components/Toaster';

/**
 * Primary content viewport. Pages keep their own `<main id="main">` landmark.
 * Shell provides scroll + canvas background; pages fill available width (padding via `.page`).
 * Toaster is viewport-fixed (bottom-end), above Dialog overlay.
 */
export function ShellMain({ children }: { children: ReactNode }) {
  return (
    <div className="shell-main ds-shell-workspace">
      {children}
      <Toaster />
    </div>
  );
}

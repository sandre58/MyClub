import type { ReactNode } from 'react';
import { Toaster } from '../design-system/components/Toaster';

/**
 * Primary content viewport. Pages keep their own `<main id="main">` landmark.
 * Shell provides scroll + canvas background; pages fill available width (padding via `.page`).
 * Toaster is canvas-local (bottom-end), below Dialog z-index.
 */
export function ShellMain({ children }: { children: ReactNode }) {
  return (
    <div className="shell-main ds-shell-workspace">
      {children}
      <Toaster />
    </div>
  );
}

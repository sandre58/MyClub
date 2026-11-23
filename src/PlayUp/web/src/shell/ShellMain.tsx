import type { ReactNode } from 'react';
import { Toaster } from '../design-system/components/Toaster';

/**
 * Primary content viewport. Pages keep their own `<main id="main">` landmark.
 * Shell provides scroll + canvas background; pages fill available width (padding via `.page`).
 * `<Toaster />` mounts here but portals to `document.body` (z-45, above Dialog z-40).
 */
export function ShellMain({ children }: { children: ReactNode }) {
  return (
    <div className="shell-main ds-shell-workspace">
      {children}
      <Toaster />
    </div>
  );
}

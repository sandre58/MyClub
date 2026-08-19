import type { ReactNode } from 'react'

/**
 * Primary content viewport. Pages keep their own `<main id="main">` landmark.
 */
export function ShellMain({ children }: { children: ReactNode }) {
  return <div className="shell-main">{children}</div>
}

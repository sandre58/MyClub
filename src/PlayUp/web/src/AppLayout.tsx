import { AppShell } from './shell/AppShell'

/**
 * Route layout integration point. Keeps App.tsx stable while the shell evolves.
 */
export function AppLayout() {
  return <AppShell />
}

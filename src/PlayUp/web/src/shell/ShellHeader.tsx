/**
 * Shell header skeleton (14.6.1). Detailed competition context and actions
 * arrive in later sub-phases.
 */
export function ShellHeader() {
  return (
    <header className="shell-header">
      <div className="shell-header__brand">Play&apos;up</div>
      <div className="shell-header__context" aria-label="Competition context">
        <span className="ds-meta">Competition context</span>
      </div>
      <div className="shell-header__actions">
        <span className="shell-header__slot" aria-label="À traiter">
          À traiter
        </span>
        <span className="shell-header__slot" aria-label="Settings">
          Settings
        </span>
        <span className="shell-header__slot" aria-label="User">
          User
        </span>
      </div>
    </header>
  )
}

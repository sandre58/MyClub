import type { ReactNode } from 'react'

/**
 * Workspace / drill-down page head.
 * Title is always --text-display. Back + eyebrow only for object drill-downs.
 * Do not confuse with PanelHead (panel/section titles).
 */
export function PageHead({
  title,
  titleMeta,
  badges,
  actions,
  note,
  tools,
  back,
  eyebrow,
  id,
}: {
  title: ReactNode
  /** Inline meta after title (e.g. occupying count). */
  titleMeta?: ReactNode
  badges?: ReactNode
  actions?: ReactNode
  note?: ReactNode
  /** Second row — page-specific ops (Teams plateau + selection). */
  tools?: ReactNode
  back?: ReactNode
  eyebrow?: ReactNode
  id?: string
}) {
  return (
    <header className="ds-page-head">
      {back ? <div className="ds-page-head__back">{back}</div> : null}
      {eyebrow ? <p className="ds-eyebrow">{eyebrow}</p> : null}
      <div className="ds-page-head__row">
        <div className="ds-page-head__title-block">
          <h1 id={id} className="ds-page-head__title">
            {title}
            {titleMeta != null ? (
              <>
                <span className="ds-page-head__title-sep"> · </span>
                <span className="ds-page-head__title-meta ds-num">{titleMeta}</span>
              </>
            ) : null}
          </h1>
          {badges}
        </div>
        {actions ? <div className="ds-page-head__actions">{actions}</div> : null}
      </div>
      {note ? <div className="ds-page-head__note">{note}</div> : null}
      {tools ? <div className="ds-page-head__tools">{tools}</div> : null}
    </header>
  )
}

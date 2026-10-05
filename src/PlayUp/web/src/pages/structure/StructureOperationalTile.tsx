import type { ReactNode } from 'react';

export function StructureOperationalTile({
  id,
  title,
  icon,
  toolbar,
  gauge,
  notes,
  actions,
  trail,
}: {
  id: string;
  title: string;
  icon: ReactNode;
  toolbar?: ReactNode;
  gauge: ReactNode;
  notes?: ReactNode;
  actions?: ReactNode;
  /** Diagnostics / issues below action groups. */
  trail?: ReactNode;
}) {
  const titleId = `${id}-title`;
  const showActions = actions != null && actions !== false;
  const showTrail = trail != null && trail !== false;
  return (
    <section
      id={id}
      className="ds-form-section structure-operational-tile"
      aria-labelledby={titleId}
    >
      <header className="ds-form-section__head structure-operational-tile__head">
        <span className="ds-form-section__icon" aria-hidden="true">
          {icon}
        </span>
        <div className="ds-form-section__copy">
          <h3 id={titleId} className="ds-form-section__title">
            {title}
          </h3>
        </div>
        {toolbar ? (
          <div className="ds-icon-toolbar structure-operational-tile__toolbar">{toolbar}</div>
        ) : null}
      </header>
      <div className="ds-form-section__body structure-operational-tile__state">
        {gauge}
      </div>
      {notes ? <div className="structure-operational-tile__notes">{notes}</div> : null}
      {showActions ? (
        <footer className="structure-operational-tile__actions">{actions}</footer>
      ) : null}
      {showTrail ? (
        <div className="structure-operational-tile__trail">{trail}</div>
      ) : null}
    </section>
  );
}

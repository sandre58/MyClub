import type { ReactNode } from 'react';

/**
 * Panel title row — optional neutral icon (V9) + eyebrow meta on the right.
 */
export function PanelHead({
  title,
  aside,
  icon,
  id,
}: {
  title: string;
  aside?: ReactNode;
  icon?: ReactNode;
  /** For aria-labelledby on parent section. */
  id?: string;
}) {
  return (
    <div className="ds-panel-head">
      <h3 id={id} className="ds-panel-head__title">
        {icon ? (
          <span className="ds-panel-head__icon" aria-hidden="true">
            {icon}
          </span>
        ) : null}
        {title}
      </h3>
      {aside ? <span className="ds-eyebrow">{aside}</span> : null}
    </div>
  );
}

import type { ReactNode } from 'react';

export function StripSection({
  index,
  title,
  titleIcon,
  titleChipRefreshing = false,
  toolbar,
  children,
  footer,
  className,
  idPrefix = 'operational-strip',
  sectionId,
}: {
  index: number;
  title: string;
  /** When set, shown instead of the numbered chip (regulation strip). */
  titleIcon?: ReactNode;
  /** Hub background refetch: spinner in the chip slot (same footprint as the puce). */
  titleChipRefreshing?: boolean;
  toolbar?: ReactNode;
  children: ReactNode;
  footer?: ReactNode;
  className?: string;
  idPrefix?: string;
  sectionId?: string;
}) {
  const titleId = `${idPrefix}-${index}-title`;

  return (
    <section
      id={sectionId}
      className={['structure-operational-strip__section', className]
        .filter(Boolean)
        .join(' ')}
      aria-labelledby={titleId}
    >
      <header className="structure-operational-strip__section-head">
        <h4
          id={titleId}
          className="structure-operational-strip__section-title"
        >
          {titleChipRefreshing ? (
            <span
              className="structure-operational-strip__section-chip-slot"
              aria-hidden="true"
            >
              <span className="ds-spinner structure-operational-strip__section-chip-spinner" />
            </span>
          ) : titleIcon ? (
            <span
              className="structure-operational-strip__section-icon"
              aria-hidden="true"
            >
              {titleIcon}
            </span>
          ) : (
            <span className="ds-chip ds-chip--rounded ds-chip--soft" aria-hidden="true">
              {index}
            </span>
          )}
          <span>{title}</span>
        </h4>
        {toolbar ? (
          <div className="structure-operational-strip__section-toolbar ds-icon-toolbar">
            {toolbar}
          </div>
        ) : null}
      </header>
      <div className="structure-operational-strip__section-body">{children}</div>
      {footer ? (
        <div className="structure-operational-strip__section-footer">{footer}</div>
      ) : null}
    </section>
  );
}

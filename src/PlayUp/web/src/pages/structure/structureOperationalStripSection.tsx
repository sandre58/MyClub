import type { ReactNode } from 'react';

export function StripSection({
  index,
  title,
  titlePresentation = 'chip',
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
  /** Chip index (default), subsection title, or hidden (parent section titles the block). */
  titlePresentation?: 'chip' | 'subtitle' | 'hidden';
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
  const hideHead = titlePresentation === 'hidden';

  const titleLeadingSlot = titleChipRefreshing ? (
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
  ) : null;

  return (
    <section
      id={sectionId}
      className={['structure-operational-strip__section', className]
        .filter(Boolean)
        .join(' ')}
      aria-labelledby={hideHead ? undefined : titleId}
      aria-label={hideHead ? title : undefined}
    >
      {hideHead ? null : (
        <header className="structure-operational-strip__section-head">
          <h4
            id={titleId}
            className={[
              'structure-operational-strip__section-title',
              titlePresentation === 'subtitle'
                ? 'structure-operational-strip__section-title--subtitle'
                : null,
            ]
              .filter(Boolean)
              .join(' ')}
          >
            {titlePresentation === 'subtitle' ? (
              <>
                {titleLeadingSlot}
                <span>{title}</span>
              </>
            ) : titleChipRefreshing ? (
              titleLeadingSlot
            ) : titleIcon ? (
              titleLeadingSlot
            ) : (
              <span
                className="ds-chip ds-chip--rounded ds-chip--soft"
                aria-hidden="true"
              >
                {index}
              </span>
            )}
            {titlePresentation === 'subtitle' ? null : <span>{title}</span>}
          </h4>
          {toolbar ? (
            <div className="structure-operational-strip__section-toolbar ds-icon-toolbar">
              {toolbar}
            </div>
          ) : null}
        </header>
      )}
      <div className="structure-operational-strip__section-body">
        {children}
      </div>
      {footer ? (
        <div className="structure-operational-strip__section-footer">
          {footer}
        </div>
      ) : null}
    </section>
  );
}

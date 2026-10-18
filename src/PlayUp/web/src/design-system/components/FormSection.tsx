import type { ReactNode } from 'react';

export type FormSectionVariant = 'panel' | 'compact';

export type FormSectionProps = {
  title: string;
  /** Optional family hint — used in edit; rare on read hub tiles. */
  description?: string;
  /** Page-local styling only (e.g. Structure regulation binding hints). */
  descriptionClassName?: string;
  icon?: ReactNode;
  children: ReactNode;
  /** Trailing header actions (edit, binding badge, …). */
  toolbar?: ReactNode;
  footer?: ReactNode;
  /** For aria-labelledby on dialogs / landmarks. */
  id?: string;
  /** `panel` = bordered card; `compact` = typographic section (e.g. Structure regulation strip). */
  variant?: FormSectionVariant;
  className?: string;
};

/**
 * Family section chrome — shared by hub read tiles and edit dialogs.
 * Same visual language; description is optional (edit-oriented).
 */
export function FormSection({
  title,
  description,
  descriptionClassName,
  icon,
  children,
  toolbar,
  footer,
  id,
  variant = 'panel',
  className,
}: FormSectionProps) {
  const titleId = id ? `${id}-title` : undefined;
  const variantClass =
    variant === 'compact' ? 'ds-form-section--compact' : null;

  return (
    <section
      className={['ds-form-section', variantClass, className]
        .filter(Boolean)
        .join(' ')}
      id={id}
      aria-labelledby={titleId}
    >
      <header className="ds-form-section__head">
        {icon ? (
          <span className="ds-form-section__icon" aria-hidden="true">
            {icon}
          </span>
        ) : null}
        <div className="ds-form-section__copy">
          <h3 id={titleId} className="ds-form-section__title">
            {title}
          </h3>
          {description ? (
            <p
              className={[
                'ds-form-section__description',
                descriptionClassName,
              ]
                .filter(Boolean)
                .join(' ')}
            >
              {description}
            </p>
          ) : null}
        </div>
        {toolbar ? (
          <div className="ds-form-section__toolbar">{toolbar}</div>
        ) : null}
      </header>
      <div className="ds-form-section__body">{children}</div>
      {footer ? <div className="ds-form-section__footer">{footer}</div> : null}
    </section>
  );
}

import type { ReactNode } from 'react';

export type FormSectionProps = {
  title: string;
  /** Optional family hint — used in edit; rare on read hub tiles. */
  description?: string;
  icon?: ReactNode;
  children: ReactNode;
  /** For aria-labelledby on dialogs / landmarks. */
  id?: string;
  className?: string;
};

/**
 * Family section chrome — shared by hub read tiles and edit dialogs.
 * Same visual language; description is optional (edit-oriented).
 */
export function FormSection({
  title,
  description,
  icon,
  children,
  id,
  className,
}: FormSectionProps) {
  const titleId = id ? `${id}-title` : undefined;

  return (
    <section
      className={['ds-form-section', className].filter(Boolean).join(' ')}
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
            <p className="ds-form-section__description">{description}</p>
          ) : null}
        </div>
      </header>
      <div className="ds-form-section__body">{children}</div>
    </section>
  );
}

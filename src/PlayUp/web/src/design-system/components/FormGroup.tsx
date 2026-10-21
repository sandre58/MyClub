import type { ReactNode } from 'react';

export type FormGroupProps = {
  title: string;
  /** Optional short hint under the subsection title. */
  description?: string;
  children: ReactNode;
  className?: string;
};

/**
 * Subsection chrome inside a FormSection — title is structural, not a Field label.
 */
export function FormGroup({
  title,
  description,
  children,
  className,
}: FormGroupProps) {
  return (
    <div className={['ds-form-group', className].filter(Boolean).join(' ')}>
      <div className="ds-form-group__head">
        <h4 className="ds-form-group__title">{title}</h4>
        {description ? (
          <p className="ds-form-group__description">{description}</p>
        ) : null}
      </div>
      <div className="ds-form-group__body">{children}</div>
    </div>
  );
}

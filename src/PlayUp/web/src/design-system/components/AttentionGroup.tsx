import type { ReactNode } from 'react';
import { PanelHead } from './PanelHead';

export function AttentionGroup({
  label,
  count,
  children,
  heading,
  headingId,
  icon,
}: {
  /** Eyebrow label (Lab). */
  label?: string;
  count?: number;
  children: ReactNode;
  /** Accessible section title (product). */
  heading?: string;
  headingId?: string;
  icon?: ReactNode;
}) {
  const eyebrow =
    label != null
      ? count != null && count > 0
        ? `${label} · ${count}`
        : label
      : undefined;

  return (
    <section
      className="ds-group ds-overview-attention"
      aria-label={heading ? undefined : label}
      aria-labelledby={headingId}
    >
      {heading ? (
        <PanelHead id={headingId} title={heading} icon={icon} />
      ) : eyebrow ? (
        <span className="ds-eyebrow">{eyebrow}</span>
      ) : null}
      {children}
    </section>
  );
}

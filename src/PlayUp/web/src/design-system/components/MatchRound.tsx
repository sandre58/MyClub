import type { ReactNode } from 'react';

export function MatchRound({
  id,
  label,
  date,
  status,
  sub,
  children,
  className,
}: {
  id?: string;
  label: ReactNode;
  date?: ReactNode;
  status?: ReactNode;
  sub?: ReactNode;
  children: ReactNode;
  className?: string;
}) {
  const classes = ['ds-match-round', className].filter(Boolean).join(' ');

  return (
    <section className={classes} aria-labelledby={id}>
      <div className="ds-match-round__head">
        {typeof label === 'string' ? (
          <span id={id} className="ds-match-round__label">
            {label}
          </span>
        ) : (
          label
        )}
        {date != null ? (
          <span className="ds-match-round__date">{date}</span>
        ) : null}
        {status}
      </div>
      {sub != null ? <p className="ds-match-round__sub">{sub}</p> : null}
      <div>{children}</div>
    </section>
  );
}

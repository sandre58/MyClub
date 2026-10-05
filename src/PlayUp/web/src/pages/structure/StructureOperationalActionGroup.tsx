import type { ReactNode } from 'react';

export function StructureOperationalActionGroup({
  label,
  labelId,
  suffix,
  rowClassName,
  children,
}: {
  label: string;
  labelId: string;
  suffix?: ReactNode;
  rowClassName?: string;
  children: ReactNode;
}) {
  return (
    <div className="structure-operational-tile__group" role="group" aria-labelledby={labelId}>
      <h4 id={labelId} className="structure-operational-tile__group-label">
        {label}
      </h4>
      <div
        className={['structure-operational-tile__group-row', rowClassName]
          .filter(Boolean)
          .join(' ')}
      >
        {children}
      </div>
      {suffix ? <div className="structure-operational-tile__group-suffix">{suffix}</div> : null}
    </div>
  );
}

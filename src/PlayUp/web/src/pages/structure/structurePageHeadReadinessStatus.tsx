import type { ReactNode } from 'react';

import { Status, type StatusTone } from '../../design-system/components/Status';

/** Structure hub PageHead — global readiness pill (DS Status, no page-local chrome). */
export function StructurePageHeadReadinessStatus({
  tone,
  icon,
  children,
  id = 'readiness-heading',
}: {
  tone: StatusTone;
  icon: ReactNode;
  children: ReactNode;
  id?: string;
}) {
  return (
    <Status
      id={id}
      density="context"
      variant="outline"
      shape="pill"
      tone={tone}
      role="status"
    >
      <span className="ds-status-inline">
        <span className="ds-status-inline__icon" aria-hidden="true">
          {icon}
        </span>
        <span>{children}</span>
      </span>
    </Status>
  );
}

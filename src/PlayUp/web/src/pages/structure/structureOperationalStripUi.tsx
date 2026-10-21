import type { ReactNode } from 'react';

import {
  CheckCircleIcon,
  DrawPendingIcon,
  InfoIcon,
  OverviewAttentionIcon,
  StructureIssueIcon,
} from '../../design-system/icons/contentIcons';
import type { IconSize } from '../../design-system/icons/Icon';
import type { StructureDiagnosticTone } from './structureDiagnosticPresentation';
import { StructureDiagnosticIssueIcon } from './StructureDiagnosticIssueIcon';

export type StripOperationalMessageKind =
  'info' | 'success' | 'attention' | 'danger';

export function diagnosticToneToStripMessageKind(
  tone: StructureDiagnosticTone,
): StripOperationalMessageKind {
  switch (tone) {
    case 'attention':
      return 'attention';
    case 'danger':
      return 'danger';
    default:
      return 'info';
  }
}

function defaultMessageIcon(
  kind: StripOperationalMessageKind,
  size: IconSize = 'sm',
) {
  switch (kind) {
    case 'success':
      return <CheckCircleIcon size={size} aria-hidden="true" />;
    case 'attention':
      return <OverviewAttentionIcon size={size} aria-hidden="true" />;
    case 'danger':
      return <StructureIssueIcon size={size} aria-hidden="true" />;
    default:
      return <InfoIcon size={size} aria-hidden="true" />;
  }
}

export function StripOperationalMessage({
  kind = 'info',
  icon,
  children,
}: {
  kind?: StripOperationalMessageKind;
  icon?: ReactNode;
  children: ReactNode;
}) {
  const role = kind === 'info' || kind === 'success' ? undefined : 'status';

  return (
    <div
      className="structure-operational-strip__message"
      data-kind={kind}
      role={role}
    >
      <span
        className="structure-operational-strip__message-icon"
        aria-hidden="true"
      >
        {icon ?? defaultMessageIcon(kind)}
      </span>
      <span className="structure-operational-strip__message-text">
        {children}
      </span>
    </div>
  );
}

export function StripOperationalDiagnosticMessage({
  tone,
  children,
}: {
  tone: StructureDiagnosticTone;
  children: ReactNode;
}) {
  return (
    <StripOperationalMessage
      kind={diagnosticToneToStripMessageKind(tone)}
      icon={<StructureDiagnosticIssueIcon tone={tone} size="sm" />}
    >
      {children}
    </StripOperationalMessage>
  );
}

/** Tirage appliqué — feature mark + copy/actions (not the inline meter message row). */
export function StripDrawAppliedPanel({
  message,
  action,
}: {
  message: ReactNode;
  action: ReactNode;
}) {
  return (
    <div className="structure-operational-strip__applied-row">
      <div
        className="structure-operational-strip__applied-mark"
        aria-hidden="true"
      >
        <DrawPendingIcon size="lg" />
      </div>
      <div className="structure-operational-strip__applied-content">
        <StripOperationalMessage kind="info">{message}</StripOperationalMessage>
        {action}
      </div>
    </div>
  );
}

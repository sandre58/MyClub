import {
  IncompleteStageIssueIcon,
  OverviewAttentionIcon,
  StructureIssueIcon,
} from '../../../design-system/icons/contentIcons';
import type { IconSize } from '../../../design-system/icons/Icon';
import type { StructureDiagnosticTone } from './structureDiagnosticPresentation';

export function StructureDiagnosticIssueIcon({
  tone,
  size = 'sm',
}: {
  tone: StructureDiagnosticTone;
  size?: IconSize;
}) {
  switch (tone) {
    case 'progress':
      return <IncompleteStageIssueIcon size={size} aria-hidden="true" />;
    case 'attention':
      return <OverviewAttentionIcon size={size} aria-hidden="true" />;
    default:
      return <StructureIssueIcon size={size} aria-hidden="true" />;
  }
}

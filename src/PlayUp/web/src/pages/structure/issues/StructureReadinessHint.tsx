import { useTranslation } from 'react-i18next';
import {
  OverviewAttentionIcon,
  StructureIssueIcon,
} from '../../../design-system/icons/contentIcons';
import { structureReadinessHintTitle } from './structureAnomalyHintCopy';
import { StructurePageHeadReadinessStatus } from '../structurePageHeadReadinessStatus';

/** PageHead readiness signal — existence only (no catalogue, no CTAs). */
export function StructureReadinessHint({
  severity,
}: {
  severity: 'error' | 'warning';
}) {
  const { t } = useTranslation('structure');
  const title = structureReadinessHintTitle({ t, severity });
  const isError = severity === 'error';

  return (
    <StructurePageHeadReadinessStatus
      tone={isError ? 'error' : 'attention'}
      icon={
        isError ? (
          <StructureIssueIcon size="sm" />
        ) : (
          <OverviewAttentionIcon size="sm" />
        )
      }
    >
      {title}
    </StructurePageHeadReadinessStatus>
  );
}

/** @deprecated Use StructureReadinessHint severity="error" */
export function StructureAnomalyHint() {
  return <StructureReadinessHint severity="error" />;
}

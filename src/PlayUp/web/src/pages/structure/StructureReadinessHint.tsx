import { useTranslation } from 'react-i18next';
import {
  OverviewAttentionIcon,
  StructureIssueIcon,
} from '../../design-system/icons/contentIcons';
import { structureReadinessHintTitle } from './structureAnomalyHintCopy';

/** PageHead readiness signal — existence only (no catalogue, no CTAs). */
export function StructureReadinessHint({
  severity,
  pausedStructureCopy = false,
}: {
  severity: 'error' | 'warning';
  pausedStructureCopy?: boolean;
}) {
  const { t } = useTranslation('structure');
  const title = structureReadinessHintTitle({
    t,
    severity,
    pausedStructureCopy,
  });
  const isError = severity === 'error';

  return (
    <div className="structure-status" role="status">
      <p
        id="readiness-heading"
        className={
          isError
            ? 'structure-status__title structure-status__title--hint-negative'
            : 'structure-status__title structure-status__title--hint-attention'
        }
      >
        {isError ? (
          <StructureIssueIcon size="sm" aria-hidden="true" />
        ) : (
          <OverviewAttentionIcon size="sm" aria-hidden="true" />
        )}
        <span>{title}</span>
      </p>
    </div>
  );
}

/** @deprecated Use StructureReadinessHint severity="error" */
export function StructureAnomalyHint() {
  return <StructureReadinessHint severity="error" />;
}

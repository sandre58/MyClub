import { useTranslation } from 'react-i18next';
import { StructureIssueIcon } from '../../design-system/icons/contentIcons';
import { structureAnomalyHintTitle } from './structureAnomalyHintCopy';

/** PageHead negative readiness — existence signal only (no catalogue, no CTAs). */
export function StructureAnomalyHint() {
  const { t } = useTranslation('structure');
  const title = structureAnomalyHintTitle({ t });

  return (
    <div className="structure-status" role="status">
      <p
        id="readiness-heading"
        className="structure-status__title structure-status__title--hint-negative"
      >
        <StructureIssueIcon size="sm" aria-hidden="true" />
        <span>{title}</span>
      </p>
    </div>
  );
}

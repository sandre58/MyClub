import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { fetchNeedsAttention } from '../../api';
import { StructureIssueIcon } from '../../design-system/icons/contentIcons';
import { queryKeys } from '../../queryKeys';
import { structureAnomalyHintTitle } from './structureAnomalyHintCopy';

type StructureAnomalyHintProps = {
  competitionId: string;
  /** When `/attention` fails, still show generic copy if Structure synthesis had gaps. */
  allowGenericOnError?: boolean;
};

/**
 * PageHead negative readiness — existence signal only (no catalogue, no CTAs).
 * Count uses the same source as the shell bell (`GET /attention`).
 */
export function StructureAnomalyHint({
  competitionId,
  allowGenericOnError = false,
}: StructureAnomalyHintProps) {
  const { t } = useTranslation('structure');

  const attentionQuery = useQuery({
    queryKey: queryKeys.competitions.attention(competitionId),
    queryFn: () => fetchNeedsAttention(competitionId),
    enabled: competitionId.length > 0,
    staleTime: 0,
  });

  if (attentionQuery.isPending) {
    return null;
  }

  const items = attentionQuery.data?.items ?? [];
  const count = attentionQuery.data?.count ?? items.length;

  if (attentionQuery.isSuccess) {
    if (count <= 0) {
      return null;
    }
  } else if (!allowGenericOnError) {
    return null;
  }

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

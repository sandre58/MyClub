import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { Navigate, useParams } from 'react-router-dom';
import { fetchStageOverview } from '../../api';
import { WaitMark } from '../../design-system/components/WaitMark';
import { queryKeys } from '../../queryKeys';
import { ErrorState } from '../../ui';
import { structureDeepLink } from '../structure/structureNavigation';

/**
 * Legacy `/stages/:stageId` URLs redirect to Structure when the stage is bound
 * to a competition. Unbound stages have no Structure hub — show a read error.
 */
export function StageLegacyRedirect() {
  const { stageId = '' } = useParams();
  const { t } = useTranslation('common');

  const stageQuery = useQuery({
    queryKey: queryKeys.stages.detail(stageId),
    queryFn: () => fetchStageOverview(stageId),
    enabled: stageId.length > 0,
  });

  if (stageQuery.isPending) {
    return <WaitMark>{t('loading')}</WaitMark>;
  }

  if (stageQuery.isError) {
    return <ErrorState error={stageQuery.error} />;
  }

  const competitionId = stageQuery.data.competitionId?.trim();
  if (!competitionId) {
    return <Navigate to="/" replace />;
  }

  return (
    <Navigate to={structureDeepLink({ competitionId, stageId })} replace />
  );
}

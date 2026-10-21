import { useTranslation } from 'react-i18next';

import { PanelHead } from '../../design-system/components/PanelHead';
import { TextLink } from '../../design-system/components/TextLink';
import { CreateMatchesIcon } from '../../design-system/icons/contentIcons';
import type { StructureStageHubSummary } from '../../types';
import { showViewMaterializedMatchesLink } from './structureMaterializeCopy';

export function StructurePhaseMatchTilePanel({
  stage,
  iconRefreshing = false,
}: {
  stage: StructureStageHubSummary;
  iconRefreshing?: boolean;
}) {
  const { t } = useTranslation('structure');
  const materialize = stage.materialize;

  const viewMatchesLink = showViewMaterializedMatchesLink(
    stage,
    materialize ?? undefined,
  ) ? (
    <TextLink to={`/stages/${stage.stageId}/matches`}>
      {t('fiche.materialize.viewPhaseMatches')}
    </TextLink>
  ) : null;

  return (
    <PanelHead
      id="aside-matchs-heading"
      density="inset"
      titleStyle="section"
      icon={<CreateMatchesIcon size="sm" />}
      iconRefreshing={iconRefreshing}
      title={t('fiche.operational.cluster.matches')}
      actions={
        viewMatchesLink ? (
          <div className="structure-phase-match-tile__head-actions">
            {viewMatchesLink}
          </div>
        ) : undefined
      }
      className="structure-phase-match-tile__head"
    />
  );
}

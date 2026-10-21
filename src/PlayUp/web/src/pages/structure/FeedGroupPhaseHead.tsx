import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { ViewIcon } from '../../design-system/icons/contentIcons';
import type { FeedGroup } from './structurePhaseFeeds';

export function FeedGroupPhaseHead({
  group,
  onViewPhase,
  renderGroupAction,
}: {
  group: FeedGroup;
  onViewPhase?: (peerId: string) => void;
  renderGroupAction?: (group: FeedGroup) => ReactNode;
}) {
  const { t } = useTranslation('structure');

  return (
    <div className="structure-feed-group__phase-head">
      <h4 className="structure-feed-group__phase-title">{group.peerName}</h4>
      <div className="structure-feed-group__phase-trail">
        {renderGroupAction ? (
          <div className="structure-feed-group__phase-actions">
            {renderGroupAction(group)}
          </div>
        ) : null}
        {onViewPhase ? (
          <button
            type="button"
            className="ds-text-link ds-text-link--icon"
            aria-label={t('composition.qualifiers.viewPhase')}
            onClick={() => onViewPhase(group.peerId)}
          >
            <ViewIcon size="sm" aria-hidden="true" />
          </button>
        ) : null}
      </div>
    </div>
  );
}

import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { ChevronRightIcon } from '../../design-system/icons/shellIcons';
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
            className="ds-text-link"
            onClick={() => onViewPhase(group.peerId)}
          >
            <span>{t('composition.qualifiers.viewPhase')}</span>
            <span className="ds-text-link__arrow" aria-hidden="true">
              <ChevronRightIcon size="sm" />
            </span>
          </button>
        ) : null}
      </div>
    </div>
  );
}

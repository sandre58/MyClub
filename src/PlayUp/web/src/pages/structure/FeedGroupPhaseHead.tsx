import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Tooltip } from '../../design-system/components/Tooltip';
import { ViewIcon } from '../../design-system/icons/contentIcons';
import type { FeedGroup } from './structurePhaseFeeds';

const compactIcon =
  'ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact';

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
          <Tooltip content={t('composition.qualifiers.viewPhase')}>
            <a
              href="#"
              className={compactIcon}
              aria-label={t('composition.qualifiers.viewPhase')}
              onClick={(event) => {
                event.preventDefault();
                onViewPhase(group.peerId);
              }}
            >
              <ViewIcon size="sm" />
            </a>
          </Tooltip>
        ) : null}
      </div>
    </div>
  );
}

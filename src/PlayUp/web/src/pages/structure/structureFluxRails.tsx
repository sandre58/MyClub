// Structure flux rails — Sorties / population list chrome.
import type { ReactNode } from 'react';
import { Chip } from '../../design-system/components/Chip';
import { PanelHead } from '../../design-system/components/PanelHead';
import { CompositionQualifierRuleLabel } from './CompositionQualifierRuleLabel';
import { FeedGroupPhaseHead } from './FeedGroupPhaseHead';
import { type FeedGroup } from './structurePhaseFeeds';

export function FluxRuleRow({
  badge,
  badgeTone,
  context,
  extra,
  badgeAsText = false,
}: {
  badge: string;
  badgeTone: 'win' | 'loss' | 'neutral' | 'accent';
  context: string;
  extra?: string;
  badgeAsText?: boolean;
}) {
  return (
    <span className="structure-flux-rule">
      <span className="structure-flux-rule__source">
        {badgeAsText ? (
          <span className="structure-flux-rule__who">{badge}</span>
        ) : (
          <Chip tone={badgeTone}>{badge}</Chip>
        )}
        {context ? (
          <span className="structure-flux-rule__context">{context}</span>
        ) : null}
      </span>
      {extra ? (
        <>
          <span className="structure-flux-rule__arrow" aria-hidden="true">
            →
          </span>
          <Chip tone="neutral">{extra}</Chip>
        </>
      ) : null}
    </span>
  );
}

export function PlacementAwardRow({
  rank,
  medal,
  badge,
  badgeTone,
  context,
}: {
  rank: number;
  medal: 'gold' | 'silver' | 'bronze' | null;
  badge: string;
  badgeTone: 'win' | 'loss';
  context: string;
}) {
  return (
    <span className="structure-flux-rule">
      <span
        className={[
          'structure-place-rank',
          medal ? `structure-place-rank--${medal}` : null,
        ]
          .filter(Boolean)
          .join(' ')}
      >
        {rank}
      </span>
      <Chip tone={badgeTone}>{badge}</Chip>
      <span className="structure-flux-rule__context">{context}</span>
    </span>
  );
}

export function FluxGroupList({
  groups,
  onOpenPeer,
  renderGroupAction,
}: {
  groups: FeedGroup[];
  onOpenPeer?: (peerId: string) => void;
  /** Compact control in the group head (e.g. edit exits on source). */
  renderGroupAction?: (group: FeedGroup) => ReactNode;
}) {
  return (
    <ul className="structure-flux-groups">
      {groups.map((group) => (
        <li key={group.peerId} className="structure-flux-group">
          <FeedGroupPhaseHead
            group={group}
            onViewPhase={onOpenPeer}
            renderGroupAction={renderGroupAction}
          />
          <ul className="structure-feed-group__rules">
            {group.rules.map((rule) => (
              <li key={rule.key} className="structure-feed-group__rule-line">
                <CompositionQualifierRuleLabel rule={rule} />
              </li>
            ))}
          </ul>
        </li>
      ))}
    </ul>
  );
}

export function FluxRail({
  id,
  title,
  icon,
  editControl,
  children,
  side,
  contentEmpty = false,
  emptyState,
  attentionBanner,
}: {
  id: string;
  title: string;
  icon: ReactNode;
  editControl?: ReactNode;
  children?: ReactNode;
  side: 'in' | 'out';
  /** When true, shows `emptyState` instead of `children`. */
  contentEmpty?: boolean;
  emptyState?: ReactNode;
  /** Diagnostic banners owned by this rail (no container chrome). */
  attentionBanner?: ReactNode;
}) {
  const titleId = `${id}-title`;
  return (
    <aside
      id={id}
      className={['structure-phase-rail', `structure-phase-rail--${side}`]
        .filter(Boolean)
        .join(' ')}
      aria-labelledby={titleId}
    >
      <PanelHead
        id={titleId}
        density="inset"
        titleStyle="section"
        icon={icon}
        title={title}
        actions={editControl}
        className="structure-flux-rail__head"
      />
      <div className="structure-phase-rail__body">
        {contentEmpty && emptyState ? (
          <div className="structure-phase-rail__empty">{emptyState}</div>
        ) : (
          children
        )}
        {attentionBanner}
      </div>
    </aside>
  );
}

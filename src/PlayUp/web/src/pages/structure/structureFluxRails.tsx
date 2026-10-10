// Structure flux rails — Sorties / population list chrome.
import type { ReactNode } from 'react';
import { Chip } from '../../design-system/components/Chip';
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
  teamsLabel,
  renderGroupAction,
}: {
  groups: FeedGroup[];
  onOpenPeer?: (peerId: string) => void;
  teamsLabel: (count: number) => string;
  /** Compact control in the group head (e.g. edit exits on source). */
  renderGroupAction?: (group: FeedGroup) => ReactNode;
}) {
  return (
    <ul className="structure-flux-groups">
      {groups.map((group) => (
        <li key={group.peerId} className="structure-flux-group">
          <div className="structure-flux-group__head">
            {onOpenPeer ? (
              <button
                type="button"
                className="structure-flux-group__peer"
                onClick={() => onOpenPeer(group.peerId)}
              >
                {group.peerName}
              </button>
            ) : (
              <span className="structure-flux-group__peer">
                {group.peerName}
              </span>
            )}
            <div className="structure-flux-group__head-trail">
              <Chip tone="neutral">{teamsLabel(group.volume)}</Chip>
              {renderGroupAction?.(group)}
            </div>
          </div>
          <ul className="structure-flux-group__rules">
            {group.rules.map((rule) => (
              <li key={rule.key} className="structure-flux-group__rule">
                <FluxRuleRow
                  badge={rule.badge}
                  badgeTone={rule.badgeTone}
                  context={rule.context}
                  extra={rule.extra}
                  badgeAsText={rule.badgeAsText}
                />
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
  empty = false,
  attentionBanner,
}: {
  id: string;
  title: string;
  icon: ReactNode;
  editControl?: ReactNode;
  children?: ReactNode;
  side: 'in' | 'out';
  /** When true: header only — body omitted so it consumes no vertical space. */
  empty?: boolean;
  /** Diagnostic banners owned by this rail (no container chrome). */
  attentionBanner?: ReactNode;
}) {
  const titleId = `${id}-title`;
  return (
    <aside
      id={id}
      className={[
        'structure-phase-rail',
        `structure-phase-rail--${side}`,
        empty ? 'structure-phase-rail--empty' : null,
      ]
        .filter(Boolean)
        .join(' ')}
      aria-labelledby={titleId}
    >
      <header className="structure-phase-rail__head">
        <span className="structure-phase-rail__icon" aria-hidden="true">
          {icon}
        </span>
        <h3 id={titleId} className="structure-phase-rail__title">
          {title}
        </h3>
        {editControl}
      </header>
      {empty ? null : (
        <div className="structure-phase-rail__body">
          {children}
          {attentionBanner}
        </div>
      )}
    </aside>
  );
}

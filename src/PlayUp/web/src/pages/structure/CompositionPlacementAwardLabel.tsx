import { CompositionQualifierRuleLabel } from './CompositionQualifierRuleLabel';
import type { FeedGroup } from './structurePhaseFeeds';

type QualifierRule = FeedGroup['rules'][number];

export function CompositionPlacementAwardLabel({
  rank,
  medal,
  rule,
}: {
  rank: number;
  medal: 'gold' | 'silver' | 'bronze' | null;
  rule: QualifierRule;
}) {
  return (
    <span className="structure-feed-group__placement-rule">
      <span
        className={[
          'structure-place-rank',
          'structure-feed-group__placement-rank',
          medal ? `structure-place-rank--${medal}` : null,
        ]
          .filter(Boolean)
          .join(' ')}
        aria-hidden="true"
      >
        {rank}
      </span>
      <CompositionQualifierRuleLabel rule={rule} />
    </span>
  );
}

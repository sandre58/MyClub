import type { FeedGroup } from './structurePhaseFeeds';

type QualifierRule = FeedGroup['rules'][number];

export function CompositionQualifierRuleLabel({ rule }: { rule: QualifierRule }) {
  const leadClass = [
    'structure-composition-qualifiers__rule-lead',
    rule.badgeTone === 'win'
      ? 'structure-composition-qualifiers__rule-lead--win'
      : rule.badgeTone === 'loss'
        ? 'structure-composition-qualifiers__rule-lead--loss'
        : 'structure-composition-qualifiers__rule-lead--primary',
  ].join(' ');

  const tailParts = [rule.context, rule.extra].filter(Boolean);

  return (
    <span className="structure-composition-qualifiers__rule-label">
      <span className={leadClass}>{rule.badge}</span>
      {tailParts.length > 0 ? (
        <>
          <span className="structure-composition-qualifiers__rule-sep" aria-hidden="true">
            ·
          </span>
          <span className="structure-composition-qualifiers__rule-tail">
            {tailParts.join(' · ')}
          </span>
        </>
      ) : null}
    </span>
  );
}

import { Tooltip } from '../../../design-system/components/Tooltip';
import { StageFormatGlyph } from '../structureFormatGlyph';
import type { StructureFormatKind } from '../../../types';
import type { FeedGroup } from './structurePhaseFeeds';

type QualifierRule = FeedGroup['rules'][number];

export function CompositionQualifierRuleLabel({
  rule,
  sourcePhaseName,
  sourcePhaseFormatKind,
}: {
  rule: QualifierRule;
  /** Équipes band — source phase glyph + tooltip (Qualifiés keeps phase header only). */
  sourcePhaseName?: string | null;
  sourcePhaseFormatKind?: StructureFormatKind | null;
}) {
  const leadClass = [
    'structure-feed-group__rule-lead',
    rule.badgeTone === 'win'
      ? 'structure-feed-group__rule-lead--win'
      : rule.badgeTone === 'loss'
        ? 'structure-feed-group__rule-lead--loss'
        : 'structure-feed-group__rule-lead--primary',
  ].join(' ');

  const tailParts = [rule.context, rule.extra].filter(Boolean);
  const phase = sourcePhaseName?.trim() ?? '';

  return (
    <span className="structure-feed-group__rule-label">
      {phase ? (
        <>
          <span className="structure-feed-group__rule-source">
            <Tooltip content={phase}>
              <span
                className="structure-feed-group__rule-source-trigger"
                aria-label={phase}
                tabIndex={0}
              >
                <StageFormatGlyph kind={sourcePhaseFormatKind} size="sm" />
              </span>
            </Tooltip>
          </span>
          <span className="structure-feed-group__rule-sep" aria-hidden="true">
            ·
          </span>
        </>
      ) : null}
      <span className={leadClass}>{rule.badge}</span>
      {tailParts.length > 0 ? (
        <>
          <span className="structure-feed-group__rule-sep" aria-hidden="true">
            ·
          </span>
          <span className="structure-feed-group__rule-tail">
            {tailParts.join(' · ')}
          </span>
        </>
      ) : null}
    </span>
  );
}
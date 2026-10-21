import { Link } from 'react-router-dom';
import { ChevronRightIcon } from '../design-system/icons/shellIcons';
import { situationPresentation } from './functionalProblemPresentation';
import {
  attentionSituationMeta,
  attentionSituationTitle,
} from './attentionRowCopy';
import { organizerProblemPresentationCode } from './organizerProblemRead';
import type { CompetitionStageSummary, OverviewSituation } from '../types';

type AttentionSituationRowProps = {
  item: OverviewSituation;
  competitionId: string;
  stages?: readonly CompetitionStageSummary[];
  onNavigate?: () => void;
};

/**
 * Product Needs-attention row (drawer + Overview preview).
 * Distinct from DS `AttentionRow` (D9 count+icon Lab recipe) — do not merge.
 * Hover A via `.ds-interactive-row` — no rest fill, no pills.
 * Meta: prefer situation description when available; else targetType label.
 * Href via Phase 1 `situationPresentation` (same identity / SoT as Overview).
 */
export function AttentionSituationRow({
  item,
  competitionId,
  stages = [],
  onNavigate,
}: AttentionSituationRowProps) {
  const presentation = situationPresentation(item, competitionId, stages);
  const href = presentation.sotHref;
  const isBlocking = item.nature === 'Blocking';
  const titleSource = organizerProblemPresentationCode(
    item.problem,
    item.source,
  );
  const meta = attentionSituationMeta(item, stages);
  const toneClass = isBlocking
    ? 'shell-attention-drawer__row--blocking'
    : 'shell-attention-drawer__row--attention';

  const content = (
    <>
      <span className="shell-attention-drawer__row-main">
        <span className="shell-attention-drawer__row-title">
          {attentionSituationTitle(presentation, titleSource, item.params)}
        </span>
        {meta ? (
          <span className="shell-attention-drawer__row-meta ds-meta">
            {meta}
          </span>
        ) : null}
      </span>
      {href ? (
        <ChevronRightIcon
          size="md"
          className="ds-interactive-row__chevron"
          aria-hidden="true"
        />
      ) : null}
    </>
  );

  return (
    <li className="shell-attention-drawer__item">
      {href ? (
        <Link
          className={`ds-interactive-row shell-attention-drawer__row ${toneClass}`}
          to={href}
          onClick={onNavigate}
        >
          {content}
        </Link>
      ) : (
        <div className={`shell-attention-drawer__row ${toneClass}`}>
          {content}
        </div>
      )}
    </li>
  );
}

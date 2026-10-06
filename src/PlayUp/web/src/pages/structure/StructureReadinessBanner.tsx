import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import {
  ArrowRightIcon,
  OverviewAttentionIcon,
  StructureIssueIcon,
} from '../../design-system/icons/contentIcons';
import { ChevronRightIcon } from '../../design-system/icons/shellIcons';
import type { StructureStageHubSummary, StructureView } from '../../types';
import type { StructureDiagnosticTone } from './structureDiagnosticPresentation';
import {
  collectStructureReadinessActionItems,
  type StructureReadinessActionItem,
} from './structureReadinessActions';
import type { StructureReadinessSummary } from './structureReadinessSummary';

function actionButtonClass(tone: StructureDiagnosticTone): string {
  const base = 'ds-btn ds-btn--sm structure-status__action-link';
  if (tone === 'danger') {
    return `${base} ds-btn--destructive`;
  }
  /* Banner CTAs: outline attention matches band chrome (progress ≠ neutral secondary). */
  return `${base} ds-btn--attention`;
}

function ReadinessActionRow({
  item,
  showStageName,
  isLast,
}: {
  item: StructureReadinessActionItem;
  showStageName: boolean;
  isLast: boolean;
}) {
  const { t } = useTranslation('structure');
  const issueLabel = t(item.titleKey, { defaultValue: item.code });
  const actionLabel = t(item.actionLabelKey ?? `readiness.action.${item.cta}`);
  const rowLabel = showStageName
    ? t('readiness.actionRowWithPhase', {
        issue: issueLabel,
        phase: item.stageName,
      })
    : issueLabel;

  return (
    <li
      className="structure-status__action-row"
      data-tone={item.tone}
      data-last={isLast ? 'true' : undefined}
    >
      <div className="structure-status__action-body">
        <span className="structure-status__action-marker" aria-hidden="true">
          <ChevronRightIcon size="sm" />
        </span>
        <span className="structure-status__action-summary">{rowLabel}</span>
        <Link
          className={actionButtonClass(item.tone)}
          data-tone={item.tone}
          to={item.href}
          aria-label={t('readiness.actionAria', {
            issue: issueLabel,
            phase: item.stageName,
            action: actionLabel,
          })}
        >
          <span>{actionLabel}</span>
          <ArrowRightIcon size="sm" />
        </Link>
      </div>
    </li>
  );
}

export function StructureReadinessBanner({
  data,
  stages,
  summary,
  selectedStageId,
}: {
  data: StructureView;
  stages: StructureStageHubSummary[];
  summary: StructureReadinessSummary;
  selectedStageId: string | null;
}) {
  const { t } = useTranslation('structure');

  const actions = useMemo(
    () => collectStructureReadinessActionItems(data, stages),
    [data, stages],
  );

  const showStageOnRows =
    stages.length > 1 ||
    (actions.length > 0 &&
      selectedStageId != null &&
      actions.some((item) => item.stageId !== selectedStageId));

  return (
    <div
      className="structure-status structure-status--banner structure-status--banner-compact"
      data-tone={summary.headerTone === 'danger' ? 'danger' : 'attention'}
      role="status"
    >
      <div className="structure-status__copy">
        <div className="structure-status__headline">
          {summary.headerTone === 'danger' ? (
            <StructureIssueIcon size="sm" aria-hidden="true" />
          ) : (
            <OverviewAttentionIcon size="sm" aria-hidden="true" />
          )}
          <p id="readiness-heading" className="structure-status__title">
            {t('readiness.problemsToHandle', {
              count: summary.totalProblemCount,
            })}
          </p>
        </div>

        {actions.length > 0 ? (
          <ul
            className="structure-status__actions"
            aria-label={t('readiness.actionsAria')}
          >
            {actions.map((item, index) => (
              <ReadinessActionRow
                key={item.id}
                item={item}
                showStageName={showStageOnRows}
                isLast={index === actions.length - 1}
              />
            ))}
          </ul>
        ) : null}
      </div>
    </div>
  );
}

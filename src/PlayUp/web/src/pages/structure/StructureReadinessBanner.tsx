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
import { structureReadinessBannerHeadline } from './structureReadinessBannerCopy';
import type { StructureReadinessSummary } from './structureReadinessSummary';
import { structureDeepLink } from './structureNavigation';

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

  const attentionStage =
    summary.firstAttentionStageId != null
      ? stages.find((s) => s.stageId === summary.firstAttentionStageId)
      : undefined;

  const showViewAttentionPhase =
    attentionStage != null &&
    (actions.length === 0 ||
      stages.length > 1 ||
      (selectedStageId != null &&
        selectedStageId !== summary.firstAttentionStageId));

  const headline = structureReadinessBannerHeadline({
    presentedProblemCount: summary.presentedProblemCount,
    priorityActionsShown: actions.length,
    t,
  });

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
            {headline}
          </p>
        </div>

        {showViewAttentionPhase && attentionStage ? (
          <p className="structure-status__detail">
            <Link
              className="ds-text-link"
              to={structureDeepLink({
                competitionId: data.competitionId,
                stageId: attentionStage.stageId,
              })}
              aria-label={t('readiness.viewAttentionPhaseAria', {
                name: attentionStage.name,
              })}
            >
              {t('readiness.viewAttentionPhase', { name: attentionStage.name })}
            </Link>
          </p>
        ) : null}

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

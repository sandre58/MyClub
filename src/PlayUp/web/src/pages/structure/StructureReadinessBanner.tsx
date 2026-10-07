import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { TextLink } from '../../design-system/components/TextLink';
import { ArrowRightIcon } from '../../design-system/icons/contentIcons';
import { StructureDiagnosticIssueIcon } from './StructureDiagnosticIssueIcon';
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

  if (tone === 'attention') {
    return `${base} ds-btn--destructive`;
  }

  return `${base} ds-btn--secondary`;
}

function ReadinessActionRow({
  item,
  showPhaseLink,
  competitionId,
  isLast,
}: {
  item: StructureReadinessActionItem;
  showPhaseLink: boolean;
  competitionId: string;
  isLast: boolean;
}) {
  const { t } = useTranslation('structure');
  const issueLabel = t(item.titleKey, { defaultValue: item.code });
  const actionLabel = t(item.actionLabelKey ?? `readiness.action.${item.cta}`);
  const phaseHref = structureDeepLink({
    competitionId,
    stageId: item.stageId,
  });

  return (
    <li
      className="structure-status__action-row"
      data-tone={item.tone}
      data-last={isLast ? 'true' : undefined}
    >
      <div className="structure-status__action-body">
        <span className="structure-status__action-marker" aria-hidden="true">
          <StructureDiagnosticIssueIcon tone={item.tone} size="md" />
        </span>
        <span className="structure-status__action-copy">
          <span className="structure-status__action-summary">{issueLabel}</span>
          {showPhaseLink ? (
            <TextLink to={phaseHref} className="structure-status__action-phase">
              {item.stageName}
            </TextLink>
          ) : null}
        </span>
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

  const headline = structureReadinessBannerHeadline({
    presentedProblemCount: summary.presentedProblemCount,
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
          <span className="structure-status__headline-icon" aria-hidden="true">
            <StructureDiagnosticIssueIcon
              tone={summary.headerTone === 'danger' ? 'danger' : 'attention'}
              size="md"
            />
          </span>
          <p id="readiness-heading" className="structure-status__title">
            {headline}
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
                showPhaseLink={showStageOnRows}
                competitionId={data.competitionId}
                isLast={index === actions.length - 1}
              />
            ))}
          </ul>
        ) : null}
      </div>
    </div>
  );
}

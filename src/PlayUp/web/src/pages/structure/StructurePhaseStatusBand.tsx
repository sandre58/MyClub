import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

import {
  CheckIcon,
  CrossIcon,
  MatchesStatIcon,
  NextActionIcon,
  RoundsStatIcon,
} from '../../design-system/icons/contentIcons';
import type { StructureStageHubSummary } from '../../types';
import { Alert } from '../../design-system/components/Alert';
import { PendingLabel } from '../../ui';
import {
  lifecycleDisabledMessage,
  lifecycleMutationMessage,
  transitionActionLabel,
  transitionPendingLabel,
} from './structurePhaseLifecycleActions';
import {
  isLifecycleActionCode,
  remediationHref,
  resolveDraftPrepareStepIds,
  resolveDraftPrepareStepStatus,
  shouldShowLifecycleRemediationLink,
  shouldShowPrimaryBlockMessage,
  type DraftPrepareStepId,
  type DraftPrepareStepStatus,
} from './structurePhaseStatusBandModel';
import { useStructureStageLifecycleMutation } from './useStructureStageLifecycleMutation';

const primaryBtn = 'ds-btn ds-btn--primary ds-btn--sm';

function transitionActionIcon(code: 'PrepareStage' | 'StartStage' | 'CompleteStage') {
  switch (code) {
    case 'PrepareStage':
      return <CheckIcon size="sm" aria-hidden="true" />;
    case 'StartStage':
      return <NextActionIcon size="sm" aria-hidden="true" />;
    case 'CompleteStage':
      return <RoundsStatIcon size="sm" aria-hidden="true" />;
    default:
      return null;
  }
}

function DraftPrepareStepper({
  stage,
}: {
  stage: StructureStageHubSummary;
}) {
  const { t } = useTranslation('structure');
  const checklist = stage.lifecycle?.prepareChecklist;
  const disabledReason = stage.lifecycle?.transition?.disabledReasonCode;

  return (
    <ol
      className="structure-phase-status-band__stepper"
      aria-label={t('lifecycle.checklistAria')}
    >
      {resolveDraftPrepareStepIds(checklist).map((stepId) => {
        const status = resolveDraftPrepareStepStatus(
          stepId,
          checklist,
          disabledReason,
        );
        const checklistHint = checklistCodeForStep(stepId);
        return (
          <li
            key={stepId}
            className="structure-phase-status-band__step"
            data-step-status={status}
          >
            <span className="structure-phase-status-band__step-mark">
              <DraftStepMark status={status} />
            </span>
            <span
              className="structure-phase-status-band__step-label"
              title={
                checklistHint
                  ? t(`lifecycle.checklist.${checklistHint}`)
                  : undefined
              }
            >
              {t(`lifecycle.draftStep.${stepId}`)}
            </span>
          </li>
        );
      })}
    </ol>
  );
}

function checklistCodeForStep(stepId: DraftPrepareStepId): string | null {
  switch (stepId) {
    case 'Teams':
      return 'Entries';
    case 'Places':
      return 'Places';
    case 'Attribution':
      return 'Attribution';
    case 'Draw':
      return 'Draw';
    case 'Exits':
      return 'Exits';
    default:
      return null;
  }
}

function DraftStepMark({ status }: { status: DraftPrepareStepStatus }) {
  if (status === 'na') {
    return <span className="structure-phase-status-band__step-na">—</span>;
  }
  if (status === 'ok') {
    return <CheckIcon size="sm" aria-hidden="true" />;
  }
  return <CrossIcon size="sm" aria-hidden="true" />;
}

function RunningMatchStats({
  finished,
  open,
  cancelled,
}: {
  finished: number;
  open: number;
  cancelled: number;
}) {
  const { t } = useTranslation('structure');

  return (
    <div className="structure-phase-status-band__match-block">
      <p className="structure-phase-status-band__match-heading">
        {t('lifecycle.statusBand.matchStatsHeading')}
      </p>
      <ul
        className="structure-phase-hero__stats structure-phase-status-band__hero-stats"
        aria-label={t('lifecycle.statusBand.matchStatsAria')}
      >
      <li className="structure-phase-hero__stat">
        <span className="structure-phase-hero__stat-icon" aria-hidden="true">
          <CheckIcon size="sm" />
        </span>
        <span className="structure-phase-hero__stat-value">{finished}</span>
        <span className="structure-phase-hero__stat-label">
          {t('lifecycle.statusBand.matchStatFinished')}
        </span>
      </li>
      {cancelled > 0 ? (
        <li className="structure-phase-hero__stat">
          <span className="structure-phase-hero__stat-icon" aria-hidden="true">
            <CrossIcon size="sm" />
          </span>
          <span className="structure-phase-hero__stat-value">{cancelled}</span>
          <span className="structure-phase-hero__stat-label">
            {t('lifecycle.statusBand.matchStatCancelled')}
          </span>
        </li>
      ) : null}
      <li className="structure-phase-hero__stat">
        <span className="structure-phase-hero__stat-icon" aria-hidden="true">
          <MatchesStatIcon size="sm" />
        </span>
        <span className="structure-phase-hero__stat-value">{open}</span>
        <span className="structure-phase-hero__stat-label">
          {t('lifecycle.statusBand.matchStatRemaining')}
        </span>
      </li>
      </ul>
    </div>
  );
}

export function StructurePhaseStatusBand({
  competitionId,
  stage,
  structureHubRefreshing,
}: {
  competitionId: string;
  stage: StructureStageHubSummary;
  structureHubRefreshing?: boolean;
}) {
  if (stage.status === 'Completed') {
    return null;
  }

  return (
    <StructurePhaseStatusBandBody
      competitionId={competitionId}
      stage={stage}
      structureHubRefreshing={structureHubRefreshing}
    />
  );
}

function StructurePhaseStatusBandBody({
  competitionId,
  stage,
  structureHubRefreshing,
}: {
  competitionId: string;
  stage: StructureStageHubSummary;
  structureHubRefreshing?: boolean;
}) {
  const { t } = useTranslation('structure');
  const transitionMutation = useStructureStageLifecycleMutation(
    competitionId,
    stage.stageId,
    t,
  );

  const lifecycle = stage.lifecycle;
  const transition = lifecycle?.transition;
  const transitionCode = transition?.code;
  const projectedAction = isLifecycleActionCode(transitionCode)
    ? transitionCode
    : null;
  const actionEnabled = transition?.enabled ?? false;
  const showActionButton = projectedAction != null;

  const disabledReason = transition?.disabledReasonCode ?? null;
  const showBlockMessage =
    projectedAction != null &&
    !actionEnabled &&
    shouldShowPrimaryBlockMessage(
      stage.status,
      disabledReason,
      lifecycle?.prepareChecklist,
    );

  const bandAriaLabel = t(`lifecycle.state.${stage.status}.lead`);
  const busy = transitionMutation.isPending || Boolean(structureHubRefreshing);

  const showDraftStepper =
    stage.status === 'Draft' && lifecycle?.prepareChecklist != null;

  const showReadyHint =
    stage.status === 'Ready' && !showBlockMessage;

  const matchProgress = lifecycle?.matchProgress;
  const showRunningStats =
    (stage.status === 'Running' || stage.status === 'Suspended') &&
    matchProgress != null;

  const remediationTarget = showBlockMessage
    ? remediationHref(competitionId, stage)
    : null;
  const remediationLabelKey = transition?.remediation?.labelKey ?? null;
  const showRemediationLink =
    showBlockMessage &&
    shouldShowLifecycleRemediationLink(disabledReason, remediationTarget);

  const runningStats =
    showRunningStats && matchProgress ? (
      <RunningMatchStats
        finished={matchProgress.finished}
        open={matchProgress.open}
        cancelled={matchProgress.cancelled}
      />
    ) : null;

  return (
    <div
      className="structure-phase-status-band"
      aria-label={bandAriaLabel}
    >
      <div className="structure-phase-status-band__copy">
        {showBlockMessage && disabledReason ? (
          <p className="structure-phase-status-band__meta">
            {lifecycleDisabledMessage(t, disabledReason)}
            {showRemediationLink && remediationTarget ? (
              <>
                {' '}
                <Link className="ds-text-link" to={remediationTarget}>
                  {disabledReason === 'ConstructionFrozen'
                    ? t('lifecycle.statusBand.viewOverview')
                    : t(`lifecycle.remediation.${remediationLabelKey ?? 'default'}`, {
                        defaultValue: t('lifecycle.remediation.default'),
                      })}
                </Link>
              </>
            ) : null}
          </p>
        ) : null}
        {showReadyHint ? (
          <p className="structure-phase-status-band__meta">
            {t('lifecycle.statusBand.readyHint')}
          </p>
        ) : null}
        {showDraftStepper ? <DraftPrepareStepper stage={stage} /> : null}
        {runningStats}
        {transitionMutation.isError ? (
          <Alert tone="danger" role="alert">
            {lifecycleMutationMessage(t, transitionMutation.error)}
          </Alert>
        ) : null}
      </div>

      {showActionButton && projectedAction ? (
        <div className="structure-phase-status-band__action">
          <button
            type="button"
            className={primaryBtn}
            disabled={busy || !actionEnabled}
            onClick={() => transitionMutation.mutate(projectedAction)}
          >
            {transitionMutation.isPending ? (
              <PendingLabel>
                {transitionPendingLabel(t, projectedAction)}
              </PendingLabel>
            ) : (
              <>
                {transitionActionIcon(projectedAction)}
                <span>{transitionActionLabel(t, projectedAction)}</span>
              </>
            )}
          </button>
        </div>
      ) : null}
    </div>
  );
}

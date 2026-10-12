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
  mutationPendingLabel,
  revertActionLabel,
  transitionActionLabel,
} from './structurePhaseLifecycleActions';
import {
  isLifecycleActionCode,
  isLifecycleRevertCode,
  remediationHref,
  shouldShowLifecycleRemediationLink,
  shouldShowPrimaryBlockMessage,
  shouldShowRevertBlockMessage,
} from './structurePhaseStatusBandModel';
import { useStructureStageLifecycleMutation } from './useStructureStageLifecycleMutation';

const primaryBtn = 'ds-btn ds-btn--primary ds-btn--sm';
const secondaryBtn = 'ds-btn ds-btn--secondary ds-btn--sm';

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
      <p className="structure-phase-status-band__meta structure-phase-status-band__match-heading">
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

  const revert = lifecycle?.revertTransition;
  const revertCode = isLifecycleRevertCode(revert?.code) ? revert.code : null;
  const showRevertButton = revertCode != null;
  const revertEnabled = revert?.enabled ?? false;

  const disabledReason = transition?.disabledReasonCode ?? null;
  const showBlockMessage =
    projectedAction != null &&
    !actionEnabled &&
    shouldShowPrimaryBlockMessage(stage.status, disabledReason);

  const revertDisabledReason = revert?.disabledReasonCode ?? null;
  const showRevertBlockMessage =
    showRevertButton &&
    !revertEnabled &&
    shouldShowRevertBlockMessage(revertDisabledReason);

  const bandAriaLabel = t(`lifecycle.state.${stage.status}.lead`);
  const busy = transitionMutation.isPending || Boolean(structureHubRefreshing);
  const pendingCode = transitionMutation.variables;

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
        {showRevertBlockMessage && revertDisabledReason ? (
          <p className="structure-phase-status-band__meta">
            {lifecycleDisabledMessage(t, revertDisabledReason)}
          </p>
        ) : null}
        {showReadyHint ? (
          <p className="structure-phase-status-band__meta">
            {t('lifecycle.statusBand.readyHint')}
          </p>
        ) : null}
        {runningStats}
        {transitionMutation.isError ? (
          <Alert tone="danger" role="alert">
            {lifecycleMutationMessage(t, transitionMutation.error)}
          </Alert>
        ) : null}
      </div>

      {showActionButton || showRevertButton ? (
        <div className="structure-phase-status-band__actions">
          {showActionButton && projectedAction ? (
            <button
              type="button"
              className={primaryBtn}
              disabled={busy || !actionEnabled}
              onClick={() => transitionMutation.mutate(projectedAction)}
            >
              {transitionMutation.isPending && pendingCode === projectedAction ? (
                <PendingLabel>
                  {mutationPendingLabel(t, projectedAction)}
                </PendingLabel>
              ) : (
                <>
                  {transitionActionIcon(projectedAction)}
                  <span>{transitionActionLabel(t, projectedAction)}</span>
                </>
              )}
            </button>
          ) : null}
          {showRevertButton && revertCode ? (
            <button
              type="button"
              className={secondaryBtn}
              disabled={busy || !revertEnabled}
              onClick={() => transitionMutation.mutate(revertCode)}
            >
              {transitionMutation.isPending && pendingCode === revertCode ? (
                <PendingLabel>{mutationPendingLabel(t, revertCode)}</PendingLabel>
              ) : (
                revertActionLabel(t, revertCode)
              )}
            </button>
          ) : null}
        </div>
      ) : null}
    </div>
  );
}

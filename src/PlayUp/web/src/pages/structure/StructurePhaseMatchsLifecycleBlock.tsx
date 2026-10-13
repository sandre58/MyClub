import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

import {
  CheckIcon,
  PauseIcon,
  PlayIcon,
  RoundsStatIcon,
  UndoIcon,
} from '../../design-system/icons/contentIcons';
import type { StructureStageLifecycleRevertCode } from '../../types';
import type { StructureStageHubSummary } from '../../types';
import { Alert } from '../../design-system/components/Alert';
import { Tooltip } from '../../design-system/components/Tooltip';
import { EmptyState, PendingLabel } from '../../ui';
import {
  mutationPendingLabel,
  revertActionLabel,
  transitionActionLabel,
} from './structurePhaseLifecycleActions';
import {
  remediationHref,
  resolveDraftPrepareBodyMessage,
  resolveLifecycleAnticipatedMessage,
  resolveLifecycleMutationMessage,
  shouldShowDraftPrepareDisabledTooltip,
  shouldShowLifecycleRemediationLink,
  shouldShowPrimaryBlockMessage,
  shouldShowRevertDisabledParagraph,
} from './problems';
import {
  isLifecycleActionCode,
  isLifecycleRevertCode,
} from './structurePhaseStatusBandModel';
import { useStructureStageLifecycleMutation } from './useStructureStageLifecycleMutation';

const primaryBtn = 'ds-btn ds-btn--primary ds-btn--sm';
const secondaryBtn = 'ds-btn ds-btn--secondary ds-btn--sm';
const suspendIconBtn =
  'ds-btn ds-btn--attention ds-btn--sm ds-icon-button';
const resumeIconBtn = 'ds-btn ds-btn--secondary ds-btn--sm ds-icon-button';

function revertActionIcon(code: StructureStageLifecycleRevertCode) {
  switch (code) {
    case 'SuspendStage':
      return <PauseIcon size="sm" aria-hidden="true" />;
    case 'ResumeStage':
      return <PlayIcon size="sm" aria-hidden="true" />;
    case 'RevertToDraftStage':
    case 'RevertToReadyStage':
      return <UndoIcon size="sm" aria-hidden="true" />;
    default:
      return null;
  }
}

function revertButtonClass(code: StructureStageLifecycleRevertCode): string {
  switch (code) {
    case 'SuspendStage':
      return suspendIconBtn;
    case 'ResumeStage':
      return resumeIconBtn;
    case 'RevertToDraftStage':
    case 'RevertToReadyStage':
      return `${secondaryBtn} ds-icon-button`;
    default:
      return secondaryBtn;
  }
}

function revertShowsLabelOnlyInTooltip(code: StructureStageLifecycleRevertCode): boolean {
  return (
    code === 'SuspendStage' ||
    code === 'ResumeStage' ||
    code === 'RevertToDraftStage' ||
    code === 'RevertToReadyStage'
  );
}

function revertPendingSpinner() {
  return <span className="ds-spinner ds-spinner--sm" aria-hidden="true" />;
}

function LifecycleRevertIconButton({
  code,
  enabled,
  busy,
  pending,
  tooltip,
  onActivate,
  t,
}: {
  code: StructureStageLifecycleRevertCode;
  enabled: boolean;
  busy: boolean;
  pending: boolean;
  tooltip: string;
  onActivate: () => void;
  t: ReturnType<typeof useTranslation<'structure'>>['t'];
}) {
  return (
    <Tooltip content={tooltip}>
      <span className="structure-operational-tile__action-wrap">
        <button
          type="button"
          className={revertButtonClass(code)}
          disabled={busy || !enabled}
          aria-label={revertActionLabel(t, code)}
          onClick={onActivate}
        >
          {pending && revertShowsLabelOnlyInTooltip(code) ? (
            revertPendingSpinner()
          ) : pending ? (
            <PendingLabel>{mutationPendingLabel(t, code)}</PendingLabel>
          ) : (
            <>
              {revertActionIcon(code)}
              {revertShowsLabelOnlyInTooltip(code) ? null : (
                revertActionLabel(t, code)
              )}
            </>
          )}
        </button>
      </span>
    </Tooltip>
  );
}

function transitionActionIcon(code: 'PrepareStage' | 'StartStage' | 'CompleteStage') {
  switch (code) {
    case 'PrepareStage':
      return <CheckIcon size="sm" aria-hidden="true" />;
    case 'StartStage':
      return <PlayIcon size="sm" aria-hidden="true" />;
    case 'CompleteStage':
      return <RoundsStatIcon size="sm" aria-hidden="true" />;
    default:
      return null;
  }
}

/** Draft — single Prepare surface (empty state); no production, no duplicate CTA. */
export function StructurePhaseDraftMatchsPrepare({
  competitionId,
  stage,
}: {
  competitionId: string;
  stage: StructureStageHubSummary;
}) {
  const { t } = useTranslation('structure');
  const transitionMutation = useStructureStageLifecycleMutation(
    competitionId,
    stage.stageId,
    t,
  );

  const transition = stage.lifecycle?.transition;
  const prepareProjected = transition?.code === 'PrepareStage';
  const actionEnabled = transition?.enabled ?? false;
  const disabledReason = transition?.disabledReasonCode ?? null;
  const lifecycleProblem = transition?.problem ?? null;
  const showBlockMessage =
    prepareProjected &&
    !actionEnabled &&
    shouldShowPrimaryBlockMessage(stage.status, disabledReason);

  const busy = transitionMutation.isPending;
  const prepareDisabledTooltip =
    prepareProjected &&
    shouldShowDraftPrepareDisabledTooltip(disabledReason, actionEnabled)
      ? resolveLifecycleAnticipatedMessage(t, disabledReason!, lifecycleProblem)
      : null;
  const statusMessage = resolveDraftPrepareBodyMessage(
    t,
    showBlockMessage,
    disabledReason,
    lifecycleProblem,
  );

  const prepareButtonInner = (
    <button
      type="button"
      className={primaryBtn}
      disabled={busy || !actionEnabled}
      aria-label={transitionActionLabel(t, 'PrepareStage')}
      onClick={() => transitionMutation.mutate('PrepareStage')}
    >
      {transitionMutation.isPending ? (
        <PendingLabel>{mutationPendingLabel(t, 'PrepareStage')}</PendingLabel>
      ) : (
        <>
          {transitionActionIcon('PrepareStage')}
          <span>{transitionActionLabel(t, 'PrepareStage')}</span>
        </>
      )}
    </button>
  );

  const prepareButton =
    prepareDisabledTooltip && !actionEnabled ? (
      <Tooltip content={prepareDisabledTooltip}>
        <span className="structure-operational-tile__action-wrap">
          {prepareButtonInner}
        </span>
      </Tooltip>
    ) : (
      prepareButtonInner
    );

  const body = <>{statusMessage}</>;

  return (
    <div
      className="structure-operational-strip__lifecycle"
      aria-label={t('lifecycle.state.Draft.lead')}
    >
      <div className="structure-operational-strip__empty">
        <EmptyState
          variant="idle"
          icon={prepareProjected ? prepareButton : undefined}
          iconAccessible={prepareProjected}
        >
          {body}
        </EmptyState>
      </div>
      {transitionMutation.isError ? (
        <Alert tone="danger" role="alert">
          {resolveLifecycleMutationMessage(t, transitionMutation.error)}
        </Alert>
      ) : null}
    </div>
  );
}

/** Ready / Running / Suspended — lifecycle messages and transition CTAs (no match progress gauge here). */
export function StructurePhaseMatchsLifecycleBlock({
  competitionId,
  stage,
  productionBusy = false,
  leadingActions,
}: {
  competitionId: string;
  stage: StructureStageHubSummary;
  productionBusy?: boolean;
  /** Materialize / dematerialize — leftmost on the lifecycle action row. */
  leadingActions?: ReactNode;
}) {
  const status = stage.status;
  if (
    status === 'Draft' ||
    status === 'Completed' ||
    status === undefined
  ) {
    return null;
  }

  return (
    <StructurePhaseMatchsLifecycleBlockBody
      competitionId={competitionId}
      stage={stage}
      productionBusy={productionBusy}
      leadingActions={leadingActions}
    />
  );
}

function StructurePhaseMatchsLifecycleBlockBody({
  competitionId,
  stage,
  productionBusy,
  leadingActions,
}: {
  competitionId: string;
  stage: StructureStageHubSummary;
  productionBusy: boolean;
  leadingActions?: ReactNode;
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
  const showPrimaryButton = projectedAction != null;

  const cancelStart = lifecycle?.cancelStartTransition;
  const cancelStartCode =
    isLifecycleRevertCode(cancelStart?.code) &&
    cancelStart.code === 'RevertToReadyStage'
      ? cancelStart.code
      : null;
  const cancelStartEnabled = cancelStart?.enabled ?? false;
  const showCancelStartControl = cancelStartCode != null;
  const cancelStartDisabledReason = cancelStart?.disabledReasonCode ?? null;
  const cancelStartTooltip =
    showCancelStartControl && !cancelStartEnabled && cancelStartDisabledReason
      ? resolveLifecycleAnticipatedMessage(
          t,
          cancelStartDisabledReason,
          cancelStart?.problem ?? null,
        )
      : cancelStartCode
        ? revertActionLabel(t, cancelStartCode)
        : null;

  const revert = lifecycle?.revertTransition;
  const revertCode = isLifecycleRevertCode(revert?.code) ? revert.code : null;
  const revertEnabled = revert?.enabled ?? false;
  const pauseResumeCode =
    revertCode === 'SuspendStage' || revertCode === 'ResumeStage'
      ? revertCode
      : null;
  const backwardRevertCode =
    revertCode === 'RevertToDraftStage' ? revertCode : null;
  const showPauseResumeControl = pauseResumeCode != null;
  const showBackwardRevertControl = backwardRevertCode != null;

  const disabledReason = transition?.disabledReasonCode ?? null;
  const lifecycleProblem = transition?.problem ?? null;
  const showBlockMessage =
    projectedAction != null &&
    !actionEnabled &&
    shouldShowPrimaryBlockMessage(stage.status, disabledReason);

  const revertDisabledReason = revert?.disabledReasonCode ?? null;
  const pauseResumeTooltip =
    showPauseResumeControl && !revertEnabled && revertDisabledReason
      ? resolveLifecycleAnticipatedMessage(
          t,
          revertDisabledReason,
          revert?.problem ?? null,
        )
      : null;
  const showRevertParagraph =
    showPauseResumeControl &&
    !revertEnabled &&
    shouldShowRevertDisabledParagraph(revertDisabledReason);

  const blockAriaLabel = t(`lifecycle.state.${stage.status}.lead`);
  const busy = transitionMutation.isPending || productionBusy;
  const pendingCode = transitionMutation.variables;

  const remediationTarget = showBlockMessage
    ? remediationHref(competitionId, stage)
    : null;
  const remediationLabelKey = transition?.remediation?.labelKey ?? null;
  const showRemediationLink =
    showBlockMessage &&
    shouldShowLifecycleRemediationLink(
      disabledReason,
      remediationTarget,
      remediationLabelKey,
    );

  const hasCopy =
    (showBlockMessage && disabledReason) ||
    (showRevertParagraph && revertDisabledReason) ||
    transitionMutation.isError;

  const primaryDisabledTooltip =
    showPrimaryButton &&
    projectedAction &&
    !actionEnabled &&
    disabledReason
      ? resolveLifecycleAnticipatedMessage(t, disabledReason, lifecycleProblem)
      : null;

  const hasLifecycleActionsEnd =
    showPrimaryButton ||
    showPauseResumeControl ||
    showCancelStartControl ||
    showBackwardRevertControl;

  const hasActions = hasLifecycleActionsEnd || leadingActions != null;

  if (!hasCopy && !hasActions) {
    return null;
  }

  return (
    <div
      className="structure-operational-strip__lifecycle"
      aria-label={blockAriaLabel}
    >
      {hasCopy ? (
        <div className="structure-operational-strip__message-stack">
          {showBlockMessage && disabledReason ? (
            <p className="structure-operational-strip__lifecycle-meta">
              {resolveLifecycleAnticipatedMessage(t, disabledReason, lifecycleProblem)}
              {showRemediationLink && remediationTarget ? (
                <>
                  {' '}
                  <Link className="ds-text-link" to={remediationTarget}>
                    {disabledReason === 'ConstructionFrozen'
                      ? t('lifecycle.statusBand.viewOverview')
                      : t(`lifecycle.remediation.${remediationLabelKey}`, {
                          defaultValue: '',
                        })}
                  </Link>
                </>
              ) : null}
            </p>
          ) : null}
          {showRevertParagraph && revertDisabledReason ? (
            <p className="structure-operational-strip__lifecycle-meta">
              {resolveLifecycleAnticipatedMessage(
                t,
                revertDisabledReason,
                revert?.problem ?? null,
              )}
            </p>
          ) : null}
          {transitionMutation.isError ? (
            <Alert tone="danger" role="alert">
              {resolveLifecycleMutationMessage(t, transitionMutation.error)}
            </Alert>
          ) : null}
        </div>
      ) : null}

      {hasActions ? (
        <div className="structure-operational-strip__section-actions">
          {leadingActions ? (
            <div className="structure-operational-strip__section-actions-start">
              {leadingActions}
            </div>
          ) : null}
          {hasLifecycleActionsEnd ? (
            <div className="structure-operational-strip__section-actions-end">
              {showPrimaryButton && projectedAction ? (
                (() => {
                  const primaryInner = (
                    <button
                      type="button"
                      className={primaryBtn}
                      disabled={busy || !actionEnabled}
                      aria-label={transitionActionLabel(t, projectedAction)}
                      onClick={() => transitionMutation.mutate(projectedAction)}
                    >
                      {transitionMutation.isPending &&
                      pendingCode === projectedAction ? (
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
                  );
                  return primaryDisabledTooltip && !actionEnabled ? (
                    <Tooltip content={primaryDisabledTooltip}>
                      <span className="structure-operational-tile__action-wrap">
                        {primaryInner}
                      </span>
                    </Tooltip>
                  ) : (
                    primaryInner
                  );
                })()
              ) : null}
              {showPauseResumeControl && pauseResumeCode ? (
                <LifecycleRevertIconButton
                  code={pauseResumeCode}
                  enabled={revertEnabled}
                  busy={busy}
                  pending={
                    transitionMutation.isPending && pendingCode === pauseResumeCode
                  }
                  tooltip={
                    pauseResumeTooltip ?? revertActionLabel(t, pauseResumeCode)
                  }
                  onActivate={() => transitionMutation.mutate(pauseResumeCode)}
                  t={t}
                />
              ) : null}
              {showCancelStartControl && cancelStartCode ? (
                <LifecycleRevertIconButton
                  code={cancelStartCode}
                  enabled={cancelStartEnabled}
                  busy={busy}
                  pending={
                    transitionMutation.isPending && pendingCode === cancelStartCode
                  }
                  tooltip={cancelStartTooltip ?? revertActionLabel(t, cancelStartCode)}
                  onActivate={() => transitionMutation.mutate(cancelStartCode)}
                  t={t}
                />
              ) : null}
              {showBackwardRevertControl && backwardRevertCode ? (
                <LifecycleRevertIconButton
                  code={backwardRevertCode}
                  enabled={revertEnabled}
                  busy={busy}
                  pending={
                    transitionMutation.isPending &&
                    pendingCode === backwardRevertCode
                  }
                  tooltip={revertActionLabel(t, backwardRevertCode)}
                  onActivate={() => transitionMutation.mutate(backwardRevertCode)}
                  t={t}
                />
              ) : null}
            </div>
          ) : null}
        </div>
      ) : null}
    </div>
  );
}

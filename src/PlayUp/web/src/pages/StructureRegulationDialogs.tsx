import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useEffect, useId, useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import {
  replaceStageDrawRules,
} from '../api';
import { ConfirmDialog } from '../design-system/components/ConfirmDialog';
import { ChoiceTile } from '../design-system/components/ChoiceTile';
import { Dialog } from '../design-system/components/Dialog';
import { Field } from '../design-system/components/Field';
import { InputNumber } from '../design-system/components/InputNumber';
import { SwitchPanel } from '../design-system/components/SwitchPanel';
import { notify } from '../design-system/toastStore';
import { useDiscardConfirm } from '../design-system/useDiscardConfirm';
import { RandomIcon, CheckIcon } from '../design-system/icons/contentIcons';
import { CloseIcon } from '../design-system/icons/shellIcons';
import { MutationError, PendingLabel } from '../ui';
import type {
  StructureStageHubSummary,
  StructureView,
  ReplaceStageDrawRulesRequest,
} from '../types';
import { invalidateAfterStructureMutation } from './structureInvalidation';
import {
  MatchRulesDialog,
  StandingRulesDialog,
} from './StageMatchStandingDialogs';
import { TieFormatDialog } from './StageTieFormatDialog';

export { MatchRulesDialog, StandingRulesDialog, TieFormatDialog };

export type DrawRulesDialogIntent = 'activate' | 'params';

function stageActions(stage: StructureStageHubSummary): string[] {
  return stage.actions ?? [];
}

export function MatchStandingEditors({
  data,
  stage,
  section,
}: {
  data: StructureView;
  stage: StructureStageHubSummary;
  section: 'matchs' | 'classement';
}) {
  const { t } = useTranslation('structure');
  const [matchOpen, setMatchOpen] = useState(false);
  const [standingOpen, setStandingOpen] = useState(false);
  const actions = stageActions(stage);
  const canSpecializeMatch = actions.includes('ReplaceMatchRules');
  const canSpecializeStanding = actions.includes('ReplaceStandingRules');
  const canRebind = actions.includes('BindToCompetition');
  const matchBound = section === 'matchs';

  if (matchBound && !canSpecializeMatch && !canRebind) {
    return null;
  }
  if (!matchBound && !canSpecializeStanding && !canRebind) {
    return null;
  }

  return (
    <>
      {matchBound ? (
        <button
          type="button"
          className="structure-action"
          onClick={() => setMatchOpen(true)}
        >
          {t('regulation.editMatch')}
          <span aria-hidden="true">→</span>
        </button>
      ) : (
        <button
          type="button"
          className="structure-action"
          onClick={() => setStandingOpen(true)}
        >
          {t('regulation.editStanding')}
          <span aria-hidden="true">→</span>
        </button>
      )}
      <MatchRulesDialog
        data={data}
        stage={stage}
        open={matchOpen}
        onClose={() => setMatchOpen(false)}
      />
      <StandingRulesDialog
        data={data}
        stage={stage}
        open={standingOpen}
        onClose={() => setStandingOpen(false)}
      />
    </>
  );
}

export function TirageEditors({
  data,
  stage,
}: {
  data: StructureView;
  stage: StructureStageHubSummary;
}) {
  const { t } = useTranslation('structure');
  const [open, setOpen] = useState(false);
  if (!stageActions(stage).includes('ReplaceDrawRules')) {
    return null;
  }

  return (
    <>
      <button
        type="button"
        className="structure-action"
        onClick={() => setOpen(true)}
      >
        {stage.hasDrawRules ? t('fiche.drawParams') : t('fiche.activateDraw')}
        <span aria-hidden="true">→</span>
      </button>
      <DrawRulesDialog
        competitionId={data.competitionId}
        stage={stage}
        open={open}
        onClose={() => setOpen(false)}
        intent={stage.hasDrawRules ? 'params' : 'activate'}
      />
    </>
  );
}

export function ConfrontationEditors({
  data,
  stage,
}: {
  data: StructureView;
  stage: StructureStageHubSummary;
}) {
  const { t } = useTranslation('structure');
  const [open, setOpen] = useState(false);
  if (
    !stageActions(stage).includes('ReplaceDefaultTieFormat') &&
    !stageActions(stage).includes('ReplaceRoundTieFormat')
  ) {
    return null;
  }

  return (
    <>
      <button
        type="button"
        className="structure-action"
        onClick={() => setOpen(true)}
      >
        {t('regulation.editTie')}
        <span aria-hidden="true">→</span>
      </button>
      <TieFormatDialog
        competitionId={data.competitionId}
        stage={stage}
        open={open}
        onClose={() => setOpen(false)}
      />
    </>
  );
}

/**
 * Activate (`DrawRules` null → Random min) or edit DrawRules params.
 * Deactivate lives under the fiche CTA group (not in this dialog).
 * V1 fields: Mode (RO) + PotRules for Groups only (SwitchPanel). Seeds/constraints not editable.
 */
export function DrawRulesDialog({
  competitionId,
  stage,
  open,
  onClose,
  intent = 'params',
}: {
  competitionId: string;
  stage: StructureStageHubSummary;
  open: boolean;
  onClose: () => void;
  intent?: DrawRulesDialogIntent;
}) {
  const { t } = useTranslation('structure');
  const { t: tCommon } = useTranslation('common');
  const { t: tReg } = useTranslation('regulation');
  const formId = useId();
  const queryClient = useQueryClient();
  const isActivate = intent === 'activate';
  const showPots = stage.formatKind === 'Groups';
  const defaultPots =
    stage.placesPerGroup != null && stage.placesPerGroup >= 2
      ? stage.placesPerGroup
      : 2;

  const [usePots, setUsePots] = useState(false);
  const [numberOfPots, setNumberOfPots] = useState(defaultPots);
  const [baselineUsePots, setBaselineUsePots] = useState(false);
  const [baselinePots, setBaselinePots] = useState(defaultPots);

  const normalizedPots = showPots && usePots ? normalizeDrawPots(numberOfPots) : null;
  const isDirty = showPots
    ? usePots !== baselineUsePots ||
      (usePots && numberOfPots !== baselinePots)
    : false;
  /** Activate may save while clean (engagement). Params require a field delta. */
  const canSave = isActivate || isDirty;

  const {
    discardOpen,
    requestClose: requestDiscardClose,
    cancelDiscard,
    confirmDiscard,
    resetDiscard,
  } = useDiscardConfirm(isDirty, onClose);

  useEffect(() => {
    if (!open) {
      return;
    }
    const pots = normalizeDrawPots(stage.numberOfPots ?? null);
    const enabled = pots != null;
    const value = pots ?? defaultPots;
    setUsePots(enabled);
    setNumberOfPots(value);
    setBaselineUsePots(enabled);
    setBaselinePots(value);
    resetDiscard();
  }, [open, stage, defaultPots, resetDiscard]);

  const saveMutation = useMutation({
    mutationFn: async () => {
      const body: ReplaceStageDrawRulesRequest = {
        clear: false,
        mode: 'Random',
        numberOfPots: showPots ? (normalizedPots ?? undefined) : undefined,
      };
      const demoted = stage.status === 'Ready';
      await replaceStageDrawRules(stage.stageId, body);
      return { demoted, activated: isActivate };
    },
    onSuccess: async (result) => {
      await invalidateAfterStructureMutation(queryClient, competitionId, {
        stageId: stage.stageId,
      });
      notify.success(
        result.activated
          ? t('regulation.drawActivatedToast')
          : t('regulation.drawParamsSavedToast'),
      );
      if (result.demoted) {
        notify.attention(t('regulation.drawSavedDemotedToast'));
      }
      onClose();
    },
  });

  const title = isActivate ? t('fiche.activateDraw') : t('fiche.drawParams');
  const saveLabel = isActivate
    ? t('fiche.activateDraw')
    : t('regulation.save');
  const busy = saveMutation.isPending;

  function requestClose() {
    requestDiscardClose(busy);
  }

  function requestSave() {
    if (busy || !canSave) {
      return;
    }
    saveMutation.mutate();
  }

  return (
    <>
      <Dialog
        open={open}
        onClose={requestClose}
        title={title}
        description={stage.name}
        size="sm"
        closeLabel={tCommon('close')}
        closeDisabled={busy || discardOpen}
        trapFocus={!discardOpen}
        footer={
          <>
            <button
              type="button"
              className="ds-btn ds-btn--ghost"
              onClick={requestClose}
              disabled={busy || discardOpen}
            >
              <CloseIcon size="sm" />
              {tCommon('cancel')}
            </button>
            <button
              type="submit"
              form={formId}
              className="ds-btn ds-btn--primary"
              disabled={busy || discardOpen || !canSave}
            >
              {saveMutation.isPending ? (
                <PendingLabel>{t('regulation.saving')}</PendingLabel>
              ) : (
                <>
                  <CheckIcon size="sm" />
                  {saveLabel}
                </>
              )}
            </button>
          </>
        }
        footerStatus={
          saveMutation.isError ? (
            <MutationError error={saveMutation.error} />
          ) : null
        }
      >
        <form
          id={formId}
          className="structure-form"
          onSubmit={(event: FormEvent) => {
            event.preventDefault();
            requestSave();
          }}
        >
          <Field label={t('regulation.drawModeLabel')}>
            <div
              className="structure-qualification__scope-tiles"
              data-count="1"
              role="radiogroup"
              aria-label={t('regulation.drawModeLabel')}
            >
              <ChoiceTile
                label={t('fiche.drawModeRandomShort')}
                description={t('fiche.drawModeRandom')}
                leading={<RandomIcon size="sm" />}
                selected
                onChange={(selected) => {
                  // Sole V1 mode — cannot deselect.
                  if (!selected) return;
                }}
              />
            </div>
          </Field>
          {showPots ? (
            <SwitchPanel
              title={t('regulation.potsPanelTitle')}
              description={t('regulation.potsPanelHint')}
              checked={usePots}
              onChange={(checked) => {
                setUsePots(checked);
                if (checked && numberOfPots < 2) {
                  setNumberOfPots(defaultPots);
                }
              }}
              switchLabel={t('regulation.enablePots')}
            >
              <Field label={t('regulation.numberOfPots')} required>
                <InputNumber
                  value={numberOfPots}
                  min={2}
                  max={16}
                  controlsLayout="split"
                  onChange={(value) => {
                    if (value != null && Number.isFinite(value)) {
                      setNumberOfPots(value);
                    }
                  }}
                />
              </Field>
            </SwitchPanel>
          ) : null}
        </form>
      </Dialog>
      <ConfirmDialog
        open={discardOpen}
        title={tReg('editor.discardTitle')}
        message={tReg('editor.discardMessage')}
        confirmLabel={tReg('editor.discardConfirm')}
        cancelLabel={tCommon('cancel')}
        closeLabel={tCommon('close')}
        danger
        onConfirm={confirmDiscard}
        onCancel={cancelDiscard}
      />
    </>
  );
}

function normalizeDrawPots(value: number | null): number | null {
  return value != null && value >= 2 ? value : null;
}

import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useEffect, useId, useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { replaceStageDrawRules } from '../api';
import { Alert } from '../design-system/components/Alert';
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

/**
 * Activate (`DrawRules` null → Random min) or edit DrawRules params.
 * Deactivate lives under the stage-card CTA group (not in this dialog).
 * Fields: Mode (RO) + PotRules for Groups only (SwitchPanel). Seeds/constraints not editable.
 * L2: when `rulesLocked`, params are view-only (Generate already consumed the rules).
 */
export function DrawRulesDialog({
  competitionId,
  stage,
  open,
  onClose,
  intent = 'params',
  rulesLocked = false,
}: {
  competitionId: string;
  stage: StructureStageHubSummary;
  open: boolean;
  onClose: () => void;
  intent?: DrawRulesDialogIntent;
  /** True when ReplaceDrawRules is Domain-blocked (generated non-cancelled Draw). */
  rulesLocked?: boolean;
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

  const normalizedPots =
    showPots && usePots ? normalizeDrawPots(numberOfPots) : null;
  const isDirty =
    !rulesLocked &&
    showPots &&
    (usePots !== baselineUsePots || (usePots && numberOfPots !== baselinePots));
  /** Activate may save while clean (engagement). Params require a field delta. Locked = view only. */
  const canSave = !rulesLocked && (isActivate || isDirty);

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
  const saveLabel = isActivate ? t('fiche.activateDraw') : t('regulation.save');
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
              {rulesLocked ? tCommon('close') : tCommon('cancel')}
            </button>
            {rulesLocked ? null : (
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
            )}
          </>
        }
        footerStatus={
          saveMutation.isError ? (
            <MutationError error={saveMutation.error} />
          ) : rulesLocked ? (
            <Alert tone="info" role="status">
              {t('regulation.drawParamsLockedHint')}
            </Alert>
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
                  // Sole available mode — cannot deselect.
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
              disabled={rulesLocked}
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
                  disabled={rulesLocked}
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

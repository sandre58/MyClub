import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useEffect, useId, useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import {
  replaceStageDrawRules,
} from '../api';
import { Dialog } from '../design-system/components/Dialog';
import { Field } from '../design-system/components/Field';
import { InputNumber } from '../design-system/components/InputNumber';
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
  const formId = useId();
  const queryClient = useQueryClient();
  const [numberOfPots, setNumberOfPots] = useState<number | null>(
    stage.numberOfPots ?? null,
  );
  const [numberOfSeeds, setNumberOfSeeds] = useState<number | null>(
    stage.numberOfSeeds ?? null,
  );

  useEffect(() => {
    if (open) {
      setNumberOfPots(stage.numberOfPots ?? null);
      setNumberOfSeeds(stage.numberOfSeeds ?? null);
    }
  }, [open, stage]);

  const saveMutation = useMutation({
    mutationFn: () => {
      const body: ReplaceStageDrawRulesRequest = {
        clear: false,
        mode: 'Random',
        numberOfPots:
          numberOfPots != null && numberOfPots >= 2
            ? numberOfPots
            : undefined,
        numberOfSeeds:
          numberOfSeeds != null && numberOfSeeds > 0
            ? numberOfSeeds
            : undefined,
      };
      return replaceStageDrawRules(stage.stageId, body);
    },
    onSuccess: async () => {
      await invalidateAfterStructureMutation(queryClient, competitionId);
      onClose();
    },
  });

  const isActivate = intent === 'activate';
  const title = isActivate ? t('fiche.activateDraw') : t('fiche.drawParams');
  const saveLabel = isActivate
    ? t('fiche.activateDraw')
    : t('regulation.save');
  const busy = saveMutation.isPending;

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={title}
      description={stage.name}
      size="sm"
      closeLabel={tCommon('close')}
      closeDisabled={busy}
      footer={
        <>
          <button
            type="button"
            className="ds-btn ds-btn--secondary"
            onClick={onClose}
            disabled={busy}
          >
            {tCommon('cancel')}
          </button>
          <button
            type="submit"
            form={formId}
            className="ds-btn ds-btn--primary"
            disabled={busy}
          >
            {saveMutation.isPending ? (
              <PendingLabel>{t('regulation.saving')}</PendingLabel>
            ) : (
              saveLabel
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
          saveMutation.mutate();
        }}
      >
        <p className="structure-detail__hint">
          {isActivate
            ? t('regulation.activateDrawHint')
            : t('regulation.drawModeHint')}
        </p>
        <Field label={t('regulation.numberOfPots')}>
          <InputNumber
            value={numberOfPots}
            min={2}
            max={16}
            controlsLayout="split"
            onChange={(value) => setNumberOfPots(value)}
          />
        </Field>
        <Field label={t('regulation.numberOfSeeds')}>
          <InputNumber
            value={numberOfSeeds}
            min={0}
            max={64}
            controlsLayout="split"
            onChange={(value) => setNumberOfSeeds(value)}
          />
        </Field>
      </form>
    </Dialog>
  );
}

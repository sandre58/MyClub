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
        {t('regulation.editDraw')}
        <span aria-hidden="true">→</span>
      </button>
      <DrawRulesDialog
        competitionId={data.competitionId}
        stage={stage}
        open={open}
        onClose={() => setOpen(false)}
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

export function DrawRulesDialog({
  competitionId,
  stage,
  open,
  onClose,
}: {
  competitionId: string;
  stage: StructureStageHubSummary;
  open: boolean;
  onClose: () => void;
}) {
  const { t } = useTranslation('structure');
  const { t: tCommon } = useTranslation('common');
  const formId = useId();
  const queryClient = useQueryClient();
  const [clear, setClear] = useState(false);
  const [numberOfPots, setNumberOfPots] = useState<number | null>(
    stage.numberOfPots ?? null,
  );
  const [numberOfSeeds, setNumberOfSeeds] = useState<number | null>(
    stage.numberOfSeeds ?? null,
  );

  useEffect(() => {
    if (open) {
      setClear(false);
      setNumberOfPots(stage.numberOfPots ?? null);
      setNumberOfSeeds(stage.numberOfSeeds ?? null);
    }
  }, [open, stage]);

  const mutation = useMutation({
    mutationFn: () => {
      const body: ReplaceStageDrawRulesRequest = clear
        ? { clear: true }
        : {
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

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={t('regulation.drawTitle')}
      description={stage.name}
      size="sm"
      closeLabel={tCommon('close')}
      footer={
        <>
          <button
            type="button"
            className="ds-btn ds-btn--secondary"
            onClick={onClose}
            disabled={mutation.isPending}
          >
            {tCommon('cancel')}
          </button>
          <button
            type="submit"
            form={formId}
            className="ds-btn ds-btn--primary"
            disabled={mutation.isPending}
          >
            {mutation.isPending ? (
              <PendingLabel>{t('regulation.saving')}</PendingLabel>
            ) : (
              t('regulation.save')
            )}
          </button>
        </>
      }
    >
      <form
        id={formId}
        className="structure-form"
        onSubmit={(event: FormEvent) => {
          event.preventDefault();
          mutation.mutate();
        }}
      >
        <label className="structure-check">
          <input
            type="checkbox"
            checked={clear}
            onChange={(event) => setClear(event.target.checked)}
          />
          {t('regulation.clearDraw')}
        </label>
        {!clear ? (
          <>
            <p className="structure-detail__hint">{t('regulation.drawModeHint')}</p>
            <Field label={t('regulation.numberOfPots')}>
              <InputNumber
                value={numberOfPots ?? 2}
                min={2}
                max={16}
                controlsLayout="split"
                onChange={(value) => setNumberOfPots(value)}
              />
            </Field>
            <Field label={t('regulation.numberOfSeeds')}>
              <InputNumber
                value={numberOfSeeds ?? 0}
                min={0}
                max={64}
                controlsLayout="split"
                onChange={(value) => setNumberOfSeeds(value)}
              />
            </Field>
          </>
        ) : null}
        {mutation.isError ? <MutationError error={mutation.error} /> : null}
      </form>
    </Dialog>
  );
}

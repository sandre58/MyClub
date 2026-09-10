import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useEffect, useId, useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import {
  bindStageRegulation,
  replaceStageDefaultTieFormat,
  replaceStageDrawRules,
  replaceStageMatchRules,
  replaceStageStandingRules,
} from '../api';
import { Dialog } from '../design-system/components/Dialog';
import { Field } from '../design-system/components/Field';
import { InputNumber } from '../design-system/components/InputNumber';
import { MutationError, PendingLabel } from '../ui';
import type {
  StructureStageHubSummary,
  StructureView,
  RankingCriterion,
  ReplaceStageDefaultTieFormatRequest,
  ReplaceStageDrawRulesRequest,
  ReplaceStageMatchRulesRequest,
  ReplaceStageStandingRulesRequest,
} from '../types';
import { invalidateAfterStructureMutation } from './structureInvalidation';

function stageActions(stage: StructureStageHubSummary): string[] {
  return stage.actions ?? [];
}

const DEFAULT_CRITERIA: RankingCriterion[] = [
  'Points',
  'GoalDifference',
  'GoalsFor',
  'HeadToHead',
];

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
  const [rebindOpen, setRebindOpen] = useState(false);
  const actions = stageActions(stage);
  const canSpecializeMatch = actions.includes('ReplaceMatchRules');
  const canSpecializeStanding = actions.includes('ReplaceStandingRules');
  const canRebind = actions.includes('BindToCompetition');
  const matchBound = section === 'matchs';
  const scope = matchBound ? 'Match' : 'Standing';

  if (matchBound && !canSpecializeMatch && !canRebind) {
    return null;
  }
  if (!matchBound && !canSpecializeStanding && !canRebind) {
    return null;
  }

  return (
    <>
      {matchBound && canSpecializeMatch && (
        <button
          type="button"
          className="structure-action"
          onClick={() => setMatchOpen(true)}
        >
          {t('regulation.personalizeMatch')}
          <span aria-hidden="true">→</span>
        </button>
      )}
      {!matchBound && canSpecializeStanding && (
        <button
          type="button"
          className="structure-action"
          onClick={() => setStandingOpen(true)}
        >
          {t('regulation.personalizeStanding')}
          <span aria-hidden="true">→</span>
        </button>
      )}
      {canRebind && (
        <button
          type="button"
          className="structure-action"
          onClick={() => setRebindOpen(true)}
        >
          {t('regulation.rebindToFrame')}
          <span aria-hidden="true">→</span>
        </button>
      )}
      <MatchRulesDialog
        competitionId={data.competitionId}
        stage={stage}
        open={matchOpen}
        onClose={() => setMatchOpen(false)}
      />
      <StandingRulesDialog
        competitionId={data.competitionId}
        stage={stage}
        open={standingOpen}
        onClose={() => setStandingOpen(false)}
      />
      <RebindDialog
        competitionId={data.competitionId}
        stage={stage}
        scope={scope}
        open={rebindOpen}
        onClose={() => setRebindOpen(false)}
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
  if (!stageActions(stage).includes('ReplaceDefaultTieFormat')) {
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

function MatchRulesDialog({
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
  const [form, setForm] = useState<ReplaceStageMatchRulesRequest>(() =>
    matchFormFromStage(stage),
  );

  useEffect(() => {
    if (open) {
      setForm(matchFormFromStage(stage));
    }
  }, [open, stage]);

  const mutation = useMutation({
    mutationFn: () => replaceStageMatchRules(stage.stageId, form),
    onSuccess: async () => {
      await invalidateAfterStructureMutation(queryClient, competitionId, {
        stageId: stage.stageId,
      });
      onClose();
    },
  });

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={t('regulation.matchTitle')}
      description={stage.name}
      size="md"
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
            <PendingLabel
              pending={mutation.isPending}
              idle={tCommon('save')}
              busy={t('regulation.saving')}
            />
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
        <Field label={t('regulation.numberOfPeriods')} required>
          <InputNumber
            value={form.numberOfPeriods}
            min={1}
            max={4}
            controlsLayout="split"
            onChange={(value) =>
              setForm((current) => ({
                ...current,
                numberOfPeriods: value ?? 2,
              }))
            }
          />
        </Field>
        <Field label={t('regulation.durationPerPeriod')} required>
          <InputNumber
            value={form.durationPerPeriod}
            min={1}
            max={60}
            controlsLayout="split"
            onChange={(value) =>
              setForm((current) => ({
                ...current,
                durationPerPeriod: value ?? 45,
              }))
            }
          />
        </Field>
        <Field label={t('regulation.halfTimeDuration')} required>
          <InputNumber
            value={form.halfTimeDuration}
            min={0}
            max={30}
            controlsLayout="split"
            onChange={(value) =>
              setForm((current) => ({
                ...current,
                halfTimeDuration: value ?? 15,
              }))
            }
          />
        </Field>
        <Field label={t('regulation.forfeitWinner')} required>
          <InputNumber
            value={form.forfeitWinnerGoals ?? 3}
            min={0}
            max={20}
            controlsLayout="split"
            onChange={(value) =>
              setForm((current) => ({
                ...current,
                forfeitWinnerGoals: value ?? 3,
              }))
            }
          />
        </Field>
        <Field label={t('regulation.forfeitLoser')} required>
          <InputNumber
            value={form.forfeitLoserGoals ?? 0}
            min={0}
            max={20}
            controlsLayout="split"
            onChange={(value) =>
              setForm((current) => ({
                ...current,
                forfeitLoserGoals: value ?? 0,
              }))
            }
          />
        </Field>
        <label className="structure-check">
          <input
            type="checkbox"
            checked={form.hasExtraTime === true}
            onChange={(event) =>
              setForm((current) => ({
                ...current,
                hasExtraTime: event.target.checked,
                extraTimeNumberOfPeriods:
                  current.extraTimeNumberOfPeriods ?? 2,
                extraTimeDurationPerPeriod:
                  current.extraTimeDurationPerPeriod ?? 15,
              }))
            }
          />
          {t('regulation.hasExtraTime')}
        </label>
        {form.hasExtraTime ? (
          <>
            <Field label={t('regulation.extraTimePeriods')} required>
              <InputNumber
                value={form.extraTimeNumberOfPeriods ?? 2}
                min={1}
                max={4}
                controlsLayout="split"
                onChange={(value) =>
                  setForm((current) => ({
                    ...current,
                    extraTimeNumberOfPeriods: value ?? 2,
                  }))
                }
              />
            </Field>
            <Field label={t('regulation.extraTimeDuration')} required>
              <InputNumber
                value={form.extraTimeDurationPerPeriod ?? 15}
                min={1}
                max={45}
                controlsLayout="split"
                onChange={(value) =>
                  setForm((current) => ({
                    ...current,
                    extraTimeDurationPerPeriod: value ?? 15,
                  }))
                }
              />
            </Field>
          </>
        ) : null}
        <label className="structure-check">
          <input
            type="checkbox"
            checked={form.hasPenaltyShootout === true}
            onChange={(event) =>
              setForm((current) => ({
                ...current,
                hasPenaltyShootout: event.target.checked,
                penaltyInitialKicksPerTeam:
                  current.penaltyInitialKicksPerTeam ?? 5,
              }))
            }
          />
          {t('regulation.hasPenaltyShootout')}
        </label>
        {form.hasPenaltyShootout ? (
          <Field label={t('regulation.shootoutKicks')} required>
            <InputNumber
              value={form.penaltyInitialKicksPerTeam ?? 5}
              min={1}
              max={15}
              controlsLayout="split"
              onChange={(value) =>
                setForm((current) => ({
                  ...current,
                  penaltyInitialKicksPerTeam: value ?? 5,
                }))
              }
            />
          </Field>
        ) : null}
        {mutation.isError ? <MutationError error={mutation.error} /> : null}
      </form>
    </Dialog>
  );
}

function StandingRulesDialog({
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
  const [form, setForm] = useState<ReplaceStageStandingRulesRequest>(() =>
    standingFormFromStage(stage),
  );

  useEffect(() => {
    if (open) {
      setForm(standingFormFromStage(stage));
    }
  }, [open, stage]);

  const mutation = useMutation({
    mutationFn: () => replaceStageStandingRules(stage.stageId, form),
    onSuccess: async () => {
      await invalidateAfterStructureMutation(queryClient, competitionId, {
        stageId: stage.stageId,
      });
      onClose();
    },
  });

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={t('regulation.standingTitle')}
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
            <PendingLabel
              pending={mutation.isPending}
              idle={tCommon('save')}
              busy={t('regulation.saving')}
            />
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
        <Field label={t('regulation.winPoints')} required>
          <InputNumber
            value={form.winPoints}
            min={0}
            max={10}
            controlsLayout="split"
            onChange={(value) =>
              setForm((current) => ({ ...current, winPoints: value ?? 3 }))
            }
          />
        </Field>
        <Field label={t('regulation.drawPoints')} required>
          <InputNumber
            value={form.drawPoints}
            min={0}
            max={10}
            controlsLayout="split"
            onChange={(value) =>
              setForm((current) => ({ ...current, drawPoints: value ?? 1 }))
            }
          />
        </Field>
        <Field label={t('regulation.lossPoints')} required>
          <InputNumber
            value={form.lossPoints}
            min={0}
            max={10}
            controlsLayout="split"
            onChange={(value) =>
              setForm((current) => ({ ...current, lossPoints: value ?? 0 }))
            }
          />
        </Field>
        <p className="structure-detail__hint">
          {t('regulation.criteriaHint', {
            list: form.rankingCriteria.join(' · '),
          })}
        </p>
        {mutation.isError ? <MutationError error={mutation.error} /> : null}
      </form>
    </Dialog>
  );
}

function RebindDialog({
  competitionId,
  stage,
  scope,
  open,
  onClose,
}: {
  competitionId: string;
  stage: StructureStageHubSummary;
  scope: 'Match' | 'Standing';
  open: boolean;
  onClose: () => void;
}) {
  const { t } = useTranslation('structure');
  const { t: tCommon } = useTranslation('common');
  const queryClient = useQueryClient();
  const mutation = useMutation({
    mutationFn: () => bindStageRegulation(stage.stageId, { scope }),
    onSuccess: async () => {
      await invalidateAfterStructureMutation(queryClient, competitionId);
      onClose();
    },
  });

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={t('regulation.rebindTitle')}
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
            type="button"
            className="ds-btn ds-btn--primary"
            disabled={mutation.isPending}
            onClick={() => mutation.mutate()}
          >
            <PendingLabel
              pending={mutation.isPending}
              idle={t('regulation.rebindConfirm')}
              busy={t('regulation.saving')}
            />
          </button>
        </>
      }
    >
      <p className="structure-detail__lede">
        {scope === 'Match'
          ? t('regulation.rebindMatchBody')
          : t('regulation.rebindStandingBody')}
      </p>
      {mutation.isError ? <MutationError error={mutation.error} /> : null}
    </Dialog>
  );
}

function DrawRulesDialog({
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
            <PendingLabel
              pending={mutation.isPending}
              idle={tCommon('save')}
              busy={t('regulation.saving')}
            />
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

function TieFormatDialog({
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
  const [form, setForm] = useState<ReplaceStageDefaultTieFormatRequest>(() =>
    tieFormFromStage(stage),
  );

  useEffect(() => {
    if (open) {
      setForm(tieFormFromStage(stage));
    }
  }, [open, stage]);

  const mutation = useMutation({
    mutationFn: () => replaceStageDefaultTieFormat(stage.stageId, form),
    onSuccess: async () => {
      await invalidateAfterStructureMutation(queryClient, competitionId);
      onClose();
    },
  });

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={t('regulation.tieTitle')}
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
            <PendingLabel
              pending={mutation.isPending}
              idle={tCommon('save')}
              busy={t('regulation.saving')}
            />
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
            checked={form.clear === true}
            onChange={(event) =>
              setForm((current) => ({ ...current, clear: event.target.checked }))
            }
          />
          {t('regulation.clearTie')}
        </label>
        {!form.clear ? (
          <>
            <Field label={t('regulation.numberOfLegs')} required>
              <InputNumber
                value={form.numberOfLegs ?? 1}
                min={1}
                max={2}
                controlsLayout="split"
                onChange={(value) =>
                  setForm((current) => ({
                    ...current,
                    numberOfLegs: value === 2 ? 2 : 1,
                    hasAwayGoalsRule:
                      value === 2 ? current.hasAwayGoalsRule : false,
                  }))
                }
              />
            </Field>
            {(form.numberOfLegs ?? 1) === 2 ? (
              <label className="structure-check">
                <input
                  type="checkbox"
                  checked={form.hasAwayGoalsRule === true}
                  onChange={(event) =>
                    setForm((current) => ({
                      ...current,
                      hasAwayGoalsRule: event.target.checked,
                    }))
                  }
                />
                {t('regulation.hasAwayGoals')}
              </label>
            ) : null}
            <label className="structure-check">
              <input
                type="checkbox"
                checked={form.hasExtraTimeRule === true}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    hasExtraTimeRule: event.target.checked,
                  }))
                }
              />
              {t('regulation.hasTieExtraTime')}
            </label>
            <label className="structure-check">
              <input
                type="checkbox"
                checked={form.hasPenaltyShootoutRule === true}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    hasPenaltyShootoutRule: event.target.checked,
                  }))
                }
              />
              {t('regulation.hasTiePenalty')}
            </label>
          </>
        ) : null}
        {mutation.isError ? <MutationError error={mutation.error} /> : null}
      </form>
    </Dialog>
  );
}

function matchFormFromStage(
  stage: StructureStageHubSummary,
): ReplaceStageMatchRulesRequest {
  return {
    durationPerPeriod: stage.durationPerPeriod,
    numberOfPeriods: stage.numberOfPeriods,
    halfTimeDuration: stage.halfTimeDuration ?? 15,
    forfeitWinnerGoals: stage.forfeitWinnerGoals ?? 3,
    forfeitLoserGoals: stage.forfeitLoserGoals ?? 0,
    hasExtraTime: stage.hasExtraTime,
    extraTimeDurationPerPeriod: stage.extraTimeDurationPerPeriod ?? null,
    extraTimeNumberOfPeriods: stage.extraTimeNumberOfPeriods ?? null,
    hasPenaltyShootout: stage.hasPenaltyShootout,
    penaltyInitialKicksPerTeam: stage.penaltyInitialKicksPerTeam ?? null,
  };
}

function standingFormFromStage(
  stage: StructureStageHubSummary,
): ReplaceStageStandingRulesRequest {
  return {
    winPoints: stage.winPoints ?? 3,
    drawPoints: stage.drawPoints ?? 1,
    lossPoints: stage.lossPoints ?? 0,
    rankingCriteria:
      stage.rankingCriteria && stage.rankingCriteria.length > 0
        ? stage.rankingCriteria
        : DEFAULT_CRITERIA,
  };
}

function tieFormFromStage(
  stage: StructureStageHubSummary,
): ReplaceStageDefaultTieFormatRequest {
  return {
    clear: false,
    numberOfLegs: stage.numberOfLegs === 2 ? 2 : 1,
    hasAwayGoalsRule: stage.hasAwayGoalsRule === true,
    hasExtraTimeRule: stage.hasTieExtraTime === true,
    hasPenaltyShootoutRule: stage.hasTiePenaltyShootout === true,
  };
}

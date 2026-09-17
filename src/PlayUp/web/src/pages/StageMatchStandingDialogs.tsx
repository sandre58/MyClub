import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  useEffect,
  useId,
  useMemo,
  useState,
  type FormEvent,
} from 'react';
import { useTranslation } from 'react-i18next';
import {
  bindStageRegulation,
  replaceStageMatchRules,
  replaceStageStandingRules,
} from '../api';
import { ConfirmDialog } from '../design-system/components/ConfirmDialog';
import { Dialog } from '../design-system/components/Dialog';
import { Field } from '../design-system/components/Field';
import { FormGroup } from '../design-system/components/FormGroup';
import { FormSection } from '../design-system/components/FormSection';
import { InputNumber } from '../design-system/components/InputNumber';
import {
  OutcomePoints,
  OutcomePointsCard,
} from '../design-system/components/OutcomePoints';
import { pointsBaremeWarning } from '../design-system/components/pointsBaremeWarning';
import { ReorderList } from '../design-system/components/ReorderList';
import { Select } from '../design-system/components/Select';
import { SwitchPanel } from '../design-system/components/SwitchPanel';
import { notify } from '../design-system/toastStore';
import { useDiscardConfirm } from '../design-system/useDiscardConfirm';
import {
  CrossIcon,
  EqualIcon,
  PlusIcon,
  TrophyIcon,
} from '../design-system/icons/contentIcons';
import {
  ClassementsNavIcon,
  MatchesNavIcon,
} from '../design-system/icons/shellIcons';
import { MutationError, PendingLabel } from '../ui';
import type {
  RankingCriterion,
  ReplaceStageMatchRulesRequest,
  ReplaceStageStandingRulesRequest,
  StructureRegulationSummary,
  StructureStageHubSummary,
  StructureView,
} from '../types';
import { invalidateAfterStructureMutation } from './structureInvalidation';
import {
  isMatchFrameBound,
  isStandingFrameBound,
} from './structureHubSections';
import {
  ALL_RANKING_CRITERIA,
  DEFAULT_RANKING_CRITERIA,
  normalizeCriteria,
} from './standingCriteria';
import './regulation.css';

function forfeitScoreWarning(
  winner: number | null | undefined,
  loser: number | null | undefined,
  t: (key: string) => string,
): string | undefined {
  if (winner == null || loser == null) {
    return undefined;
  }
  if (winner <= loser) {
    return t('editor.forfeitScoreWarning');
  }
  return undefined;
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

function matchFormFromFrame(
  regulation: StructureRegulationSummary,
): ReplaceStageMatchRulesRequest {
  return {
    durationPerPeriod: regulation.durationPerPeriod,
    numberOfPeriods: regulation.numberOfPeriods,
    halfTimeDuration: regulation.halfTimeDuration ?? 15,
    forfeitWinnerGoals: regulation.forfeitWinnerGoals ?? 3,
    forfeitLoserGoals: regulation.forfeitLoserGoals ?? 0,
    hasExtraTime: regulation.hasExtraTime === true,
    extraTimeDurationPerPeriod: regulation.extraTimeDurationPerPeriod ?? null,
    extraTimeNumberOfPeriods: regulation.extraTimeNumberOfPeriods ?? null,
    hasPenaltyShootout: regulation.hasPenaltyShootout === true,
    penaltyInitialKicksPerTeam: regulation.penaltyInitialKicksPerTeam ?? null,
  };
}

function stableMatchForm(
  form: ReplaceStageMatchRulesRequest,
): ReplaceStageMatchRulesRequest {
  return {
    ...form,
    halfTimeDuration: form.halfTimeDuration ?? 15,
    forfeitWinnerGoals: form.forfeitWinnerGoals ?? 3,
    forfeitLoserGoals: form.forfeitLoserGoals ?? 0,
    hasExtraTime: form.hasExtraTime === true,
    hasPenaltyShootout: form.hasPenaltyShootout === true,
    extraTimeDurationPerPeriod: form.hasExtraTime
      ? (form.extraTimeDurationPerPeriod ?? 15)
      : null,
    extraTimeNumberOfPeriods: form.hasExtraTime
      ? (form.extraTimeNumberOfPeriods ?? 2)
      : null,
    penaltyInitialKicksPerTeam: form.hasPenaltyShootout
      ? (form.penaltyInitialKicksPerTeam ?? 5)
      : null,
  };
}

function standingFormFromStage(
  stage: StructureStageHubSummary,
): ReplaceStageStandingRulesRequest {
  return {
    winPoints: stage.winPoints ?? 3,
    drawPoints: stage.drawPoints ?? 1,
    lossPoints: stage.lossPoints ?? 0,
    rankingCriteria: normalizeCriteria(
      stage.rankingCriteria && stage.rankingCriteria.length > 0
        ? stage.rankingCriteria
        : DEFAULT_RANKING_CRITERIA,
    ),
  };
}

function standingFormFromFrame(
  regulation: StructureRegulationSummary,
): ReplaceStageStandingRulesRequest {
  return {
    winPoints: regulation.winPoints,
    drawPoints: regulation.drawPoints,
    lossPoints: regulation.lossPoints,
    rankingCriteria: normalizeCriteria(regulation.rankingCriteria),
  };
}

function stableStandingForm(
  form: ReplaceStageStandingRulesRequest,
): ReplaceStageStandingRulesRequest {
  return {
    ...form,
    rankingCriteria: normalizeCriteria(form.rankingCriteria),
  };
}

function sameJson(a: unknown, b: unknown): boolean {
  return JSON.stringify(a) === JSON.stringify(b);
}

export function MatchRulesDialog({
  data,
  stage,
  open,
  onClose,
}: {
  data: StructureView;
  stage: StructureStageHubSummary;
  open: boolean;
  onClose: () => void;
}) {
  const { t } = useTranslation('structure');
  const { t: tReg } = useTranslation('regulation');
  const { t: tCommon } = useTranslation('common');
  const formId = useId();
  const queryClient = useQueryClient();
  const wasBound = isMatchFrameBound(stage.defaultsBinding);
  const canReplace = (stage.actions ?? []).includes('ReplaceMatchRules');
  const canBind = (stage.actions ?? []).includes('BindToCompetition');

  const [followFrame, setFollowFrame] = useState(wasBound);
  const [form, setForm] = useState(() => matchFormFromStage(stage));
  const [baselineForm, setBaselineForm] = useState(() =>
    matchFormFromStage(stage),
  );

  const formDirty = !sameJson(
    stableMatchForm(form),
    stableMatchForm(baselineForm),
  );
  const isDirty = followFrame !== wasBound || (!followFrame && formDirty);
  /**
   * Bind when following and currently unbound.
   * Replace only when values changed — Domain unbinds Match parts solely on value delta.
   */
  const canSave = followFrame
    ? canBind && !wasBound
    : canReplace && formDirty;

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
    const bound = isMatchFrameBound(stage.defaultsBinding);
    const next = matchFormFromStage(stage);
    setFollowFrame(bound);
    setForm(next);
    setBaselineForm(next);
    resetDiscard();
  }, [open, stage, resetDiscard]);

  const mutation = useMutation({
    mutationFn: async () => {
      if (followFrame) {
        await bindStageRegulation(stage.stageId, { scope: 'Match' });
        return { demoted: false as boolean };
      }
      const demoted = stage.status === 'Ready';
      await replaceStageMatchRules(stage.stageId, stableMatchForm(form));
      return { demoted };
    },
    onSuccess: async (result) => {
      await invalidateAfterStructureMutation(queryClient, data.competitionId, {
        stageId: stage.stageId,
      });
      if (result.demoted) {
        notify.attention(t('regulation.matchSavedDemotedToast'));
      } else {
        notify.success(t('regulation.matchSavedToast'));
      }
      onClose();
    },
  });

  const setNumber =
    (key: keyof ReplaceStageMatchRulesRequest) => (value: number | null) => {
      if (value == null || !Number.isFinite(value)) {
        return;
      }
      setForm((current) => ({ ...current, [key]: value }));
    };

  const forfeitWarning = forfeitScoreWarning(
    form.forfeitWinnerGoals,
    form.forfeitLoserGoals,
    tReg,
  );
  const fieldsLocked = followFrame;

  function requestSave() {
    if (mutation.isPending || !canSave) {
      return;
    }
    mutation.mutate();
  }

  function requestClose() {
    requestDiscardClose(mutation.isPending);
  }

  return (
    <>
      <Dialog
        open={open}
        onClose={requestClose}
        title={t('regulation.matchTitle')}
        description={stage.name}
        size="md"
        closeLabel={tCommon('close')}
        closeDisabled={mutation.isPending || discardOpen}
        trapFocus={!discardOpen}
        footer={
          <>
            <button
              type="button"
              className="ds-btn ds-btn--ghost"
              disabled={mutation.isPending || discardOpen}
              onClick={requestClose}
            >
              {tCommon('cancel')}
            </button>
            <button
              type="submit"
              form={formId}
              className="ds-btn ds-btn--primary"
              disabled={mutation.isPending || !canSave}
            >
              {mutation.isPending ? (
                <PendingLabel>{t('regulation.saving')}</PendingLabel>
              ) : (
                tReg('editor.save')
              )}
            </button>
          </>
        }
        footerStatus={
          mutation.isError ? <MutationError error={mutation.error} /> : null
        }
      >
        <form
          id={formId}
          className="regulation-editor regulation-editor--stage"
          onSubmit={(event: FormEvent) => {
            event.preventDefault();
            requestSave();
          }}
        >
          <SwitchPanel
            title={t('regulation.followFrame')}
            description={
              wasBound && !followFrame && !formDirty
                ? t('regulation.personalizeMatchHint')
                : t('regulation.followFrameMatchHint')
            }
            checked={followFrame}
            onChange={(checked) => {
              if (checked && !canBind) {
                return;
              }
              if (!checked && !canReplace) {
                return;
              }
              setFollowFrame(checked);
              if (checked) {
                setForm(matchFormFromFrame(data.regulation));
              }
            }}
            switchLabel={t('regulation.followFrame')}
          />

          <FormSection
            id={`${formId}-match`}
            title={tReg('families.match')}
            description={tReg('editor.hint.matchDuration')}
            icon={<MatchesNavIcon size="md" />}
          >
            <fieldset
              className="regulation-editor__fieldset"
              disabled={fieldsLocked}
            >
              <div className="regulation-editor__triple">
                <Field label={tReg('editor.numberOfPeriods')} required>
                  <InputNumber
                    value={form.numberOfPeriods}
                    min={1}
                    max={4}
                    controlsLayout="split"
                    disabled={fieldsLocked}
                    onChange={setNumber('numberOfPeriods')}
                    required
                  />
                </Field>
                <Field label={tReg('editor.durationPerPeriod')} required>
                  <InputNumber
                    value={form.durationPerPeriod}
                    min={1}
                    max={120}
                    controlsLayout="split"
                    suffix={tReg('editor.minutesSuffix')}
                    disabled={fieldsLocked}
                    onChange={setNumber('durationPerPeriod')}
                    required
                  />
                </Field>
                <Field label={tReg('editor.halfTimeDuration')} required>
                  <InputNumber
                    value={form.halfTimeDuration ?? 0}
                    min={0}
                    max={60}
                    controlsLayout="split"
                    suffix={tReg('editor.minutesSuffix')}
                    disabled={fieldsLocked}
                    onChange={setNumber('halfTimeDuration')}
                    required
                  />
                </Field>
              </div>

              <SwitchPanel
                title={tReg('editor.extraTime')}
                description={tReg('editor.hint.extraTime')}
                checked={form.hasExtraTime === true}
                disabled={fieldsLocked}
                onChange={(checked) =>
                  setForm((current) => ({
                    ...current,
                    hasExtraTime: checked,
                    extraTimeNumberOfPeriods:
                      current.extraTimeNumberOfPeriods ?? 2,
                    extraTimeDurationPerPeriod:
                      current.extraTimeDurationPerPeriod ?? 15,
                  }))
                }
                switchLabel={tReg('editor.enableExtraTime')}
              >
                <div className="ds-form--inline">
                  <Field label={tReg('editor.extraTimePeriods')} required>
                    <InputNumber
                      value={form.extraTimeNumberOfPeriods ?? 2}
                      min={1}
                      max={4}
                      controlsLayout="split"
                      disabled={fieldsLocked}
                      onChange={setNumber('extraTimeNumberOfPeriods')}
                    />
                  </Field>
                  <Field label={tReg('editor.extraTimeDuration')} required>
                    <InputNumber
                      value={form.extraTimeDurationPerPeriod ?? 15}
                      min={1}
                      max={45}
                      controlsLayout="split"
                      suffix={tReg('editor.minutesSuffix')}
                      disabled={fieldsLocked}
                      onChange={setNumber('extraTimeDurationPerPeriod')}
                    />
                  </Field>
                </div>
              </SwitchPanel>

              <SwitchPanel
                title={tReg('editor.shootout')}
                description={tReg('editor.hint.penaltyShootout')}
                checked={form.hasPenaltyShootout === true}
                disabled={fieldsLocked}
                onChange={(checked) =>
                  setForm((current) => ({
                    ...current,
                    hasPenaltyShootout: checked,
                    penaltyInitialKicksPerTeam:
                      current.penaltyInitialKicksPerTeam ?? 5,
                  }))
                }
                switchLabel={tReg('editor.enableShootout')}
              >
                <Field label={tReg('editor.shootoutKicks')} required width="sm">
                  <InputNumber
                    value={form.penaltyInitialKicksPerTeam ?? 5}
                    min={1}
                    max={15}
                    controlsLayout="split"
                    disabled={fieldsLocked}
                    onChange={setNumber('penaltyInitialKicksPerTeam')}
                  />
                </Field>
              </SwitchPanel>

              <FormGroup
                title={tReg('forfeit.heading')}
                description={tReg('editor.hint.administrativeResult')}
              >
                <div className="ds-form--inline">
                  <Field
                    label={tReg('editor.forfeitWinner')}
                    required
                    message={forfeitWarning}
                    messageTone={forfeitWarning ? 'warning' : 'hint'}
                  >
                    <InputNumber
                      value={form.forfeitWinnerGoals ?? 3}
                      min={0}
                      max={20}
                      controlsLayout="split"
                      disabled={fieldsLocked}
                      onChange={setNumber('forfeitWinnerGoals')}
                    />
                  </Field>
                  <Field label={tReg('editor.forfeitLoser')} required>
                    <InputNumber
                      value={form.forfeitLoserGoals ?? 0}
                      min={0}
                      max={20}
                      controlsLayout="split"
                      disabled={fieldsLocked}
                      onChange={setNumber('forfeitLoserGoals')}
                    />
                  </Field>
                </div>
              </FormGroup>
            </fieldset>
          </FormSection>
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
        onCancel={cancelDiscard}
        onConfirm={confirmDiscard}
      />
    </>
  );
}

export function StandingRulesDialog({
  data,
  stage,
  open,
  onClose,
}: {
  data: StructureView;
  stage: StructureStageHubSummary;
  open: boolean;
  onClose: () => void;
}) {
  const { t } = useTranslation('structure');
  const { t: tReg } = useTranslation('regulation');
  const { t: tCommon } = useTranslation('common');
  const formId = useId();
  const queryClient = useQueryClient();
  const boundFlag = isStandingFrameBound(stage.defaultsBinding);
  const wasBound = boundFlag !== false;
  const canReplace = (stage.actions ?? []).includes('ReplaceStandingRules');
  const canBind = (stage.actions ?? []).includes('BindToCompetition');

  const [followFrame, setFollowFrame] = useState(wasBound);
  const [form, setForm] = useState(() => standingFormFromStage(stage));
  const [baselineForm, setBaselineForm] = useState(() =>
    standingFormFromStage(stage),
  );
  const [addCriterion, setAddCriterion] = useState<string | null>(null);

  const formDirty = !sameJson(
    stableStandingForm(form),
    stableStandingForm(baselineForm),
  );
  const isDirty = followFrame !== wasBound || (!followFrame && formDirty);
  /**
   * Standing replace always unbinds Points + RankingCriteria — same values still personalize.
   */
  const canSave = followFrame
    ? canBind && !wasBound
    : canReplace && (formDirty || wasBound);

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
    const bound = isStandingFrameBound(stage.defaultsBinding) !== false;
    const next = standingFormFromStage(stage);
    setFollowFrame(bound);
    setForm(next);
    setBaselineForm(next);
    setAddCriterion(null);
    resetDiscard();
  }, [open, stage, resetDiscard]);

  const mutation = useMutation({
    mutationFn: async () => {
      if (followFrame) {
        await bindStageRegulation(stage.stageId, { scope: 'Standing' });
        return;
      }
      await replaceStageStandingRules(stage.stageId, {
        ...form,
        rankingCriteria: normalizeCriteria(form.rankingCriteria),
      });
    },
    onSuccess: async () => {
      await invalidateAfterStructureMutation(queryClient, data.competitionId, {
        stageId: stage.stageId,
      });
      notify.success(t('regulation.standingSavedToast'));
      onClose();
    },
  });

  const setNumber =
    (key: 'winPoints' | 'drawPoints' | 'lossPoints') =>
    (value: number | null) => {
      if (value == null || !Number.isFinite(value)) {
        return;
      }
      setForm((current) => ({ ...current, [key]: value }));
    };

  const baremeWarning = pointsBaremeWarning(
    form.winPoints,
    form.drawPoints,
    form.lossPoints,
    tReg('editor.pointsBaremeWarning'),
  );
  const criteria = form.rankingCriteria ?? DEFAULT_RANKING_CRITERIA;
  const availableCriteria = useMemo(
    () =>
      ALL_RANKING_CRITERIA.filter((criterion) => !criteria.includes(criterion)),
    [criteria],
  );
  const fieldsLocked = followFrame;

  function requestSave() {
    if (mutation.isPending || !canSave) {
      return;
    }
    mutation.mutate();
  }

  function requestClose() {
    requestDiscardClose(mutation.isPending);
  }

  return (
    <>
      <Dialog
        open={open}
        onClose={requestClose}
        title={t('regulation.standingTitle')}
        description={stage.name}
        size="md"
        closeLabel={tCommon('close')}
        closeDisabled={mutation.isPending || discardOpen}
        trapFocus={!discardOpen}
        footer={
          <>
            <button
              type="button"
              className="ds-btn ds-btn--ghost"
              disabled={mutation.isPending || discardOpen}
              onClick={requestClose}
            >
              {tCommon('cancel')}
            </button>
            <button
              type="submit"
              form={formId}
              className="ds-btn ds-btn--primary"
              disabled={mutation.isPending || !canSave}
            >
              {mutation.isPending ? (
                <PendingLabel>{t('regulation.saving')}</PendingLabel>
              ) : (
                tReg('editor.save')
              )}
            </button>
          </>
        }
        footerStatus={
          mutation.isError ? <MutationError error={mutation.error} /> : null
        }
      >
        <form
          id={formId}
          className="regulation-editor regulation-editor--stage"
          onSubmit={(event: FormEvent) => {
            event.preventDefault();
            requestSave();
          }}
        >
          <SwitchPanel
            title={t('regulation.followFrame')}
            description={t('regulation.followFrameStandingHint')}
            checked={followFrame}
            onChange={(checked) => {
              if (checked && !canBind) {
                return;
              }
              if (!checked && !canReplace) {
                return;
              }
              setFollowFrame(checked);
              if (checked) {
                setForm(standingFormFromFrame(data.regulation));
              }
            }}
            switchLabel={t('regulation.followFrame')}
          />

          <FormSection
            id={`${formId}-standing`}
            title={tReg('families.standing')}
            description={tReg('editor.hint.points')}
            icon={<ClassementsNavIcon size="md" />}
          >
            <fieldset
              className="regulation-editor__fieldset"
              disabled={fieldsLocked}
            >
              <Field
                label={tReg('points.heading')}
                message={baremeWarning}
                messageTone={baremeWarning ? 'warning' : 'hint'}
              >
                <OutcomePoints aria-label={tReg('points.aria')}>
                  <OutcomePointsCard
                    tone="win"
                    label={tReg('points.win')}
                    icon={<TrophyIcon size="sm" />}
                    value={
                      <InputNumber
                        value={form.winPoints}
                        min={0}
                        max={99}
                        controlsLayout="split"
                        aria-label={tReg('editor.winPoints')}
                        disabled={fieldsLocked}
                        onChange={setNumber('winPoints')}
                        required
                      />
                    }
                  />
                  <OutcomePointsCard
                    tone="draw"
                    label={tReg('points.draw')}
                    icon={<EqualIcon size="sm" />}
                    value={
                      <InputNumber
                        value={form.drawPoints}
                        min={0}
                        max={99}
                        controlsLayout="split"
                        aria-label={tReg('editor.drawPoints')}
                        disabled={fieldsLocked}
                        onChange={setNumber('drawPoints')}
                        required
                      />
                    }
                  />
                  <OutcomePointsCard
                    tone="loss"
                    label={tReg('points.loss')}
                    icon={<CrossIcon size="sm" />}
                    value={
                      <InputNumber
                        value={form.lossPoints}
                        min={0}
                        max={99}
                        controlsLayout="split"
                        aria-label={tReg('editor.lossPoints')}
                        disabled={fieldsLocked}
                        onChange={setNumber('lossPoints')}
                        required
                      />
                    }
                  />
                </OutcomePoints>
              </Field>

              <Field label={tReg('criteria.heading')}>
                <p className="ds-field__message ds-field__message--hint">
                  {tReg('editor.hint.rankingCriteria')}
                </p>
                <ReorderList
                  items={criteria}
                  getKey={(item) => item}
                  minMoveIndex={1}
                  disabled={fieldsLocked}
                  canDrag={(item) => item !== 'Points'}
                  canRemove={(item) => item !== 'Points'}
                  onReorder={(next) => {
                    const withoutPoints = next.filter(
                      (item) => item !== 'Points',
                    );
                    setForm((current) => ({
                      ...current,
                      rankingCriteria: ['Points', ...withoutPoints],
                    }));
                  }}
                  onRemove={(item) => {
                    if (item === 'Points') {
                      return;
                    }
                    setForm((current) => ({
                      ...current,
                      rankingCriteria: normalizeCriteria(
                        (current.rankingCriteria ?? []).filter(
                          (entry) => entry !== item,
                        ),
                      ),
                    }));
                  }}
                  aria-label={tReg('criteria.aria')}
                  renderContent={(item) => tReg(`criteria.${item}`)}
                />
                <Select
                  className="regulation-editor__add-criteria"
                  value={addCriterion}
                  placeholder={tReg('editor.addCriterionPlaceholder')}
                  leadingIcon={<PlusIcon size="sm" />}
                  disabled={fieldsLocked}
                  options={availableCriteria.map((criterion) => ({
                    value: criterion,
                    label: tReg(`criteria.${criterion}`),
                  }))}
                  onChange={(next) => {
                    setAddCriterion(null);
                    if (!next || next === 'Points') {
                      return;
                    }
                    setForm((current) => ({
                      ...current,
                      rankingCriteria: normalizeCriteria([
                        ...(current.rankingCriteria ?? []),
                        next as RankingCriterion,
                      ]),
                    }));
                  }}
                />
              </Field>
            </fieldset>
          </FormSection>
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
        onCancel={cancelDiscard}
        onConfirm={confirmDiscard}
      />
    </>
  );
}

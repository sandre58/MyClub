import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  useEffect,
  useId,
  useMemo,
  useState,
  type SubmitEvent,
  type ReactNode,
} from 'react';
import { useTranslation } from 'react-i18next';
import { replaceCompetitionRegulation } from '../api';
import {
  ChoiceSwatch,
  ChoiceTile,
} from '../design-system/components/ChoiceTile';
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
import { useDiscardConfirm } from '../design-system/useDiscardConfirm';
import {
  CheckIcon,
  CrossIcon,
  EqualIcon,
  MinusIcon,
  OverviewAttentionIcon,
  PersonIcon,
  PlusIcon,
  TrophyIcon,
} from '../design-system/icons/overviewIcons';
import {
  ClassementsNavIcon,
  MatchesNavIcon,
  RegulationNavIcon,
} from '../design-system/icons/shellIcons';
import { MutationError, PendingLabel } from '../ui';
import type {
  DisciplinaryType,
  StructureView,
  RankingCriterion,
  ReplaceRegulationRequest,
} from '../types';
import { invalidateAfterStructureMutation } from './structureInvalidation';
import {
  buildRegulationImpactPreview,
  type FamilyImpactLine,
  type HeritablePartKey,
  type RegulationImpactPreview,
} from './regulationImpact';
import {
  ALL_RANKING_CRITERIA,
  DEFAULT_RANKING_CRITERIA,
  normalizeCriteria,
} from './standingCriteria';
import './regulation.css';

const CARD_SWATCH: Record<DisciplinaryType, string> = {
  Yellow: '#F5C518',
  Red: '#E11D48',
  White: '#F8FAFC',
};

function formFromView(data: StructureView): ReplaceRegulationRequest {
  const regulation = data.regulation;
  return {
    minimumTeams: regulation.minimumTeams,
    maximumTeams: regulation.maximumTeams,
    durationPerPeriod: regulation.durationPerPeriod,
    numberOfPeriods: regulation.numberOfPeriods,
    halfTimeDuration: regulation.halfTimeDuration ?? 15,
    winPoints: regulation.winPoints,
    drawPoints: regulation.drawPoints,
    lossPoints: regulation.lossPoints,
    forfeitWinnerGoals: regulation.forfeitWinnerGoals ?? 3,
    forfeitLoserGoals: regulation.forfeitLoserGoals ?? 0,
    allowedTypes: [...(regulation.allowedTypes ?? [])],
    rankingCriteria: normalizeCriteria(regulation.rankingCriteria),
    hasExtraTime: regulation.hasExtraTime === true,
    extraTimeDurationPerPeriod: regulation.extraTimeDurationPerPeriod ?? 15,
    extraTimeNumberOfPeriods: regulation.extraTimeNumberOfPeriods ?? 2,
    hasPenaltyShootout: regulation.hasPenaltyShootout === true,
    penaltyInitialKicksPerTeam: regulation.penaltyInitialKicksPerTeam ?? 5,
  };
}

/** Normalize for dirty compare — criteria order + sorted allowedTypes. */
function stableForm(form: ReplaceRegulationRequest): ReplaceRegulationRequest {
  return {
    ...form,
    rankingCriteria: normalizeCriteria(form.rankingCriteria),
    allowedTypes: [...(form.allowedTypes ?? [])].sort(),
  };
}

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

function familyDetail(
  line: FamilyImpactLine,
  t: (key: string) => string,
): string {
  return line.changedParts
    .map((part: HeritablePartKey) => t(`editor.impact.part.${part}`))
    .join(t('editor.impact.detailSeparator'));
}

function buildImpactMessage(
  preview: RegulationImpactPreview,
  t: (key: string, options?: Record<string, unknown>) => string,
): ReactNode {
  return (
    <div className="regulation-impact">
      {preview.demotesToDraft ? (
        <section
          className="regulation-impact__section"
          aria-label={t('editor.impact.competitionState')}
        >
          <h4 className="regulation-impact__heading">
            {t('editor.impact.competitionState')}
          </h4>
          <p className="regulation-impact__consequence regulation-impact__consequence--attention">
            <span
              className="regulation-impact__consequence-icon"
              aria-hidden="true"
            >
              <OverviewAttentionIcon size="sm" />
            </span>
            <span>{t('editor.impact.demotesToDraft')}</span>
          </p>
        </section>
      ) : null}

      {preview.competitionOnly.entry || preview.competitionOnly.discipline ? (
        <section
          className="regulation-impact__section"
          aria-label={t('editor.impact.frame')}
        >
          <h4 className="regulation-impact__heading">
            {t('editor.impact.frame')}
          </h4>
          {preview.competitionOnly.entry ? (
            <div className="regulation-impact__family-block">
              <p className="regulation-impact__family">
                {t('families.entries')}
              </p>
              <p className="regulation-impact__consequence regulation-impact__consequence--updated">
                <span
                  className="regulation-impact__consequence-icon"
                  aria-hidden="true"
                >
                  <CheckIcon size="sm" />
                </span>
                <span>{t('editor.impact.competitionOnlyLine')}</span>
              </p>
            </div>
          ) : null}
          {preview.competitionOnly.discipline ? (
            <div className="regulation-impact__family-block">
              <p className="regulation-impact__family">
                {t('families.discipline')}
              </p>
              <p className="regulation-impact__consequence regulation-impact__consequence--updated">
                <span
                  className="regulation-impact__consequence-icon"
                  aria-hidden="true"
                >
                  <CheckIcon size="sm" />
                </span>
                <span>{t('editor.impact.competitionOnlyLine')}</span>
              </p>
            </div>
          ) : null}
        </section>
      ) : null}

      {preview.families.length > 0 ? (
        <section
          className="regulation-impact__section"
          aria-label={t('editor.impact.stagesUpdate')}
        >
          <h4 className="regulation-impact__heading">
            {t('editor.impact.stagesUpdate')}
          </h4>
          {preview.families.map((line) => {
            const detail = familyDetail(line, t);
            return (
              <div
                key={line.family}
                className="regulation-impact__family-block"
              >
                <p className="regulation-impact__family">
                  {t(`editor.impact.family.${line.family}`)}
                </p>
                {detail ? (
                  <p className="regulation-impact__changes">{detail}</p>
                ) : null}
                {line.inherit > 0 ? (
                  <p className="regulation-impact__consequence regulation-impact__consequence--updated">
                    <span
                      className="regulation-impact__consequence-icon"
                      aria-hidden="true"
                    >
                      <CheckIcon size="sm" />
                    </span>
                    <span>
                      {t('editor.impact.updated', { count: line.inherit })}
                    </span>
                  </p>
                ) : null}
                {line.keepOverride > 0 ? (
                  <p className="regulation-impact__consequence regulation-impact__consequence--kept">
                    <span
                      className="regulation-impact__consequence-icon"
                      aria-hidden="true"
                    >
                      <CrossIcon size="sm" />
                    </span>
                    <span>
                      {t('editor.impact.keep', { count: line.keepOverride })}
                    </span>
                  </p>
                ) : null}
                {line.inherit === 0 && line.keepOverride === 0 ? (
                  <p className="regulation-impact__consequence regulation-impact__consequence--none">
                    <span
                      className="regulation-impact__consequence-icon"
                      aria-hidden="true"
                    >
                      <MinusIcon size="sm" />
                    </span>
                    <span>{t('editor.impact.noneUpdated')}</span>
                  </p>
                ) : null}
              </div>
            );
          })}
        </section>
      ) : null}

      {preview.runningIgnored ? (
        <p className="regulation-impact__footnote">
          {t('editor.impact.runningIgnored')}
        </p>
      ) : null}
    </div>
  );
}

/** Shared ReplaceRegulation dialog — Règlement hub + Structure hub (F3 / Lot 2.5). */
export function RegulationEditorDialog({
  data,
  open,
  onClose,
}: {
  data: StructureView;
  open: boolean;
  onClose: () => void;
}) {
  const { t } = useTranslation('regulation');
  const { t: tCommon } = useTranslation('common');
  const queryClient = useQueryClient();
  const formId = useId();
  const [form, setForm] = useState(() => formFromView(data));
  const [baseline, setBaseline] = useState(() => formFromView(data));
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [pendingPreview, setPendingPreview] =
    useState<RegulationImpactPreview | null>(null);
  const [addCriterion, setAddCriterion] = useState<string | null>(null);

  const isDirty =
    JSON.stringify(stableForm(form)) !== JSON.stringify(stableForm(baseline));

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
    const next = formFromView(data);
    setForm(next);
    setBaseline(next);
    setConfirmOpen(false);
    setPendingPreview(null);
    setAddCriterion(null);
    resetDiscard();
  }, [open, data, resetDiscard]);

  const mutation = useMutation({
    mutationFn: () => {
      const body: ReplaceRegulationRequest = {
        ...form,
        rankingCriteria: normalizeCriteria(form.rankingCriteria),
        extraTimeDurationPerPeriod: form.hasExtraTime
          ? form.extraTimeDurationPerPeriod
          : null,
        extraTimeNumberOfPeriods: form.hasExtraTime
          ? form.extraTimeNumberOfPeriods
          : null,
        penaltyInitialKicksPerTeam: form.hasPenaltyShootout
          ? form.penaltyInitialKicksPerTeam
          : null,
      };
      return replaceCompetitionRegulation(data.competitionId, body);
    },
    onSuccess: async () => {
      await invalidateAfterStructureMutation(
        queryClient,
        data.competitionId,
      );
      setConfirmOpen(false);
      setPendingPreview(null);
      onClose();
    },
  });

  const setNumber =
    (key: keyof ReplaceRegulationRequest) => (value: number | null) => {
      if (value == null || !Number.isFinite(value)) {
        return;
      }
      setForm((current) => ({ ...current, [key]: value }));
    };

  function setMinimumTeams(value: number | null) {
    if (value == null || !Number.isFinite(value)) {
      return;
    }
    setForm((current) => ({
      ...current,
      minimumTeams: value,
      maximumTeams: Math.max(current.maximumTeams, value),
    }));
  }

  function setMaximumTeams(value: number | null) {
    if (value == null || !Number.isFinite(value)) {
      return;
    }
    setForm((current) => ({
      ...current,
      maximumTeams: value,
      minimumTeams: Math.min(current.minimumTeams, value),
    }));
  }

  const baremeWarning = pointsBaremeWarning(
    form.winPoints,
    form.drawPoints,
    form.lossPoints,
    t('editor.pointsBaremeWarning'),
  );
  const forfeitWarning = forfeitScoreWarning(
    form.forfeitWinnerGoals,
    form.forfeitLoserGoals,
    t,
  );

  const criteria = form.rankingCriteria ?? DEFAULT_RANKING_CRITERIA;
  const availableCriteria = useMemo(
    () =>
      ALL_RANKING_CRITERIA.filter((criterion) => !criteria.includes(criterion)),
    [criteria],
  );

  function toggleAllowedType(type: DisciplinaryType) {
    setForm((current) => {
      const selected = current.allowedTypes ?? [];
      const next = selected.includes(type)
        ? selected.filter((item) => item !== type)
        : [...selected, type];
      return { ...current, allowedTypes: next };
    });
  }

  function requestSave() {
    if (mutation.isPending || !isDirty) {
      return;
    }
    const preview = buildRegulationImpactPreview(
      {
        ...form,
        rankingCriteria: normalizeCriteria(form.rankingCriteria),
      },
      data,
    );
    if (!preview.hasChanges) {
      return;
    }
    setPendingPreview(preview);
    setConfirmOpen(true);
  }

  function requestClose() {
    if (confirmOpen) {
      return;
    }
    requestDiscardClose(mutation.isPending);
  }

  return (
    <>
      <Dialog
        open={open}
        onClose={requestClose}
        title={t('editor.title')}
        description={t('editor.subtitle')}
        closeLabel={tCommon('close')}
        closeDisabled={mutation.isPending || confirmOpen || discardOpen}
        trapFocus={!confirmOpen && !discardOpen}
        size="lg"
        footer={
          <>
            <button
              type="button"
              className="ds-btn ds-btn--ghost"
              disabled={mutation.isPending || confirmOpen || discardOpen}
              onClick={requestClose}
            >
              {tCommon('cancel')}
            </button>
            <button
              type="submit"
              form={formId}
              className="ds-btn ds-btn--primary"
              disabled={mutation.isPending || !isDirty}
            >
              {mutation.isPending ? (
                <PendingLabel>{t('editor.saving')}</PendingLabel>
              ) : (
                t('editor.save')
              )}
            </button>
          </>
        }
      >
        <form
          id={formId}
          className="regulation-editor"
          onSubmit={(event: SubmitEvent) => {
            event.preventDefault();
            requestSave();
          }}
        >
          <div className="regulation-editor__grid">
            <div className="regulation-editor__column">
              <FormSection
                id={`${formId}-entries`}
                title={t('families.entries')}
                description={t('editor.hint.entry')}
                icon={<PersonIcon size="md" />}
              >
                <div className="ds-form--inline">
                  <Field label={t('editor.minimumTeams')} required>
                    <InputNumber
                      value={form.minimumTeams}
                      min={1}
                      max={256}
                      controlsLayout="split"
                      aria-label={t('editor.minimumTeams')}
                      onChange={setMinimumTeams}
                      required
                    />
                  </Field>
                  <Field label={t('editor.maximumTeams')} required>
                    <InputNumber
                      value={form.maximumTeams}
                      min={1}
                      max={256}
                      controlsLayout="split"
                      aria-label={t('editor.maximumTeams')}
                      onChange={setMaximumTeams}
                      required
                    />
                  </Field>
                </div>
              </FormSection>

              <FormSection
                id={`${formId}-standing`}
                title={t('families.standing')}
                description={t('editor.hint.points')}
                icon={<ClassementsNavIcon size="md" />}
              >
                <Field
                  label={t('points.heading')}
                  message={baremeWarning}
                  messageTone={baremeWarning ? 'warning' : 'hint'}
                >
                  <OutcomePoints aria-label={t('points.aria')}>
                    <OutcomePointsCard
                      tone="win"
                      label={t('points.win')}
                      icon={<TrophyIcon size="sm" />}
                      value={
                        <InputNumber
                          value={form.winPoints}
                          min={0}
                          max={99}
                          controlsLayout="split"
                          aria-label={t('editor.winPoints')}
                          onChange={setNumber('winPoints')}
                          required
                        />
                      }
                    />
                    <OutcomePointsCard
                      tone="draw"
                      label={t('points.draw')}
                      icon={<EqualIcon size="sm" />}
                      value={
                        <InputNumber
                          value={form.drawPoints}
                          min={0}
                          max={99}
                          controlsLayout="split"
                          aria-label={t('editor.drawPoints')}
                          onChange={setNumber('drawPoints')}
                          required
                        />
                      }
                    />
                    <OutcomePointsCard
                      tone="loss"
                      label={t('points.loss')}
                      icon={<CrossIcon size="sm" />}
                      value={
                        <InputNumber
                          value={form.lossPoints}
                          min={0}
                          max={99}
                          controlsLayout="split"
                          aria-label={t('editor.lossPoints')}
                          onChange={setNumber('lossPoints')}
                          required
                        />
                      }
                    />
                  </OutcomePoints>
                </Field>

                <Field label={t('criteria.heading')}>
                  <p className="ds-field__message ds-field__message--hint">
                    {t('editor.hint.rankingCriteria')}
                  </p>
                  <ReorderList
                    items={criteria}
                    getKey={(item) => item}
                    minMoveIndex={1}
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
                    aria-label={t('criteria.aria')}
                    renderContent={(item) => t(`criteria.${item}`)}
                  />
                  <Select
                    className="regulation-editor__add-criteria"
                    value={addCriterion}
                    placeholder={t('editor.addCriterionPlaceholder')}
                    leadingIcon={<PlusIcon size="sm" />}
                    options={availableCriteria.map((criterion) => ({
                      value: criterion,
                      label: t(`criteria.${criterion}`),
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

                <FormGroup
                  title={t('forfeit.heading')}
                  description={t('editor.hint.administrativeResult')}
                >
                  <div className="ds-form--inline">
                    <Field
                      label={t('editor.forfeitWinner')}
                      required
                      message={forfeitWarning}
                      messageTone={forfeitWarning ? 'warning' : 'hint'}
                    >
                      <InputNumber
                        value={form.forfeitWinnerGoals ?? 3}
                        min={0}
                        max={20}
                        controlsLayout="split"
                        aria-label={t('editor.forfeitWinner')}
                        onChange={setNumber('forfeitWinnerGoals')}
                      />
                    </Field>
                    <Field label={t('editor.forfeitLoser')} required>
                      <InputNumber
                        value={form.forfeitLoserGoals ?? 0}
                        min={0}
                        max={20}
                        controlsLayout="split"
                        aria-label={t('editor.forfeitLoser')}
                        onChange={setNumber('forfeitLoserGoals')}
                      />
                    </Field>
                  </div>
                </FormGroup>
              </FormSection>
            </div>

            <div className="regulation-editor__column">
              <FormSection
                id={`${formId}-match`}
                title={t('families.match')}
                description={t('editor.hint.matchDuration')}
                icon={<MatchesNavIcon size="md" />}
              >
                <div className="regulation-editor__triple">
                  <Field label={t('editor.numberOfPeriods')} required>
                    <InputNumber
                      value={form.numberOfPeriods}
                      min={1}
                      max={4}
                      controlsLayout="split"
                      onChange={setNumber('numberOfPeriods')}
                      required
                    />
                  </Field>
                  <Field label={t('editor.durationPerPeriod')} required>
                    <InputNumber
                      value={form.durationPerPeriod}
                      min={1}
                      max={120}
                      controlsLayout="split"
                      suffix={t('editor.minutesSuffix')}
                      onChange={setNumber('durationPerPeriod')}
                      required
                    />
                  </Field>
                  <Field label={t('editor.halfTimeDuration')} required>
                    <InputNumber
                      value={form.halfTimeDuration ?? 0}
                      min={0}
                      max={60}
                      controlsLayout="split"
                      suffix={t('editor.minutesSuffix')}
                      onChange={setNumber('halfTimeDuration')}
                      required
                    />
                  </Field>
                </div>

                <SwitchPanel
                  title={t('editor.extraTime')}
                  description={t('editor.hint.extraTime')}
                  checked={form.hasExtraTime === true}
                  onChange={(checked) =>
                    setForm((current) => ({
                      ...current,
                      hasExtraTime: checked,
                    }))
                  }
                  switchLabel={t('editor.enableExtraTime')}
                >
                  <div className="ds-form--inline">
                    <Field label={t('editor.extraTimePeriods')} required>
                      <InputNumber
                        value={form.extraTimeNumberOfPeriods ?? 2}
                        min={1}
                        max={4}
                        controlsLayout="split"
                        onChange={setNumber('extraTimeNumberOfPeriods')}
                      />
                    </Field>
                    <Field label={t('editor.extraTimeDuration')} required>
                      <InputNumber
                        value={form.extraTimeDurationPerPeriod ?? 15}
                        min={1}
                        max={45}
                        controlsLayout="split"
                        suffix={t('editor.minutesSuffix')}
                        onChange={setNumber('extraTimeDurationPerPeriod')}
                      />
                    </Field>
                  </div>
                </SwitchPanel>

                <SwitchPanel
                  title={t('editor.shootout')}
                  description={t('editor.hint.penaltyShootout')}
                  checked={form.hasPenaltyShootout === true}
                  onChange={(checked) =>
                    setForm((current) => ({
                      ...current,
                      hasPenaltyShootout: checked,
                    }))
                  }
                  switchLabel={t('editor.enableShootout')}
                >
                  <Field label={t('editor.shootoutKicks')} required width="sm">
                    <InputNumber
                      value={form.penaltyInitialKicksPerTeam ?? 5}
                      min={1}
                      max={15}
                      controlsLayout="split"
                      onChange={setNumber('penaltyInitialKicksPerTeam')}
                    />
                  </Field>
                </SwitchPanel>
              </FormSection>

              <FormSection
                id={`${formId}-discipline`}
                title={t('families.discipline')}
                description={t('editor.hint.discipline')}
                icon={<RegulationNavIcon size="md" />}
              >
                <Field label={t('discipline.subtitle')}>
                  <div
                    className="ds-choice-tile-row"
                    role="group"
                    aria-label={t('discipline.aria')}
                  >
                    {(['Yellow', 'Red', 'White'] as const).map((type) => (
                      <ChoiceTile
                        key={type}
                        label={t(`discipline.${type}`)}
                        selected={(form.allowedTypes ?? []).includes(type)}
                        leading={
                          <ChoiceSwatch
                            color={CARD_SWATCH[type]}
                            label={t(`discipline.${type}`)}
                          />
                        }
                        onChange={() => toggleAllowedType(type)}
                      />
                    ))}
                  </div>
                </Field>
              </FormSection>
            </div>
          </div>

          {mutation.isError ? <MutationError error={mutation.error} /> : null}
        </form>
      </Dialog>

      <ConfirmDialog
        open={confirmOpen && pendingPreview != null}
        title={t('editor.impactTitle')}
        message={pendingPreview ? buildImpactMessage(pendingPreview, t) : null}
        confirmLabel={t('editor.impactConfirm')}
        cancelLabel={tCommon('cancel')}
        closeLabel={tCommon('close')}
        confirmPending={mutation.isPending}
        confirmPendingLabel={t('editor.saving')}
        onCancel={() => {
          setConfirmOpen(false);
          setPendingPreview(null);
        }}
        onConfirm={() => mutation.mutate()}
      />

      <ConfirmDialog
        open={discardOpen}
        title={t('editor.discardTitle')}
        message={t('editor.discardMessage')}
        confirmLabel={t('editor.discardConfirm')}
        cancelLabel={tCommon('cancel')}
        closeLabel={tCommon('close')}
        onCancel={cancelDiscard}
        onConfirm={confirmDiscard}
      />
    </>
  );
}

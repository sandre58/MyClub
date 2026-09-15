import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useEffect, useId, useMemo, useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import {
  replaceRoundTieFormat,
  replaceStageDefaultTieFormat,
} from '../api';
import { ConfirmDialog } from '../design-system/components/ConfirmDialog';
import { Dialog } from '../design-system/components/Dialog';
import { Field } from '../design-system/components/Field';
import { SwitchPanel } from '../design-system/components/SwitchPanel';
import { ToggleButtonGroup } from '../design-system/components/ToggleButtonGroup';
import { Tooltip } from '../design-system/components/Tooltip';
import { ConfrontationIcon } from '../design-system/icons/contentIcons';
import {
  AttentionIcon,
  RegulationNavIcon,
} from '../design-system/icons/shellIcons';
import { notify } from '../design-system/toastStore';
import { useDiscardConfirm } from '../design-system/useDiscardConfirm';
import { MutationError, PendingLabel } from '../ui';
import type {
  ReplaceRoundTieFormatRequest,
  ReplaceStageDefaultTieFormatRequest,
  StructureConfrontationSegment,
  StructureStageHubSummary,
  StructureTieFormatSummary,
} from '../types';
import { invalidateAfterStructureMutation } from './structureInvalidation';
import './regulation.css';

type TieFormState = {
  numberOfLegs: 1 | 2;
  /** Domain V1: forced true when two legs, false when one. */
  aggregateScoring: boolean;
  hasAwayGoalsRule: boolean;
  hasExtraTimeRule: boolean;
  hasPenaltyShootoutRule: boolean;
};

type RoundTieRow = {
  roundId: string;
  name: string;
  sortOrder: number;
};

type NavTarget = 'phase' | string;

function defaultOneLegForm(): TieFormState {
  return {
    numberOfLegs: 1,
    aggregateScoring: false,
    hasAwayGoalsRule: false,
    hasExtraTimeRule: false,
    hasPenaltyShootoutRule: false,
  };
}

function withLegs(form: TieFormState, legs: 1 | 2): TieFormState {
  const twoLegs = legs === 2;
  return {
    ...form,
    numberOfLegs: legs,
    aggregateScoring: twoLegs,
    hasAwayGoalsRule: twoLegs ? form.hasAwayGoalsRule : false,
  };
}

function tieFormFromSummary(
  summary: StructureTieFormatSummary | null | undefined,
): TieFormState {
  if (!summary) {
    return defaultOneLegForm();
  }
  const legs = summary.numberOfLegs === 2 ? 2 : 1;
  const aggregate = legs === 2;
  return {
    numberOfLegs: legs,
    aggregateScoring: aggregate,
    hasAwayGoalsRule: aggregate && summary.hasAwayGoalsRule === true,
    hasExtraTimeRule: summary.hasTieExtraTime === true,
    hasPenaltyShootoutRule: summary.hasTiePenaltyShootout === true,
  };
}

function tieFormFromSegment(segment: StructureConfrontationSegment): TieFormState {
  const legs = segment.numberOfLegs === 2 ? 2 : 1;
  const aggregate = legs === 2;
  return {
    numberOfLegs: legs,
    aggregateScoring: aggregate,
    hasAwayGoalsRule: aggregate && segment.hasAwayGoalsRule === true,
    hasExtraTimeRule: segment.hasTieExtraTime === true,
    hasPenaltyShootoutRule: segment.hasTiePenaltyShootout === true,
  };
}

function stableTie(form: TieFormState): TieFormState {
  const legs = form.numberOfLegs === 2 ? 2 : 1;
  const aggregate = legs === 2;
  return {
    numberOfLegs: legs,
    aggregateScoring: aggregate,
    hasAwayGoalsRule: aggregate && form.hasAwayGoalsRule,
    hasExtraTimeRule: form.hasExtraTimeRule,
    hasPenaltyShootoutRule: form.hasPenaltyShootoutRule,
  };
}

function sameTie(a: TieFormState, b: TieFormState): boolean {
  return JSON.stringify(stableTie(a)) === JSON.stringify(stableTie(b));
}

function cloneTie(form: TieFormState): TieFormState {
  return { ...stableTie(form) };
}

function toRequest(form: TieFormState): ReplaceStageDefaultTieFormatRequest {
  const stable = stableTie(form);
  return {
    clear: false,
    numberOfLegs: stable.numberOfLegs,
    hasAwayGoalsRule: stable.hasAwayGoalsRule,
    hasExtraTimeRule: stable.hasExtraTimeRule,
    hasPenaltyShootoutRule: stable.hasPenaltyShootoutRule,
  };
}

function flattenRounds(
  segments: StructureConfrontationSegment[] | null | undefined,
): RoundTieRow[] {
  const rows: RoundTieRow[] = [];
  for (const segment of segments ?? []) {
    for (const round of segment.rounds) {
      rows.push({
        roundId: round.roundId,
        name: round.name,
        sortOrder: round.sortOrder,
      });
    }
  }
  return rows.sort((a, b) => a.sortOrder - b.sortOrder);
}

function roundFormsFromStage(
  stage: StructureStageHubSummary,
): Record<string, TieFormState> {
  const next: Record<string, TieFormState> = {};
  for (const segment of stage.confrontationSegments ?? []) {
    const form = tieFormFromSegment(segment);
    for (const round of segment.rounds) {
      next[round.roundId] = form;
    }
  }
  return next;
}

function TieFormatFields({
  form,
  onChange,
  disabled = false,
}: {
  form: TieFormState;
  onChange: (next: TieFormState) => void;
  disabled?: boolean;
}) {
  const { t } = useTranslation('structure');
  const twoLegs = form.numberOfLegs === 2;
  const aggregateOn = twoLegs && form.aggregateScoring;

  return (
    <fieldset className="regulation-editor__fieldset" disabled={disabled}>
      <Field label={t('regulation.numberOfLegs')} required>
        <ToggleButtonGroup
          aria-label={t('regulation.numberOfLegs')}
          value={form.numberOfLegs === 2 ? '2' : '1'}
          disabled={disabled}
          onChange={(value) => {
            onChange(withLegs(form, value === '2' ? 2 : 1));
          }}
          options={[
            { value: '1', label: t('regulation.legsOne') },
            { value: '2', label: t('regulation.legsTwo') },
          ]}
        />
      </Field>

      {/* Collapses in flow when 1 leg; spacer below keeps dialog height stable. */}
      <div
        className="tie-format-editor__legs-options"
        data-visible={twoLegs ? 'true' : 'false'}
        aria-hidden={twoLegs ? undefined : true}
      >
        <SwitchPanel
          title={t('regulation.aggregateScoring')}
          description={t('regulation.aggregateScoringHint')}
          checked={form.aggregateScoring}
          disabled
          onChange={() => undefined}
          switchLabel={t('regulation.aggregateScoring')}
        />

        <SwitchPanel
          title={t('regulation.hasAwayGoals')}
          description={t('regulation.hasAwayGoalsHint')}
          checked={form.hasAwayGoalsRule}
          disabled={disabled || !aggregateOn}
          onChange={(checked) =>
            onChange({ ...form, hasAwayGoalsRule: checked })
          }
          switchLabel={t('regulation.hasAwayGoals')}
        />
      </div>

      <div className="tie-format-editor__equality">
        <p className="tie-format-editor__equality-title">
          {t('regulation.tieEqualityTitle')}
        </p>
        <SwitchPanel
          title={t('regulation.hasTieExtraTime')}
          description={t('regulation.hasTieExtraTimeHint')}
          checked={form.hasExtraTimeRule}
          disabled={disabled}
          onChange={(checked) =>
            onChange({ ...form, hasExtraTimeRule: checked })
          }
          switchLabel={t('regulation.hasTieExtraTime')}
        />
        <SwitchPanel
          title={t('regulation.hasTiePenalty')}
          description={t('regulation.hasTiePenaltyHint')}
          checked={form.hasPenaltyShootoutRule}
          disabled={disabled}
          onChange={(checked) =>
            onChange({ ...form, hasPenaltyShootoutRule: checked })
          }
          switchLabel={t('regulation.hasTiePenalty')}
        />
      </div>

      <div className="tie-format-editor__height-fill" aria-hidden="true" />
    </fieldset>
  );
}

export function TieFormatDialog({
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
  const { t: tReg } = useTranslation('regulation');
  const { t: tCommon } = useTranslation('common');
  const formId = useId();
  const queryClient = useQueryClient();

  const canEditDefault = (stage.actions ?? []).includes(
    'ReplaceDefaultTieFormat',
  );
  const canEditRounds = (stage.actions ?? []).includes('ReplaceRoundTieFormat');

  const rounds = useMemo(
    () => flattenRounds(stage.confrontationSegments),
    [stage.confrontationSegments],
  );

  const [phaseForm, setPhaseForm] = useState(() =>
    tieFormFromSummary(stage.defaultTieFormat),
  );
  const [baselinePhase, setBaselinePhase] = useState(() =>
    tieFormFromSummary(stage.defaultTieFormat),
  );
  const [roundForms, setRoundForms] = useState(() => roundFormsFromStage(stage));
  const [baselineRounds, setBaselineRounds] = useState(() =>
    roundFormsFromStage(stage),
  );
  /** Rounds currently opened for custom editing (even if values still match phase). */
  const [overrideEditing, setOverrideEditing] = useState<Record<string, true>>(
    {},
  );
  const [nav, setNav] = useState<NavTarget>('phase');

  const phaseDirty = canEditDefault && !sameTie(phaseForm, baselinePhase);
  const dirtyRoundIds = rounds
    .map((round) => round.roundId)
    .filter((id) => {
      const current = roundForms[id];
      const baseline = baselineRounds[id];
      if (!current || !baseline) {
        return false;
      }
      return canEditRounds && !sameTie(current, baseline);
    });
  const isDirty = phaseDirty || dirtyRoundIds.length > 0;
  const canSave = isDirty && (canEditDefault || canEditRounds);

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
    const nextPhase = tieFormFromSummary(stage.defaultTieFormat);
    const nextRounds = roundFormsFromStage(stage);
    setPhaseForm(nextPhase);
    setBaselinePhase(nextPhase);
    setRoundForms(nextRounds);
    setBaselineRounds(nextRounds);
    setOverrideEditing({});
    const canDefault = (stage.actions ?? []).includes('ReplaceDefaultTieFormat');
    const firstRound = flattenRounds(stage.confrontationSegments)[0];
    setNav(canDefault ? 'phase' : (firstRound?.roundId ?? 'phase'));
    resetDiscard();
  }, [open, stage, resetDiscard]);

  const mutation = useMutation({
    mutationFn: async () => {
      const tasks: Promise<void>[] = [];
      if (phaseDirty) {
        tasks.push(
          replaceStageDefaultTieFormat(stage.stageId, toRequest(phaseForm)),
        );
      }
      for (const roundId of dirtyRoundIds) {
        const form = roundForms[roundId];
        if (!form) {
          continue;
        }
        const body = toRequest(form) as ReplaceRoundTieFormatRequest;
        tasks.push(replaceRoundTieFormat(stage.stageId, roundId, body));
      }
      await Promise.all(tasks);
      return {
        demoted: stage.status === 'Ready' && tasks.length > 0,
      };
    },
    onSuccess: async (result) => {
      await invalidateAfterStructureMutation(queryClient, competitionId, {
        stageId: stage.stageId,
      });
      if (result.demoted) {
        notify.attention(t('regulation.tieSavedDemotedToast'));
      } else {
        notify.success(t('regulation.tieSavedToast'));
      }
      onClose();
    },
  });

  function requestSave() {
    if (mutation.isPending || !canSave) {
      return;
    }
    mutation.mutate();
  }

  function requestClose() {
    requestDiscardClose(mutation.isPending);
  }

  function isRoundCustomized(roundId: string): boolean {
    const form = roundForms[roundId];
    if (!form) {
      return false;
    }
    return Boolean(overrideEditing[roundId]) || !sameTie(form, phaseForm);
  }

  function startRoundOverride(roundId: string) {
    setRoundForms((current) => ({
      ...current,
      [roundId]: cloneTie(current[roundId] ?? phaseForm),
    }));
    setOverrideEditing((current) => ({ ...current, [roundId]: true }));
  }

  function revertRoundToPhase(roundId: string) {
    setRoundForms((current) => ({
      ...current,
      [roundId]: cloneTie(phaseForm),
    }));
    setOverrideEditing((current) => {
      const next = { ...current };
      delete next[roundId];
      return next;
    });
  }

  const selectedRound =
    nav === 'phase' ? null : rounds.find((round) => round.roundId === nav);
  const selectedRoundForm =
    selectedRound != null
      ? (roundForms[selectedRound.roundId] ?? cloneTie(phaseForm))
      : null;
  const selectedIsCustom =
    selectedRound != null && isRoundCustomized(selectedRound.roundId);

  const showSplit = canEditRounds && rounds.length > 0;

  return (
    <>
      <Dialog
        open={open}
        onClose={requestClose}
        title={t('regulation.tieTitle')}
        description={stage.name}
        size="lg"
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
      >
        <form
          id={formId}
          className="regulation-editor regulation-editor--stage tie-format-editor"
          onSubmit={(event: FormEvent) => {
            event.preventDefault();
            requestSave();
          }}
        >
          <div
            className={
              showSplit
                ? 'tie-format-editor__layout tie-format-editor__layout--split'
                : 'tie-format-editor__layout'
            }
          >
            {showSplit ? (
              <nav
                className="tie-format-editor__nav"
                aria-label={t('regulation.tieNavAria')}
              >
                <p className="tie-format-editor__nav-label">
                  {t('regulation.tieNavFormat')}
                </p>
                {canEditDefault ? (
                  <button
                    type="button"
                    className="tie-format-editor__nav-item tie-format-editor__nav-item--phase ds-selectable-tile"
                    data-selected={nav === 'phase' ? 'true' : 'false'}
                    aria-current={nav === 'phase' ? 'true' : undefined}
                    onClick={() => setNav('phase')}
                  >
                    <span className="tie-format-editor__nav-item-row">
                      <RegulationNavIcon size="sm" aria-hidden />
                      <span className="tie-format-editor__nav-item-title">
                        {t('regulation.tieNavPhase')}
                      </span>
                    </span>
                  </button>
                ) : null}

                <p className="tie-format-editor__nav-label">
                  {t('regulation.tieNavRounds')}
                </p>
                <ul className="tie-format-editor__nav-list">
                  {rounds.map((round) => {
                    const customized = isRoundCustomized(round.roundId);
                    const active = nav === round.roundId;
                    return (
                      <li key={round.roundId}>
                        <button
                          type="button"
                          className="tie-format-editor__nav-item ds-selectable-tile"
                          data-selected={active ? 'true' : 'false'}
                          aria-current={active ? 'true' : undefined}
                          onClick={() => setNav(round.roundId)}
                        >
                          <span className="tie-format-editor__nav-item-row">
                            <ConfrontationIcon size="sm" aria-hidden />
                            <span className="tie-format-editor__nav-item-title">
                              {round.name}
                            </span>
                          </span>
                          {customized ? (
                            <Tooltip content={t('regulation.tieNavCustom')}>
                              <span className="tie-format-editor__nav-override">
                                <AttentionIcon size="sm" aria-hidden />
                                <span className="ds-visually-hidden">
                                  {t('regulation.tieNavCustom')}
                                </span>
                              </span>
                            </Tooltip>
                          ) : null}
                        </button>
                      </li>
                    );
                  })}
                </ul>
              </nav>
            ) : null}

            <div className="tie-format-editor__detail">
              {nav === 'phase' || !showSplit ? (
                canEditDefault ? (
                  <>
                    <header className="tie-format-editor__detail-header">
                      <div className="tie-format-editor__detail-heading">
                        <h3 className="tie-format-editor__detail-title">
                          {t('regulation.tieDefaultTitle')}
                        </h3>
                      </div>
                    </header>
                    <div className="tie-format-editor__detail-body">
                      <TieFormatFields
                        form={phaseForm}
                        onChange={setPhaseForm}
                      />
                    </div>
                  </>
                ) : (
                  <p className="ds-field__message ds-field__message--hint">
                    {t('regulation.tieSelectRoundEmpty')}
                  </p>
                )
              ) : selectedRound && selectedRoundForm ? (
                <>
                  <header className="tie-format-editor__detail-header">
                    <div className="tie-format-editor__detail-heading">
                      <h3 className="tie-format-editor__detail-title">
                        {selectedRound.name}
                      </h3>
                    </div>
                  </header>

                  <div className="tie-format-editor__detail-body">
                    <div
                      className="tie-format-editor__override"
                      data-enabled={selectedIsCustom ? 'true' : 'false'}
                    >
                      <SwitchPanel
                        title={
                          selectedIsCustom
                            ? t('regulation.tieOverrideOnTitle')
                            : t('regulation.tieOverrideOffTitle')
                        }
                        description={
                          selectedIsCustom
                            ? t('regulation.tieOverrideOnHint')
                            : t('regulation.tieOverrideOffHint')
                        }
                        checked={selectedIsCustom}
                        disabled={mutation.isPending}
                        onChange={(checked) => {
                          if (checked) {
                            startRoundOverride(selectedRound.roundId);
                          } else {
                            revertRoundToPhase(selectedRound.roundId);
                          }
                        }}
                        switchLabel={t('regulation.tieOverrideRound')}
                      />
                    </div>
                    <TieFormatFields
                      form={
                        selectedIsCustom ? selectedRoundForm : phaseForm
                      }
                      disabled={!selectedIsCustom}
                      onChange={(next) =>
                        setRoundForms((current) => ({
                          ...current,
                          [selectedRound.roundId]: next,
                        }))
                      }
                    />
                  </div>
                </>
              ) : null}
            </div>
          </div>

          {mutation.isError ? <MutationError error={mutation.error} /> : null}
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

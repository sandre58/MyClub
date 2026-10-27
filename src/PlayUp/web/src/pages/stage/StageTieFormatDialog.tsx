import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useId, useMemo, useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { replaceRoundTieFormat, replaceStageDefaultTieFormat } from '../../api';
import { Alert } from '../../design-system/components/Alert';
import { ConfirmDialog } from '../../design-system/components/ConfirmDialog';
import { Dialog } from '../../design-system/components/Dialog';
import { Field } from '../../design-system/components/Field';
import { SwitchPanel } from '../../design-system/components/SwitchPanel';
import { ToggleButtonGroup } from '../../design-system/components/ToggleButtonGroup';
import { Tooltip } from '../../design-system/components/Tooltip';
import {
  CheckIcon,
  ConfrontationIcon,
} from '../../design-system/icons/contentIcons';
import {
  AttentionIcon,
  CloseIcon,
  RegulationNavIcon,
} from '../../design-system/icons/shellIcons';
import { notify } from '../../design-system/toastStore';
import { useClearMutationsOnOpen } from '../../design-system/useClearMutationsOnOpen';
import { useSyncWhen } from '../../design-system/useRenderSync';
import { useDiscardConfirm } from '../../design-system/useDiscardConfirm';
import { MutationError, PendingLabel } from '../../ui';
import type {
  ReplaceRoundTieFormatRequest,
  StructureConfrontationSegment,
  StructureStageHubSummary,
} from '../../types';
import { scheduleStructureRefresh } from '../structure/structureInvalidation';
import {
  applyPhaseFormToInheritedRounds,
  buildInheritsAtLoad,
  isRoundCustomized,
  roundPersistenceUpdates,
  shouldShowRoundNavigation,
  singleRoundDriftRoundId,
  type TieFormatSessionFlags,
} from './tieFormatEditorState';
import {
  cloneTie,
  roundFormsFromStage,
  sameTie,
  tieFormFromSummary,
  toRequest,
  withLegs,
  type TieFormState,
} from './tieFormatForm';
import '../regulation/regulation-shared.css';

type RoundTieRow = {
  roundId: string;
  name: string;
  sortOrder: number;
};

type NavTarget = 'phase' | string;

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
  structureStages,
  open,
  onClose,
}: {
  competitionId: string;
  stage: StructureStageHubSummary;
  structureStages?: readonly StructureStageHubSummary[];
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
  const roundIds = useMemo(
    () => rounds.map((round) => round.roundId),
    [rounds],
  );

  const [phaseForm, setPhaseForm] = useState(() =>
    tieFormFromSummary(stage.defaultTieFormat),
  );
  const [baselinePhase, setBaselinePhase] = useState(() =>
    tieFormFromSummary(stage.defaultTieFormat),
  );
  const [roundForms, setRoundForms] = useState(() =>
    roundFormsFromStage(stage),
  );
  const [baselineRounds, setBaselineRounds] = useState(() =>
    roundFormsFromStage(stage),
  );
  const [inheritsAtLoad, setInheritsAtLoad] = useState<Record<string, boolean>>(
    () => ({}),
  );
  /** Session-only: organizer explicitly enabled per-round override. */
  const [overrideEditing, setOverrideEditing] = useState<Record<string, true>>(
    {},
  );
  /** Session-only: single-round historical drift realigned to phase default. */
  const [sessionRealigned, setSessionRealigned] = useState<
    Record<string, true>
  >({});
  const [nav, setNav] = useState<NavTarget>('phase');

  const sessionFlags = useMemo(
    (): TieFormatSessionFlags => ({
      inheritsAtLoad,
      sessionOverride: overrideEditing,
      sessionRealigned,
    }),
    [inheritsAtLoad, overrideEditing, sessionRealigned],
  );

  const phaseDirty = canEditDefault && !sameTie(phaseForm, baselinePhase);
  const roundUpdates = useMemo(
    () =>
      roundPersistenceUpdates(
        roundIds,
        sessionFlags,
        phaseForm,
        roundForms,
        baselineRounds,
      ),
    [roundIds, sessionFlags, phaseForm, roundForms, baselineRounds],
  );
  const roundUpdateIds = Object.keys(roundUpdates);
  const isDirty = phaseDirty || (canEditRounds && roundUpdateIds.length > 0);
  const canSave = isDirty && (canEditDefault || canEditRounds);

  const {
    discardOpen,
    requestClose: requestDiscardClose,
    cancelDiscard,
    confirmDiscard,
    resetDiscard,
  } = useDiscardConfirm(isDirty, onClose);

  useSyncWhen(open, stage.stageId, () => {
    const nextPhase = tieFormFromSummary(stage.defaultTieFormat);
    const nextRounds = roundFormsFromStage(stage);
    const nextRoundIds = flattenRounds(stage.confrontationSegments).map(
      (round) => round.roundId,
    );
    setPhaseForm(nextPhase);
    setBaselinePhase(nextPhase);
    setRoundForms(nextRounds);
    setBaselineRounds(nextRounds);
    setInheritsAtLoad(buildInheritsAtLoad(nextRoundIds, nextRounds, nextPhase));
    setOverrideEditing({});
    setSessionRealigned({});
    const canDefault = (stage.actions ?? []).includes(
      'ReplaceDefaultTieFormat',
    );
    setNav(canDefault ? 'phase' : (nextRoundIds[0] ?? 'phase'));
    resetDiscard();
  });

  const mutation = useMutation({
    mutationFn: async () => {
      const tasks: Promise<void>[] = [];
      if (phaseDirty) {
        tasks.push(
          replaceStageDefaultTieFormat(stage.stageId, toRequest(phaseForm)),
        );
      }
      for (const roundId of roundUpdateIds) {
        const form = roundUpdates[roundId];
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
    onSuccess: (result) => {
      onClose();
      // Saved format shows on the phase. Toast only when the phase drops to Draft.
      if (result.demoted) {
        notify.attention(t('regulation.tieSavedDemotedToast'));
      }
      scheduleStructureRefresh(queryClient, competitionId, ['hubStage'], {
        stageId: stage.stageId,
      });
    },
  });

  useClearMutationsOnOpen(open, mutation);

  function requestSave() {
    if (mutation.isPending || !canSave) {
      return;
    }
    mutation.mutate();
  }

  function requestClose() {
    requestDiscardClose(mutation.isPending);
  }

  function updatePhaseForm(next: TieFormState) {
    setPhaseForm(next);
    setRoundForms((current) =>
      applyPhaseFormToInheritedRounds(roundIds, sessionFlags, next, current),
    );
  }

  function roundIsCustomized(roundId: string): boolean {
    return isRoundCustomized(roundId, sessionFlags);
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

  function realignRoundToPhase(roundId: string) {
    setSessionRealigned((current) => ({ ...current, [roundId]: true }));
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

  const driftRoundId = singleRoundDriftRoundId(
    roundIds,
    baselineRounds,
    baselinePhase,
  );
  const showHistoricalDrift =
    rounds.length === 1 &&
    driftRoundId != null &&
    !sessionRealigned[driftRoundId];

  const selectedRound =
    nav === 'phase' ? null : rounds.find((round) => round.roundId === nav);
  const selectedRoundForm =
    selectedRound != null
      ? (roundForms[selectedRound.roundId] ?? cloneTie(phaseForm))
      : null;
  const selectedIsCustom =
    selectedRound != null && roundIsCustomized(selectedRound.roundId);

  const showSplit = shouldShowRoundNavigation(rounds.length, canEditRounds);

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
              <CloseIcon size="sm" />
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
                <>
                  <CheckIcon size="sm" />
                  {tReg('editor.save')}
                </>
              )}
            </button>
          </>
        }
        footerStatus={
          mutation.isError ? (
            <MutationError
              error={mutation.error}
              structureStages={structureStages}
            />
          ) : null
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
                    const customized = roundIsCustomized(round.roundId);
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
                      {showHistoricalDrift && driftRoundId ? (
                        <Alert tone="warning">
                          <p>{t('regulation.tieSingleRoundDriftHint')}</p>
                          <button
                            type="button"
                            className="ds-btn ds-btn--secondary ds-btn--sm"
                            disabled={mutation.isPending}
                            onClick={() => realignRoundToPhase(driftRoundId)}
                          >
                            {t('regulation.tieRealignToPhase')}
                          </button>
                        </Alert>
                      ) : null}
                      <TieFormatFields
                        form={phaseForm}
                        onChange={updatePhaseForm}
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
                      form={selectedIsCustom ? selectedRoundForm : phaseForm}
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

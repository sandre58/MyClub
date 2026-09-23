import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useEffect, useId, useMemo, useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import {
  addCompetitionStage,
  rebuildStageStructure,
  removeCompetitionStage,
} from '../api';
import { Dialog } from '../design-system/components/Dialog';
import { Tooltip } from '../design-system/components/Tooltip';
import {
  ArrowRightIcon,
  CheckIcon,
  PlusIcon,
  TrashIcon,
} from '../design-system/icons/contentIcons';
import {
  ChevronLeftIcon,
  CloseIcon,
} from '../design-system/icons/shellIcons';
import { structureFormatKindLabel } from '../i18n/enumLabels';
import { queryKeys } from '../queryKeys';
import { MutationError, PendingLabel } from '../ui';
import type {
  StructureFormatKind,
  StructureStageHubSummary,
  StructureView,
} from '../types';
import { invalidateAfterStructureMutation } from './structureInvalidation';
import { StructureProgressionDialog } from './StructureProgressionDialog';
import { StructureQualificationDialog } from './StructureQualificationDialog';
import {
  defaultSkeletonForm,
  SkeletonFields,
  skeletonPayload,
  skeletonStepValid,
} from './structureSkeletonForm';

function stageActions(stage: StructureStageHubSummary): string[] {
  return stage.actions ?? [];
}

/** Remove-phase control for the phase fiche (N2) — not page-level chrome. */
export function RemovePhaseAction({
  data,
  stage,
}: {
  data: StructureView;
  stage: StructureStageHubSummary;
}) {
  const { t } = useTranslation('structure');
  const canRemove = stageActions(stage).includes('RemoveStage');
  const [removeOpen, setRemoveOpen] = useState(false);

  return (
    <>
      <Tooltip
        content={
          canRemove ? t('graph.removePhase') : t('graph.removePhaseDisabled')
        }
      >
        <span>
          <button
            type="button"
            className="ds-btn ds-btn--secondary"
            disabled={!canRemove}
            onClick={() => setRemoveOpen(true)}
          >
            <TrashIcon size="sm" />
            {t('graph.removePhase')}
          </button>
        </span>
      </Tooltip>
      {canRemove && (
        <RemovePhaseDialog
          data={data}
          stage={stage}
          open={removeOpen}
          onClose={() => setRemoveOpen(false)}
        />
      )}
    </>
  );
}

export function RelationEditors({
  data,
  stage,
  section,
}: {
  data: StructureView;
  stage: StructureStageHubSummary;
  section: 'qualification' | 'progression';
}) {
  const { t } = useTranslation('structure');
  const [open, setOpen] = useState(false);
  const canEditQual = stageActions(stage).includes('ReplaceQualificationRules');
  const canEditProg = stageActions(stage).includes('ReplaceProgressionRules');

  if (section === 'qualification' && !canEditQual) {
    return null;
  }
  if (section === 'progression' && !canEditProg) {
    return null;
  }

  return (
    <>
      <button
        type="button"
        className="structure-action"
        onClick={() => setOpen(true)}
      >
        {section === 'qualification'
          ? t('graph.editQualification')
          : t('graph.editProgression')}
        <span aria-hidden="true">→</span>
      </button>
      {section === 'qualification' ? (
        <QualificationRulesDialog
          data={data}
          stage={stage}
          open={open}
          onClose={() => setOpen(false)}
        />
      ) : (
        <ProgressionRulesDialog
          data={data}
          stage={stage}
          open={open}
          onClose={() => setOpen(false)}
        />
      )}
    </>
  );
}

/** Thin wrapper — Prog V2 authoring lives in StructureProgressionDialog. */
export function ProgressionRulesDialog({
  data,
  stage,
  open,
  onClose,
  openedFromDestinationStageId = null,
}: {
  data: StructureView;
  stage: StructureStageHubSummary;
  open: boolean;
  onClose: () => void;
  openedFromDestinationStageId?: string | null;
}) {
  return (
    <StructureProgressionDialog
      data={data}
      stage={stage}
      open={open}
      onClose={onClose}
      openedFromDestinationStageId={openedFromDestinationStageId}
    />
  );
}

export function AddPhaseDialog({
  competitionId,
  open,
  onClose,
}: {
  competitionId: string;
  open: boolean;
  onClose: () => void;
}) {
  const { t } = useTranslation('structure');
  const { t: tCommon } = useTranslation('common');
  const formId = useId();
  const queryClient = useQueryClient();
  const [step, setStep] = useState<1 | 2>(1);
  const [name, setName] = useState('');
  const [skeleton, setSkeleton] = useState(defaultSkeletonForm());

  useEffect(() => {
    if (open) {
      setStep(1);
      setName('');
      setSkeleton(defaultSkeletonForm());
    }
  }, [open]);

  const mutation = useMutation({
    mutationFn: () =>
      addCompetitionStage(competitionId, {
        format: skeleton.format,
        name: name.trim(),
        ...skeletonPayload(skeleton),
      }),
    onSuccess: async (response) => {
      queryClient.setQueryData(
        queryKeys.competitions.structure(competitionId),
        response.structure,
      );
      await invalidateAfterStructureMutation(queryClient, competitionId);
      onClose();
    },
  });

  const identityOk = name.trim().length > 0;
  const skeletonOk = skeletonStepValid(skeleton);

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={t('graph.addPhaseTitle')}
      description={
        step === 1 ? t('skeleton.wizardIdentityLede') : t('skeleton.wizardSkeletonLede')
      }
      closeLabel={tCommon('close')}
      closeDisabled={mutation.isPending}
      size="md"
      footer={
        <>
          <button
            type="button"
            className="ds-btn ds-btn--ghost"
            disabled={mutation.isPending}
            onClick={step === 1 ? onClose : () => setStep(1)}
          >
            {step === 1 ? (
              <>
                <CloseIcon size="sm" />
                {tCommon('cancel')}
              </>
            ) : (
              <>
                <ChevronLeftIcon size="sm" />
                {t('skeleton.back')}
              </>
            )}
          </button>
          {step === 1 ? (
            <button
              type="button"
              className="ds-btn ds-btn--primary"
              disabled={!identityOk}
              onClick={() => setStep(2)}
            >
              <ArrowRightIcon size="sm" />
              {t('skeleton.next')}
            </button>
          ) : (
            <button
              type="submit"
              form={formId}
              className="ds-btn ds-btn--primary"
              disabled={!skeletonOk || mutation.isPending}
            >
              {mutation.isPending ? (
                <PendingLabel />
              ) : (
                <>
                  <PlusIcon size="sm" />
                  {t('graph.addPhaseSubmit')}
                </>
              )}
            </button>
          )}
        </>
      }
      footerStatus={
        mutation.isError ? <MutationError error={mutation.error} /> : null
      }
    >
      <form
        id={formId}
        onSubmit={(event: FormEvent) => {
          event.preventDefault();
          if (step !== 2 || !identityOk || !skeletonOk || mutation.isPending) {
            return;
          }
          mutation.mutate();
        }}
      >
        {step === 1 ? (
          <>
            <label className="ds-field">
              <span className="ds-field__label">{t('graph.phaseName')}</span>
              <input
                className="ds-input"
                value={name}
                onChange={(event) => setName(event.target.value)}
                required
                autoFocus
              />
            </label>
            <label className="ds-field">
              <span className="ds-field__label">{t('structure.format')}</span>
              <select
                className="ds-input"
                value={skeleton.format}
                onChange={(event) =>
                  setSkeleton({
                    ...skeleton,
                    format: event.target.value as StructureFormatKind,
                  })
                }
              >
                <option value="Championship">
                  {structureFormatKindLabel('Championship')}
                </option>
                <option value="Groups">
                  {structureFormatKindLabel('Groups')}
                </option>
                <option value="Cup">{structureFormatKindLabel('Cup')}</option>
                <option value="Swiss">
                  {structureFormatKindLabel('Swiss')}
                </option>
              </select>
              <span className="caption">{t('skeleton.formatImmutable')}</span>
            </label>
          </>
        ) : (
          <SkeletonFields
            state={skeleton}
            onChange={setSkeleton}
            t={t}
            formatLocked
          />
        )}
      </form>
    </Dialog>
  );
}

export function EditSkeletonDialog({
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
  const { t: tCommon } = useTranslation('common');
  const formId = useId();
  const queryClient = useQueryClient();
  const format = (stage.formatKind ?? 'Championship') as StructureFormatKind;
  const [stageName, setStageName] = useState(stage.name);
  const [confirmRebuild, setConfirmRebuild] = useState(false);
  const [skeleton, setSkeleton] = useState(() => ({
    ...defaultSkeletonForm(format),
    matchdayCount: Math.max(1, stage.matchdayCount || 1),
    groupCount: Math.max(2, stage.groupCount || 2),
    participantsPerGroup: Math.max(2, stage.placesPerGroup || 2),
    bracketSize: Math.max(2, stage.slotCount || 4),
    swissRoundCount: Math.max(1, stage.swissRoundCount || 3),
    matchGenerationFormat:
      data.structure.matchGenerationFormat ?? 'SingleRoundRobin',
  }));

  useEffect(() => {
    if (!open) {
      return;
    }
    const kind = (stage.formatKind ?? 'Championship') as StructureFormatKind;
    setStageName(stage.name);
    setConfirmRebuild(false);
    setSkeleton({
      ...defaultSkeletonForm(kind),
      matchdayCount: Math.max(1, stage.matchdayCount || 1),
      groupCount: Math.max(2, stage.groupCount || 2),
      participantsPerGroup: Math.max(2, stage.placesPerGroup || 2),
      bracketSize: Math.max(2, stage.slotCount || 4),
      swissRoundCount: Math.max(1, stage.swissRoundCount || 3),
      matchGenerationFormat:
        data.structure.matchGenerationFormat ?? 'SingleRoundRobin',
    });
  }, [open, stage, data.structure.matchGenerationFormat]);

  const mutation = useMutation({
    mutationFn: () =>
      rebuildStageStructure(stage.stageId, {
        format: skeleton.format,
        stageName: stageName.trim() || stage.name,
        ...skeletonPayload(skeleton),
      }),
    onSuccess: async (response) => {
      queryClient.setQueryData(
        queryKeys.competitions.structure(data.competitionId),
        response.structure,
      );
      await invalidateAfterStructureMutation(queryClient, data.competitionId);
      onClose();
    },
  });

  const canSubmit =
    stageName.trim().length > 0 &&
    skeletonStepValid(skeleton) &&
    confirmRebuild;

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={t('skeleton.editTitle')}
      description={t('skeleton.editLede')}
      closeLabel={tCommon('close')}
      closeDisabled={mutation.isPending}
      size="md"
      footer={
        <>
          <button
            type="button"
            className="ds-btn ds-btn--ghost"
            disabled={mutation.isPending}
            onClick={onClose}
          >
            <CloseIcon size="sm" />
            {tCommon('cancel')}
          </button>
          <button
            type="submit"
            form={formId}
            className="ds-btn ds-btn--primary"
            disabled={!canSubmit || mutation.isPending}
          >
            {mutation.isPending ? (
              <PendingLabel>{t('structure.configuring')}</PendingLabel>
            ) : (
              <>
                <CheckIcon size="sm" />
                {t('structure.rebuildSubmit')}
              </>
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
        onSubmit={(event: FormEvent) => {
          event.preventDefault();
          if (!canSubmit || mutation.isPending) {
            return;
          }
          mutation.mutate();
        }}
      >
        <label className="ds-field">
          <span className="ds-field__label">{t('graph.phaseName')}</span>
          <input
            className="ds-input"
            value={stageName}
            onChange={(event) => setStageName(event.target.value)}
            required
          />
        </label>
        <SkeletonFields
          state={skeleton}
          onChange={setSkeleton}
          t={t}
          formatLocked
        />
        <label className="ds-field ds-field--checkbox">
          <input
            type="checkbox"
            checked={confirmRebuild}
            onChange={(event) => setConfirmRebuild(event.target.checked)}
          />
          <span>
            {t('structure.rebuildConfirm', {
              matchdays: stage.matchdayCount ?? 0,
              groups: stage.groupCount ?? 0,
              rounds: stage.roundCount ?? 0,
              slots: stage.slotCount ?? 0,
            })}
          </span>
        </label>
      </form>
    </Dialog>
  );
}

export function RemovePhaseDialog({
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
  const { t: tCommon } = useTranslation('common');
  const queryClient = useQueryClient();

  const inboundQual = data.stages.reduce((count, peer) => {
    if (peer.stageId === stage.stageId) {
      return count;
    }
    return (
      count +
      (peer.qualificationPaths ?? []).filter(
        (path) => path.destinationStageId === stage.stageId,
      ).length
    );
  }, 0);
  const inboundProg = data.stages.reduce((count, peer) => {
    if (peer.stageId === stage.stageId) {
      return count;
    }
    return (
      count +
      (peer.progressionPaths ?? []).filter(
        (path) => path.destinationStageId === stage.stageId,
      ).length
    );
  }, 0);

  const mutation = useMutation({
    mutationFn: () => removeCompetitionStage(data.competitionId, stage.stageId),
    onSuccess: async (response) => {
      queryClient.setQueryData(
        queryKeys.competitions.structure(data.competitionId),
        response.structure,
      );
      await invalidateAfterStructureMutation(queryClient, data.competitionId);
      onClose();
    },
  });

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={t('graph.removePhaseTitle')}
      closeLabel={tCommon('close')}
      closeDisabled={mutation.isPending}
      footer={
        <>
          <button
            type="button"
            className="ds-btn ds-btn--ghost"
            disabled={mutation.isPending}
            onClick={onClose}
          >
            <CloseIcon size="sm" />
            {tCommon('cancel')}
          </button>
          <button
            type="button"
            className="ds-btn ds-btn--destructive"
            disabled={mutation.isPending}
            onClick={() => mutation.mutate()}
          >
            {mutation.isPending ? (
              <PendingLabel />
            ) : (
              <>
                <TrashIcon size="sm" />
                {t('graph.removePhaseSubmit')}
              </>
            )}
          </button>
        </>
      }
      footerStatus={
        mutation.isError ? <MutationError error={mutation.error} /> : null
      }
    >
      <p>{t('graph.removePhaseBody', { name: stage.name })}</p>
      {(inboundQual > 0 || inboundProg > 0) && (
        <p className="structure-issues__hint">
          {t('graph.removePhaseImpact', {
            qualification: inboundQual,
            progression: inboundProg,
          })}
        </p>
      )}
    </Dialog>
  );
}

export function QualificationRulesDialog({
  data,
  stage,
  open,
  onClose,
  openedFromDestinationStageId = null,
}: {
  data: StructureView;
  stage: StructureStageHubSummary;
  open: boolean;
  onClose: () => void;
  /** Fiche stage when the dialog was opened from destination Population. */
  openedFromDestinationStageId?: string | null;
}) {
  return (
    <StructureQualificationDialog
      data={data}
      stage={stage}
      open={open}
      onClose={onClose}
      openedFromDestinationStageId={openedFromDestinationStageId}
    />
  );
}

import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useEffect, useId, useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import {
  addCompetitionStage,
  rebuildStageStructure,
  removeCompetitionStage,
} from '../api';
import { Alert } from '../design-system/components/Alert';
import { ChoiceTile } from '../design-system/components/ChoiceTile';
import { ConfirmDialog } from '../design-system/components/ConfirmDialog';
import { Dialog } from '../design-system/components/Dialog';
import { Field } from '../design-system/components/Field';
import { TextInput } from '../design-system/components/TextInput';
import { Tooltip } from '../design-system/components/Tooltip';
import { useDiscardConfirm } from '../design-system/useDiscardConfirm';
import {
  ChampionshipFormatIcon,
  CupFormatIcon,
  EmptySelectionIcon,
  GroupsFormatIcon,
  PlusIcon,
  StructureIcon,
  SwissFormatIcon,
  TrashIcon,
} from '../design-system/icons/contentIcons';
import { CloseIcon } from '../design-system/icons/shellIcons';
import { structureFormatKindLabel } from '../i18n/enumLabels';
import { queryKeys } from '../queryKeys';
import { EmptyState, MutationError, PendingLabel } from '../ui';
import type {
  MatchGenerationFormat,
  StructureFormatKind,
  StructureStageHubSummary,
  StructureView,
} from '../types';
import { AddPhaseFormatParams } from './AddPhaseFormatParams';
import { invalidateAfterStructureMutation } from './structureInvalidation';
import { StructureProgressionDialog } from './StructureProgressionDialog';
import { StructureQualificationDialog } from './StructureQualificationDialog';
import {
  defaultSkeletonForm,
  skeletonPayload,
  skeletonStepValid,
} from './structureSkeletonForm';

const ADD_PHASE_FORMATS: StructureFormatKind[] = [
  'Championship',
  'Groups',
  'Cup',
  'Swiss',
];

function addPhaseFormatIcon(format: StructureFormatKind) {
  switch (format) {
    case 'Championship':
      return <ChampionshipFormatIcon size="sm" />;
    case 'Groups':
      return <GroupsFormatIcon size="sm" />;
    case 'Cup':
      return <CupFormatIcon size="sm" />;
    case 'Swiss':
      return <SwissFormatIcon size="sm" />;
  }
}

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
  onCreated,
}: {
  competitionId: string;
  open: boolean;
  onClose: () => void;
  /** Called after a successful create so the hub can select the new phase. */
  onCreated?: (stageId: string) => void;
}) {
  const { t } = useTranslation('structure');
  const { t: tCommon } = useTranslation('common');
  const formId = useId();
  const nameId = useId();
  const queryClient = useQueryClient();
  const [name, setName] = useState('');
  const [formatPicked, setFormatPicked] = useState(false);
  const [skeleton, setSkeleton] = useState(defaultSkeletonForm());

  const dirty = name.trim().length > 0 || formatPicked;
  const {
    discardOpen,
    requestClose: requestDiscardClose,
    cancelDiscard,
    confirmDiscard,
    resetDiscard,
  } = useDiscardConfirm(dirty, onClose);

  useEffect(() => {
    if (!open) {
      return;
    }
    setName('');
    setFormatPicked(false);
    setSkeleton(defaultSkeletonForm());
    resetDiscard();
  }, [open, resetDiscard]);

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
      onCreated?.(response.stageId);
    },
  });

  const canSubmit =
    name.trim().length > 0 && formatPicked && skeletonStepValid(skeleton);

  function requestClose() {
    requestDiscardClose(mutation.isPending);
  }

  return (
    <>
      <Dialog
        open={open}
        onClose={requestClose}
        title={t('graph.addPhaseTitle')}
        description={t('skeleton.wizardIdentityLede')}
        closeLabel={tCommon('close')}
        closeDisabled={mutation.isPending || discardOpen}
        trapFocus={!discardOpen}
        size="md"
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
              disabled={!canSubmit || mutation.isPending}
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
          </>
        }
        footerStatus={
          mutation.isError ? <MutationError error={mutation.error} /> : null
        }
      >
        <form
          id={formId}
          className="structure-add-phase"
          onSubmit={(event: FormEvent) => {
            event.preventDefault();
            if (!canSubmit || mutation.isPending) {
              return;
            }
            mutation.mutate();
          }}
        >
          <Field label={t('graph.phaseName')} htmlFor={nameId} required>
            <TextInput
              id={nameId}
              value={name}
              onChange={(event) => setName(event.target.value)}
              placeholder={t('structure.stageNamePlaceholder')}
              required
              autoFocus
            />
          </Field>

          <div
            className="structure-qualification__scope-tiles"
            data-count="4"
            role="radiogroup"
            aria-label={t('skeleton.phaseType')}
          >
            {ADD_PHASE_FORMATS.map((format) => (
              <ChoiceTile
                key={format}
                label={structureFormatKindLabel(format)}
                description={t(`skeleton.formatTile.${format}`)}
                leading={addPhaseFormatIcon(format)}
                selected={formatPicked && skeleton.format === format}
                onChange={(selected) => {
                  if (!selected) return;
                  setFormatPicked(true);
                  setSkeleton(defaultSkeletonForm(format));
                }}
              />
            ))}
          </div>

          <div
            className="structure-add-phase__props"
            data-filled={formatPicked ? 'true' : 'false'}
          >
            {formatPicked ? (
              <>
                <p
                  className="structure-add-phase__format-consequence"
                  role="status"
                >
                  {t('skeleton.formatConsequence')}
                </p>
                <AddPhaseFormatParams state={skeleton} onChange={setSkeleton} />
              </>
            ) : (
              <EmptyState
                variant="idle"
                icon={<EmptySelectionIcon size="lg" />}
                title={t('skeleton.formatAwaitingTitle')}
              >
                {t('skeleton.formatAwaitingType')}
              </EmptyState>
            )}
          </div>
        </form>
      </Dialog>
      <ConfirmDialog
        open={discardOpen}
        title={t('skeleton.discardTitle')}
        message={t('skeleton.discardMessage')}
        confirmLabel={t('skeleton.discardConfirm')}
        cancelLabel={tCommon('cancel')}
        closeLabel={tCommon('close')}
        onCancel={cancelDiscard}
        onConfirm={confirmDiscard}
      />
    </>
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
  const [baseline, setBaseline] = useState(() =>
    skeletonFromStage(stage, data.structure.matchGenerationFormat),
  );
  const [skeleton, setSkeleton] = useState(baseline);

  const dirty = !skeletonFormEqual(skeleton, baseline);
  const {
    discardOpen,
    requestClose: requestDiscardClose,
    cancelDiscard,
    confirmDiscard,
    resetDiscard,
  } = useDiscardConfirm(dirty, onClose);

  useEffect(() => {
    if (!open) {
      return;
    }
    const next = skeletonFromStage(
      stage,
      data.structure.matchGenerationFormat,
    );
    setBaseline(next);
    setSkeleton(next);
    resetDiscard();
  }, [open, stage, data.structure.matchGenerationFormat, resetDiscard]);

  const mutation = useMutation({
    mutationFn: () =>
      rebuildStageStructure(stage.stageId, {
        format: skeleton.format,
        stageName: null,
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

  const canSubmit = skeletonStepValid(skeleton);

  function requestClose() {
    requestDiscardClose(mutation.isPending);
  }

  return (
    <>
      <Dialog
        open={open}
        onClose={requestClose}
        title={t('skeleton.editTitle')}
        description={t('skeleton.editLede')}
        closeLabel={tCommon('close')}
        closeDisabled={mutation.isPending || discardOpen}
        trapFocus={!discardOpen}
        size="md"
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
              disabled={!canSubmit || mutation.isPending}
            >
              {mutation.isPending ? (
                <PendingLabel>{t('structure.configuring')}</PendingLabel>
              ) : (
                <>
                  <StructureIcon size="sm" />
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
          className="structure-edit-forme"
          onSubmit={(event: FormEvent) => {
            event.preventDefault();
            if (!canSubmit || mutation.isPending) {
              return;
            }
            mutation.mutate();
          }}
        >
          <div
            className="structure-qualification__scope-tiles"
            data-count="1"
            role="group"
            aria-label={t('skeleton.phaseType')}
          >
            <ChoiceTile
              label={structureFormatKindLabel(format)}
              description={t(`skeleton.formatTile.${format}`)}
              leading={addPhaseFormatIcon(format)}
              selected
              disabled
              onChange={() => undefined}
            />
          </div>

          <AddPhaseFormatParams state={skeleton} onChange={setSkeleton} />

          <Alert tone="warning" role="status">
            {t('structure.rebuildImpact')}
          </Alert>
        </form>
      </Dialog>
      <ConfirmDialog
        open={discardOpen}
        title={t('skeleton.editDiscardTitle')}
        message={t('skeleton.editDiscardMessage')}
        confirmLabel={t('skeleton.discardConfirm')}
        cancelLabel={tCommon('cancel')}
        closeLabel={tCommon('close')}
        danger
        onCancel={cancelDiscard}
        onConfirm={confirmDiscard}
      />
    </>
  );
}

function skeletonFromStage(
  stage: StructureStageHubSummary,
  matchGenerationFormat: MatchGenerationFormat | null | undefined,
) {
  const kind = (stage.formatKind ?? 'Championship') as StructureFormatKind;
  return {
    ...defaultSkeletonForm(kind),
    groupCount: Math.max(2, stage.groupCount || 2),
    participantsPerGroup: Math.max(2, stage.placesPerGroup || 2),
    bracketSize: Math.max(2, stage.slotCount || 4),
    swissRoundCount: Math.max(1, stage.swissRoundCount || 3),
    matchGenerationFormat: matchGenerationFormat ?? 'SingleRoundRobin',
  };
}

function skeletonFormEqual(
  a: ReturnType<typeof defaultSkeletonForm>,
  b: ReturnType<typeof defaultSkeletonForm>,
): boolean {
  return (
    a.format === b.format &&
    a.groupCount === b.groupCount &&
    a.participantsPerGroup === b.participantsPerGroup &&
    a.bracketSize === b.bracketSize &&
    a.swissRoundCount === b.swissRoundCount &&
    a.matchGenerationFormat === b.matchGenerationFormat
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
      <p className="structure-issues__hint">{t('graph.removePhaseStructure')}</p>
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

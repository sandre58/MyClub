import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useId, useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import {
  addCompetitionStage,
  insertCompetitionStage,
  rebuildStageStructure,
  removeCompetitionStage,
} from '../../api';
import { Alert } from '../../design-system/components/Alert';
import { ChoiceTile } from '../../design-system/components/ChoiceTile';
import { ConfirmDialog } from '../../design-system/components/ConfirmDialog';
import { Dialog } from '../../design-system/components/Dialog';
import { Field } from '../../design-system/components/Field';
import { TextInput } from '../../design-system/components/TextInput';
import { useClearMutationsOnOpen } from '../../design-system/useClearMutationsOnOpen';
import { useSyncWhen } from '../../design-system/useRenderSync';
import { useDiscardConfirm } from '../../design-system/useDiscardConfirm';
import {
  ChampionshipFormatIcon,
  CupFormatIcon,
  EmptySelectionIcon,
  GroupsFormatIcon,
  PlusIcon,
  StructureIcon,
  SwissFormatIcon,
  TrashIcon,
} from '../../design-system/icons/contentIcons';
import { CloseIcon } from '../../design-system/icons/shellIcons';
import { structureFormatKindLabel } from '../../i18n/enumLabels';
import { queryKeys } from '../../queryKeys';
import { EmptyState, MutationError, PendingLabel } from '../../ui';
import type {
  MatchGenerationFormat,
  StageInsertPosition,
  StructureFormatKind,
  StructureStageHubSummary,
  StructureView,
} from '../../types';
import { AddPhaseFormatParams } from './AddPhaseFormatParams';
import { scheduleStructureRefresh } from './structureInvalidation';
import { StructureProgressionDialog } from './StructureProgressionDialog';
import { StructureQualificationDialog } from './StructureQualificationDialog';
import {
  defaultSkeletonForm,
  skeletonPayload,
  skeletonStepValid,
} from './structureSkeletonForm';
import { insertSplitsLinkedPhases } from './structureInsertLinkedPhases';
import { countInboundQualProgRules } from './structureStageQualProgProjection';

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

/** Thin wrapper — Progression authoring lives in StructureProgressionDialog. */
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
  structureStages = [],
  open,
  onClose,
  onCreated,
  placement = null,
  anchorName = '',
}: {
  competitionId: string;
  structureStages?: readonly StructureStageHubSummary[];
  open: boolean;
  onClose: () => void;
  /** Called after a successful create so the hub can select the new phase. */
  onCreated?: (stageId: string) => void;
  /** When set, birth is positioned against this anchor. Append stays the default. */
  placement?: {
    anchorStageId: string;
    position: StageInsertPosition;
  } | null;
  anchorName?: string;
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

  const placementKey = placement
    ? `${placement.anchorStageId}:${placement.position}`
    : 'append';
  useSyncWhen(open, `add-phase:${placementKey}`, () => {
    setName('');
    setFormatPicked(false);
    setSkeleton(defaultSkeletonForm());
    resetDiscard();
  });

  const birthPayload = {
    format: skeleton.format,
    name: name.trim(),
    ...skeletonPayload(skeleton),
  };
  const mutation = useMutation({
    mutationFn: () =>
      placement
        ? insertCompetitionStage(competitionId, {
            ...birthPayload,
            anchorStageId: placement.anchorStageId,
            position: placement.position,
          })
        : addCompetitionStage(competitionId, birthPayload),
    onSuccess: (response) => {
      queryClient.setQueryData(
        queryKeys.competitions.structure(competitionId),
        response.structure,
      );
      onClose();
      onCreated?.(response.stageId);
      scheduleStructureRefresh(queryClient, competitionId, ['graph']);
    },
  });

  useClearMutationsOnOpen(open, mutation);

  const canSubmit =
    name.trim().length > 0 && formatPicked && skeletonStepValid(skeleton);
  const splitsLinkedPhases =
    placement !== null && insertSplitsLinkedPhases(structureStages, placement);

  function requestClose() {
    requestDiscardClose(mutation.isPending);
  }

  return (
    <>
      <Dialog
        open={open}
        onClose={requestClose}
        title={
          placement ? t('graph.insertPhaseTitle') : t('graph.addPhaseTitle')
        }
        description={
          placement
            ? t('graph.insertPhaseLede', {
                position: t(
                  placement.position === 'Before'
                    ? 'graph.positionBefore'
                    : 'graph.positionAfter',
                ),
                anchor: anchorName,
              })
            : t('skeleton.wizardIdentityLede')
        }
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
                <PendingLabel>{t('working')}</PendingLabel>
              ) : (
                <>
                  <PlusIcon size="sm" />
                  {placement
                    ? t('graph.insertPhaseSubmit')
                    : t('graph.addPhaseSubmit')}
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
          className="structure-add-phase"
          onSubmit={(event: FormEvent) => {
            event.preventDefault();
            if (!canSubmit || mutation.isPending) {
              return;
            }
            mutation.mutate();
          }}
        >
          {splitsLinkedPhases ? (
            <Alert tone="warning" role="status">
              {t('graph.insertLinkedPhasesWarning')}
            </Alert>
          ) : null}

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

  useSyncWhen(
    open,
    `${stage.stageId}:${data.structure.matchGenerationFormat}`,
    () => {
      const next = skeletonFromStage(
        stage,
        data.structure.matchGenerationFormat,
      );
      setBaseline(next);
      setSkeleton(next);
      resetDiscard();
    },
  );

  const mutation = useMutation({
    mutationFn: () =>
      rebuildStageStructure(stage.stageId, {
        format: skeleton.format,
        stageName: null,
        ...skeletonPayload(skeleton),
      }),
    onSuccess: (response) => {
      queryClient.setQueryData(
        queryKeys.competitions.structure(data.competitionId),
        response.structure,
      );
      onClose();
      scheduleStructureRefresh(queryClient, data.competitionId, ['graph']);
    },
  });

  useClearMutationsOnOpen(open, mutation);

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
          mutation.isError ? (
            <MutationError
              error={mutation.error}
              structureStages={data.stages}
            />
          ) : null
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

  const { qual: inboundQual, prog: inboundProg } = countInboundQualProgRules(
    data.stages,
    stage.stageId,
    stage.stageId,
  );

  const mutation = useMutation({
    mutationFn: () => removeCompetitionStage(data.competitionId, stage.stageId),
    onSuccess: (response) => {
      queryClient.setQueryData(
        queryKeys.competitions.structure(data.competitionId),
        response.structure,
      );
      onClose();
      scheduleStructureRefresh(queryClient, data.competitionId, ['graph']);
    },
  });

  useClearMutationsOnOpen(open, mutation);

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
              <PendingLabel>{t('working')}</PendingLabel>
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
        mutation.isError ? (
          <MutationError error={mutation.error} structureStages={data.stages} />
        ) : null
      }
    >
      <p>{t('graph.removePhaseBody', { name: stage.name })}</p>
      <p className="structure-issues__hint">
        {t('graph.removePhaseStructure')}
      </p>
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
  /** Stage card when the dialog was opened from destination Population. */
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

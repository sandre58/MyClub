import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useId, useMemo, useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import {
  addCompetitionStage,
  fetchStageOverview,
  rebuildStageStructure,
  removeCompetitionStage,
  replaceStagePlacementAwardRules,
} from '../api';
import { Dialog } from '../design-system/components/Dialog';
import { Tooltip } from '../design-system/components/Tooltip';
import { structureFormatKindLabel } from '../i18n/enumLabels';
import { queryKeys } from '../queryKeys';
import { MutationError, PendingLabel } from '../ui';
import type {
  StructureFormatKind,
  StructurePlacementAward,
  StructureStageHubSummary,
  StructureView,
  ProgressionOutcome,
} from '../types';
import { invalidateAfterStructureMutation } from './structureInvalidation';
import { listFixtureOptions } from './structureFixtureLabels';
import { StructureProgressionDialog } from './StructureProgressionDialog';
import { StructureQualificationDialog } from './StructureQualificationDialog';
import {
  defaultSkeletonForm,
  SkeletonFields,
  skeletonPayload,
  skeletonStepValid,
} from './structureSkeletonForm';

type PlacementDraft = {
  sourceFixtureId: string;
  outcome: ProgressionOutcome;
  rank: string;
};

function stageActions(stage: StructureStageHubSummary): string[] {
  return stage.actions ?? [];
}

function FixtureSourceSelect({
  stageId,
  value,
  onChange,
  required,
}: {
  stageId: string;
  value: string;
  onChange: (fixtureId: string) => void;
  required?: boolean;
}) {
  const { t } = useTranslation('structure');
  const overviewQuery = useQuery({
    queryKey: queryKeys.stages.detail(stageId),
    queryFn: () => fetchStageOverview(stageId),
  });
  const options = useMemo(
    () => listFixtureOptions(overviewQuery.data?.rounds ?? []),
    [overviewQuery.data?.rounds],
  );
  const known = options.some((o) => o.id === value);

  return (
    <select
      className="ds-input"
      value={value}
      onChange={(event) => onChange(event.target.value)}
      required={required}
      disabled={overviewQuery.isLoading}
    >
      <option value="">{t('graph.chooseFixture')}</option>
      {options.map((option) => (
        <option key={option.id} value={option.id}>
          {option.label}
        </option>
      ))}
      {value && !known ? (
        <option value={value}>
          {t('graph.unknownFixture', { id: value.slice(0, 8) })}
        </option>
      ) : null}
    </select>
  );
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
            {step === 1 ? tCommon('cancel') : t('skeleton.back')}
          </button>
          {step === 1 ? (
            <button
              type="button"
              className="ds-btn ds-btn--primary"
              disabled={!identityOk}
              onClick={() => setStep(2)}
            >
              {t('skeleton.next')}
            </button>
          ) : (
            <button
              type="submit"
              form={formId}
              className="ds-btn ds-btn--primary"
              disabled={!skeletonOk || mutation.isPending}
            >
              {mutation.isPending ? <PendingLabel /> : t('graph.addPhaseSubmit')}
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
              t('structure.rebuildSubmit')
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
            {tCommon('cancel')}
          </button>
          <button
            type="button"
            className="ds-btn ds-btn--destructive"
            disabled={mutation.isPending}
            onClick={() => mutation.mutate()}
          >
            {mutation.isPending ? <PendingLabel /> : t('graph.removePhaseSubmit')}
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

export function PlacementAwardRulesDialog({
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
  const [rows, setRows] = useState<PlacementDraft[]>([]);

  useEffect(() => {
    if (!open) {
      return;
    }
    const existing = stage.placementAwards ?? [];
    setRows(
      existing.length > 0
        ? existing.map((path) => ({
            sourceFixtureId: path.sourceFixtureId ?? '',
            outcome: path.outcome,
            rank: String(path.rank),
          }))
        : [
            {
              sourceFixtureId: '',
              outcome: 'Winner',
              rank: '1',
            },
          ],
    );
  }, [open, stage.stageId, stage.placementAwards]);

  const mutation = useMutation({
    mutationFn: (paths: StructurePlacementAward[] | null) =>
      replaceStagePlacementAwardRules(stage.stageId, { paths }),
    onSuccess: async () => {
      await invalidateAfterStructureMutation(queryClient, data.competitionId);
      onClose();
    },
  });

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={t('graph.editPlacementTitle')}
      description={t('graph.editPlacementLede')}
      closeLabel={tCommon('close')}
      closeDisabled={mutation.isPending}
      size="lg"
      footer={
        <>
          <button
            type="button"
            className="ds-btn ds-btn--ghost"
            disabled={mutation.isPending}
            onClick={onClose}
          >
            {tCommon('cancel')}
          </button>
          <button
            type="button"
            className="ds-btn ds-btn--secondary"
            disabled={mutation.isPending}
            onClick={() => mutation.mutate(null)}
          >
            {t('graph.clearRules')}
          </button>
          <button
            type="submit"
            form={formId}
            className="ds-btn ds-btn--primary"
            disabled={mutation.isPending || rows.length === 0}
          >
            {mutation.isPending ? <PendingLabel /> : t('graph.saveRules')}
          </button>
        </>
      }
      footerStatus={
        mutation.isError ? <MutationError error={mutation.error} /> : null
      }
    >
      <form
        id={formId}
        className="structure-graph-dialog"
        onSubmit={(event: FormEvent) => {
          event.preventDefault();
          if (mutation.isPending) {
            return;
          }
          mutation.mutate(
            rows.map((row) => ({
              sourceFixtureId: row.sourceFixtureId.trim(),
              outcome: row.outcome,
              rank: Number(row.rank),
            })),
          );
        }}
      >
        <ul className="structure-graph-rows">
          {rows.map((row, index) => (
            <li key={index} className="structure-graph-row">
              <label className="ds-field">
                <span className="ds-field__label">{t('graph.sourceFixture')}</span>
                <FixtureSourceSelect
                  stageId={stage.stageId}
                  value={row.sourceFixtureId}
                  required
                  onChange={(sourceFixtureId) => {
                    const next = [...rows];
                    next[index] = { ...row, sourceFixtureId };
                    setRows(next);
                  }}
                />
              </label>
              <label className="ds-field">
                <span className="ds-field__label">{t('graph.outcome')}</span>
                <select
                  className="ds-input"
                  value={row.outcome}
                  onChange={(event) => {
                    const next = [...rows];
                    next[index] = {
                      ...row,
                      outcome: event.target.value as ProgressionOutcome,
                    };
                    setRows(next);
                  }}
                >
                  <option value="Winner">Winner</option>
                  <option value="Loser">Loser</option>
                </select>
              </label>
              <label className="ds-field">
                <span className="ds-field__label">{t('graph.rank')}</span>
                <input
                  className="ds-input"
                  type="number"
                  min={1}
                  value={row.rank}
                  onChange={(event) => {
                    const next = [...rows];
                    next[index] = { ...row, rank: event.target.value };
                    setRows(next);
                  }}
                  required
                />
              </label>
              <button
                type="button"
                className="ds-btn ds-btn--ghost"
                onClick={() => setRows(rows.filter((_, i) => i !== index))}
              >
                {t('graph.removeRow')}
              </button>
            </li>
          ))}
        </ul>
        <button
          type="button"
          className="ds-btn ds-btn--secondary"
          onClick={() =>
            setRows([
              ...rows,
              {
                sourceFixtureId: '',
                outcome: 'Loser',
                rank: String(rows.length + 1),
              },
            ])
          }
        >
          {t('graph.addRow')}
        </button>
      </form>
    </Dialog>
  );
}

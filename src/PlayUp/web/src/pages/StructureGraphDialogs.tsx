import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useEffect, useId, useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import {
  addCompetitionStage,
  removeCompetitionStage,
  replaceStageProgressionRules,
  replaceStageQualificationRules,
} from '../api';
import { Dialog } from '../design-system/components/Dialog';
import { queryKeys } from '../queryKeys';
import { MutationError, PendingLabel } from '../ui';
import type {
  StructureProgressionPath,
  StructureQualificationPath,
  StructureStageHubSummary,
  StructureView,
  ProgressionOutcome,
  SelectionMode,
} from '../types';
import { invalidateAfterStructureMutation } from './structureInvalidation';

type QualDraft = {
  order: string;
  selectionMode: SelectionMode;
  selectionValue: string;
  destinationStageId: string;
  destinationSlotKey: string;
};

type ProgDraft = {
  sourceFixtureId: string;
  outcome: ProgressionOutcome;
  destinationStageId: string;
  destinationSlotKey: string;
};

function stageActions(stage: StructureStageHubSummary): string[] {
  return stage.actions ?? [];
}

export function StructureGraphToolbar({
  data,
  stage,
}: {
  data: StructureView;
  stage: StructureStageHubSummary | null;
}) {
  const { t } = useTranslation('structure');
  const canAdd = data.actions.includes('AddCompetitionStage');
  const canRemove =
    stage != null && stageActions(stage).includes('RemoveStage');
  const [addOpen, setAddOpen] = useState(false);
  const [removeOpen, setRemoveOpen] = useState(false);

  if (!canAdd && !canRemove) {
    return null;
  }

  return (
    <div className="structure-graph-toolbar">
      {canAdd && (
        <button
          type="button"
          className="ds-btn ds-btn--secondary"
          onClick={() => setAddOpen(true)}
        >
          {t('graph.addPhase')}
        </button>
      )}
      {canRemove && stage && (
        <button
          type="button"
          className="ds-btn ds-btn--secondary"
          onClick={() => setRemoveOpen(true)}
        >
          {t('graph.removePhase')}
        </button>
      )}
      <AddPhaseDialog
        competitionId={data.competitionId}
        open={addOpen}
        onClose={() => setAddOpen(false)}
      />
      {stage && (
        <RemovePhaseDialog
          data={data}
          stage={stage}
          open={removeOpen}
          onClose={() => setRemoveOpen(false)}
        />
      )}
    </div>
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

export function StructureIssuesBanner({
  stage,
}: {
  stage: StructureStageHubSummary;
}) {
  const { t } = useTranslation('structure');
  const issues = stage.structureIssues ?? [];
  if (issues.length === 0) {
    return null;
  }

  return (
    <div className="structure-issues" role="status">
      <p className="structure-issues__title">{t('graph.issuesHeading')}</p>
      <ul className="structure-issues__list">
        {issues.map((code) => (
          <li key={code}>{t(`graph.issues.${code}`, { defaultValue: code })}</li>
        ))}
      </ul>
      <p className="structure-issues__hint">{t('graph.issuesDraftHint')}</p>
    </div>
  );
}

function AddPhaseDialog({
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
  const [name, setName] = useState('');

  useEffect(() => {
    if (open) {
      setName('');
    }
  }, [open]);

  const mutation = useMutation({
    mutationFn: () => addCompetitionStage(competitionId, name.trim()),
    onSuccess: async () => {
      await invalidateAfterStructureMutation(queryClient, competitionId);
      onClose();
    },
  });

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={t('graph.addPhaseTitle')}
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
            type="submit"
            form={formId}
            className="ds-btn ds-btn--primary"
            disabled={!name.trim() || mutation.isPending}
          >
            {mutation.isPending ? <PendingLabel /> : t('graph.addPhaseSubmit')}
          </button>
        </>
      }
    >
      <form
        id={formId}
        onSubmit={(event: FormEvent) => {
          event.preventDefault();
          if (!name.trim() || mutation.isPending) {
            return;
          }
          mutation.mutate();
        }}
      >
        <label className="ds-field">
          <span className="ds-field__label">{t('graph.phaseName')}</span>
          <input
            className="ds-input"
            value={name}
            onChange={(event) => setName(event.target.value)}
            required
          />
        </label>
        <MutationError error={mutation.error} />
      </form>
    </Dialog>
  );
}

function RemovePhaseDialog({
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
            className="ds-btn ds-btn--primary"
            disabled={mutation.isPending}
            onClick={() => mutation.mutate()}
          >
            {mutation.isPending ? <PendingLabel /> : t('graph.removePhaseSubmit')}
          </button>
        </>
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
      <MutationError error={mutation.error} />
    </Dialog>
  );
}

function QualificationRulesDialog({
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
  const peerStages = data.stages.filter((peer) => peer.stageId !== stage.stageId);
  const defaultDest = peerStages[0]?.stageId ?? '';
  const [rows, setRows] = useState<QualDraft[]>([]);

  useEffect(() => {
    if (!open) {
      return;
    }
    const existing = stage.qualificationPaths ?? [];
    setRows(
      existing.length > 0
        ? existing.map((path) => ({
            order: String(path.order),
            selectionMode: path.selectionMode,
            selectionValue: String(path.selectionValue),
            destinationStageId: path.destinationStageId,
            destinationSlotKey: path.destinationSlotKey,
          }))
        : [
            {
              order: '1',
              selectionMode: 'Top',
              selectionValue: '1',
              destinationStageId: defaultDest,
              destinationSlotKey: '',
            },
          ],
    );
  }, [open, stage.stageId, stage.qualificationPaths, defaultDest]);

  const mutation = useMutation({
    mutationFn: (paths: StructureQualificationPath[] | null) =>
      replaceStageQualificationRules(stage.stageId, { paths }),
    onSuccess: async () => {
      await invalidateAfterStructureMutation(queryClient, data.competitionId);
      onClose();
    },
  });

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={t('graph.editQualificationTitle')}
      description={t('graph.editQualificationLede')}
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
              order: Number(row.order),
              selectionMode: row.selectionMode,
              selectionValue: Number(row.selectionValue),
              destinationStageId: row.destinationStageId,
              destinationSlotKey: row.destinationSlotKey.trim(),
              rankingScope: 'Overall',
            })),
          );
        }}
      >
        <ul className="structure-graph-rows">
          {rows.map((row, index) => (
            <li key={index} className="structure-graph-row">
              <label className="ds-field">
                <span className="ds-field__label">{t('graph.order')}</span>
                <input
                  className="ds-input"
                  type="number"
                  min={1}
                  value={row.order}
                  onChange={(event) => {
                    const next = [...rows];
                    next[index] = { ...row, order: event.target.value };
                    setRows(next);
                  }}
                  required
                />
              </label>
              <label className="ds-field">
                <span className="ds-field__label">{t('graph.selectionMode')}</span>
                <select
                  className="ds-input"
                  value={row.selectionMode}
                  onChange={(event) => {
                    const next = [...rows];
                    next[index] = {
                      ...row,
                      selectionMode: event.target.value as SelectionMode,
                    };
                    setRows(next);
                  }}
                >
                  {(['Position', 'Top', 'Bottom', 'Best', 'Worst'] as SelectionMode[]).map(
                    (mode) => (
                      <option key={mode} value={mode}>
                        {mode}
                      </option>
                    ),
                  )}
                </select>
              </label>
              <label className="ds-field">
                <span className="ds-field__label">{t('graph.selectionValue')}</span>
                <input
                  className="ds-input"
                  type="number"
                  min={1}
                  value={row.selectionValue}
                  onChange={(event) => {
                    const next = [...rows];
                    next[index] = { ...row, selectionValue: event.target.value };
                    setRows(next);
                  }}
                  required
                />
              </label>
              <label className="ds-field">
                <span className="ds-field__label">{t('graph.destinationStage')}</span>
                <select
                  className="ds-input"
                  value={row.destinationStageId}
                  onChange={(event) => {
                    const next = [...rows];
                    next[index] = {
                      ...row,
                      destinationStageId: event.target.value,
                    };
                    setRows(next);
                  }}
                  required
                >
                  <option value="">{t('graph.chooseStage')}</option>
                  {peerStages.map((peer) => (
                    <option key={peer.stageId} value={peer.stageId}>
                      {peer.name}
                    </option>
                  ))}
                </select>
              </label>
              <label className="ds-field">
                <span className="ds-field__label">{t('graph.destinationSlot')}</span>
                <input
                  className="ds-input"
                  value={row.destinationSlotKey}
                  onChange={(event) => {
                    const next = [...rows];
                    next[index] = {
                      ...row,
                      destinationSlotKey: event.target.value,
                    };
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
                order: String(rows.length + 1),
                selectionMode: 'Top',
                selectionValue: '1',
                destinationStageId: defaultDest,
                destinationSlotKey: '',
              },
            ])
          }
        >
          {t('graph.addRow')}
        </button>
        <MutationError error={mutation.error} />
      </form>
    </Dialog>
  );
}

function ProgressionRulesDialog({
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
  const peerStages = data.stages.filter((peer) => peer.stageId !== stage.stageId);
  const defaultDest = peerStages[0]?.stageId ?? '';
  const [rows, setRows] = useState<ProgDraft[]>([]);

  useEffect(() => {
    if (!open) {
      return;
    }
    const existing = stage.progressionPaths ?? [];
    setRows(
      existing.length > 0
        ? existing.map((path) => ({
            sourceFixtureId: path.sourceFixtureId,
            outcome: path.outcome,
            destinationStageId: path.destinationStageId,
            destinationSlotKey: path.destinationSlotKey,
          }))
        : [
            {
              sourceFixtureId: '',
              outcome: 'Winner',
              destinationStageId: defaultDest,
              destinationSlotKey: '',
            },
          ],
    );
  }, [open, stage.stageId, stage.progressionPaths, defaultDest]);

  const mutation = useMutation({
    mutationFn: (paths: StructureProgressionPath[] | null) =>
      replaceStageProgressionRules(stage.stageId, { paths }),
    onSuccess: async () => {
      await invalidateAfterStructureMutation(queryClient, data.competitionId);
      onClose();
    },
  });

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={t('graph.editProgressionTitle')}
      description={t('graph.editProgressionLede')}
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
              destinationStageId: row.destinationStageId,
              destinationSlotKey: row.destinationSlotKey.trim(),
            })),
          );
        }}
      >
        <ul className="structure-graph-rows">
          {rows.map((row, index) => (
            <li key={index} className="structure-graph-row">
              <label className="ds-field">
                <span className="ds-field__label">{t('graph.sourceFixture')}</span>
                <input
                  className="ds-input"
                  value={row.sourceFixtureId}
                  onChange={(event) => {
                    const next = [...rows];
                    next[index] = { ...row, sourceFixtureId: event.target.value };
                    setRows(next);
                  }}
                  required
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
                <span className="ds-field__label">{t('graph.destinationStage')}</span>
                <select
                  className="ds-input"
                  value={row.destinationStageId}
                  onChange={(event) => {
                    const next = [...rows];
                    next[index] = {
                      ...row,
                      destinationStageId: event.target.value,
                    };
                    setRows(next);
                  }}
                  required
                >
                  <option value="">{t('graph.chooseStage')}</option>
                  {peerStages.map((peer) => (
                    <option key={peer.stageId} value={peer.stageId}>
                      {peer.name}
                    </option>
                  ))}
                </select>
              </label>
              <label className="ds-field">
                <span className="ds-field__label">{t('graph.destinationSlot')}</span>
                <input
                  className="ds-input"
                  value={row.destinationSlotKey}
                  onChange={(event) => {
                    const next = [...rows];
                    next[index] = {
                      ...row,
                      destinationSlotKey: event.target.value,
                    };
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
                outcome: 'Winner',
                destinationStageId: defaultDest,
                destinationSlotKey: '',
              },
            ])
          }
        >
          {t('graph.addRow')}
        </button>
        <MutationError error={mutation.error} />
      </form>
    </Dialog>
  );
}

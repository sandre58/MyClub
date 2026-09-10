import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useId, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  addStageGroup,
  addStageMatchday,
  addStageRound,
  addStageSlot,
  renameStage,
  replaceStageMatchGenerationFormat,
  replaceStageSwissSettings,
} from '../api';
import { Dialog } from '../design-system/components/Dialog';
import { matchGenerationFormatLabel } from '../i18n/enumLabels';
import { MutationError, PendingLabel } from '../ui';
import type {
  MatchGenerationFormat,
  OrganisationStageHubSummary,
  OrganisationView,
} from '../types';
import { invalidateAfterStructureMutation } from './structureInvalidation';

function stageActions(stage: OrganisationStageHubSummary): string[] {
  return stage.actions ?? [];
}

/**
 * Locale Construction editors (Lot 3) — additive / rename / RR / Swiss K.
 * Rebuild stays on StructureEditorDialog.
 */
export function ConstructionLocaleActions({
  data,
  stage,
}: {
  data: OrganisationView;
  stage: OrganisationStageHubSummary;
}) {
  const { t } = useTranslation('structure');
  const actions = stageActions(stage);
  const [renameOpen, setRenameOpen] = useState(false);
  const [rrOpen, setRrOpen] = useState(false);
  const [swissOpen, setSwissOpen] = useState(false);
  const queryClient = useQueryClient();

  const addMatchday = useMutation({
    mutationFn: () => addStageMatchday(stage.stageId),
    onSuccess: async () => {
      await invalidateAfterStructureMutation(queryClient, data.competitionId);
    },
  });
  const addGroup = useMutation({
    mutationFn: () => addStageGroup(stage.stageId),
    onSuccess: async () => {
      await invalidateAfterStructureMutation(queryClient, data.competitionId);
    },
  });
  const addRound = useMutation({
    mutationFn: () => addStageRound(stage.stageId, t('locale.defaultRoundName')),
    onSuccess: async () => {
      await invalidateAfterStructureMutation(queryClient, data.competitionId);
    },
  });
  const addSlot = useMutation({
    mutationFn: () =>
      addStageSlot(stage.stageId, `S${data.structure.slotCount + 1}`),
    onSuccess: async () => {
      await invalidateAfterStructureMutation(queryClient, data.competitionId);
    },
  });

  const anyLocale =
    actions.includes('RenameStage') ||
    actions.includes('AddMatchday') ||
    actions.includes('AddGroup') ||
    actions.includes('AddRound') ||
    actions.includes('AddSlot') ||
    actions.includes('ReplaceMatchGenerationFormat') ||
    actions.includes('ReplaceSwissSettings');

  if (!anyLocale) {
    return null;
  }

  return (
    <div className="structure-locale-actions">
      <p className="structure-detail__lede">{t('locale.heading')}</p>
      <div className="structure-graph-toolbar">
        {actions.includes('RenameStage') && (
          <button
            type="button"
            className="ds-btn ds-btn--secondary"
            onClick={() => setRenameOpen(true)}
          >
            {t('locale.rename')}
          </button>
        )}
        {actions.includes('AddMatchday') && (
          <button
            type="button"
            className="ds-btn ds-btn--secondary"
            disabled={addMatchday.isPending}
            onClick={() => addMatchday.mutate()}
          >
            {addMatchday.isPending ? <PendingLabel /> : t('locale.addMatchday')}
          </button>
        )}
        {actions.includes('AddGroup') && (
          <button
            type="button"
            className="ds-btn ds-btn--secondary"
            disabled={addGroup.isPending}
            onClick={() => addGroup.mutate()}
          >
            {addGroup.isPending ? <PendingLabel /> : t('locale.addGroup')}
          </button>
        )}
        {actions.includes('AddRound') && (
          <button
            type="button"
            className="ds-btn ds-btn--secondary"
            disabled={addRound.isPending}
            onClick={() => addRound.mutate()}
          >
            {addRound.isPending ? <PendingLabel /> : t('locale.addRound')}
          </button>
        )}
        {actions.includes('AddSlot') && (
          <button
            type="button"
            className="ds-btn ds-btn--secondary"
            disabled={addSlot.isPending}
            onClick={() => addSlot.mutate()}
          >
            {addSlot.isPending ? <PendingLabel /> : t('locale.addSlot')}
          </button>
        )}
        {actions.includes('ReplaceMatchGenerationFormat') && (
          <button
            type="button"
            className="ds-btn ds-btn--secondary"
            onClick={() => setRrOpen(true)}
          >
            {t('locale.editMatchGeneration')}
          </button>
        )}
        {actions.includes('ReplaceSwissSettings') && (
          <button
            type="button"
            className="ds-btn ds-btn--secondary"
            onClick={() => setSwissOpen(true)}
          >
            {t('locale.editSwiss')}
          </button>
        )}
      </div>
      <MutationError
        error={
          addMatchday.error ??
          addGroup.error ??
          addRound.error ??
          addSlot.error ??
          null
        }
      />
      <RenameStageDialog
        data={data}
        stage={stage}
        open={renameOpen}
        onClose={() => setRenameOpen(false)}
      />
      <MatchGenerationDialog
        data={data}
        stage={stage}
        open={rrOpen}
        onClose={() => setRrOpen(false)}
      />
      <SwissSettingsDialog
        data={data}
        stage={stage}
        open={swissOpen}
        onClose={() => setSwissOpen(false)}
      />
    </div>
  );
}

function RenameStageDialog({
  data,
  stage,
  open,
  onClose,
}: {
  data: OrganisationView;
  stage: OrganisationStageHubSummary;
  open: boolean;
  onClose: () => void;
}) {
  const { t } = useTranslation('structure');
  const { t: tCommon } = useTranslation('common');
  const formId = useId();
  const queryClient = useQueryClient();
  const [name, setName] = useState(stage.name);

  const mutation = useMutation({
    mutationFn: () => renameStage(stage.stageId, name.trim()),
    onSuccess: async () => {
      await invalidateAfterStructureMutation(queryClient, data.competitionId);
      onClose();
    },
  });

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={t('locale.renameTitle')}
      closeLabel={tCommon('close')}
      closeDisabled={mutation.isPending}
      footer={
        <>
          <button
            type="button"
            className="ds-btn ds-btn--ghost"
            onClick={onClose}
            disabled={mutation.isPending}
          >
            {tCommon('cancel')}
          </button>
          <button
            type="submit"
            form={formId}
            className="ds-btn ds-btn--primary"
            disabled={!name.trim() || mutation.isPending}
          >
            {mutation.isPending ? <PendingLabel /> : t('locale.save')}
          </button>
        </>
      }
    >
      <form
        id={formId}
        onSubmit={(event) => {
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

function MatchGenerationDialog({
  data,
  stage,
  open,
  onClose,
}: {
  data: OrganisationView;
  stage: OrganisationStageHubSummary;
  open: boolean;
  onClose: () => void;
}) {
  const { t } = useTranslation('structure');
  const { t: tCommon } = useTranslation('common');
  const formId = useId();
  const queryClient = useQueryClient();
  const [format, setFormat] = useState<MatchGenerationFormat>(
    data.structure.matchGenerationFormat ?? 'SingleRoundRobin',
  );

  const mutation = useMutation({
    mutationFn: () => replaceStageMatchGenerationFormat(stage.stageId, format),
    onSuccess: async () => {
      await invalidateAfterStructureMutation(queryClient, data.competitionId);
      onClose();
    },
  });

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={t('locale.editMatchGenerationTitle')}
      closeLabel={tCommon('close')}
      closeDisabled={mutation.isPending}
      footer={
        <>
          <button
            type="button"
            className="ds-btn ds-btn--ghost"
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
            {mutation.isPending ? <PendingLabel /> : t('locale.save')}
          </button>
        </>
      }
    >
      <form
        id={formId}
        onSubmit={(event) => {
          event.preventDefault();
          if (mutation.isPending) {
            return;
          }
          mutation.mutate();
        }}
      >
        <label className="ds-field">
          <span className="ds-field__label">
            {t('structure.matchGenerationFormat')}
          </span>
          <select
            className="ds-input"
            value={format}
            onChange={(event) =>
              setFormat(event.target.value as MatchGenerationFormat)
            }
          >
            <option value="SingleRoundRobin">
              {matchGenerationFormatLabel('SingleRoundRobin')}
            </option>
            <option value="DoubleRoundRobin">
              {matchGenerationFormatLabel('DoubleRoundRobin')}
            </option>
          </select>
        </label>
        <MutationError error={mutation.error} />
      </form>
    </Dialog>
  );
}

function SwissSettingsDialog({
  data,
  stage,
  open,
  onClose,
}: {
  data: OrganisationView;
  stage: OrganisationStageHubSummary;
  open: boolean;
  onClose: () => void;
}) {
  const { t } = useTranslation('structure');
  const { t: tCommon } = useTranslation('common');
  const formId = useId();
  const queryClient = useQueryClient();
  const [roundCount, setRoundCount] = useState(
    Math.max(1, stage.swissRoundCount ?? data.structure.swissRoundCount ?? 3),
  );

  const mutation = useMutation({
    mutationFn: () => replaceStageSwissSettings(stage.stageId, roundCount),
    onSuccess: async () => {
      await invalidateAfterStructureMutation(queryClient, data.competitionId);
      onClose();
    },
  });

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={t('locale.editSwissTitle')}
      closeLabel={tCommon('close')}
      closeDisabled={mutation.isPending}
      footer={
        <>
          <button
            type="button"
            className="ds-btn ds-btn--ghost"
            onClick={onClose}
            disabled={mutation.isPending}
          >
            {tCommon('cancel')}
          </button>
          <button
            type="submit"
            form={formId}
            className="ds-btn ds-btn--primary"
            disabled={mutation.isPending || roundCount < 1}
          >
            {mutation.isPending ? <PendingLabel /> : t('locale.save')}
          </button>
        </>
      }
    >
      <form
        id={formId}
        onSubmit={(event) => {
          event.preventDefault();
          if (mutation.isPending || roundCount < 1) {
            return;
          }
          mutation.mutate();
        }}
      >
        <label className="ds-field">
          <span className="ds-field__label">{t('structure.swissRoundCount')}</span>
          <input
            className="ds-input"
            type="number"
            min={1}
            value={roundCount}
            onChange={(event) =>
              setRoundCount(Number(event.target.value) || 1)
            }
            required
          />
        </label>
        <MutationError error={mutation.error} />
      </form>
    </Dialog>
  );
}

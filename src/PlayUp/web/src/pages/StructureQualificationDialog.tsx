// -----------------------------------------------------------------------
// Qualification dialog V1 — Sorties source, slot-only paths (Qui → Où).
// -----------------------------------------------------------------------

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Trash2 } from 'lucide-react';
import { useEffect, useId, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  fetchStageOverview,
  fetchStageSchematic,
  replaceStageQualificationRules,
} from '../api';
import { ChoiceTile } from '../design-system/components/ChoiceTile';
import { Dialog } from '../design-system/components/Dialog';
import { Field } from '../design-system/components/Field';
import { InputNumber } from '../design-system/components/InputNumber';
import { Select } from '../design-system/components/Select';
import { LucideIcon } from '../design-system/icons/Icon';
import { ChevronDownIcon } from '../design-system/icons/shellIcons';
import { notify } from '../design-system/toastStore';
import { queryKeys } from '../queryKeys';
import type {
  SchematicCase,
  StageSlot,
  StructureStageHubSummary,
  StructureView,
} from '../types';
import { MutationError, PendingLabel } from '../ui';
import { invalidateAfterStructureMutation } from './structureInvalidation';
import {
  destinationKey,
  emptyQualRow,
  findDuplicateSlotKeys,
  isQualRowComplete,
  parsePositiveInt,
  pathToQualRow,
  summarizeWho,
  toApiPath,
  type QualRowDraft,
  type QualScopeUi,
} from './structureQualificationDraft';

type StructureQualificationDialogProps = {
  data: StructureView;
  stage: StructureStageHubSummary;
  open: boolean;
  onClose: () => void;
};

function listGroupsFromSchematic(cases: SchematicCase[]): {
  id: string;
  name: string;
}[] {
  const map = new Map<string, string>();
  for (const item of cases) {
    const id = item.formPosition.groupId?.trim();
    if (!id) continue;
    const name = item.formPosition.groupName?.trim() || id;
    if (!map.has(id)) map.set(id, name);
  }
  return [...map.entries()]
    .map(([id, name]) => ({ id, name }))
    .sort((a, b) =>
      a.name.localeCompare(b.name, undefined, { sensitivity: 'base' }),
    );
}

function slotLabel(slot: StageSlot): string {
  const name = slot.displayName?.trim();
  return name ? `${slot.slotKey} — ${name}` : slot.slotKey;
}

export function StructureQualificationDialog({
  data,
  stage,
  open,
  onClose,
}: StructureQualificationDialogProps) {
  const { t, i18n } = useTranslation('structure');
  const { t: tCommon } = useTranslation('common');
  const queryClient = useQueryClient();
  const locale = i18n.language ?? 'fr';

  const peerStages = useMemo(
    () => data.stages.filter((peer) => peer.stageId !== stage.stageId),
    [data.stages, stage.stageId],
  );
  const defaultDest = peerStages[0]?.stageId ?? '';

  const [rows, setRows] = useState<QualRowDraft[]>([]);
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [draft, setDraft] = useState<QualRowDraft | null>(null);

  const sourceSchematicQuery = useQuery({
    queryKey: queryKeys.stages.schematic(stage.stageId),
    queryFn: () => fetchStageSchematic(stage.stageId),
    enabled: open,
  });

  const groups = useMemo(
    () => listGroupsFromSchematic(sourceSchematicQuery.data?.cases ?? []),
    [sourceSchematicQuery.data?.cases],
  );

  const destStageIdForSlots =
    draft?.destinationStageId ||
    rows.find((r) => r.id === expandedId)?.destinationStageId ||
    '';

  const destOverviewQuery = useQuery({
    queryKey: queryKeys.stages.detail(destStageIdForSlots),
    queryFn: () => fetchStageOverview(destStageIdForSlots),
    enabled: open && !!destStageIdForSlots,
  });

  const destSlots = destOverviewQuery.data?.slots ?? [];

  useEffect(() => {
    if (!open) {
      setExpandedId(null);
      setDraft(null);
      return;
    }
    const existing = stage.qualificationPaths ?? [];
    setRows(existing.map(pathToQualRow));
    setExpandedId(null);
    setDraft(null);
  }, [open, stage.stageId, stage.qualificationPaths]);

  const duplicates = useMemo(() => findDuplicateSlotKeys(rows), [rows]);
  const validatedCount = rows.filter((r) => r.validated).length;
  const hasInvalidValidated = rows.some(
    (r) => r.validated && !isQualRowComplete(r),
  );
  const hasDuplicate = duplicates.size > 0;
  const expandedIncomplete =
    draft != null && expandedId != null && !isQualRowComplete(draft);
  const hasUnvalidatedDraft = rows.some((r) => !r.validated);

  const canSave =
    peerStages.length > 0 &&
    !hasInvalidValidated &&
    !hasDuplicate &&
    !expandedIncomplete &&
    !hasUnvalidatedDraft &&
    expandedId == null &&
    (validatedCount > 0 ||
      (rows.length === 0 && (stage.qualificationPaths?.length ?? 0) > 0));

  const stageNameById = useMemo(() => {
    const map = new Map(data.stages.map((s) => [s.stageId, s.name]));
    return map;
  }, [data.stages]);

  const mutation = useMutation({
    mutationFn: () => {
      const paths = rows
        .filter((r) => r.validated)
        .map((row, index) => toApiPath(row, index + 1));
      return replaceStageQualificationRules(stage.stageId, {
        paths: paths.length > 0 ? paths : null,
      });
    },
    onSuccess: async () => {
      await invalidateAfterStructureMutation(queryClient, data.competitionId);
      notify.success(t('qualification.toastUpdated'));
      onClose();
    },
  });

  const closeExpansion = () => {
    setExpandedId(null);
    setDraft(null);
  };

  const toggleRow = (row: QualRowDraft) => {
    if (expandedId === row.id) {
      closeExpansion();
      return;
    }
    setExpandedId(row.id);
    setDraft({ ...row });
  };

  const addRow = () => {
    if (peerStages.length === 0) return;
    const row = emptyQualRow(defaultDest);
    row.position = '1';
    if (groups[0]) {
      row.groupId = groups[0].id;
      row.groupName = groups[0].name;
    }
    setRows((prev) => [...prev, row]);
    setExpandedId(row.id);
    setDraft({ ...row });
  };

  const removeRow = (id: string) => {
    setRows((prev) => prev.filter((r) => r.id !== id));
    if (expandedId === id) closeExpansion();
  };

  const validateDraft = () => {
    if (!draft || !isQualRowComplete(draft)) return;
    const groupName =
      draft.scope === 'Group'
        ? (groups.find((g) => g.id === draft.groupId)?.name ?? draft.groupName)
        : '';
    const committed: QualRowDraft = {
      ...draft,
      groupName,
      validated: true,
      destinationSlotKey: draft.destinationSlotKey.trim(),
    };
    setRows((prev) =>
      prev.map((r) => (r.id === committed.id ? committed : r)),
    );
    closeExpansion();
  };

  const updateDraft = (patch: Partial<QualRowDraft>) => {
    setDraft((prev) => (prev ? { ...prev, ...patch } : prev));
  };

  const setScope = (scope: QualScopeUi) => {
    updateDraft({
      scope,
      groupId: scope === 'Group' ? draft?.groupId || groups[0]?.id || '' : '',
      groupName:
        scope === 'Group' ? draft?.groupName || groups[0]?.name || '' : '',
      acrossGroupsPosition:
        scope === 'AcrossGroups' ? draft?.acrossGroupsPosition || '1' : '',
      position: draft?.position || '1',
    });
  };

  return (
    <Dialog
      open={open}
      onClose={onClose}
      size="lg"
      title={t('qualification.title', { phase: stage.name })}
      description={
        peerStages.length === 0
          ? t('graph.editExitNeedsPeer')
          : t('qualification.lede')
      }
      closeLabel={tCommon('close')}
      closeDisabled={mutation.isPending}
      footer={
        <>
          <button
            type="button"
            className="ds-btn ds-btn--secondary"
            disabled={mutation.isPending}
            onClick={onClose}
          >
            {tCommon('cancel')}
          </button>
          <button
            type="button"
            className="ds-btn ds-btn--primary"
            disabled={mutation.isPending || !canSave}
            onClick={() => mutation.mutate()}
          >
            {mutation.isPending ? (
              <PendingLabel>{t('qualification.saving')}</PendingLabel>
            ) : (
              t('qualification.save')
            )}
          </button>
        </>
      }
    >
      <div className="structure-qualification">
        {mutation.isError ? <MutationError error={mutation.error} /> : null}

        <p className="structure-qualification__counter" aria-live="polite">
          {t('qualification.counter', { count: validatedCount })}
        </p>

        <ul className="structure-qualification__list">
          {rows.map((row) => {
            const isExpanded = expandedId === row.id;
            const display = isExpanded && draft ? draft : row;
            const isDup =
              display.validated && duplicates.has(destinationKey(display));
            const incomplete =
              !display.validated || !isQualRowComplete(display);
            const destName =
              stageNameById.get(display.destinationStageId) ??
              display.destinationStageId;
            const who =
              summarizeWho(display, locale, t) || t('qualification.newPath');
            const where =
              display.destinationStageId.trim() &&
              display.destinationSlotKey.trim()
                ? t('qualification.summary.where', {
                    phase: destName,
                    slot: display.destinationSlotKey.trim(),
                  })
                : null;
            const statusMessage = isDup
              ? t('qualification.duplicateSlot')
              : incomplete
                ? t('qualification.incompleteHint')
                : null;

            const draftIsDup =
              draft != null &&
              draft.id === row.id &&
              !!draft.destinationStageId.trim() &&
              !!draft.destinationSlotKey.trim() &&
              (duplicates.has(destinationKey(draft)) ||
                rows.some(
                  (other) =>
                    other.id !== draft.id &&
                    other.validated &&
                    destinationKey(other) === destinationKey(draft),
                ));

            return (
              <li
                key={row.id}
                className={[
                  'structure-qualification__item',
                  isExpanded
                    ? 'structure-qualification__item--expanded'
                    : null,
                  incomplete || isDup
                    ? 'structure-qualification__item--attention'
                    : null,
                ]
                  .filter(Boolean)
                  .join(' ')}
              >
                <div className="structure-qualification__hit">
                  <button
                    type="button"
                    className="ds-interactive-row structure-qualification__row"
                    onClick={() => toggleRow(row)}
                    aria-expanded={isExpanded}
                  >
                    <span className="structure-qualification__copy">
                      <span className="structure-qualification__who">
                        {who}
                      </span>
                      {where ? (
                        <span className="structure-qualification__where">
                          {where}
                        </span>
                      ) : null}
                      {statusMessage && !isExpanded ? (
                        <span
                          className="structure-qualification__status"
                          role="status"
                        >
                          {statusMessage}
                        </span>
                      ) : null}
                    </span>
                    <span
                      className={[
                        'ds-interactive-row__chevron',
                        isExpanded
                          ? 'structure-qualification__chevron--open'
                          : null,
                      ]
                        .filter(Boolean)
                        .join(' ')}
                      aria-hidden="true"
                    >
                      <ChevronDownIcon size="sm" />
                    </span>
                  </button>
                  {!isExpanded ? (
                    <button
                      type="button"
                      className="ds-btn ds-btn--ghost ds-btn--destructive ds-icon-button structure-qualification__trash"
                      aria-label={t('qualification.remove')}
                      disabled={mutation.isPending}
                      onClick={(event) => {
                        event.stopPropagation();
                        removeRow(row.id);
                      }}
                    >
                      <LucideIcon icon={Trash2} size="sm" />
                    </button>
                  ) : null}
                </div>

                {isExpanded && draft && draft.id === row.id ? (
                  <QualPathEditor
                    draft={draft}
                    groups={groups}
                    peerStages={peerStages}
                    destSlots={destSlots}
                    slotsLoading={destOverviewQuery.isLoading}
                    isDup={draftIsDup}
                    canValidate={isQualRowComplete(draft) && !draftIsDup}
                    onChange={updateDraft}
                    onScope={setScope}
                    onValidate={validateDraft}
                    onRemove={() => removeRow(row.id)}
                  />
                ) : null}
              </li>
            );
          })}
        </ul>

        <button
          type="button"
          className="ds-btn ds-btn--secondary"
          disabled={peerStages.length === 0 || mutation.isPending}
          onClick={addRow}
        >
          {t('qualification.add')}
        </button>
      </div>
    </Dialog>
  );
}

function QualPathEditor({
  draft,
  groups,
  peerStages,
  destSlots,
  slotsLoading,
  isDup,
  canValidate,
  onChange,
  onScope,
  onValidate,
  onRemove,
}: {
  draft: QualRowDraft;
  groups: { id: string; name: string }[];
  peerStages: StructureStageHubSummary[];
  destSlots: StageSlot[];
  slotsLoading: boolean;
  isDup: boolean;
  canValidate: boolean;
  onChange: (patch: Partial<QualRowDraft>) => void;
  onScope: (scope: QualScopeUi) => void;
  onValidate: () => void;
  onRemove: () => void;
}) {
  const { t } = useTranslation('structure');
  const groupFieldId = useId();
  const positionFieldId = useId();
  const acrossPFieldId = useId();
  const acrossKFieldId = useId();
  const pointsFieldId = useId();
  const phaseFieldId = useId();
  const slotFieldId = useId();

  const groupOptions = useMemo(() => {
    const options = groups.map((g) => ({ value: g.id, label: g.name }));
    if (
      draft.groupId &&
      !groups.some((g) => g.id === draft.groupId)
    ) {
      options.push({
        value: draft.groupId,
        label: draft.groupName || draft.groupId,
      });
    }
    return options;
  }, [groups, draft.groupId, draft.groupName]);

  const phaseOptions = peerStages.map((peer) => ({
    value: peer.stageId,
    label: peer.name,
  }));

  const slotOptions = useMemo(() => {
    const options = destSlots.map((slot) => ({
      value: slot.slotKey,
      label: slotLabel(slot),
    }));
    if (
      draft.destinationSlotKey &&
      !destSlots.some((s) => s.slotKey === draft.destinationSlotKey)
    ) {
      options.push({
        value: draft.destinationSlotKey,
        label: draft.destinationSlotKey,
      });
    }
    return options;
  }, [destSlots, draft.destinationSlotKey]);

  const positionValue = parsePositiveInt(draft.position);
  const acrossPValue = parsePositiveInt(draft.acrossGroupsPosition);
  const pointsValue =
    draft.minimumPoints.trim() === ''
      ? null
      : Number(draft.minimumPoints);

  return (
    <div className="structure-qualification__editor">
      <p className="structure-qualification__section-title">
        {t('qualification.who')}
      </p>

      <div
        className="structure-qualification__scope-tiles"
        role="radiogroup"
        aria-label={t('qualification.scope')}
      >
        {(
          [
            ['Group', 'scopeGroup'],
            ['Overall', 'scopeOverall'],
            ['AcrossGroups', 'scopeAcross'],
          ] as const
        ).map(([value, labelKey]) => (
          <ChoiceTile
            key={value}
            label={t(`qualification.${labelKey}`)}
            selected={draft.scope === value}
            onChange={(selected) => {
              if (selected) onScope(value);
            }}
          />
        ))}
      </div>

      {draft.scope === 'Group' ? (
        <Field label={t('qualification.group')} htmlFor={groupFieldId}>
          <Select
            id={groupFieldId}
            options={groupOptions}
            value={draft.groupId || null}
            placeholder={t('qualification.chooseGroup')}
            onChange={(value) => {
              const id = value ?? '';
              const name = groups.find((g) => g.id === id)?.name ?? '';
              onChange({ groupId: id, groupName: name });
            }}
          />
        </Field>
      ) : null}

      {draft.scope === 'AcrossGroups' ? (
        <div className="structure-qualification__fields">
          <Field
            label={t('qualification.acrossPlace')}
            htmlFor={acrossPFieldId}
            width="sm"
          >
            <InputNumber
              id={acrossPFieldId}
              min={1}
              value={acrossPValue}
              controlsLayout="split"
              onChange={(value) =>
                onChange({
                  acrossGroupsPosition: value != null ? String(value) : '',
                })
              }
            />
          </Field>
          <Field
            label={t('qualification.acrossRank')}
            htmlFor={acrossKFieldId}
            width="sm"
          >
            <InputNumber
              id={acrossKFieldId}
              min={1}
              value={positionValue}
              controlsLayout="split"
              onChange={(value) =>
                onChange({ position: value != null ? String(value) : '' })
              }
            />
          </Field>
        </div>
      ) : (
        <Field
          label={t('qualification.position')}
          htmlFor={positionFieldId}
          width="sm"
        >
          <InputNumber
            id={positionFieldId}
            min={1}
            value={positionValue}
            controlsLayout="split"
            onChange={(value) =>
              onChange({ position: value != null ? String(value) : '' })
            }
          />
        </Field>
      )}

      <div className="structure-qualification__fields">
        <Field label={t('qualification.condition')}>
          <Select
            options={[
              {
                value: 'none',
                label: t('qualification.conditionNone'),
              },
              {
                value: 'points',
                label: t('qualification.conditionPoints'),
              },
            ]}
            value={draft.conditionKind}
            onChange={(value) =>
              onChange({
                conditionKind: (value as 'none' | 'points') ?? 'none',
                minimumPoints:
                  value === 'points' ? draft.minimumPoints || '0' : '',
              })
            }
          />
        </Field>
        {draft.conditionKind === 'points' ? (
          <Field
            label={t('qualification.minimumPoints')}
            htmlFor={pointsFieldId}
            width="sm"
          >
            <InputNumber
              id={pointsFieldId}
              min={0}
              value={Number.isFinite(pointsValue) ? pointsValue : null}
              controlsLayout="split"
              onChange={(value) =>
                onChange({
                  minimumPoints: value != null ? String(value) : '',
                })
              }
            />
          </Field>
        ) : null}
      </div>

      <hr className="structure-qualification__rule" />

      <p className="structure-qualification__section-title">
        {t('qualification.where')}
      </p>

      <Field
        label={t('qualification.destinationPhase')}
        htmlFor={phaseFieldId}
      >
        <Select
          id={phaseFieldId}
          options={phaseOptions}
          value={draft.destinationStageId || null}
          placeholder={t('graph.chooseStage')}
          onChange={(value) =>
            onChange({
              destinationStageId: value ?? '',
              destinationSlotKey: '',
            })
          }
        />
      </Field>

      <Field
        label={t('qualification.destinationSlot')}
        htmlFor={slotFieldId}
        message={
          isDup
            ? t('qualification.duplicateSlot')
            : !draft.destinationSlotKey.trim()
              ? t('qualification.slotRequired')
              : undefined
        }
        messageTone={
          isDup || !draft.destinationSlotKey.trim() ? 'error' : 'hint'
        }
      >
        <Select
          id={slotFieldId}
          options={slotOptions}
          value={draft.destinationSlotKey || null}
          placeholder={t('qualification.chooseSlot')}
          disabled={!draft.destinationStageId || slotsLoading}
          invalid={isDup || !draft.destinationSlotKey.trim()}
          onChange={(value) =>
            onChange({ destinationSlotKey: value ?? '' })
          }
        />
      </Field>

      <div className="structure-qualification__editor-actions">
        <button
          type="button"
          className="ds-btn ds-btn--destructive"
          onClick={onRemove}
        >
          {t('qualification.remove')}
        </button>
        <button
          type="button"
          className="ds-btn ds-btn--primary"
          disabled={!canValidate}
          onClick={onValidate}
        >
          {t('qualification.validate')}
        </button>
      </div>
    </div>
  );
}

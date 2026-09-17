// -----------------------------------------------------------------------
// Qualifications dialog — authoring Intentions (population destinations).
// -----------------------------------------------------------------------

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ListPlus, ListMinus, Trash2 } from 'lucide-react';
import { useEffect, useId, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  fetchStageSchematic,
  replaceStageQualificationRules,
} from '../api';
import { Alert } from '../design-system/components/Alert';
import { ChoiceTile } from '../design-system/components/ChoiceTile';
import { Dialog } from '../design-system/components/Dialog';
import { Field } from '../design-system/components/Field';
import { InputNumber } from '../design-system/components/InputNumber';
import { Select } from '../design-system/components/Select';
import { Tooltip } from '../design-system/components/Tooltip';
import { LucideIcon } from '../design-system/icons/Icon';
import {
  ChampionshipFormatIcon,
  CupFormatIcon,
  EmptySelectionIcon,
  GroupsFormatIcon,
  OverviewAttentionIcon,
  PlusIcon,
  StandingRulesIcon,
  StructureIcon,
  SwissFormatIcon,
} from '../design-system/icons/contentIcons';
import { ChevronDownIcon } from '../design-system/icons/shellIcons';
import { notify } from '../design-system/toastStore';
import { queryKeys } from '../queryKeys';
import type {
  QualificationIntentSourceKind,
  SchematicCase,
  StructureFormatKind,
  StructureStageHubSummary,
  StructureView,
} from '../types';
import { EmptyState, MutationError, PendingLabel } from '../ui';
import { invalidateAfterStructureMutation } from './structureInvalidation';
import {
  emptyQualIntent,
  expandOccurrences,
  incompleteIntentReason,
  intentFromApi,
  isIntentComplete,
  ordinalRank,
  parsePositiveInt,
  pathToSingletonIntent,
  serializeIntents,
  summarizeIntentWho,
  toApiIntent,
  type QualIntentDraft,
} from './structureQualificationDraft';

type StructureQualificationDialogProps = {
  data: StructureView;
  stage: StructureStageHubSummary;
  open: boolean;
  onClose: () => void;
  /**
   * When set, dialog was opened from this destination phase Population
   * (ownership stays on `stage` = source).
   */
  openedFromDestinationStageId?: string | null;
};

function listGroupsFromSchematic(cases: SchematicCase[]): {
  id: string;
  name: string;
}[] {
  const map = new Map<string, string>();
  const order: string[] = [];
  for (const item of cases) {
    const id = item.formPosition.groupId?.trim();
    if (!id) continue;
    const name = item.formPosition.groupName?.trim() || id;
    if (!map.has(id)) {
      map.set(id, name);
      order.push(id);
    }
  }
  return order.map((id) => ({ id, name: map.get(id)! }));
}

function scopeKindIcon(
  kind: QualificationIntentSourceKind,
  size: 'sm' | 'md' | 'lg' = 'sm',
) {
  switch (kind) {
    case 'AcrossGroups':
      return <SwissFormatIcon size={size} />;
    case 'Overall':
      return <StandingRulesIcon size={size} />;
    case 'EachGroup':
    case 'SingleGroup':
    default:
      return <GroupsFormatIcon size={size} />;
  }
}

function stageFormatIcon(kind?: StructureFormatKind | null) {
  switch (kind) {
    case 'Cup':
      return <CupFormatIcon size="sm" aria-hidden="true" />;
    case 'Championship':
      return <ChampionshipFormatIcon size="sm" aria-hidden="true" />;
    case 'Groups':
      return <GroupsFormatIcon size="sm" aria-hidden="true" />;
    case 'Swiss':
      return <SwissFormatIcon size="sm" aria-hidden="true" />;
    default:
      return <StructureIcon size="sm" aria-hidden="true" />;
  }
}

export function StructureQualificationDialog({
  data,
  stage,
  open,
  onClose,
  openedFromDestinationStageId = null,
}: StructureQualificationDialogProps) {
  const { t, i18n } = useTranslation('structure');
  const { t: tCommon } = useTranslation('common');
  const queryClient = useQueryClient();
  const locale = i18n.language ?? 'fr';

  const peerStages = useMemo(
    () => data.stages.filter((peer) => peer.stageId !== stage.stageId),
    [data.stages, stage.stageId],
  );
  const contextDestinationId = openedFromDestinationStageId?.trim() || '';
  const defaultDest =
    (contextDestinationId &&
    peerStages.some((peer) => peer.stageId === contextDestinationId)
      ? contextDestinationId
      : null) ??
    peerStages[0]?.stageId ??
    '';
  const stageNameById = useMemo(
    () => new Map(data.stages.map((s) => [s.stageId, s.name])),
    [data.stages],
  );
  const stageById = useMemo(
    () => new Map(data.stages.map((s) => [s.stageId, s])),
    [data.stages],
  );

  const [intents, setIntents] = useState<QualIntentDraft[]>([]);
  const [baselineSerialized, setBaselineSerialized] = useState('');
  const [needsIntentMigration, setNeedsIntentMigration] = useState(false);
  const [expandedId, setExpandedId] = useState<string | null>(null);

  const sourceSchematicQuery = useQuery({
    queryKey: queryKeys.stages.schematic(stage.stageId),
    queryFn: () => fetchStageSchematic(stage.stageId),
    enabled: open,
  });

  const groups = useMemo(
    () => listGroupsFromSchematic(sourceSchematicQuery.data?.cases ?? []),
    [sourceSchematicQuery.data?.cases],
  );
  const hasGroups = groups.length > 0;

  useEffect(() => {
    if (!open) {
      setIntents([]);
      setBaselineSerialized('');
      setNeedsIntentMigration(false);
      setExpandedId(null);
      return;
    }
    const apiIntents = stage.qualificationIntents ?? [];
    const next =
      apiIntents.length > 0
        ? apiIntents.map(intentFromApi)
        : (stage.qualificationPaths ?? []).map(pathToSingletonIntent);
    setIntents(next);
    setBaselineSerialized(serializeIntents(next));
    setNeedsIntentMigration(
      apiIntents.length === 0 && (stage.qualificationPaths?.length ?? 0) > 0,
    );
    setExpandedId(null);
  }, [open, stage.stageId, stage.qualificationIntents, stage.qualificationPaths]);

  const entryTotal = useMemo(() => {
    return intents.reduce(
      (sum, intent) => sum + expandOccurrences(intent, groups).length,
      0,
    );
  }, [intents, groups]);

  /** Live draft contribution per destination (this dialog — not real Population). */
  const draftEntriesByDestination = useMemo(() => {
    const map = new Map<string, number>();
    for (const intent of intents) {
      const destId = intent.destinationStageId.trim();
      if (!destId) continue;
      const n = expandOccurrences(intent, groups).length;
      if (n <= 0) continue;
      map.set(destId, (map.get(destId) ?? 0) + n);
    }
    return map;
  }, [intents, groups]);

  const overCapacityWarnings = useMemo(() => {
    const warnings: {
      stageId: string;
      phase: string;
      count: number;
      capacity: number;
    }[] = [];
    for (const [stageId, count] of draftEntriesByDestination) {
      const dest = stageById.get(stageId);
      const capacity = dest?.compositionCapacity;
      if (capacity == null || capacity <= 0 || count <= capacity) continue;
      warnings.push({
        stageId,
        phase: dest?.name ?? stageId,
        count,
        capacity,
      });
    }
    return warnings;
  }, [draftEntriesByDestination, stageById]);

  const intentCount = intents.length;

  const mutation = useMutation({
    mutationFn: () => {
      const payload = intents.map((intent, index) =>
        toApiIntent(intent, index + 1),
      );
      return replaceStageQualificationRules(stage.stageId, {
        intents: payload,
        paths: null,
      });
    },
    onSuccess: async () => {
      await invalidateAfterStructureMutation(queryClient, data.competitionId);
      notify.success(t('qualification.toastUpdated'));
      onClose();
    },
  });

  const dirty =
    mutation.isPending ||
    needsIntentMigration ||
    serializeIntents(intents) !== baselineSerialized;

  const canSave =
    !mutation.isPending &&
    intents.every((intent) => isIntentComplete(intent, groups));

  function toggleRow(intent: QualIntentDraft) {
    setExpandedId((prev) => (prev === intent.id ? null : intent.id));
  }

  function addIntent() {
    const next = emptyQualIntent(defaultDest);
    if (!hasGroups) next.sourceKind = 'Overall';
    setIntents((prev) => [...prev, next]);
    setExpandedId(next.id);
  }

  function removeIntent(id: string) {
    setIntents((prev) => prev.filter((i) => i.id !== id));
    setExpandedId((prev) => (prev === id ? null : prev));
  }

  function updateIntent(next: QualIntentDraft) {
    setIntents((prev) => prev.map((i) => (i.id === next.id ? next : i)));
  }

  const contextDestinationName = contextDestinationId
    ? (stageNameById.get(contextDestinationId) ?? contextDestinationId)
    : null;

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={t('qualification.title', { phase: stage.name })}
      description={
        contextDestinationName
          ? t('qualification.hintFromDestination', { source: stage.name })
          : t('qualification.hint')
      }
      size="lg"
      footerStatus={
        mutation.isError || overCapacityWarnings.length > 0 ? (
          <>
            {mutation.isError ? (
              <MutationError error={mutation.error} />
            ) : null}
            {overCapacityWarnings.length > 0 ? (
              <Alert tone="warning" role="status">
                {overCapacityWarnings.map((warning) => (
                  <p
                    key={warning.stageId}
                    className="structure-qualification__hint-line"
                  >
                    {t('qualification.overCapacityWarning', {
                      count: warning.count,
                      capacity: warning.capacity,
                      phase: warning.phase,
                    })}
                  </p>
                ))}
              </Alert>
            ) : null}
          </>
        ) : null
      }
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
            disabled={!canSave || !dirty}
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
        <div
          className="structure-qualification__summary"
          aria-live="polite"
        >
          <div className="structure-qualification__facts">
            <div className="structure-qualification__fact structure-qualification__fact--secondary">
              <span className="structure-qualification__fact-value">
                {intentCount}
              </span>
              <span className="structure-qualification__fact-label">
                {t('qualification.factIntents', { count: intentCount })}
              </span>
            </div>
            <span
              className="structure-qualification__fact-rule"
              aria-hidden="true"
            />
            <div className="structure-qualification__fact structure-qualification__fact--primary">
              <span className="structure-qualification__fact-value">
                {entryTotal}
              </span>
              <span className="structure-qualification__fact-label">
                {t('qualification.factEntries', { count: entryTotal })}
              </span>
            </div>
          </div>
          <button
            type="button"
            className="ds-btn ds-btn--primary"
            disabled={peerStages.length === 0 || mutation.isPending}
            onClick={addIntent}
          >
            <PlusIcon size="sm" />
            <span>{t('qualification.add')}</span>
          </button>
        </div>

        {peerStages.length === 0 ? (
          <EmptyState
            variant="idle"
            icon={<StructureIcon size="lg" />}
            title={t('qualification.emptyNoPeerTitle')}
          >
            {t('qualification.emptyNoPeerBody')}
          </EmptyState>
        ) : intents.length === 0 ? (
          <EmptyState
            variant="idle"
            icon={<EmptySelectionIcon size="lg" />}
            title={t('qualification.emptyTitle')}
          >
            {t('qualification.emptyBody')}
          </EmptyState>
        ) : (
        <ul className="structure-qualification__list">
          {intents.map((intent) => {
            const isExpanded = expandedId === intent.id;
            const who =
              summarizeIntentWho(intent, locale, t) ||
              t('qualification.newPath');
            const destName =
              stageNameById.get(intent.destinationStageId) ??
              intent.destinationStageId;
            const occCount = expandOccurrences(intent, groups).length;
            const where =
              intent.destinationStageId.trim() && occCount > 0
                ? t('qualification.summary.whereCount', {
                    phase: destName,
                    count: occCount,
                  })
                : null;
            const incompleteReason = incompleteIntentReason(intent, groups);
            const statusMessage =
              incompleteReason == null
                ? null
                : t(`qualification.incompleteHint${incompleteReason}`);

            return (
              <li key={intent.id} className="structure-qualification__item">
                <div
                  className="structure-qualification__card ds-selectable-tile"
                  data-selected={isExpanded ? 'true' : 'false'}
                >
                  <div className="structure-qualification__hit">
                    <button
                      type="button"
                      className="structure-qualification__row"
                      onClick={() => toggleRow(intent)}
                      aria-expanded={isExpanded}
                    >
                      <span
                        className="structure-qualification__scope-icon"
                        aria-hidden="true"
                      >
                        {scopeKindIcon(intent.sourceKind, 'lg')}
                      </span>
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
                            <span
                              className="structure-qualification__status-icon"
                              aria-hidden="true"
                            >
                              <OverviewAttentionIcon size="sm" />
                            </span>
                            {statusMessage}
                          </span>
                        ) : null}
                      </span>
                      <span
                        className={[
                          'structure-qualification__chevron',
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
                    <button
                      type="button"
                      className="ds-btn ds-btn--ghost ds-btn--destructive ds-icon-button structure-qualification__trash"
                      aria-label={t('qualification.remove')}
                      disabled={mutation.isPending}
                      onClick={(event) => {
                        event.stopPropagation();
                        removeIntent(intent.id);
                      }}
                    >
                      <LucideIcon icon={Trash2} size="sm" />
                    </button>
                  </div>

                  {isExpanded ? (
                    <div className="structure-qualification__panel">
                      <QualIntentEditor
                        draft={intent}
                        groups={groups}
                        hasGroups={hasGroups}
                        peerStages={peerStages}
                        draftEntriesByDestination={draftEntriesByDestination}
                        locale={locale}
                        onChange={updateIntent}
                      />
                    </div>
                  ) : null}
                </div>
              </li>
            );
          })}
        </ul>
        )}
      </div>
    </Dialog>
  );
}

function PositionFields({
  draft,
  fromId,
  toId,
  onChange,
  singleLabel,
  rangeLabel,
  message,
}: {
  draft: QualIntentDraft;
  fromId: string;
  toId: string;
  onChange: (next: QualIntentDraft) => void;
  singleLabel?: string;
  rangeLabel?: string;
  message?: string;
}) {
  const { t } = useTranslation('structure');
  const isRange = draft.positionFrom !== draft.positionTo;

  return (
    <div className="structure-qualification__positions">
      <div className="structure-qualification__range">
        <Field
          label={
            isRange
              ? (rangeLabel ?? t('qualification.positions'))
              : (singleLabel ?? t('qualification.position'))
          }
          htmlFor={fromId}
          width="sm"
        >
          <InputNumber
            id={fromId}
            min={1}
            value={parsePositiveInt(draft.positionFrom)}
            controlsLayout="split"
            onChange={(value) => {
              const next = value != null ? String(value) : '';
              onChange({
                ...draft,
                positionFrom: next,
                ...(isRange ? {} : { positionTo: next }),
              });
            }}
          />
        </Field>
        {isRange ? (
          <>
            <span className="structure-qualification__range-sep" aria-hidden>
              {t('qualification.rangeTo')}
            </span>
            <Field label={'\u00a0'} htmlFor={toId} width="sm">
              <InputNumber
                id={toId}
                min={1}
                value={parsePositiveInt(draft.positionTo)}
                controlsLayout="split"
                onChange={(value) =>
                  onChange({
                    ...draft,
                    positionTo: value != null ? String(value) : '',
                  })
                }
              />
            </Field>
            <Tooltip content={t('qualification.removePositionRange')}>
              <button
                type="button"
                className="ds-btn ds-btn--ghost ds-icon-button structure-qualification__range-toggle"
                aria-label={t('qualification.removePositionRange')}
                aria-pressed="true"
                onClick={() =>
                  onChange({ ...draft, positionTo: draft.positionFrom })
                }
              >
                <LucideIcon icon={ListMinus} size="sm" />
              </button>
            </Tooltip>
          </>
        ) : (
          <Tooltip content={t('qualification.addPositionRange')}>
            <button
              type="button"
              className="ds-btn ds-btn--ghost ds-icon-button structure-qualification__range-toggle"
              aria-label={t('qualification.addPositionRange')}
              aria-pressed="false"
              onClick={() => {
                const from = parsePositiveInt(draft.positionFrom) ?? 1;
                onChange({
                  ...draft,
                  positionTo: String(from + 1),
                });
              }}
            >
              <LucideIcon icon={ListPlus} size="sm" />
            </button>
          </Tooltip>
        )}
      </div>
      {message ? (
        <p className="structure-qualification__field-hint" role="status">
          {message}
        </p>
      ) : null}
    </div>
  );
}

function QualIntentEditor({
  draft,
  groups,
  hasGroups,
  peerStages,
  draftEntriesByDestination,
  locale,
  onChange,
}: {
  draft: QualIntentDraft;
  groups: { id: string; name: string }[];
  hasGroups: boolean;
  peerStages: StructureStageHubSummary[];
  draftEntriesByDestination: Map<string, number>;
  locale: string;
  onChange: (next: QualIntentDraft) => void;
}) {
  const { t } = useTranslation('structure');
  const fromId = useId();
  const toId = useId();
  const acrossId = useId();
  const pointsId = useId();

  const scopeOptions: {
    value: QualificationIntentSourceKind;
    labelKey: string;
    descriptionKey: string;
  }[] = hasGroups
    ? [
        {
          value: 'EachGroup',
          labelKey: 'scopeGroupTile',
          descriptionKey: 'scopeGroupHint',
        },
        {
          value: 'AcrossGroups',
          labelKey: 'scopeAcrossTile',
          descriptionKey: 'scopeAcrossHint',
        },
        ...(draft.sourceKind === 'Overall'
          ? [
              {
                value: 'Overall' as const,
                labelKey: 'scopeOverallTile',
                descriptionKey: 'scopeOverallHint',
              },
            ]
          : []),
      ]
    : [
        {
          value: 'Overall',
          labelKey: 'scopeOverallTile',
          descriptionKey: 'scopeOverallHint',
        },
      ];

  const groupTileSelected =
    draft.sourceKind === 'EachGroup' || draft.sourceKind === 'SingleGroup';

  return (
    <div className="structure-qualification__editor">
      <p className="structure-qualification__section-title">
        {t('qualification.who')}
      </p>

      <div
        className="structure-qualification__scope-tiles"
        data-count={String(scopeOptions.length)}
        role="radiogroup"
        aria-label={t('qualification.scope')}
      >
        {scopeOptions.map(({ value, labelKey, descriptionKey }) => {
          const selected =
            value === 'EachGroup'
              ? groupTileSelected
              : draft.sourceKind === value;
          return (
            <ChoiceTile
              key={value}
              label={t(`qualification.${labelKey}`)}
              description={t(`qualification.${descriptionKey}`)}
              leading={scopeKindIcon(value)}
              selected={selected}
              onChange={(sel) => {
                if (!sel) return;
                if (value === 'EachGroup') {
                  onChange({
                    ...draft,
                    sourceKind: 'EachGroup',
                    groupId: '',
                    groupName: '',
                  });
                } else {
                  onChange({
                    ...draft,
                    sourceKind: value,
                    groupId: '',
                    groupName: '',
                  });
                }
              }}
            />
          );
        })}
      </div>

      {draft.sourceKind === 'AcrossGroups' ? (
        <div className="structure-qualification__fields">
          <Field
            label={t('qualification.acrossPlace')}
            message={t('qualification.acrossPlaceHint', {
              place: ordinalRank(
                Math.max(parsePositiveInt(draft.acrossGroupsPosition) ?? 1, 1),
                locale,
              ),
            })}
            htmlFor={acrossId}
            className="structure-qualification__control-sm"
          >
            <InputNumber
              id={acrossId}
              min={1}
              value={parsePositiveInt(draft.acrossGroupsPosition)}
              controlsLayout="split"
              onChange={(value) =>
                onChange({
                  ...draft,
                  acrossGroupsPosition: value != null ? String(value) : '',
                })
              }
            />
          </Field>
          <PositionFields
            draft={draft}
            fromId={fromId}
            toId={toId}
            onChange={onChange}
            singleLabel={t('qualification.position')}
            rangeLabel={t('qualification.positions')}
            message={
              draft.positionFrom === draft.positionTo
                ? t('qualification.acrossAmongHintOne', {
                    rank: ordinalRank(
                      Math.max(parsePositiveInt(draft.positionFrom) ?? 1, 1),
                      locale,
                    ),
                  })
                : t('qualification.acrossAmongHintRange', {
                    from: ordinalRank(
                      Math.max(parsePositiveInt(draft.positionFrom) ?? 1, 1),
                      locale,
                    ),
                    to: ordinalRank(
                      Math.max(parsePositiveInt(draft.positionTo) ?? 1, 1),
                      locale,
                    ),
                  })
            }
          />
        </div>
      ) : groupTileSelected && hasGroups ? (
        <div className="structure-qualification__fields">
          <Field label={t('qualification.group')}>
            <Select
              options={[
                {
                  value: '',
                  label: t('qualification.eachGroup'),
                },
                ...groups.map((g) => ({ value: g.id, label: g.name })),
              ]}
              value={
                draft.sourceKind === 'SingleGroup' ? draft.groupId || null : ''
              }
              placeholder={t('qualification.eachGroup')}
              onChange={(value) => {
                const id = value ?? '';
                if (!id) {
                  onChange({
                    ...draft,
                    sourceKind: 'EachGroup',
                    groupId: '',
                    groupName: '',
                  });
                  return;
                }
                onChange({
                  ...draft,
                  sourceKind: 'SingleGroup',
                  groupId: id,
                  groupName: groups.find((g) => g.id === id)?.name ?? '',
                });
              }}
            />
          </Field>
          <PositionFields
            draft={draft}
            fromId={fromId}
            toId={toId}
            onChange={onChange}
          />
        </div>
      ) : (
        <PositionFields
          draft={draft}
          fromId={fromId}
          toId={toId}
          onChange={onChange}
        />
      )}

      <div className="structure-qualification__fields">
        <Field label={t('qualification.condition')}>
          <Select
            options={[
              { value: 'none', label: t('qualification.conditionNone') },
              { value: 'points', label: t('qualification.conditionPoints') },
            ]}
            value={draft.conditionKind}
            onChange={(value) =>
              onChange({
                ...draft,
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
            htmlFor={pointsId}
            width="sm"
          >
            <InputNumber
              id={pointsId}
              min={0}
              value={
                draft.minimumPoints.trim() === ''
                  ? null
                  : Number(draft.minimumPoints)
              }
              controlsLayout="split"
              onChange={(value) =>
                onChange({
                  ...draft,
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

      {peerStages.length === 0 ? (
        <p className="structure-qualification__field-hint" role="status">
          {t('qualification.emptyNoPeerBody')}
        </p>
      ) : (
        <div
          className="structure-qualification__scope-tiles"
          data-count={String(Math.min(peerStages.length, 3))}
          role="radiogroup"
          aria-label={t('qualification.destinationPhase')}
        >
          {peerStages.map((peer) => {
            const contribution = draftEntriesByDestination.get(peer.stageId) ?? 0;
            const capacity = peer.compositionCapacity;
            const description =
              capacity != null && capacity > 0 ? (
                <DestinationDraftMeter
                  count={contribution}
                  capacity={capacity}
                  label={t('qualification.tileContribution', {
                    count: contribution,
                    capacity,
                  })}
                  ariaLabel={t('qualification.tileContributionAria', {
                    phase: peer.name,
                    count: contribution,
                    capacity,
                  })}
                />
              ) : contribution > 0 ? (
                t('qualification.tileContributionEntries', {
                  count: contribution,
                })
              ) : undefined;
            return (
              <ChoiceTile
                key={peer.stageId}
                label={peer.name}
                description={description}
                leading={stageFormatIcon(peer.formatKind)}
                selected={draft.destinationStageId === peer.stageId}
                onChange={(selected) => {
                  if (!selected) return;
                  onChange({
                    ...draft,
                    destinationStageId: peer.stageId,
                  });
                }}
              />
            );
          })}
        </div>
      )}
    </div>
  );
}

/** Compact draft contribution vs Places N — not real Population coverage. */
function DestinationDraftMeter({
  count,
  capacity,
  label,
  ariaLabel,
}: {
  count: number;
  capacity: number;
  label: string;
  ariaLabel: string;
}) {
  const tone = count === capacity ? 'exact' : count < capacity ? 'short' : 'over';
  const ratio =
    capacity > 0 ? Math.min(1, Math.max(0, count / capacity)) : count > 0 ? 1 : 0;

  return (
    <span
      className={`structure-qualification__tile-meter structure-qualification__tile-meter--${tone}`}
      role="img"
      aria-label={ariaLabel}
    >
      <span className="structure-qualification__tile-meter-label" aria-hidden="true">
        {label}
      </span>
      <span className="structure-qualification__tile-meter-track" aria-hidden="true">
        <span
          className="structure-qualification__tile-meter-fill"
          style={{ width: `${ratio * 100}%` }}
        />
      </span>
    </span>
  );
}

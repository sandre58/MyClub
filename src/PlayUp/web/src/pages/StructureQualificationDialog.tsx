// -----------------------------------------------------------------------
// Qualifications dialog — authoring Intentions (1 → N paths).
// -----------------------------------------------------------------------

import { useMutation, useQueries, useQuery, useQueryClient } from '@tanstack/react-query';
import { ListPlus, ListMinus, Trash2 } from 'lucide-react';
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
import { Tooltip } from '../design-system/components/Tooltip';
import { LucideIcon } from '../design-system/icons/Icon';
import {
  GroupsFormatIcon,
  OverviewAttentionIcon,
  StandingRulesIcon,
  SwissFormatIcon,
} from '../design-system/icons/contentIcons';
import { ChevronDownIcon } from '../design-system/icons/shellIcons';
import { notify } from '../design-system/toastStore';
import { queryKeys } from '../queryKeys';
import type {
  QualificationIntentSourceKind,
  SchematicCase,
  StageSlot,
  StructureStageHubSummary,
  StructureView,
} from '../types';
import { MutationError, PendingLabel } from '../ui';
import { invalidateAfterStructureMutation } from './structureInvalidation';
import {
  applySlotOverride,
  emptyQualIntent,
  expandOccurrences,
  findDuplicateSlotsAcrossIntents,
  intentFromApi,
  isIntentComplete,
  mapDestinations,
  occurrenceLabel,
  ordinalRank,
  parsePositiveInt,
  pathToSingletonIntent,
  resetToCanonical,
  summarizeIntentWho,
  toApiIntent,
  type QualIntentDraft,
  type SourceOccurrence,
} from './structureQualificationDraft';

type StructureQualificationDialogProps = {
  data: StructureView;
  stage: StructureStageHubSummary;
  open: boolean;
  onClose: () => void;
};

type OrphanPrompt = {
  intentId: string;
  previous: QualIntentDraft;
  next: QualIntentDraft;
  orphans: { label: string; slotKey: string }[];
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

function slotLabel(slot: StageSlot): string {
  const name = slot.displayName?.trim();
  return name ? `${slot.slotKey} — ${name}` : slot.slotKey;
}

function scopeKindIcon(kind: QualificationIntentSourceKind) {
  switch (kind) {
    case 'AcrossGroups':
      return <SwissFormatIcon size="lg" />;
    case 'Overall':
      return <StandingRulesIcon size="lg" />;
    case 'EachGroup':
    case 'SingleGroup':
    default:
      return <GroupsFormatIcon size="lg" />;
  }
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
  const stageNameById = useMemo(
    () => new Map(data.stages.map((s) => [s.stageId, s.name])),
    [data.stages],
  );

  const [intents, setIntents] = useState<QualIntentDraft[]>([]);
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [draft, setDraft] = useState<QualIntentDraft | null>(null);
  const [orphanPrompt, setOrphanPrompt] = useState<OrphanPrompt | null>(null);

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

  const destStageIdForSlots =
    draft?.destinationStageId ||
    intents.find((i) => i.id === expandedId)?.destinationStageId ||
    '';

  const destinationStageIds = useMemo(() => {
    const ids = new Set<string>();
    for (const intent of intents) {
      const destinationId =
        intent.id === draft?.id && draft
          ? draft.destinationStageId
          : intent.destinationStageId;
      if (destinationId.trim()) ids.add(destinationId);
    }
    if (destStageIdForSlots.trim()) ids.add(destStageIdForSlots);
    return [...ids];
  }, [intents, draft, destStageIdForSlots]);

  const destOverviewQueries = useQueries({
    queries: destinationStageIds.map((stageId) => ({
      queryKey: queryKeys.stages.detail(stageId),
      queryFn: () => fetchStageOverview(stageId),
      enabled: open && stageId.length > 0,
    })),
  });

  const slotKeysByStage = useMemo(() => {
    const map = new Map<string, string[]>();
    destinationStageIds.forEach((stageId, index) => {
      const slots = destOverviewQueries[index]?.data?.slots;
      if (slots) {
        map.set(
          stageId,
          slots.map((slot) => slot.slotKey),
        );
      }
    });
    return map;
  }, [destinationStageIds, destOverviewQueries]);

  const destSlots = useMemo(() => {
    if (!destStageIdForSlots) return [] as StageSlot[];
    const index = destinationStageIds.indexOf(destStageIdForSlots);
    if (index < 0) return [] as StageSlot[];
    return destOverviewQueries[index]?.data?.slots ?? [];
  }, [destStageIdForSlots, destinationStageIds, destOverviewQueries]);

  const destSlotKeys = useMemo(
    () => destSlots.map((s) => s.slotKey),
    [destSlots],
  );

  const destSlotsLoading = useMemo(() => {
    if (!destStageIdForSlots) return false;
    const index = destinationStageIds.indexOf(destStageIdForSlots);
    if (index < 0) return false;
    return destOverviewQueries[index]?.isLoading === true;
  }, [destStageIdForSlots, destinationStageIds, destOverviewQueries]);

  useEffect(() => {
    if (!open) {
      setIntents([]);
      setExpandedId(null);
      setDraft(null);
      setOrphanPrompt(null);
      return;
    }
    const apiIntents = stage.qualificationIntents ?? [];
    if (apiIntents.length > 0) {
      setIntents(apiIntents.map(intentFromApi));
    } else {
      setIntents((stage.qualificationPaths ?? []).map(pathToSingletonIntent));
    }
    setExpandedId(null);
    setDraft(null);
  }, [open, stage.stageId, stage.qualificationIntents, stage.qualificationPaths]);

  const duplicates = useMemo(() => {
    const validated = intents.map((i) =>
      i.id === draft?.id && draft ? { ...draft, validated: true } : i,
    );
    return findDuplicateSlotsAcrossIntents(validated, groups, slotKeysByStage);
  }, [intents, draft, groups, slotKeysByStage]);

  const destinationTotal = useMemo(() => {
    return intents.reduce((sum, intent) => {
      const display = intent.id === draft?.id && draft ? draft : intent;
      if (!display.validated && intent.id !== draft?.id) return sum;
      return sum + expandOccurrences(display, groups).length;
    }, 0);
  }, [intents, draft, groups]);

  const intentCount = useMemo(
    () => intents.filter((i) => i.validated || i.id === draft?.id).length,
    [intents, draft],
  );

  const mutation = useMutation({
    mutationFn: () => {
      const payload = intents
        .filter((i) => i.validated)
        .map((intent, index) => toApiIntent(intent, index + 1));
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
    JSON.stringify(intents.filter((i) => i.validated).map((i) => i.id)) !==
      JSON.stringify(
        (stage.qualificationIntents ?? []).map((i) => i.intentId),
      ) ||
    (intents.length === 0 &&
      ((stage.qualificationIntents?.length ?? 0) > 0 ||
        (stage.qualificationPaths?.length ?? 0) > 0));

  const openIncomplete =
    draft != null &&
    (() => {
      const slots = slotKeysByStage.get(draft.destinationStageId);
      if (slots == null) return destSlotsLoading;
      return (
        !isIntentComplete(draft, groups, slots) ||
        [...duplicates].some((k) =>
          k.startsWith(`${draft.destinationStageId}\0`),
        )
      );
    })();

  const canSave =
    !mutation.isPending &&
    !openIncomplete &&
    orphanPrompt == null &&
    intents.every((intent) => {
      if (!intent.validated) return true;
      const slots = slotKeysByStage.get(intent.destinationStageId);
      if (slots == null) return false;
      return isIntentComplete(intent, groups, slots);
    });

  function toggleRow(intent: QualIntentDraft) {
    if (expandedId === intent.id) {
      setExpandedId(null);
      setDraft(null);
      return;
    }
    setExpandedId(intent.id);
    setDraft({ ...intent });
  }

  function addIntent() {
    const next = emptyQualIntent(defaultDest);
    if (!hasGroups) next.sourceKind = 'Overall';
    setIntents((prev) => [...prev, next]);
    setExpandedId(next.id);
    setDraft(next);
  }

  function removeIntent(id: string) {
    setIntents((prev) => prev.filter((i) => i.id !== id));
    if (expandedId === id || draft?.id === id) {
      setExpandedId(null);
      setDraft(null);
    }
  }

  function validateDraft() {
    if (!draft) return;
    if (!isIntentComplete(draft, groups, destSlotKeys)) return;
    const committed = { ...draft, validated: true, showDestinations: false };
    setIntents((prev) =>
      prev.map((i) => (i.id === committed.id ? committed : i)),
    );
    setExpandedId(null);
    setDraft(null);
  }

  function tryUpdateDraft(next: QualIntentDraft) {
    if (!draft) return;
    const structural =
      next.sourceKind !== draft.sourceKind ||
      next.positionFrom !== draft.positionFrom ||
      next.positionTo !== draft.positionTo ||
      next.acrossGroupsPosition !== draft.acrossGroupsPosition ||
      next.destinationStageId !== draft.destinationStageId ||
      next.groupId !== draft.groupId;

    if (draft.mappingMode === 'Custom' && structural) {
      const beforeKeys = new Set(
        draft.slotOverrides.map(
          (o) =>
            `${o.scope}\0${o.position}\0${o.groupId ?? ''}\0${o.acrossGroupsPosition ?? ''}`,
        ),
      );
      const after = expandOccurrences(next, groups);
      const afterKeys = new Set(
        after.map(
          (o) =>
            `${o.scope}\0${o.position}\0${o.groupId ?? ''}\0${o.acrossGroupsPosition ?? ''}`,
        ),
      );
      const orphans = draft.slotOverrides
        .filter((o) => {
          const key = `${o.scope}\0${o.position}\0${o.groupId ?? ''}\0${o.acrossGroupsPosition ?? ''}`;
          return beforeKeys.has(key) && !afterKeys.has(key);
        })
        .map((o) => ({
          label: occurrenceLabel(
            {
              scope: o.scope,
              position: o.position,
              groupId: o.groupId,
              groupName:
                groups.find((g) => g.id === o.groupId)?.name ?? o.groupId,
              acrossGroupsPosition: o.acrossGroupsPosition,
            },
            (n) => ordinalRank(n, locale),
            t,
          ),
          slotKey: o.slotKey,
        }));

      if (orphans.length > 0) {
        setOrphanPrompt({
          intentId: draft.id,
          previous: draft,
          next,
          orphans,
        });
        setDraft(next);
        return;
      }
    }

    if (orphanPrompt?.intentId === draft.id) {
      setOrphanPrompt(null);
    }
    setDraft(next);
  }

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={t('qualification.title', { phase: stage.name })}
      size="lg"
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
      {mutation.isError ? <MutationError error={mutation.error} /> : null}

      <div className="structure-qualification">
        <div
          className="structure-qualification__summary"
          aria-live="polite"
        >
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
              {destinationTotal}
            </span>
            <span className="structure-qualification__fact-label">
              {t('qualification.factDestinations', {
                count: destinationTotal,
              })}
            </span>
          </div>
        </div>

        <ul className="structure-qualification__list">
          {intents.map((intent) => {
            const isExpanded = expandedId === intent.id;
            const display = isExpanded && draft ? draft : intent;
            const who =
              summarizeIntentWho(display, locale, t) ||
              t('qualification.newPath');
            const destName =
              stageNameById.get(display.destinationStageId) ??
              display.destinationStageId;
            const occCount = expandOccurrences(display, groups).length;
            const where =
              display.destinationStageId.trim() && occCount > 0
                ? t('qualification.summary.whereCount', {
                    phase: destName,
                    count: occCount,
                  })
                : null;
            const slotsForIntent =
              slotKeysByStage.get(display.destinationStageId) ?? null;
            const incomplete =
              display.validated &&
              slotsForIntent != null &&
              !isIntentComplete(display, groups, slotsForIntent);
            const isDup = [...duplicates].some((k) =>
              k.startsWith(`${display.destinationStageId}\0`),
            );
            const statusMessage = isDup
              ? t('qualification.duplicateSlot')
              : incomplete
                ? t('qualification.incompleteHint')
                : null;

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
                        {scopeKindIcon(display.sourceKind)}
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
                    {!isExpanded ? (
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
                    ) : null}
                  </div>

                  {isExpanded && draft && draft.id === intent.id ? (
                    <div className="structure-qualification__panel">
                      <QualIntentEditor
                        draft={draft}
                        groups={groups}
                        hasGroups={hasGroups}
                        peerStages={peerStages}
                        destSlots={destSlots}
                        slotsLoading={destSlotsLoading}
                        canValidate={
                          isIntentComplete(draft, groups, destSlotKeys) &&
                          ![...duplicates].some((k) =>
                            k.startsWith(`${draft.destinationStageId}\0`),
                          )
                        }
                        locale={locale}
                        onChange={tryUpdateDraft}
                        onValidate={validateDraft}
                        onRemove={() => removeIntent(intent.id)}
                        onResetCanonical={() =>
                          setDraft(resetToCanonical(draft))
                        }
                        onOverrideSlot={(occurrence, slotKey) =>
                          setDraft(
                            applySlotOverride(
                              draft,
                              occurrence,
                              slotKey,
                              groups,
                              destSlotKeys,
                            ),
                          )
                        }
                      />
                    </div>
                  ) : null}
                </div>
              </li>
            );
          })}
        </ul>

        <button
          type="button"
          className="ds-btn ds-btn--secondary"
          disabled={peerStages.length === 0 || mutation.isPending}
          onClick={addIntent}
        >
          {t('qualification.add')}
        </button>

        {orphanPrompt ? (
          <div
            className="structure-qualification__orphan"
            role="alertdialog"
            aria-labelledby="qual-orphan-title"
          >
            <p
              id="qual-orphan-title"
              className="structure-qualification__orphan-title"
            >
              {t('qualification.orphanTitle', {
                count: orphanPrompt.orphans.length,
              })}
            </p>
            <ul className="structure-qualification__orphan-list">
              {orphanPrompt.orphans.map((o) => (
                <li key={`${o.label}-${o.slotKey}`}>
                  {o.label} → {o.slotKey}{' '}
                  <span className="structure-qualification__orphan-mark">
                    ({t('qualification.orphanGone')})
                  </span>
                </li>
              ))}
            </ul>
            <div className="structure-qualification__orphan-actions">
              <button
                type="button"
                className="ds-btn ds-btn--secondary"
                onClick={() => {
                  setDraft(orphanPrompt.previous);
                  setOrphanPrompt(null);
                }}
              >
                {t('qualification.orphanCancel')}
              </button>
              <button
                type="button"
                className="ds-btn ds-btn--primary"
                onClick={() => {
                  setDraft(resetToCanonical(orphanPrompt.next));
                  setOrphanPrompt(null);
                }}
              >
                {t('qualification.orphanReset')}
              </button>
            </div>
          </div>
        ) : null}
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
  destSlots,
  slotsLoading,
  canValidate,
  locale,
  onChange,
  onValidate,
  onRemove,
  onResetCanonical,
  onOverrideSlot,
}: {
  draft: QualIntentDraft;
  groups: { id: string; name: string }[];
  hasGroups: boolean;
  peerStages: StructureStageHubSummary[];
  destSlots: StageSlot[];
  slotsLoading: boolean;
  canValidate: boolean;
  locale: string;
  onChange: (next: QualIntentDraft) => void;
  onValidate: () => void;
  onRemove: () => void;
  onResetCanonical: () => void;
  onOverrideSlot: (occurrence: SourceOccurrence, slotKey: string) => void;
}) {
  const { t } = useTranslation('structure');
  const fromId = useId();
  const toId = useId();
  const acrossId = useId();
  const pointsId = useId();

  const slotKeys = destSlots.map((s) => s.slotKey);
  const mapped = mapDestinations(
    draft,
    groups,
    slotKeys,
    (n) => ordinalRank(n, locale),
    t,
  );
  const insufficient = mapped.length > slotKeys.length && slotKeys.length > 0;
  const showMappingList =
    draft.showDestinations ||
    draft.mappingMode === 'Custom' ||
    insufficient ||
    mapped.some((m) => !m.slotKey);

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

  // When user picks "Groupe" tile we use EachGroup by default; SingleGroup via toggle
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
                  label: t('qualification.allGroups'),
                },
                ...groups.map((g) => ({ value: g.id, label: g.name })),
              ]}
              value={
                draft.sourceKind === 'SingleGroup' ? draft.groupId || null : ''
              }
              placeholder={t('qualification.allGroups')}
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

      <div
        className="structure-qualification__scope-tiles"
        data-count={String(Math.min(peerStages.length, 3))}
        role="radiogroup"
        aria-label={t('qualification.destinationPhase')}
      >
        {peerStages.map((peer) => (
          <ChoiceTile
            key={peer.stageId}
            label={peer.name}
            selected={draft.destinationStageId === peer.stageId}
            onChange={(selected) => {
              if (!selected) return;
              onChange({
                ...draft,
                destinationStageId: peer.stageId,
              });
            }}
          />
        ))}
      </div>

      <div className="structure-qualification__destinations">
        <p className="structure-qualification__dest-heading">
          {t('qualification.destinationsHeading', { count: mapped.length })}
        </p>
        {!showMappingList ? (
          <div className="structure-qualification__dest-summary">
            <p>
              {t('qualification.mappingAuto', { count: mapped.length })}
            </p>
            <button
              type="button"
              className="ds-btn ds-btn--ghost"
              onClick={() =>
                onChange({ ...draft, showDestinations: true })
              }
            >
              {t('qualification.showDestinations')}
            </button>
          </div>
        ) : (
          <>
            {insufficient ? (
              <p className="structure-qualification__status" role="status">
                {t('qualification.slotsInsufficient', {
                  needed: mapped.length,
                  available: slotKeys.length,
                })}
              </p>
            ) : null}
            <ul className="structure-qualification__dest-list">
              {mapped.map((row) => (
                <li key={`${row.label}-${row.occurrence.position}-${row.occurrence.groupId ?? ''}`}>
                  <span className="structure-qualification__dest-label">
                    {row.label}
                  </span>
                  <Select
                    options={destSlots.map((s) => ({
                      value: s.slotKey,
                      label: slotLabel(s),
                    }))}
                    value={row.slotKey || null}
                    placeholder={t('qualification.chooseSlot')}
                    disabled={!draft.destinationStageId || slotsLoading}
                    onChange={(value) =>
                      onOverrideSlot(row.occurrence, value ?? '')
                    }
                  />
                </li>
              ))}
            </ul>
            {draft.mappingMode === 'Custom' ? (
              <button
                type="button"
                className="ds-btn ds-btn--ghost"
                onClick={onResetCanonical}
              >
                {t('qualification.redistributeAuto')}
              </button>
            ) : null}
          </>
        )}
      </div>

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

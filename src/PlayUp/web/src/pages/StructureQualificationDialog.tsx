// -----------------------------------------------------------------------
// Qualifications dialog — authoring Intentions (Population | Place Auto).
// UX Place = Placement destination picker (Slot | Group).
// -----------------------------------------------------------------------

import {
  useMutation,
  useQueries,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query';
import { ListPlus, ListMinus, Trash2 } from 'lucide-react';
import { useEffect, useId, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  fetchStageSchematic,
  replaceStageQualificationRules,
} from '../api';
import { Alert } from '../design-system/components/Alert';
import { ChoiceTile } from '../design-system/components/ChoiceTile';
import { ConfirmDialog } from '../design-system/components/ConfirmDialog';
import { Dialog } from '../design-system/components/Dialog';
import { Field } from '../design-system/components/Field';
import { InputNumber } from '../design-system/components/InputNumber';
import { Select } from '../design-system/components/Select';
import { Tooltip } from '../design-system/components/Tooltip';
import { LucideIcon } from '../design-system/icons/Icon';
import { ToastToneIcon } from '../design-system/icons/toastIcons';
import {
  ChampionshipFormatIcon,
  CheckIcon,
  CupFormatIcon,
  EmptySelectionIcon,
  GroupsFormatIcon,
  OverviewAttentionIcon,
  PlusIcon,
  StandingRulesIcon,
  StructureIcon,
  SwissFormatIcon,
} from '../design-system/icons/contentIcons';
import {
  ChevronDownIcon,
  CloseIcon,
} from '../design-system/icons/shellIcons';
import { notify } from '../design-system/toastStore';
import { useDiscardConfirm } from '../design-system/useDiscardConfirm';
import { queryKeys } from '../queryKeys';
import type {
  QualificationIntentSourceKind,
  SchematicCase,
  StageSchematic,
  StructureFormatKind,
  StructureStageHubSummary,
  StructureView,
} from '../types';
import { EmptyState, LoadingState, MutationError, PendingLabel } from '../ui';
import { invalidateAfterStructureMutation } from './structureInvalidation';
import { sortiesAvalPeerStages } from './structureSortiesDestinations';
import { SortiesWhoWhereFlow } from './SortiesWhoWhereFlow';
import {
  areProgressionPlacesLabeled,
  listLabeledPlaces,
  placeGrainForFormat,
} from './structurePlaceLabel';
import { expectedPopulationWithQualDraft } from './structurePopulationVolume';
import { DestinationDraftMeter } from './DestinationDraftMeter';
import {
  countUnmappedPlaceSlots,
  emptyQualIntent,
  expandOccurrences,
  fillEmptyPlaceKeysAllowingReuse,
  fillEmptyPlaceSlotKeys,
  hasAnyDuplicateSourceOccurrence,
  hasDuplicateSourceOccurrence,
  incompleteIntentReason,
  intentFromApi,
  isIntentComplete,
  occurrenceLabel,
  ordinalRank,
  parsePositiveInt,
  pathToSingletonIntent,
  placeMapKeys,
  resizeDestinationSlotKeys,
  serializeIntents,
  summarizeIntentWho,
  syncPlaceSlotKeys,
  toApiIntent,
  applyDestinationToDraft,
  applyTargetKindToDraft,
  isFormOnlyDestination,
  normalizeFormOnlyDestinationDraft,
  showsPopulationPlaceChoice,
  type QualIntentDraft,
  type QualTargetKind,
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
  const { t: tReg } = useTranslation('regulation');
  const queryClient = useQueryClient();
  const locale = i18n.language ?? 'fr';

  /** Inter-Stage downstream only — self / upstream excluded (Sorties ownership). */
  const peerStages = useMemo(
    () => sortiesAvalPeerStages(data.stages, stage.stageId),
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
  /** False until source schematic has settled for this open session. */
  const [sessionReady, setSessionReady] = useState(false);

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

  const dirty =
    sessionReady &&
    (needsIntentMigration ||
      serializeIntents(intents, groups) !== baselineSerialized);
  const {
    discardOpen,
    requestClose: requestDiscardClose,
    cancelDiscard,
    confirmDiscard,
    resetDiscard,
  } = useDiscardConfirm(dirty, onClose);

  const peerSchematicQueries = useQueries({
    queries: peerStages.map((peer) => ({
      queryKey: queryKeys.stages.schematic(peer.stageId),
      queryFn: () => fetchStageSchematic(peer.stageId),
      enabled: open && peerStages.length > 0,
    })),
  });

  const destinationSchematicById = useMemo(() => {
    const map = new Map<string, StageSchematic | undefined>();
    peerStages.forEach((peer, index) => {
      map.set(peer.stageId, peerSchematicQueries[index]?.data);
    });
    return map;
  }, [peerSchematicQueries, peerStages]);

  function placesLabeledFor(intent: QualIntentDraft): boolean {
    if (intent.targetKind !== 'place') return true;
    if (intent.destinationForm) return true;
    const destId = intent.destinationStageId.trim();
    if (!destId) return true;
    const peer = stageById.get(destId);
    if (isFormOnlyDestination(peer?.formatKind)) return true;
    const peerIndex = peerStages.findIndex((p) => p.stageId === destId);
    if (peerIndex >= 0 && peerSchematicQueries[peerIndex]?.isPending) {
      // Still loading — do not block as PlaceUnavailable yet.
      return true;
    }
    return areProgressionPlacesLabeled(destinationSchematicById.get(destId));
  }

  const mutation = useMutation({
    mutationFn: () => {
      const payload = intents.map((intent, index) =>
        toApiIntent(intent, index + 1, groups),
      );
      return replaceStageQualificationRules(stage.stageId, {
        intents: payload,
      });
    },
    onSuccess: async () => {
      await invalidateAfterStructureMutation(queryClient, data.competitionId);
      notify.success(t('qualification.toastUpdated'));
      onClose();
    },
  });

  useEffect(() => {
    if (!open) {
      // Keep last paint (intents + sessionReady) during Dialog exit animation.
      return;
    }

    // Reset prior success/error so a reopen can hydrate.
    mutation.reset();
    setSessionReady(false);
  }, [open]); // eslint-disable-line react-hooks/exhaustive-deps -- open edge only

  useEffect(() => {
    if (!open) {
      return;
    }
    // Wait for groups before Expand/sync — avoids EachGroup → 0 flash.
    if (sourceSchematicQuery.isLoading) {
      return;
    }
    // Do not rehydrate from invalidate while saving / closing.
    if (mutation.isPending || mutation.isSuccess) {
      return;
    }

    const apiIntents = stage.qualificationIntents ?? [];
    const next = (
      apiIntents.length > 0
        ? apiIntents.map(intentFromApi)
        : (stage.qualificationPaths ?? []).map(pathToSingletonIntent)
    ).map((intent) => {
      const synced = syncPlaceSlotKeys(intent, groups);
      const peer = stageById.get(synced.destinationStageId.trim());
      return normalizeFormOnlyDestinationDraft(synced, peer?.formatKind);
    });
    setIntents(next);
    setBaselineSerialized(serializeIntents(next, groups));
    setNeedsIntentMigration(
      apiIntents.length === 0 && (stage.qualificationPaths?.length ?? 0) > 0,
    );
    setExpandedId(null);
    setSessionReady(true);
    resetDiscard();
  }, [
    open,
    stage.stageId,
    stage.qualificationIntents,
    stage.qualificationPaths,
    groups,
    sourceSchematicQuery.isLoading,
    mutation.isPending,
    mutation.isSuccess,
    resetDiscard,
    stageById,
  ]);

  const entryTotal = useMemo(() => {
    return intents.reduce(
      (sum, intent) => sum + expandOccurrences(intent, groups).length,
      0,
    );
  }, [intents, groups]);

  /** Live draft contribution (Z) per destination — separate from expected Population X. */
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
      draft: number;
    }[] = [];
    const destinations = new Set(draftEntriesByDestination.keys());
    for (const peer of peerStages) {
      destinations.add(peer.stageId);
    }
    for (const stageId of destinations) {
      const dest = stageById.get(stageId);
      if (!dest) continue;
      const capacity = dest.compositionCapacity;
      if (capacity == null || capacity <= 0) continue;
      const draft = draftEntriesByDestination.get(stageId) ?? 0;
      const expected = expectedPopulationWithQualDraft({
        data,
        destination: dest,
        sourceStageId: stage.stageId,
        draftQualVolume: draft,
      });
      if (expected <= capacity) continue;
      warnings.push({
        stageId,
        phase: dest.name,
        count: expected,
        capacity,
        draft,
      });
    }
    return warnings;
  }, [data, draftEntriesByDestination, peerStages, stage.stageId, stageById]);

  const intentCount = intents.length;

  const intentsComplete =
    sessionReady &&
    intents.every((intent) =>
      isIntentComplete(intent, groups, placesLabeledFor(intent), intents),
    );

  const canSave = !mutation.isPending && intentsComplete;

  const unmappedPlaceSlots = useMemo(
    () =>
      sessionReady ? countUnmappedPlaceSlots(intents, groups) : 0,
    [intents, groups, sessionReady],
  );

  const hasDuplicatePlaceTarget = useMemo(
    () =>
      sessionReady &&
      intents.some(
        (intent) =>
          incompleteIntentReason(
            intent,
            groups,
            placesLabeledFor(intent),
            intents,
          ) === 'DuplicatePlace',
      ),
    [intents, groups, sessionReady],
  );

  const firstIncompleteReason = useMemo(() => {
    if (!sessionReady) return null;
    for (const intent of intents) {
      const reason = incompleteIntentReason(
        intent,
        groups,
        placesLabeledFor(intent),
        intents,
      );
      if (reason != null) return reason;
    }
    return null;
  }, [intents, groups, sessionReady]);

  const saveBlockedReason =
    !sessionReady || mutation.isPending || mutation.isSuccess
      ? null
      : unmappedPlaceSlots > 0
        ? t('qualification.saveBlockedUnmappedPlaces', {
            count: unmappedPlaceSlots,
          })
        : hasDuplicatePlaceTarget
          ? t('qualification.saveBlockedDuplicatePlace')
          : firstIncompleteReason != null
            ? t(`qualification.incompleteHint${firstIncompleteReason}`)
            : null;

  const hasDuplicateSources = useMemo(
    () => hasAnyDuplicateSourceOccurrence(intents, groups),
    [intents, groups],
  );

  function requestClose() {
    requestDiscardClose(mutation.isPending);
  }

  function toggleRow(intent: QualIntentDraft) {
    setExpandedId((prev) => (prev === intent.id ? null : intent.id));
  }

  function addIntent() {
    const peer = stageById.get(defaultDest);
    const next = applyDestinationToDraft(
      emptyQualIntent(defaultDest),
      defaultDest,
      peer?.formatKind,
    );
    if (!hasGroups) next.sourceKind = 'Overall';
    setIntents((prev) => [...prev, next]);
    setExpandedId(next.id);
  }

  function removeIntent(id: string) {
    setIntents((prev) => prev.filter((i) => i.id !== id));
    setExpandedId((prev) => (prev === id ? null : prev));
  }

  function updateIntent(next: QualIntentDraft) {
    setIntents((prev) =>
      prev.map((i) =>
        i.id === next.id ? syncPlaceSlotKeys(next, groups) : i,
      ),
    );
  }

  const contextDestinationName = contextDestinationId
    ? (stageNameById.get(contextDestinationId) ?? contextDestinationId)
    : null;

  return (
    <>
      <Dialog
        open={open}
        onClose={requestClose}
        title={t('qualification.title', { phase: stage.name })}
        description={
          contextDestinationName
            ? t('qualification.hintFromDestination', { source: stage.name })
            : t('qualification.hint')
        }
        size="lg"
        closeLabel={tCommon('close')}
        closeDisabled={mutation.isPending || discardOpen}
        trapFocus={!discardOpen}
        footerStatus={
          mutation.isError ||
          overCapacityWarnings.length > 0 ||
          hasDuplicateSources ||
          saveBlockedReason ? (
            <>
              {mutation.isError ? (
                <MutationError error={mutation.error} />
              ) : null}
              {saveBlockedReason ? (
                <Alert tone="danger" role="alert">
                  <p className="structure-qualification__hint-line">
                    {saveBlockedReason}
                  </p>
                </Alert>
              ) : null}
              {hasDuplicateSources ? (
                <Alert tone="warning" role="status">
                  <p className="structure-qualification__hint-line">
                    {t('qualification.duplicateWarning')}
                  </p>
                </Alert>
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
                        draft: warning.draft,
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
              disabled={mutation.isPending || discardOpen}
              onClick={requestClose}
            >
              <CloseIcon size="sm" />
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
                <>
                  <CheckIcon size="sm" />
                  {t('qualification.save')}
                </>
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
                {sessionReady ? intentCount : '—'}
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
                {sessionReady ? entryTotal : '—'}
              </span>
              <span className="structure-qualification__fact-label">
                {t('qualification.factEntries', { count: entryTotal })}
              </span>
            </div>
          </div>
          <button
            type="button"
            className="ds-btn ds-btn--primary"
            disabled={
              !sessionReady || peerStages.length === 0 || mutation.isPending
            }
            onClick={addIntent}
          >
            <PlusIcon size="sm" />
            <span>{t('qualification.add')}</span>
          </button>
        </div>

        {!sessionReady ? (
          <LoadingState size="region" />
        ) : peerStages.length === 0 ? (
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
            const filledSlots = (
              placeMapKeys(intent)?.keys ?? intent.destinationSlotKeys
            ).filter((k) => k.trim());
            const where =
              intent.targetKind === 'place' &&
              intent.destinationStageId.trim()
                ? t('qualification.summary.wherePlaceMapped', {
                    phase: destName,
                    filled: filledSlots.length,
                    count: occCount,
                  })
                : intent.targetKind === 'place'
                  ? t('qualification.summary.wherePlaceFallback')
                  : intent.destinationStageId.trim() && occCount > 0
                    ? t('qualification.summary.whereCount', {
                        phase: destName,
                        count: occCount,
                      })
                    : null;
            const incompleteReason = incompleteIntentReason(
              intent,
              groups,
              placesLabeledFor(intent),
              intents,
            );
            const statusMessage =
              incompleteReason == null
                ? null
                : t(`qualification.incompleteHint${incompleteReason}`);
            const sourceDuplicate = hasDuplicateSourceOccurrence(
              intent,
              intents,
              groups,
            );

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
                        <span className="structure-qualification__who-row">
                          {incompleteReason != null && statusMessage ? (
                            <Tooltip content={statusMessage}>
                              <span
                                className="structure-qualification__blocking-mark"
                                aria-hidden="true"
                              >
                                <ToastToneIcon tone="error" size="sm" />
                              </span>
                            </Tooltip>
                          ) : null}
                          {sourceDuplicate ? (
                            <Tooltip
                              content={t('qualification.duplicateTooltip')}
                            >
                              <span
                                className="structure-qualification__dup-mark"
                                aria-hidden="true"
                              >
                                <OverviewAttentionIcon size="sm" />
                              </span>
                            </Tooltip>
                          ) : null}
                          <span className="structure-qualification__who">
                            {who}
                          </span>
                        </span>
                        {where ? (
                          <span className="structure-qualification__where">
                            {where}
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
                        data={data}
                        sourceStageId={stage.stageId}
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
  data,
  sourceStageId,
  groups,
  hasGroups,
  peerStages,
  draftEntriesByDestination,
  locale,
  onChange,
}: {
  draft: QualIntentDraft;
  data: StructureView;
  sourceStageId: string;
  groups: { id: string; name: string }[];
  hasGroups: boolean;
  peerStages: StructureStageHubSummary[];
  draftEntriesByDestination: Map<string, number>;
  locale: string;
  onChange: (next: QualIntentDraft) => void;
}) {
  const { t } = useTranslation('structure');
  const { t: tCommon } = useTranslation('common');
  const fromId = useId();
  const toId = useId();
  const acrossId = useId();
  const pointsId = useId();
  const placeFillHintId = useId();

  const destStageId = draft.destinationStageId.trim();
  const destPeer = peerStages.find((p) => p.stageId === destStageId);
  const destFormat = destPeer?.formatKind;
  const formOnly = isFormOnlyDestination(destFormat);
  const showKindChoice = showsPopulationPlaceChoice(destFormat);

  const destSchematicQuery = useQuery({
    queryKey: queryKeys.stages.schematic(destStageId),
    queryFn: () => fetchStageSchematic(destStageId),
    enabled: draft.targetKind === 'place' && !formOnly && !!destStageId,
  });
  const placesLabeled =
    formOnly || areProgressionPlacesLabeled(destSchematicQuery.data);
  const labeledPlaces = useMemo(
    () => listLabeledPlaces(destSchematicQuery.data, t),
    [destSchematicQuery.data, t],
  );
  /** UX Place grain from dest schematic (Groups / Cup / Form). */
  const placeGrain = formOnly
    ? 'form'
    : (labeledPlaces[0]?.grain ??
      placeGrainForFormat(destSchematicQuery.data?.formatKind) ??
      placeGrainForFormat(destFormat) ??
      'slot');

  function setDestination(peer: StructureStageHubSummary) {
    onChange(applyDestinationToDraft(draft, peer.stageId, peer.formatKind));
  }

  function setTargetKind(kind: QualTargetKind) {
    if (formOnly) return;
    onChange(applyTargetKindToDraft(draft, kind));
  }

  const placeUnavailable =
    showKindChoice &&
    draft.targetKind === 'place' &&
    !placesLabeled &&
    !destSchematicQuery.isLoading;
  const placeOccurrences = useMemo(
    () =>
      draft.targetKind === 'place' && !formOnly
        ? expandOccurrences(draft, groups)
        : [],
    [draft, groups, formOnly],
  );
  const placeSlotKeys = useMemo(() => {
    const source =
      placeGrain === 'group'
        ? draft.destinationGroupIds
        : draft.destinationSlotKeys;
    return resizeDestinationSlotKeys(source, placeOccurrences.length);
  }, [
    draft.destinationGroupIds,
    draft.destinationSlotKeys,
    placeGrain,
    placeOccurrences.length,
  ]);

  function applyPlaceKeys(next: string[]) {
    onChange({
      ...draft,
      targetKind: 'place',
      destinationForm: false,
      destinationSlotKeys: placeGrain === 'slot' ? next : [],
      destinationGroupIds: placeGrain === 'group' ? next : [],
    });
  }

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
      <SortiesWhoWhereFlow
        sourceLabel={t('qualification.who')}
        destinationLabel={t('qualification.where')}
        feedsLabel={t('qualification.feeds')}
        source={
          <>
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
          </>
        }
        destination={
          <>
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
            const draftTotal =
              draftEntriesByDestination.get(peer.stageId) ?? 0;
            const draftThisIntent =
              draft.destinationStageId.trim() === peer.stageId
                ? expandOccurrences(draft, groups).length
                : 0;
            const capacity = peer.compositionCapacity;
            const expected =
              capacity != null && capacity > 0
                ? expectedPopulationWithQualDraft({
                    data,
                    destination: peer,
                    sourceStageId,
                    draftQualVolume: draftTotal,
                  })
                : null;
            const description =
              expected != null && capacity != null && capacity > 0 ? (
                <DestinationDraftMeter
                  count={expected}
                  capacity={capacity}
                  draft={draftThisIntent}
                  label={t('qualification.tileContribution', {
                    count: expected,
                    capacity,
                  })}
                  draftLabel={t('qualification.tileContributionDraft', {
                    count: draftThisIntent,
                  })}
                  ariaLabel={t('qualification.tileContributionAria', {
                    phase: peer.name,
                    count: expected,
                    capacity,
                    draft: draftThisIntent,
                  })}
                />
              ) : draftThisIntent > 0 ? (
                t('qualification.tileContributionEntries', {
                  count: draftThisIntent,
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
                  setDestination(peer);
                }}
              />
            );
          })}
        </div>
      )}

      {showKindChoice && destStageId ? (
      <div
        className="structure-qualification__scope-tiles"
        data-count="2"
        role="radiogroup"
        aria-label={t('qualification.destinationKind')}
      >
        <ChoiceTile
          label={t('qualification.kindPopulation')}
          description={t('qualification.kindPopulationHint')}
          leading={<StructureIcon size="sm" />}
          selected={draft.targetKind === 'population'}
          onChange={(selected) => {
            if (!selected) return;
            setTargetKind('population');
          }}
        />
        <ChoiceTile
          label={t('qualification.kindPlace')}
          description={t('qualification.kindPlaceHint')}
          leading={<CupFormatIcon size="sm" />}
          selected={draft.targetKind === 'place'}
          onChange={(selected) => {
            if (!selected) return;
            setTargetKind('place');
          }}
        />
      </div>
      ) : null}

      {draft.targetKind === 'place' &&
      !formOnly &&
      destStageId &&
      placeGrain !== 'form' ? (
        destSchematicQuery.isLoading ? (
          <ul
            className="structure-qualification__place-map structure-qualification__place-map--skeleton"
            aria-busy="true"
            aria-label={tCommon('loading')}
          >
            {Array.from({
              length: Math.max(placeOccurrences.length, 3),
            }).map((_, index) => (
              <li key={`skel-${index}`} aria-hidden="true">
                <span className="structure-qualification__place-map-skel-label" />
                <span className="structure-qualification__place-map-skel-control" />
              </li>
            ))}
          </ul>
        ) : placeUnavailable || !placesLabeled ? (
          <p className="structure-qualification__field-hint" role="status">
            {t('qualification.emptyNoPlacePeerBody')}
          </p>
        ) : labeledPlaces.length === 0 ? (
          <p className="structure-qualification__field-hint" role="status">
            {t('qualification.placeEmpty')}
          </p>
        ) : placeOccurrences.length === 0 ? (
          <p className="structure-qualification__field-hint" role="status">
            {t('qualification.placeMapEmptySelection')}
          </p>
        ) : (
          <div className="structure-qualification__place-map-block">
            <div className="structure-qualification__place-map-toolbar">
              {peerStages.length > 0 ? (
                <p className="structure-qualification__place-map-heading">
                  {t('qualification.placeMapHeading')}
                </p>
              ) : null}
              <span className="structure-qualification__place-fill">
                <span
                  id={placeFillHintId}
                  className="ds-visually-hidden"
                >
                  {t('qualification.placeFillHint')}
                </span>
                <Tooltip content={t('qualification.placeFillHint')}>
                  <button
                    type="button"
                    className="ds-btn ds-btn--ghost"
                    aria-describedby={placeFillHintId}
                    disabled={(() => {
                      const emptyCount = placeSlotKeys.filter(
                        (k) => !k.trim(),
                      ).length;
                      if (emptyCount === 0) return true;
                      if (labeledPlaces.length === 0) return true;
                      if (placeGrain === 'group') return false;
                      const used = new Set(
                        placeSlotKeys
                          .map((k) => k.trim())
                          .filter((k) => k.length > 0),
                      );
                      return !labeledPlaces.some(
                        (place) => !used.has(place.apiIdentity),
                      );
                    })()}
                    onClick={() => {
                      const ids = labeledPlaces.map(
                        (place) => place.apiIdentity,
                      );
                      const next =
                        placeGrain === 'group'
                          ? fillEmptyPlaceKeysAllowingReuse(placeSlotKeys, ids)
                          : fillEmptyPlaceSlotKeys(placeSlotKeys, ids);
                      applyPlaceKeys(next);
                    }}
                  >
                    <LucideIcon icon={ListPlus} size="sm" />
                    {t('qualification.placeFillEmpties')}
                  </button>
                </Tooltip>
              </span>
            </div>
            <ul
              className="structure-qualification__place-map"
              aria-label={t('qualification.placeMapAria')}
            >
              {placeOccurrences.map((occ, index) => {
                const selected = placeSlotKeys[index]?.trim() || null;
                const rowId = `qual-place-${draft.id}-${index}`;
                const rowInvalid = !selected;
                return (
                  <li
                    key={`${occ.scope}:${occ.groupId ?? ''}:${occ.position}:${occ.acrossGroupsPosition ?? ''}:${index}`}
                    data-invalid={rowInvalid ? 'true' : 'false'}
                  >
                    <label
                      className="structure-qualification__place-map-label"
                      htmlFor={rowId}
                    >
                      {occurrenceLabel(
                        occ,
                        (n) => ordinalRank(n, locale),
                        t,
                      )}
                    </label>
                    <div className="structure-qualification__place-map-control">
                      <Select
                        id={rowId}
                        options={labeledPlaces.map((place) => ({
                          value: place.apiIdentity,
                          label: place.label,
                          disabled:
                            placeGrain === 'slot' &&
                            placeSlotKeys.some(
                              (key, j) =>
                                j !== index &&
                                key.trim() === place.apiIdentity,
                            ),
                        }))}
                        value={selected}
                        invalid={rowInvalid}
                        placeholder={t('qualification.placeSlotPlaceholder')}
                        allowClear
                        aria-label={t('qualification.placeMapRowAria', {
                          source: occurrenceLabel(
                            occ,
                            (n) => ordinalRank(n, locale),
                            t,
                          ),
                        })}
                        onChange={(value) => {
                          const next = placeSlotKeys.slice();
                          next[index] = value?.trim() ?? '';
                          applyPlaceKeys(next);
                        }}
                      />
                      {rowInvalid ? (
                        <span
                          className="structure-qualification__place-map-error"
                          aria-hidden="true"
                        >
                          <ToastToneIcon tone="error" size="sm" />
                        </span>
                      ) : null}
                    </div>
                  </li>
                );
              })}
            </ul>
          </div>
        )
      ) : null}
          </>
        }
      />
    </div>
  );
}

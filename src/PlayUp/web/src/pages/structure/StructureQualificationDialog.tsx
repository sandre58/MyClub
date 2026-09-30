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
import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { fetchStageSchematic, replaceStageQualificationRules } from '../../api';
import { Alert } from '../../design-system/components/Alert';
import { ConfirmDialog } from '../../design-system/components/ConfirmDialog';
import { Dialog } from '../../design-system/components/Dialog';
import { Tooltip } from '../../design-system/components/Tooltip';
import { ToastToneIcon } from '../../design-system/icons/toastIcons';
import {
  CheckIcon,
  EmptySelectionIcon,
  GroupsFormatIcon,
  OverviewAttentionIcon,
  PlusIcon,
  StandingRulesIcon,
  StructureIcon,
  SwissFormatIcon,
  TrashIcon,
} from '../../design-system/icons/contentIcons';
import {
  ChevronDownIcon,
  CloseIcon,
} from '../../design-system/icons/shellIcons';
import { notify } from '../../design-system/toastStore';
import { useDiscardConfirm } from '../../design-system/useDiscardConfirm';
import { queryKeys } from '../../queryKeys';
import type {
  QualificationIntentSourceKind,
  SchematicCase,
  StageSchematic,
  StructureStageHubSummary,
  StructureView,
} from '../../types';
import {
  EmptyState,
  LoadingState,
  MutationError,
  PendingLabel,
  persistentStructureMutationSotHref,
} from '../../ui';
import { StructureQualificationIntentEditor } from './StructureQualificationIntentEditor';
import { invalidateAfterStructureMutation } from './structureInvalidation';
import { sortiesAvalPeerStages } from './structureSortiesDestinations';
import { areProgressionPlacesLabeled } from './structurePlaceLabel';
import { expectedPopulationWithQualDraft } from './structurePopulationVolume';
import {
  countUnmappedPlaceSlots,
  emptyQualIntent,
  expandOccurrences,
  hasAnyDuplicateSourceOccurrence,
  hasDuplicateSourceOccurrence,
  incompleteIntentReason,
  intentFromApi,
  isIntentComplete,
  pathToSingletonIntent,
  placeMapKeys,
  serializeIntents,
  summarizeIntentWho,
  syncPlaceSlotKeys,
  toApiIntent,
  applyDestinationToDraft,
  isFormOnlyDestination,
  normalizeFormOnlyDestinationDraft,
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
    () => (sessionReady ? countUnmappedPlaceSlots(intents, groups) : 0),
    [intents, groups, sessionReady],
  );

  const hasDuplicatePlaceTarget =
    sessionReady &&
    intents.some(
      (intent) =>
        incompleteIntentReason(
          intent,
          groups,
          placesLabeledFor(intent),
          intents,
        ) === 'DuplicatePlace',
    );

  const firstIncompleteReason = (() => {
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
  })();

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
      prev.map((i) => (i.id === next.id ? syncPlaceSlotKeys(next, groups) : i)),
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
                <MutationError
                  error={mutation.error}
                  sotHref={persistentStructureMutationSotHref(
                    mutation.error,
                    data.competitionId,
                  )}
                />
              ) : null}
              {saveBlockedReason ? (
                <Alert tone="warning" role="status">
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
          <div className="structure-qualification__summary" aria-live="polite">
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
                      className="structure-qualification__tile ds-selectable-tile"
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
                                    <ToastToneIcon tone="attention" size="sm" />
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
                          <TrashIcon size="sm" />
                        </button>
                      </div>

                      {isExpanded ? (
                        <div className="structure-qualification__panel">
                          <StructureQualificationIntentEditor
                            draft={intent}
                            data={data}
                            sourceStageId={stage.stageId}
                            groups={groups}
                            hasGroups={hasGroups}
                            peerStages={peerStages}
                            draftEntriesByDestination={
                              draftEntriesByDestination
                            }
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

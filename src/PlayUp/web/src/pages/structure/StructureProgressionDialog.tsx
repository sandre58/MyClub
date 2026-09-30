// -----------------------------------------------------------------------
// Progression dialog — Intent Round × Outcome → Destination.
// UX Place = Placement destination picker (Slot | Group).
// Place D1: Expand[i] ↔ destinationSlotKeys[i] XOR destinationGroupIds[i].
// -----------------------------------------------------------------------

import {
  useMutation,
  useQueries,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query';
import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  fetchStageOverview,
  fetchStageSchematic,
  replaceStageProgressionRules,
} from '../../api';
import { Alert } from '../../design-system/components/Alert';
import { ConfirmDialog } from '../../design-system/components/ConfirmDialog';
import { Dialog } from '../../design-system/components/Dialog';
import { Tooltip } from '../../design-system/components/Tooltip';
import { ToastToneIcon } from '../../design-system/icons/toastIcons';
import {
  CheckIcon,
  CupFormatIcon,
  EmptySelectionIcon,
  PlusIcon,
  StructureIcon,
  TrashIcon,
  TrophyIcon,
} from '../../design-system/icons/contentIcons';
import {
  ChevronDownIcon,
  CloseIcon,
} from '../../design-system/icons/shellIcons';
import { notify } from '../../design-system/toastStore';
import { useDiscardConfirm } from '../../design-system/useDiscardConfirm';
import { queryKeys } from '../../queryKeys';
import type {
  ProgressionOutcome,
  StageBracketPair,
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
import {
  StructureProgressionIntentEditor,
  type ProgRoundOption,
} from './StructureProgressionIntentEditor';
import { invalidateAfterStructureMutation } from './structureInvalidation';
import { isPopulationDestination } from './structureProgressionDraft';
import { sortiesAvalPeerStages } from './structureSortiesDestinations';
import {
  championshipTerminalRound,
  roundsWithFixtures,
} from './structureProgressionChampionshipPath';
import {
  countUnmappedPlaceSlots,
  emptyProgIntent,
  expandContribution,
  expandCount,
  incompleteIntentReason,
  intentFromApi,
  isIntentComplete,
  pathToSingletonIntent,
  placeMapKeys,
  resizeDestinationSlotKeys,
  serializeIntents,
  summarizeIntentWho,
  syncPlaceSlotKeys,
  toApiIntent,
  applyDestinationToDraft,
  isFormOnlyDestination,
  normalizeFormOnlyDestinationDraft,
  type ProgIntentDraft,
  type ProgTargetKind,
} from './structureProgressionDraft';
import { areProgressionPlacesLabeled } from './structurePlaceLabel';
import { expectedPopulationWithProgDraft } from './structurePopulationVolume';

type StructureProgressionDialogProps = {
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

const EMPTY_BRACKET_PAIRS: StageBracketPair[] = [];

function resolveRoundIdForPairOrFixture(
  rounds: ProgRoundOption[],
  _sourcePairKey: string,
): { roundId: string; roundName: string } | null {
  // Cup mono-round: all pairs belong to the structural form / first round.
  // SourcePairKey is BracketPair.PairKey — never FixtureId.
  if (rounds.length === 1) {
    return { roundId: rounds[0]!.id, roundName: rounds[0]!.name };
  }
  const terminal = championshipTerminalRound(rounds);
  if (terminal) {
    return { roundId: terminal.id, roundName: terminal.name };
  }
  return rounds[0]
    ? { roundId: rounds[0].id, roundName: rounds[0].name }
    : null;
}

export function StructureProgressionDialog({
  data,
  stage,
  open,
  onClose,
  openedFromDestinationStageId = null,
}: StructureProgressionDialogProps) {
  const { t } = useTranslation('structure');
  const { t: tCommon } = useTranslation('common');
  const { t: tReg } = useTranslation('regulation');
  const queryClient = useQueryClient();

  const overviewQuery = useQuery({
    queryKey: queryKeys.stages.detail(stage.stageId),
    queryFn: () => fetchStageOverview(stage.stageId),
    enabled: open,
  });
  const sourceSchematicQuery = useQuery({
    queryKey: queryKeys.stages.schematic(stage.stageId),
    queryFn: () => fetchStageSchematic(stage.stageId),
    enabled: open,
  });
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

  const [intents, setIntents] = useState<ProgIntentDraft[]>([]);
  const [baselineSerialized, setBaselineSerialized] = useState('');
  const [expandedId, setExpandedId] = useState<string | null>(null);
  /** False until overview + source schematic have settled for this open session. */
  const [sessionReady, setSessionReady] = useState(false);

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

  function placesLabeledFor(intent: ProgIntentDraft): boolean {
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

  const dirty =
    sessionReady && serializeIntents(intents) !== baselineSerialized;
  const {
    discardOpen,
    requestClose: requestDiscardClose,
    cancelDiscard,
    confirmDiscard,
    resetDiscard,
  } = useDiscardConfirm(dirty, onClose);

  const rounds: ProgRoundOption[] = useMemo(
    () =>
      (overviewQuery.data?.rounds ?? []).map((r) => ({
        id: r.id,
        name: r.name,
        fixtures: r.fixtures,
      })),
    [overviewQuery.data?.rounds],
  );
  const bracketPairs = overviewQuery.data?.bracketPairs ?? EMPTY_BRACKET_PAIRS;
  const expandSourceCount =
    bracketPairs.length > 0 ? bracketPairs.length : null; // null = derive from round fixtures
  const playableRounds = useMemo(() => roundsWithFixtures(rounds), [rounds]);
  const championshipTerminal = useMemo(
    () => championshipTerminalRound(rounds),
    [rounds],
  );
  const championshipTerminalRoundId = championshipTerminal?.id ?? null;
  const roundById = useMemo(
    () => new Map(rounds.map((r) => [r.id, r])),
    [rounds],
  );

  const mutation = useMutation({
    mutationFn: () => {
      if (intents.length === 0) {
        return replaceStageProgressionRules(stage.stageId, {
          intents: null,
        });
      }
      return replaceStageProgressionRules(stage.stageId, {
        intents: intents.map((draft, index) => toApiIntent(draft, index + 1)),
      });
    },
    onSuccess: async () => {
      await invalidateAfterStructureMutation(queryClient, data.competitionId);
      notify.success(t('progression.toastUpdated'));
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
    if (sourceSchematicQuery.isLoading) {
      return;
    }
    // Do not rehydrate from invalidate while saving / closing.
    if (mutation.isPending || mutation.isSuccess) {
      return;
    }

    const fromIntents = stage.progressionIntents ?? [];
    if (fromIntents.length > 0) {
      const next = fromIntents.map((intent) => {
        const synced = syncPlaceSlotKeys(intentFromApi(intent, stage.stageId));
        const peer = stageById.get(synced.destinationStageId.trim());
        return normalizeFormOnlyDestinationDraft(synced, peer?.formatKind);
      });
      setIntents(next);
      setBaselineSerialized(serializeIntents(next));
      setExpandedId(null);
      setSessionReady(true);
      resetDiscard();
      return;
    }

    // Wait for rounds when falling back from path-list projection.
    if (overviewQuery.isLoading) {
      return;
    }

    const existingPaths = stage.progressionPaths ?? [];
    if (existingPaths.length === 0) {
      setIntents([]);
      setBaselineSerialized(serializeIntents([]));
      setExpandedId(null);
      setSessionReady(true);
      resetDiscard();
      return;
    }

    type PathGroup = {
      roundId: string;
      roundName: string;
      outcome: ProgressionOutcome;
      destinationStageId: string;
      targetKind: ProgTargetKind;
      paths: typeof existingPaths;
    };
    const groups = new Map<string, PathGroup>();
    for (const path of existingPaths) {
      const resolved = resolveRoundIdForPairOrFixture(
        rounds,
        path.sourcePairKey,
      );
      if (!resolved) continue;
      const targetKind: ProgTargetKind =
        !path.destinationForm &&
        isPopulationDestination(path.destinationSlotKey) &&
        !path.destinationGroupId?.trim()
          ? 'population'
          : 'place';
      const placeMode = path.destinationForm
        ? 'form'
        : path.destinationGroupId?.trim()
          ? 'group'
          : 'slot';
      const key = `${resolved.roundId}|${path.outcome}|${path.destinationStageId}|${targetKind}|${placeMode}`;
      const existing = groups.get(key);
      if (existing) {
        existing.paths.push(path);
        continue;
      }
      groups.set(key, {
        roundId: resolved.roundId,
        roundName: resolved.roundName,
        outcome: path.outcome,
        destinationStageId: path.destinationStageId,
        targetKind,
        paths: [path],
      });
    }

    const next: ProgIntentDraft[] = [];
    let order = 1;
    const orderedPairKeys = [...bracketPairs]
      .map((p) => p.pairKey)
      .sort((a, b) => a.localeCompare(b));
    for (const group of groups.values()) {
      const round = roundById.get(group.roundId);
      const expandCount =
        orderedPairKeys.length > 0
          ? orderedPairKeys.length
          : (round?.fixtures.length ?? group.paths.length);
      if (group.targetKind === 'population') {
        const seed = pathToSingletonIntent(
          group.paths[0],
          stage.stageId,
          group.roundId,
          group.roundName,
          order++,
        );
        next.push(
          syncPlaceSlotKeys({
            ...seed,
            targetKind: 'population',
            destinationSlotKeys: [],
            destinationGroupIds: [],
            destinationForm: false,
            expandedPathCount: expandCount,
          }),
        );
        continue;
      }

      const isFormPlace = group.paths.some((p) => !!p.destinationForm);
      if (isFormPlace) {
        next.push(
          syncPlaceSlotKeys({
            id: crypto.randomUUID(),
            order: order++,
            roundId: group.roundId,
            roundName: group.roundName,
            outcome: group.outcome,
            targetKind: 'place',
            destinationStageId: group.destinationStageId,
            destinationSlotKeys: [],
            destinationGroupIds: [],
            destinationForm: true,
            expandedPathCount: expandCount,
          }),
        );
        continue;
      }

      const isGroupPlace = group.paths.some((p) =>
        Boolean(p.destinationGroupId?.trim()),
      );
      const keys = resizeDestinationSlotKeys([], expandCount);
      const sortedPaths = [...group.paths].sort((a, b) =>
        a.sourcePairKey.localeCompare(b.sourcePairKey),
      );
      for (const path of sortedPaths) {
        let idx =
          orderedPairKeys.length > 0
            ? orderedPairKeys.indexOf(path.sourcePairKey)
            : sortedPaths.indexOf(path);
        if (idx < 0) {
          idx = sortedPaths.indexOf(path);
        }
        const identity = isGroupPlace
          ? (path.destinationGroupId?.trim() ?? '')
          : (path.destinationSlotKey?.trim() ?? '');
        if (idx >= 0 && identity) {
          keys[idx] = identity;
        }
      }
      next.push(
        syncPlaceSlotKeys({
          id: crypto.randomUUID(),
          order: order++,
          roundId: group.roundId,
          roundName: group.roundName,
          outcome: group.outcome,
          targetKind: 'place',
          destinationStageId: group.destinationStageId,
          destinationSlotKeys: isGroupPlace ? [] : keys,
          destinationGroupIds: isGroupPlace ? keys : [],
          destinationForm: false,
          expandedPathCount: expandCount,
        }),
      );
    }

    const normalized = next.map((intent) => {
      const peer = stageById.get(intent.destinationStageId.trim());
      return normalizeFormOnlyDestinationDraft(intent, peer?.formatKind);
    });
    setIntents(normalized);
    setBaselineSerialized(serializeIntents(normalized));
    setExpandedId(null);
    setSessionReady(true);
    resetDiscard();
  }, [
    open,
    stage.stageId,
    stage.progressionIntents,
    stage.progressionPaths,
    overviewQuery.isLoading,
    sourceSchematicQuery.isLoading,
    rounds,
    roundById,
    bracketPairs,
    mutation.isPending,
    mutation.isSuccess,
    resetDiscard,
    stageById,
  ]);

  const pathTotal = useMemo(
    () => intents.reduce((sum, intent) => sum + expandContribution(intent), 0),
    [intents],
  );

  const draftEntriesByDestination = useMemo(() => {
    const map = new Map<string, number>();
    for (const intent of intents) {
      const destId = intent.destinationStageId.trim();
      if (!destId) continue;
      map.set(destId, (map.get(destId) ?? 0) + expandContribution(intent));
    }
    return map;
  }, [intents]);

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
      const expected = expectedPopulationWithProgDraft({
        data,
        destination: dest,
        sourceStageId: stage.stageId,
        draftProgVolume: draft,
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

  const canAuthor = peerStages.length > 0;

  const intentsComplete =
    sessionReady &&
    intents.every((intent) =>
      isIntentComplete(
        intent,
        intents,
        placesLabeledFor(intent),
        championshipTerminalRoundId,
      ),
    );

  const canSave = !mutation.isPending && intentsComplete;

  const unmappedPlaceSlots = useMemo(
    () => (sessionReady ? countUnmappedPlaceSlots(intents) : 0),
    [intents, sessionReady],
  );

  const hasDuplicatePlaceTarget =
    sessionReady &&
    intents.some(
      (intent) =>
        incompleteIntentReason(
          intent,
          intents,
          placesLabeledFor(intent),
          championshipTerminalRoundId,
        ) === 'DuplicatePlace',
    );

  const firstIncompleteReason = (() => {
    if (!sessionReady) return null;
    for (const intent of intents) {
      const reason = incompleteIntentReason(
        intent,
        intents,
        placesLabeledFor(intent),
        championshipTerminalRoundId,
      );
      if (reason != null) return reason;
    }
    return null;
  })();

  const saveBlockedReason =
    !sessionReady || mutation.isPending || mutation.isSuccess
      ? null
      : unmappedPlaceSlots > 0
        ? t('progression.saveBlockedUnmappedPlaces', {
            count: unmappedPlaceSlots,
          })
        : hasDuplicatePlaceTarget
          ? t('progression.saveBlockedDuplicatePlace')
          : firstIncompleteReason != null
            ? t(`progression.incompleteHint${firstIncompleteReason}`)
            : null;

  function requestClose() {
    requestDiscardClose(mutation.isPending);
  }

  function toggleRow(intent: ProgIntentDraft) {
    setExpandedId((prev) => (prev === intent.id ? null : intent.id));
  }

  function addIntent() {
    const peer = stageById.get(defaultDest);
    const next = applyDestinationToDraft(
      emptyProgIntent(defaultDest, 'population', intents.length + 1),
      defaultDest,
      peer?.formatKind,
    );
    if (championshipTerminal) {
      next.roundId = championshipTerminal.id;
      next.roundName = championshipTerminal.name;
      next.expandedPathCount =
        expandSourceCount ?? championshipTerminal.fixtures.length;
    }
    setIntents((prev) => [...prev, next]);
    setExpandedId(next.id);
  }

  function removeIntent(id: string) {
    setIntents((prev) => prev.filter((p) => p.id !== id));
    setExpandedId((prev) => (prev === id ? null : prev));
  }

  function updateIntent(next: ProgIntentDraft) {
    setIntents((prev) =>
      prev.map((p) => (p.id === next.id ? syncPlaceSlotKeys(next) : p)),
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
        title={t('progression.title', { phase: stage.name })}
        description={
          contextDestinationName
            ? t('progression.hintFromDestination', { source: stage.name })
            : t('progression.hint')
        }
        size="lg"
        closeLabel={tCommon('close')}
        closeDisabled={mutation.isPending || discardOpen}
        trapFocus={!discardOpen}
        footerStatus={
          mutation.isError ||
          overCapacityWarnings.length > 0 ||
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
                <Alert tone="danger" role="alert">
                  <p className="structure-qualification__hint-line">
                    {saveBlockedReason}
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
                      {t('progression.overCapacityWarning', {
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
                <PendingLabel>{t('progression.saving')}</PendingLabel>
              ) : (
                <>
                  <CheckIcon size="sm" />
                  {t('progression.save')}
                </>
              )}
            </button>
          </>
        }
      >
        <div className="structure-qualification structure-progression">
          <div className="structure-qualification__summary" aria-live="polite">
            <div className="structure-qualification__facts">
              <div className="structure-qualification__fact structure-qualification__fact--secondary">
                <span className="structure-qualification__fact-value">
                  {sessionReady ? intents.length : '—'}
                </span>
                <span className="structure-qualification__fact-label">
                  {t('progression.factRules', { count: intents.length })}
                </span>
              </div>
              <span
                className="structure-qualification__fact-rule"
                aria-hidden="true"
              />
              <div className="structure-qualification__fact structure-qualification__fact--primary">
                <span className="structure-qualification__fact-value">
                  {sessionReady ? pathTotal : '—'}
                </span>
                <span className="structure-qualification__fact-label">
                  {t('progression.factEntries', { count: pathTotal })}
                </span>
              </div>
            </div>
            <button
              type="button"
              className="ds-btn ds-btn--primary"
              disabled={!sessionReady || !canAuthor || mutation.isPending}
              onClick={addIntent}
            >
              <PlusIcon size="sm" />
              <span>{t('progression.add')}</span>
            </button>
          </div>

          {!sessionReady ? (
            <LoadingState size="region" />
          ) : !canAuthor ? (
            <EmptyState
              variant="idle"
              icon={<StructureIcon size="lg" />}
              title={t('progression.emptyNoPeerTitle')}
            >
              {t('progression.emptyNoPeerBody')}
            </EmptyState>
          ) : intents.length === 0 ? (
            <EmptyState
              variant="idle"
              icon={<EmptySelectionIcon size="lg" />}
              title={t('progression.emptyTitle')}
            >
              {t('progression.emptyBody')}
            </EmptyState>
          ) : (
            <ul className="structure-qualification__list">
              {intents.map((intent) => {
                const isExpanded = expandedId === intent.id;
                const who =
                  summarizeIntentWho(intent, t) || t('progression.newPath');
                const destName =
                  stageNameById.get(intent.destinationStageId) ??
                  intent.destinationStageId;
                const filledSlots = (
                  placeMapKeys(intent)?.keys ?? intent.destinationSlotKeys
                ).filter((k) => k.trim());
                const where =
                  intent.targetKind === 'population' &&
                  intent.destinationStageId.trim()
                    ? t('progression.summary.whereCount', {
                        phase: destName,
                        count:
                          expandCount(intent) || expandContribution(intent),
                      })
                    : intent.targetKind === 'place' &&
                        intent.destinationStageId.trim()
                      ? t('progression.summary.wherePlaceMapped', {
                          phase: destName,
                          filled: filledSlots.length,
                          count: expandCount(intent),
                        })
                      : intent.targetKind === 'place'
                        ? t('progression.summary.wherePlaceFallback')
                        : null;
                const incompleteReason = incompleteIntentReason(
                  intent,
                  intents,
                  placesLabeledFor(intent),
                  championshipTerminalRoundId,
                );
                const statusMessage =
                  incompleteReason == null
                    ? null
                    : t(`progression.incompleteHint${incompleteReason}`);

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
                            {intent.outcome === 'Winner' ? (
                              <TrophyIcon size="lg" />
                            ) : (
                              <CupFormatIcon size="lg" />
                            )}
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
                          aria-label={t('progression.remove')}
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
                          <StructureProgressionIntentEditor
                            draft={intent}
                            data={data}
                            sourceStage={stage}
                            peerStages={peerStages}
                            rounds={rounds}
                            playableRounds={playableRounds}
                            championshipTerminal={championshipTerminal}
                            bracketPairs={bracketPairs}
                            roundsLoading={overviewQuery.isLoading}
                            draftEntriesByDestination={
                              draftEntriesByDestination
                            }
                            onChange={(next) => {
                              const round = roundById.get(next.roundId);
                              const count =
                                expandSourceCount ??
                                round?.fixtures.length ??
                                next.expandedPathCount;
                              updateIntent({
                                ...next,
                                roundName: round?.name ?? next.roundName,
                                expandedPathCount: count,
                              });
                            }}
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

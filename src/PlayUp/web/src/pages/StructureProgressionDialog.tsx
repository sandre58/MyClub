// -----------------------------------------------------------------------
// Progression dialog — Intent Round × Outcome → Destination (Prog V3).
// UX Place = Placement destination picker (Slot | Group).
// Place D1: Expand[i] ↔ destinationSlotKeys[i] XOR destinationGroupIds[i].
// -----------------------------------------------------------------------

import { useMutation, useQueries, useQuery, useQueryClient } from '@tanstack/react-query';
import { ListPlus, Trash2 } from 'lucide-react';
import { useEffect, useId, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  fetchStageOverview,
  fetchStageSchematic,
  replaceStageProgressionRules,
} from '../api';
import { Alert } from '../design-system/components/Alert';
import { ChoiceTile } from '../design-system/components/ChoiceTile';
import { ConfirmDialog } from '../design-system/components/ConfirmDialog';
import { Dialog } from '../design-system/components/Dialog';
import { Field } from '../design-system/components/Field';
import { Select } from '../design-system/components/Select';
import { Tooltip } from '../design-system/components/Tooltip';
import { LucideIcon } from '../design-system/icons/Icon';
import { ToastToneIcon } from '../design-system/icons/toastIcons';
import {
  ChampionshipFormatIcon,
  CupFormatIcon,
  EmptySelectionIcon,
  GroupsFormatIcon,
  PlusIcon,
  StructureIcon,
  SwissFormatIcon,
  TrophyIcon,
} from '../design-system/icons/contentIcons';
import { ChevronDownIcon } from '../design-system/icons/shellIcons';
import { notify } from '../design-system/toastStore';
import { useDiscardConfirm } from '../design-system/useDiscardConfirm';
import { queryKeys } from '../queryKeys';
import type {
  ProgressionOutcome,
  StageFixture,
  StageSchematic,
  StructureFormatKind,
  StructureStageHubSummary,
  StructureView,
} from '../types';
import { EmptyState, LoadingState, MutationError, PendingLabel } from '../ui';
import { DestinationDraftMeter } from './DestinationDraftMeter';
import { invalidateAfterStructureMutation } from './structureInvalidation';
import { isPopulationDestination } from './structureProgression';
import { sortiesAvalPeerStages } from './structureSortiesDestinations';
import {
  championshipTerminalRound,
  roundsWithFixtures,
} from './structureProgressionChampionshipPath';
import { SortiesWhoWhereFlow } from './SortiesWhoWhereFlow';
import {
  countUnmappedPlaceSlots,
  emptyProgIntent,
  expandContribution,
  expandCount,
  fillEmptyPlaceKeysAllowingReuse,
  fillEmptyPlaceSlotKeys,
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
  applyTargetKindToDraft,
  isFormOnlyDestination,
  normalizeFormOnlyDestinationDraft,
  showsPopulationPlaceChoice,
  type ProgIntentDraft,
  type ProgTargetKind,
} from './structureProgressionDraft';
import {
  areProgressionPlacesLabeled,
  listLabeledPlaces,
  placeGrainForFormat,
} from './structurePlaceLabel';
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

type ProgRoundOption = {
  id: string;
  name: string;
  fixtures: StageFixture[];
};

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

function resolveRoundIdForFixture(
  rounds: ProgRoundOption[],
  fixtureId: string,
): { roundId: string; roundName: string } | null {
  for (const round of rounds) {
    if (round.fixtures.some((f) => f.id === fixtureId)) {
      return { roundId: round.id, roundName: round.name };
    }
  }
  return null;
}

function fixturePlaceMapLabel(
  fixture: StageFixture,
  index: number,
  t: (key: string, opts?: Record<string, unknown>) => string,
): string {
  const base = t('progression.matchOrdinal', { n: index + 1 });
  const a = fixture.slotAKey?.trim();
  const b = fixture.slotBKey?.trim();
  if (!a && !b) return base;
  return `${base} · ${a || '—'} vs ${b || '—'}`;
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
  /** Inter-Stage aval only — self / amont excluded (Sorties ownership). */
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

  const dirty = sessionReady && serializeIntents(intents) !== baselineSerialized;
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
          paths: null,
        });
      }
      return replaceStageProgressionRules(stage.stageId, {
        intents: intents.map((draft, index) => toApiIntent(draft, index + 1)),
        paths: null,
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
      const resolved = resolveRoundIdForFixture(rounds, path.sourceFixtureId);
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
    for (const group of groups.values()) {
      const round = roundById.get(group.roundId);
      const fixtureCount = round?.fixtures.length ?? group.paths.length;
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
            expandedPathCount: fixtureCount,
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
            expandedPathCount: fixtureCount,
          }),
        );
        continue;
      }

      const isGroupPlace = group.paths.some((p) =>
        Boolean(p.destinationGroupId?.trim()),
      );
      const keys = resizeDestinationSlotKeys([], fixtureCount);
      for (const path of group.paths) {
        const idx =
          round?.fixtures.findIndex((f) => f.id === path.sourceFixtureId) ?? -1;
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
          expandedPathCount: fixtureCount,
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
    mutation.isPending,
    mutation.isSuccess,
    resetDiscard,
    stageById,
  ]);

  const pathTotal = useMemo(
    () =>
      intents.reduce((sum, intent) => sum + expandContribution(intent), 0),
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
  }, [
    data,
    draftEntriesByDestination,
    peerStages,
    stage.stageId,
    stageById,
  ]);

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

  const hasDuplicatePlaceTarget = useMemo(
    () =>
      sessionReady &&
      intents.some(
        (intent) =>
          incompleteIntentReason(
            intent,
            intents,
            placesLabeledFor(intent),
            championshipTerminalRoundId,
          ) === 'DuplicatePlace',
      ),
    [championshipTerminalRoundId, intents, sessionReady],
  );

  const firstIncompleteReason = useMemo(() => {
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
  }, [championshipTerminalRoundId, intents, sessionReady]);

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
      next.expandedPathCount = championshipTerminal.fixtures.length;
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
                <MutationError error={mutation.error} />
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
                t('progression.save')
              )}
            </button>
          </>
        }
      >
        <div className="structure-qualification structure-progression">
          <div
            className="structure-qualification__summary"
            aria-live="polite"
          >
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
                        count: expandCount(intent) || expandContribution(intent),
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
                  <li
                    key={intent.id}
                    className="structure-qualification__item"
                  >
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
                          <LucideIcon icon={Trash2} size="sm" />
                        </button>
                      </div>

                      {isExpanded ? (
                        <div className="structure-qualification__panel">
                          <ProgIntentEditor
                            draft={intent}
                            data={data}
                            sourceStage={stage}
                            peerStages={peerStages}
                            rounds={rounds}
                            playableRounds={playableRounds}
                            championshipTerminal={championshipTerminal}
                            roundsLoading={overviewQuery.isLoading}
                            draftEntriesByDestination={
                              draftEntriesByDestination
                            }
                            onChange={(next) => {
                              const round = roundById.get(next.roundId);
                              updateIntent({
                                ...next,
                                roundName: round?.name ?? next.roundName,
                                expandedPathCount:
                                  round?.fixtures.length ??
                                  next.expandedPathCount,
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

function ProgIntentEditor({
  draft,
  data,
  sourceStage,
  peerStages,
  rounds,
  playableRounds,
  championshipTerminal,
  roundsLoading,
  draftEntriesByDestination,
  onChange,
}: {
  draft: ProgIntentDraft;
  data: StructureView;
  sourceStage: StructureStageHubSummary;
  peerStages: StructureStageHubSummary[];
  rounds: ProgRoundOption[];
  playableRounds: ProgRoundOption[];
  championshipTerminal: ProgRoundOption | null;
  roundsLoading: boolean;
  draftEntriesByDestination: Map<string, number>;
  onChange: (next: ProgIntentDraft) => void;
}) {
  const { t } = useTranslation('structure');
  const { t: tCommon } = useTranslation('common');
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
  /** UX Place grain from dest schematic (Groups A1 vs Cup slots). */
  const placeGrain = formOnly
    ? 'form'
    : (labeledPlaces[0]?.grain ??
      placeGrainForFormat(destSchematicQuery.data?.formatKind) ??
      placeGrainForFormat(destFormat) ??
      'slot');

  function setDestination(peer: StructureStageHubSummary) {
    onChange(applyDestinationToDraft(draft, peer.stageId, peer.formatKind));
  }

  function setTargetKind(kind: ProgTargetKind) {
    if (formOnly) return;
    onChange(applyTargetKindToDraft(draft, kind));
  }

  const placeUnavailable =
    showKindChoice &&
    draft.targetKind === 'place' &&
    !placesLabeled &&
    !destSchematicQuery.isLoading;

  const selectedRound = useMemo(
    () => rounds.find((r) => r.id === draft.roundId) ?? null,
    [draft.roundId, rounds],
  );
  const roundFixtures = selectedRound?.fixtures ?? [];
  const placeSlotKeys = useMemo(() => {
    const source =
      placeGrain === 'group'
        ? draft.destinationGroupIds
        : draft.destinationSlotKeys;
    return resizeDestinationSlotKeys(source, roundFixtures.length);
  }, [
    draft.destinationGroupIds,
    draft.destinationSlotKeys,
    placeGrain,
    roundFixtures.length,
  ]);

  function applyPlaceKeys(next: string[]) {
    const aligned = resizeDestinationSlotKeys(next, roundFixtures.length);
    onChange({
      ...draft,
      targetKind: 'place',
      destinationForm: false,
      destinationSlotKeys: placeGrain === 'slot' ? aligned : [],
      destinationGroupIds: placeGrain === 'group' ? aligned : [],
    });
  }

  const showRoundField = rounds.length > 1;
  const roundSelectDisabled =
    roundsLoading ||
    draft.outcome === 'Winner' ||
    playableRounds.length === 0;

  const roundSelectOptions = useMemo(() => {
    const source =
      playableRounds.length > 0 ? playableRounds : rounds;
    const options = [
      { value: '', label: t('progression.chooseRound') },
      ...source.map((o) => ({
        value: o.id,
        label:
          o.fixtures.length > 0
            ? t('progression.roundOption', {
                name: o.name,
                count: o.fixtures.length,
              })
            : t('progression.roundOptionEmpty', { name: o.name }),
      })),
    ];
    if (draft.roundId && !source.some((o) => o.id === draft.roundId)) {
      options.push({
        value: draft.roundId,
        label:
          draft.roundName ||
          t('progression.roundFallback', {
            id: draft.roundId.slice(0, 8),
          }),
      });
    }
    return options;
  }, [draft.roundId, draft.roundName, playableRounds, rounds, t]);

  function applyOutcome(outcome: ProgressionOutcome) {
    if (outcome === 'Winner') {
      if (!championshipTerminal) {
        onChange({ ...draft, outcome });
        return;
      }
      onChange({
        ...draft,
        outcome,
        roundId: championshipTerminal.id,
        roundName: championshipTerminal.name,
        expandedPathCount: championshipTerminal.fixtures.length,
      });
      return;
    }
    onChange({ ...draft, outcome });
  }

  function destinationTileDescription(dest: StructureStageHubSummary) {
    const draftTotal = draftEntriesByDestination.get(dest.stageId) ?? 0;
    const draftThisIntent =
      draft.destinationStageId.trim() === dest.stageId
        ? expandContribution(draft)
        : 0;
    const capacity = dest.compositionCapacity;
    const expected =
      capacity != null && capacity > 0
        ? expectedPopulationWithProgDraft({
            data,
            destination: dest,
            sourceStageId: sourceStage.stageId,
            draftProgVolume: draftTotal,
          })
        : null;
    if (expected != null && capacity != null && capacity > 0) {
      return (
        <DestinationDraftMeter
          count={expected}
          capacity={capacity}
          draft={draftThisIntent}
          label={t('progression.tileContribution', {
            count: expected,
            capacity,
          })}
          draftLabel={t('progression.tileContributionDraft', {
            count: draftThisIntent,
          })}
          ariaLabel={t('progression.tileContributionAria', {
            phase: dest.name,
            count: expected,
            capacity,
            draft: draftThisIntent,
          })}
        />
      );
    }
    if (draftThisIntent > 0) {
      return t('progression.tileContributionEntries', {
        count: draftThisIntent,
      });
    }
    return undefined;
  }

  return (
    <div className="structure-qualification__editor">
      <SortiesWhoWhereFlow
        sourceLabel={t('progression.who')}
        destinationLabel={t('progression.where')}
        feedsLabel={t('progression.feeds')}
        source={
          <>
      <div
        className="structure-qualification__scope-tiles"
        data-count="2"
        role="radiogroup"
        aria-label={t('progression.outcome')}
      >
        <ChoiceTile
          label={t('progression.outcomeWinner')}
          description={t('progression.outcomeWinnerHint')}
          leading={<TrophyIcon size="sm" />}
          selected={draft.outcome === 'Winner'}
          onChange={(selected) => {
            if (!selected) return;
            applyOutcome('Winner');
          }}
        />
        <ChoiceTile
          label={t('progression.outcomeLoser')}
          description={t('progression.outcomeLoserHint')}
          leading={<CupFormatIcon size="sm" />}
          selected={draft.outcome === 'Loser'}
          onChange={(selected) => {
            if (!selected) return;
            applyOutcome('Loser');
          }}
        />
      </div>

      {showRoundField ? (
        <>
          <Field label={t('progression.round')}>
            <Select
              options={roundSelectOptions}
              value={draft.roundId || null}
              placeholder={t('progression.chooseRound')}
              disabled={roundSelectDisabled}
              onChange={(value) => {
                if (draft.outcome === 'Winner') return;
                const id = value ?? '';
                const round =
                  playableRounds.find((r) => r.id === id) ??
                  rounds.find((r) => r.id === id);
                onChange({
                  ...draft,
                  roundId: id,
                  roundName: round?.name ?? '',
                  expandedPathCount: round?.fixtures.length ?? 0,
                });
              }}
            />
          </Field>
          {playableRounds.length === 0 ? (
            <p className="structure-qualification__field-hint" role="status">
              {t('progression.roundNeedsFixturesHint')}
            </p>
          ) : draft.outcome === 'Winner' ? (
            <p className="structure-qualification__field-hint" role="status">
              {t('progression.roundWinnerLockedHint')}
            </p>
          ) : null}
        </>
      ) : null}
          </>
        }
        destination={
          <>
      {peerStages.length === 0 ? (
        <p className="structure-qualification__field-hint" role="status">
          {t('progression.emptyNoPeerBody')}
        </p>
      ) : (
        <div
          className="structure-qualification__scope-tiles"
          data-count={String(Math.min(peerStages.length, 3))}
          role="radiogroup"
          aria-label={t('progression.destinationPhase')}
        >
          {peerStages.map((peer) => (
            <ChoiceTile
              key={peer.stageId}
              label={peer.name}
              description={destinationTileDescription(peer)}
              leading={stageFormatIcon(peer.formatKind)}
              selected={draft.destinationStageId === peer.stageId}
              onChange={(selected) => {
                if (!selected) return;
                setDestination(peer);
              }}
            />
          ))}
        </div>
      )}

      {showKindChoice && destStageId ? (
        <div
          className="structure-qualification__scope-tiles"
          data-count="2"
          role="radiogroup"
          aria-label={t('progression.destinationKind')}
        >
          <ChoiceTile
            label={t('progression.kindPopulation')}
            description={t('progression.kindPopulationHint')}
            leading={<StructureIcon size="sm" />}
            selected={draft.targetKind === 'population'}
            onChange={(selected) => {
              if (!selected) return;
              setTargetKind('population');
            }}
          />
          <ChoiceTile
            label={t('progression.kindPlace')}
            description={t('progression.kindPlaceHint')}
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
              length: Math.max(roundFixtures.length, 3),
            }).map((_, index) => (
              <li key={`skel-${index}`} aria-hidden="true">
                <span className="structure-qualification__place-map-skel-label" />
                <span className="structure-qualification__place-map-skel-control" />
              </li>
            ))}
          </ul>
        ) : placeUnavailable || !placesLabeled ? (
          <p className="structure-qualification__field-hint" role="status">
            {t('progression.emptyNoPlacePeerBody')}
          </p>
        ) : labeledPlaces.length === 0 ? (
          <p className="structure-qualification__field-hint" role="status">
            {t('progression.placeEmpty')}
          </p>
        ) : roundFixtures.length === 0 ? (
          <p className="structure-qualification__field-hint" role="status">
            {t('progression.placeMapEmptySelection')}
          </p>
        ) : (
          <div className="structure-qualification__place-map-block">
            <div className="structure-qualification__place-map-toolbar">
              {peerStages.length > 0 ? (
                <p className="structure-qualification__place-map-heading">
                  {t('progression.placeMapHeading')}
                </p>
              ) : null}
              <span className="structure-qualification__place-fill">
                <span
                  id={placeFillHintId}
                  className="ds-visually-hidden"
                >
                  {t('progression.placeFillHint')}
                </span>
                <Tooltip content={t('progression.placeFillHint')}>
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
                          ? fillEmptyPlaceKeysAllowingReuse(
                              placeSlotKeys,
                              ids,
                            )
                          : fillEmptyPlaceSlotKeys(placeSlotKeys, ids);
                      applyPlaceKeys(next);
                    }}
                  >
                    <LucideIcon icon={ListPlus} size="sm" />
                    {t('progression.placeFillEmpties')}
                  </button>
                </Tooltip>
              </span>
            </div>
            <ul
              className="structure-qualification__place-map"
              aria-label={t('progression.placeMapAria')}
            >
              {roundFixtures.map((fixture, index) => {
                const selected = placeSlotKeys[index]?.trim() || null;
                const rowId = `prog-place-${draft.id}-${index}`;
                const rowInvalid = !selected;
                const sourceLabel = fixturePlaceMapLabel(fixture, index, t);
                return (
                  <li
                    key={fixture.id}
                    data-invalid={rowInvalid ? 'true' : 'false'}
                  >
                    <label
                      className="structure-qualification__place-map-label"
                      htmlFor={rowId}
                    >
                      {sourceLabel}
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
                        placeholder={t('progression.placeSlotPlaceholder')}
                        allowClear
                        aria-label={t('progression.placeMapRowAria', {
                          source: sourceLabel,
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

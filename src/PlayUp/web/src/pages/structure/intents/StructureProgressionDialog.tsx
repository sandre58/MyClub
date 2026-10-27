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
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  fetchStageOverview,
  fetchStageSchematic,
  replaceStageProgressionRules,
} from '../../../api';
import { Alert } from '../../../design-system/components/Alert';
import { ConfirmDialog } from '../../../design-system/components/ConfirmDialog';
import { Dialog } from '../../../design-system/components/Dialog';
import { Tooltip } from '../../../design-system/components/Tooltip';
import { ToastToneIcon } from '../../../design-system/icons/toastIcons';
import {
  CheckIcon,
  CupFormatIcon,
  EmptySelectionIcon,
  PlusIcon,
  StructureIcon,
  StructureIssueIcon,
  TrashIcon,
  TrophyIcon,
} from '../../../design-system/icons/contentIcons';
import {
  ChevronDownIcon,
  CloseIcon,
} from '../../../design-system/icons/shellIcons';
import { notify } from '../../../design-system/toastStore';
import { useClearMutationsOnOpen } from '../../../design-system/useClearMutationsOnOpen';
import { useOpenEdge, useSyncWhen } from '../../../design-system/useRenderSync';
import { useDiscardConfirm } from '../../../design-system/useDiscardConfirm';
import { queryKeys } from '../../../queryKeys';
import type {
  StageBracketPair,
  StageSchematic,
  StructureStageHubSummary,
  StructureView,
} from '../../../types';
import {
  EmptyState,
  LoadingState,
  MutationError,
  PendingLabel,
} from '../../../ui';
import { persistentStructureMutationSotHref } from '../../../persistentStructureMutationSot';
import {
  StructureProgressionIntentEditor,
  type ProgRoundOption,
} from './StructureProgressionIntentEditor';
import { scheduleStructureRefresh } from '../structureInvalidation';
import { sortiesAvalPeerStages } from './structureSortiesDestinations';
import { isMissingDestinationStage } from './structureSortiesDestination';
import { structureIssuePresentation } from '../../../shell/functionalProblemPresentation';
import { findStructureIssueProblem } from '../../../shell/organizerProblemRead';
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
  placeMapKeys,
  serializeIntents,
  summarizeIntentWho,
  syncPlaceSlotKeys,
  toApiIntent,
  applyDestinationToDraft,
  isFormOnlyDestination,
  normalizeFormOnlyDestinationDraft,
  type ProgIntentDraft,
} from './structureProgressionDraft';
import { areProgressionPlacesLabeled } from '../draw/structurePlaceLabel';
import { expectedPopulationWithProgDraft } from '../composition/structurePopulationVolume';
import { resolveDestinationMeterCapacity } from '../composition/structurePlaces';

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
    onSuccess: () => {
      onClose();
      notify.success(t('progression.toastUpdated'));
      scheduleStructureRefresh(queryClient, data.competitionId, ['graph']);
    },
  });

  useOpenEdge(open, { onOpen: () => setSessionReady(false) });

  useClearMutationsOnOpen(open, mutation);

  const progressionHydrateKey = [
    stage.stageId,
    sourceSchematicQuery.isLoading ? 'loading' : 'ready',
    mutation.isPending ? 'p' : '',
    mutation.isSuccess ? 's' : '',
    JSON.stringify(stage.progressionIntents ?? []),
  ].join('|');

  useSyncWhen(
    open &&
      !sourceSchematicQuery.isLoading &&
      !mutation.isPending &&
      !mutation.isSuccess,
    progressionHydrateKey,
    () => {
      const fromIntents = stage.progressionIntents ?? [];
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
    },
  );

  const pathTotal = useMemo(
    () => intents.reduce((sum, intent) => sum + expandContribution(intent), 0),
    [intents],
  );

  const missingDestinationCount = useMemo(
    () =>
      intents.filter((intent) =>
        isMissingDestinationStage(intent.destinationStageId, stageById),
      ).length,
    [intents, stageById],
  );

  const hostDanglingEcho = useMemo(() => {
    const codes = (stage.structureIssues ?? []).filter(
      (code) =>
        code === 'DanglingProgressionTarget' ||
        code === 'MissingProgressionDestinationSlot' ||
        code === 'MissingProgressionDestinationGroup',
    );
    return [...new Set(codes)].map((code) =>
      t(
        structureIssuePresentation({
          code,
          competitionId: data.competitionId,
          stageId: stage.stageId,
          problem: findStructureIssueProblem(stage, code),
        }).titleKey,
        { defaultValue: code },
      ),
    );
  }, [data.competitionId, stage, t]);

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
      const capacity = resolveDestinationMeterCapacity(dest);
      if (capacity == null) continue;
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
                  structureStages={data.stages}
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

          {sessionReady && missingDestinationCount > 0 ? (
            <Alert tone="danger" role="status">
              <p className="structure-qualification__hint-line">
                {t('progression.missingDestinationsAlert', {
                  count: missingDestinationCount,
                })}
              </p>
              {hostDanglingEcho.length > 0 ? (
                <p className="structure-qualification__hint-line structure-qualification__hint-echo">
                  {hostDanglingEcho.join(' · ')}
                </p>
              ) : null}
            </Alert>
          ) : null}

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
                const destId = intent.destinationStageId.trim();
                const missingDest = isMissingDestinationStage(
                  destId,
                  stageById,
                );
                const destName = missingDest
                  ? t('progression.missingDestinationPhase')
                  : (stageNameById.get(destId) ?? destId);
                const filledSlots = (
                  placeMapKeys(intent)?.keys ?? intent.destinationSlotKeys
                ).filter((k) => k.trim());
                const expandN =
                  expandCount(intent) || expandContribution(intent);
                // Form-only Champ/Swiss: no slot map — avoid filled/count "0/N places".
                const where =
                  intent.targetKind === 'population' && destId
                    ? t('progression.summary.whereCount', {
                        phase: destName,
                        count: expandN,
                      })
                    : intent.targetKind === 'place' &&
                        intent.destinationForm &&
                        destId
                      ? expandN > 0
                        ? t('progression.summary.whereCount', {
                            phase: destName,
                            count: expandN,
                          })
                        : `→ ${destName}`
                      : intent.targetKind === 'place' && destId
                        ? t('progression.summary.wherePlaceMapped', {
                            phase: destName,
                            filled: filledSlots.length,
                            count: expandCount(intent),
                          })
                        : intent.targetKind === 'place'
                          ? t('progression.summary.wherePlaceFallback')
                          : missingDest
                            ? `→ ${destName}`
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
                      className={[
                        'structure-qualification__tile',
                        'ds-selectable-tile',
                        missingDest
                          ? 'structure-qualification__tile--missing-dest'
                          : null,
                      ]
                        .filter(Boolean)
                        .join(' ')}
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
                              {missingDest ? (
                                <Tooltip
                                  content={t(
                                    'progression.missingDestinationTooltip',
                                    { id: destId },
                                  )}
                                >
                                  <span
                                    className="structure-qualification__blocking-mark"
                                    aria-hidden="true"
                                  >
                                    <StructureIssueIcon size="sm" />
                                  </span>
                                </Tooltip>
                              ) : incompleteReason != null && statusMessage ? (
                                <Tooltip content={statusMessage}>
                                  <span
                                    className="structure-qualification__blocking-mark"
                                    aria-hidden="true"
                                  >
                                    <ToastToneIcon tone="attention" size="sm" />
                                  </span>
                                </Tooltip>
                              ) : null}
                              <span className="structure-qualification__who">
                                {who}
                              </span>
                            </span>
                            {where ? (
                              <span
                                className={[
                                  'structure-qualification__where',
                                  missingDest
                                    ? 'structure-qualification__where--missing'
                                    : null,
                                ]
                                  .filter(Boolean)
                                  .join(' ')}
                              >
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

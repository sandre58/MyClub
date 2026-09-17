// -----------------------------------------------------------------------
// Progression dialog — Intent Round × Outcome → Destination (Prog V3).
// -----------------------------------------------------------------------

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Trash2 } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  fetchStageOverview,
  fetchStageSchematic,
  replaceStageProgressionRules,
} from '../api';
import { Alert } from '../design-system/components/Alert';
import { ChoiceTile } from '../design-system/components/ChoiceTile';
import { Dialog } from '../design-system/components/Dialog';
import { Field } from '../design-system/components/Field';
import { Select } from '../design-system/components/Select';
import { LucideIcon } from '../design-system/icons/Icon';
import {
  ChampionshipFormatIcon,
  CupFormatIcon,
  EmptySelectionIcon,
  GroupsFormatIcon,
  OverviewAttentionIcon,
  PlusIcon,
  StructureIcon,
  SwissFormatIcon,
  TrophyIcon,
} from '../design-system/icons/contentIcons';
import { ChevronDownIcon } from '../design-system/icons/shellIcons';
import { notify } from '../design-system/toastStore';
import { queryKeys } from '../queryKeys';
import type {
  ProgressionOutcome,
  StructureFormatKind,
  StructureStageHubSummary,
  StructureView,
} from '../types';
import { EmptyState, MutationError, PendingLabel } from '../ui';
import { invalidateAfterStructureMutation } from './structureInvalidation';
import {
  areProgressionPlacesLabeled,
  emptyProgIntent,
  expandContribution,
  incompleteIntentReason,
  intentFromApi,
  isIntentComplete,
  pathToSingletonIntent,
  serializeIntents,
  summarizeIntentWho,
  toApiIntent,
  type ProgIntentDraft,
  type ProgTargetKind,
} from './structureProgressionDraft';
import {
  listLabeledCupPlaces,
  placeLabelForDestinationSlotKey,
} from './structurePlaceLabel';

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
  rounds: { id: string; name: string; fixtures: { id: string }[] }[],
  fixtureId: string,
): { roundId: string; roundName: string } | null {
  for (const round of rounds) {
    if (round.fixtures.some((f) => f.id === fixtureId)) {
      return { roundId: round.id, roundName: round.name };
    }
  }
  return null;
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
  const queryClient = useQueryClient();

  const overviewQuery = useQuery({
    queryKey: queryKeys.stages.detail(stage.stageId),
    queryFn: () => fetchStageOverview(stage.stageId),
    enabled: open,
  });
  const schematicQuery = useQuery({
    queryKey: queryKeys.stages.schematic(stage.stageId),
    queryFn: () => fetchStageSchematic(stage.stageId),
    enabled: open,
  });
  const placesLabeled = areProgressionPlacesLabeled(schematicQuery.data);
  const labeledPlaces = useMemo(
    () => listLabeledCupPlaces(schematicQuery.data, t),
    [schematicQuery.data, t],
  );
  const placeLabelByIdentity = useMemo(() => {
    const map = new Map<string, string>();
    for (const place of labeledPlaces) {
      map.set(place.apiIdentity, place.label);
    }
    return map;
  }, [labeledPlaces]);

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

  const [intents, setIntents] = useState<ProgIntentDraft[]>([]);
  const [baselineSerialized, setBaselineSerialized] = useState('');
  const [expandedId, setExpandedId] = useState<string | null>(null);

  const rounds = overviewQuery.data?.rounds ?? [];
  const roundOptions = useMemo(
    () =>
      rounds.map((r) => ({
        id: r.id,
        name: r.name,
        fixtureCount: r.fixtures.length,
      })),
    [rounds],
  );
  const roundById = useMemo(
    () => new Map(roundOptions.map((r) => [r.id, r])),
    [roundOptions],
  );

  useEffect(() => {
    if (!open) {
      setIntents([]);
      setBaselineSerialized('');
      setExpandedId(null);
      return;
    }

    const fromIntents = stage.progressionIntents ?? [];
    if (fromIntents.length > 0) {
      const next = fromIntents.map((intent) =>
        intentFromApi(intent, stage.stageId),
      );
      setIntents(next);
      setBaselineSerialized(serializeIntents(next));
      setExpandedId(null);
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
      return;
    }

    const next: ProgIntentDraft[] = [];
    let order = 1;
    for (const path of existingPaths) {
      const resolved = resolveRoundIdForFixture(rounds, path.sourceFixtureId);
      if (!resolved) continue;
      const existing = next.find(
        (intent) =>
          intent.roundId === resolved.roundId &&
          intent.outcome === path.outcome &&
          intent.destinationStageId === path.destinationStageId &&
          (intent.destinationSlotKey || null) ===
            (path.destinationSlotKey ?? null),
      );
      if (existing) {
        existing.expandedPathCount += 1;
        continue;
      }
      next.push(
        pathToSingletonIntent(
          path,
          stage.stageId,
          resolved.roundId,
          resolved.roundName,
          order++,
        ),
      );
    }
    setIntents(next);
    setBaselineSerialized(serializeIntents(next));
    setExpandedId(null);
  }, [
    open,
    stage.stageId,
    stage.progressionIntents,
    stage.progressionPaths,
    overviewQuery.isLoading,
    rounds,
  ]);

  const populationCount = useMemo(
    () => intents.filter((p) => p.targetKind === 'population').length,
    [intents],
  );
  const placeCount = useMemo(
    () => intents.filter((p) => p.targetKind === 'place').length,
    [intents],
  );

  const draftEntriesByDestination = useMemo(() => {
    const map = new Map<string, number>();
    for (const intent of intents) {
      if (intent.targetKind !== 'population') continue;
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

  const canAuthor = peerStages.length > 0 || placesLabeled;

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

  const dirty =
    mutation.isPending || serializeIntents(intents) !== baselineSerialized;

  const canSave =
    !mutation.isPending &&
    intents.every((intent) => isIntentComplete(intent, intents, placesLabeled));

  function toggleRow(intent: ProgIntentDraft) {
    setExpandedId((prev) => (prev === intent.id ? null : intent.id));
  }

  function addIntent() {
    const next = emptyProgIntent(
      peerStages.length > 0 ? defaultDest : stage.stageId,
      peerStages.length > 0 ? 'population' : 'place',
      intents.length + 1,
    );
    setIntents((prev) => [...prev, next]);
    setExpandedId(next.id);
  }

  function removeIntent(id: string) {
    setIntents((prev) => prev.filter((p) => p.id !== id));
    setExpandedId((prev) => (prev === id ? null : prev));
  }

  function updateIntent(next: ProgIntentDraft) {
    setIntents((prev) => prev.map((p) => (p.id === next.id ? next : p)));
  }

  const contextDestinationName = contextDestinationId
    ? (stageNameById.get(contextDestinationId) ?? contextDestinationId)
    : null;

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={t('progression.title', { phase: stage.name })}
      description={
        contextDestinationName
          ? t('progression.hintFromDestination', { source: stage.name })
          : t('progression.hint')
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
                    {t('progression.overCapacityWarning', {
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
            <div className="structure-qualification__fact structure-qualification__fact--primary">
              <span className="structure-qualification__fact-value">
                {intents.length}
              </span>
              <span className="structure-qualification__fact-label">
                {t('progression.factRules', { count: intents.length })}
              </span>
            </div>
            {placesLabeled || placeCount > 0 ? (
              <>
                <span
                  className="structure-qualification__fact-rule"
                  aria-hidden="true"
                />
                <div className="structure-qualification__fact structure-qualification__fact--secondary">
                  <span className="structure-qualification__fact-value">
                    {populationCount}
                  </span>
                  <span className="structure-qualification__fact-label">
                    {t('progression.factPopulation', {
                      count: populationCount,
                    })}
                  </span>
                </div>
                <span
                  className="structure-qualification__fact-rule"
                  aria-hidden="true"
                />
                <div className="structure-qualification__fact structure-qualification__fact--secondary">
                  <span className="structure-qualification__fact-value">
                    {placeCount}
                  </span>
                  <span className="structure-qualification__fact-label">
                    {t('progression.factPlace', { count: placeCount })}
                  </span>
                </div>
              </>
            ) : null}
          </div>
          <button
            type="button"
            className="ds-btn ds-btn--primary"
            disabled={!canAuthor || mutation.isPending}
            onClick={addIntent}
          >
            <PlusIcon size="sm" />
            <span>{t('progression.add')}</span>
          </button>
        </div>

        {!canAuthor ? (
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
                intent.targetKind === 'place'
                  ? stage.name
                  : (stageNameById.get(intent.destinationStageId) ??
                    intent.destinationStageId);
              const where =
                intent.targetKind === 'population' &&
                intent.destinationStageId.trim()
                  ? t('progression.summary.wherePopulation', {
                      phase: destName,
                    })
                  : intent.targetKind === 'place'
                    ? (() => {
                        const placeLabel =
                          placeLabelByIdentity.get(
                            intent.destinationSlotKey.trim(),
                          ) ??
                          placeLabelForDestinationSlotKey(
                            schematicQuery.data,
                            intent.destinationSlotKey,
                            t,
                          );
                        return placeLabel
                          ? t('progression.summary.wherePlace', {
                              place: placeLabel,
                            })
                          : t('progression.summary.wherePlaceFallback');
                      })()
                    : null;
              const incompleteReason = incompleteIntentReason(
                intent,
                intents,
                placesLabeled,
              );
              const statusMessage =
                incompleteReason == null
                  ? null
                  : t(`progression.incompleteHint${incompleteReason}`);
              const expandHint =
                intent.expandedPathCount > 0
                  ? t('progression.expandPreview', {
                      count: intent.expandedPathCount,
                    })
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
                          {intent.outcome === 'Winner' ? (
                            <TrophyIcon size="lg" />
                          ) : (
                            <CupFormatIcon size="lg" />
                          )}
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
                          {expandHint && !isExpanded ? (
                            <span className="structure-qualification__where">
                              {expandHint}
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
                          sourceStage={stage}
                          peerStages={peerStages}
                          placesLabeled={placesLabeled}
                          labeledPlaces={labeledPlaces}
                          roundOptions={roundOptions}
                          roundsLoading={overviewQuery.isLoading}
                          draftEntriesByDestination={draftEntriesByDestination}
                          onChange={(next) => {
                            const round = roundById.get(next.roundId);
                            updateIntent({
                              ...next,
                              roundName: round?.name ?? next.roundName,
                              expandedPathCount:
                                round?.fixtureCount ?? next.expandedPathCount,
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
  );
}

function ProgIntentEditor({
  draft,
  sourceStage,
  peerStages,
  placesLabeled,
  labeledPlaces,
  roundOptions,
  roundsLoading,
  draftEntriesByDestination,
  onChange,
}: {
  draft: ProgIntentDraft;
  sourceStage: StructureStageHubSummary;
  peerStages: StructureStageHubSummary[];
  placesLabeled: boolean;
  labeledPlaces: { apiIdentity: string; label: string }[];
  roundOptions: { id: string; name: string; fixtureCount: number }[];
  roundsLoading: boolean;
  draftEntriesByDestination: Map<string, number>;
  onChange: (next: ProgIntentDraft) => void;
}) {
  const { t } = useTranslation('structure');

  const roundSelectOptions = useMemo(() => {
    const options = [
      { value: '', label: t('progression.chooseRound') },
      ...roundOptions.map((o) => ({
        value: o.id,
        label:
          o.fixtureCount > 0
            ? t('progression.roundOption', {
                name: o.name,
                count: o.fixtureCount,
              })
            : o.name,
      })),
    ];
    if (
      draft.roundId &&
      !roundOptions.some((o) => o.id === draft.roundId)
    ) {
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
  }, [draft.roundId, draft.roundName, roundOptions, t]);

  function setTargetKind(kind: ProgTargetKind) {
    if (kind === 'place' && !placesLabeled) {
      return;
    }
    if (kind === 'population') {
      onChange({
        ...draft,
        targetKind: 'population',
        destinationStageId:
          draft.targetKind === 'population' &&
          peerStages.some((p) => p.stageId === draft.destinationStageId)
            ? draft.destinationStageId
            : (peerStages[0]?.stageId ?? ''),
        destinationSlotKey: '',
      });
      return;
    }
    onChange({
      ...draft,
      targetKind: 'place',
      destinationStageId: sourceStage.stageId,
      destinationSlotKey: draft.destinationSlotKey,
    });
  }

  return (
    <div className="structure-qualification__editor">
      <p className="structure-qualification__section-title">
        {t('progression.source')}
      </p>

      <Field label={t('progression.round')}>
        <Select
          options={roundSelectOptions}
          value={draft.roundId || null}
          placeholder={t('progression.chooseRound')}
          disabled={roundsLoading}
          onChange={(value) => {
            const id = value ?? '';
            const round = roundOptions.find((r) => r.id === id);
            onChange({
              ...draft,
              roundId: id,
              roundName: round?.name ?? '',
              expandedPathCount: round?.fixtureCount ?? 0,
            });
          }}
        />
      </Field>

      {draft.expandedPathCount > 0 ? (
        <p className="structure-qualification__field-hint" role="status">
          {t('progression.expandPreview', {
            count: draft.expandedPathCount,
          })}
        </p>
      ) : null}

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
            onChange({ ...draft, outcome: 'Winner' as ProgressionOutcome });
          }}
        />
        <ChoiceTile
          label={t('progression.outcomeLoser')}
          description={t('progression.outcomeLoserHint')}
          leading={<CupFormatIcon size="sm" />}
          selected={draft.outcome === 'Loser'}
          onChange={(selected) => {
            if (!selected) return;
            onChange({ ...draft, outcome: 'Loser' as ProgressionOutcome });
          }}
        />
      </div>

      <hr className="structure-qualification__rule" />

      <p className="structure-qualification__section-title">
        {t('progression.destination')}
      </p>

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
          disabled={peerStages.length === 0}
          onChange={(selected) => {
            if (!selected) return;
            setTargetKind('population');
          }}
        />
        <ChoiceTile
          label={t('progression.kindPlace')}
          description={
            placesLabeled
              ? t('progression.kindPlaceHint')
              : t('progression.kindPlaceUnavailable')
          }
          leading={<CupFormatIcon size="sm" />}
          selected={draft.targetKind === 'place'}
          disabled={!placesLabeled}
          onChange={(selected) => {
            if (!selected) return;
            setTargetKind('place');
          }}
        />
      </div>

      {draft.targetKind === 'place' && !placesLabeled ? (
        <p className="structure-qualification__field-hint" role="status">
          {t('progression.placeGatedBody')}
        </p>
      ) : null}

      {draft.targetKind === 'population' ? (
        peerStages.length === 0 ? (
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
            {peerStages.map((peer) => {
              const contribution =
                draftEntriesByDestination.get(peer.stageId) ?? 0;
              const capacity = peer.compositionCapacity;
              const description =
                capacity != null && capacity > 0
                  ? t('progression.tileContribution', {
                      count: contribution,
                      capacity,
                    })
                  : contribution > 0
                    ? t('progression.tileContributionEntries', {
                        count: contribution,
                      })
                    : undefined;
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
                      destinationSlotKey: '',
                    });
                  }}
                />
              );
            })}
          </div>
        )
      ) : placesLabeled ? (
        labeledPlaces.length === 0 ? (
          <p className="structure-qualification__field-hint" role="status">
            {t('progression.placeEmpty')}
          </p>
        ) : (
          <div
            className="structure-qualification__scope-tiles"
            data-count={String(Math.min(labeledPlaces.length, 3))}
            role="radiogroup"
            aria-label={t('progression.placeSelectPending')}
          >
            {labeledPlaces.map((place) => (
              <ChoiceTile
                key={place.apiIdentity}
                label={place.label}
                leading={<CupFormatIcon size="sm" />}
                selected={draft.destinationSlotKey === place.apiIdentity}
                onChange={(selected) => {
                  if (!selected) return;
                  onChange({
                    ...draft,
                    targetKind: 'place',
                    destinationStageId: sourceStage.stageId,
                    destinationSlotKey: place.apiIdentity,
                  });
                }}
              />
            ))}
          </div>
        )
      ) : null}
    </div>
  );
}

// -----------------------------------------------------------------------
// Progression dialog — atomic ProgressionPath authoring (chassis Qual / V2).
// -----------------------------------------------------------------------

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Trash2 } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  fetchStageOverview,
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
import { listFixtureOptions } from './structureFixtureLabels';
import {
  areProgressionPlacesLabeled,
  emptyProgPath,
  incompletePathReason,
  isPathComplete,
  pathFromApi,
  serializePaths,
  summarizePathWho,
  toApiPath,
  type ProgPathDraft,
  type ProgTargetKind,
} from './structureProgressionDraft';

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
  const placesLabeled = areProgressionPlacesLabeled();

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

  const [paths, setPaths] = useState<ProgPathDraft[]>([]);
  const [baselineSerialized, setBaselineSerialized] = useState('');
  const [expandedId, setExpandedId] = useState<string | null>(null);

  const overviewQuery = useQuery({
    queryKey: queryKeys.stages.detail(stage.stageId),
    queryFn: () => fetchStageOverview(stage.stageId),
    enabled: open,
  });
  const fixtureOptions = useMemo(
    () => listFixtureOptions(overviewQuery.data?.rounds ?? []),
    [overviewQuery.data?.rounds],
  );
  const fixtureLabelById = useMemo(
    () => new Map(fixtureOptions.map((o) => [o.id, o.label])),
    [fixtureOptions],
  );

  useEffect(() => {
    if (!open) {
      setPaths([]);
      setBaselineSerialized('');
      setExpandedId(null);
      return;
    }
    const existing = stage.progressionPaths ?? [];
    const next =
      existing.length > 0
        ? existing.map((path) => pathFromApi(path, stage.stageId))
        : [];
    setPaths(next);
    setBaselineSerialized(serializePaths(next));
    setExpandedId(null);
  }, [open, stage.stageId, stage.progressionPaths]);

  const populationCount = useMemo(
    () => paths.filter((p) => p.targetKind === 'population').length,
    [paths],
  );
  const placeCount = useMemo(
    () => paths.filter((p) => p.targetKind === 'place').length,
    [paths],
  );

  const draftEntriesByDestination = useMemo(() => {
    const map = new Map<string, number>();
    for (const path of paths) {
      if (path.targetKind !== 'population') continue;
      const destId = path.destinationStageId.trim();
      if (!destId) continue;
      map.set(destId, (map.get(destId) ?? 0) + 1);
    }
    return map;
  }, [paths]);

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

  const canAuthor =
    peerStages.length > 0 || placesLabeled;

  const mutation = useMutation({
    mutationFn: () => {
      if (paths.length === 0) {
        return replaceStageProgressionRules(stage.stageId, { paths: null });
      }
      return replaceStageProgressionRules(stage.stageId, {
        paths: paths.map(toApiPath),
      });
    },
    onSuccess: async () => {
      await invalidateAfterStructureMutation(queryClient, data.competitionId);
      notify.success(t('progression.toastUpdated'));
      onClose();
    },
  });

  const dirty =
    mutation.isPending || serializePaths(paths) !== baselineSerialized;

  const canSave =
    !mutation.isPending &&
    paths.every((path) => isPathComplete(path, paths, placesLabeled));

  function toggleRow(path: ProgPathDraft) {
    setExpandedId((prev) => (prev === path.id ? null : path.id));
  }

  function addPath() {
    const next = emptyProgPath(
      peerStages.length > 0 ? defaultDest : stage.stageId,
      peerStages.length > 0 ? 'population' : 'place',
    );
    setPaths((prev) => [...prev, next]);
    setExpandedId(next.id);
  }

  function removePath(id: string) {
    setPaths((prev) => prev.filter((p) => p.id !== id));
    setExpandedId((prev) => (prev === id ? null : prev));
  }

  function updatePath(next: ProgPathDraft) {
    setPaths((prev) => prev.map((p) => (p.id === next.id ? next : p)));
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
                {paths.length}
              </span>
              <span className="structure-qualification__fact-label">
                {t('progression.factRules', { count: paths.length })}
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
            onClick={addPath}
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
        ) : paths.length === 0 ? (
          <EmptyState
            variant="idle"
            icon={<EmptySelectionIcon size="lg" />}
            title={t('progression.emptyTitle')}
          >
            {t('progression.emptyBody')}
          </EmptyState>
        ) : (
          <ul className="structure-qualification__list">
            {paths.map((path) => {
              const isExpanded = expandedId === path.id;
              const who =
                summarizePathWho(path, t) || t('progression.newPath');
              const destName =
                path.targetKind === 'place'
                  ? stage.name
                  : (stageNameById.get(path.destinationStageId) ??
                    path.destinationStageId);
              const where =
                path.targetKind === 'population' &&
                path.destinationStageId.trim()
                  ? t('progression.summary.wherePopulation', {
                      phase: destName,
                    })
                  : path.targetKind === 'place'
                    ? t('progression.summary.wherePlace')
                    : null;
              const incompleteReason = incompletePathReason(
                path,
                paths,
                placesLabeled,
              );
              const statusMessage =
                incompleteReason == null
                  ? null
                  : t(`progression.incompleteHint${incompleteReason}`);

              return (
                <li key={path.id} className="structure-qualification__item">
                  <div
                    className="structure-qualification__card ds-selectable-tile"
                    data-selected={isExpanded ? 'true' : 'false'}
                  >
                    <div className="structure-qualification__hit">
                      <button
                        type="button"
                        className="structure-qualification__row"
                        onClick={() => toggleRow(path)}
                        aria-expanded={isExpanded}
                      >
                        <span
                          className="structure-qualification__scope-icon"
                          aria-hidden="true"
                        >
                          {path.outcome === 'Winner' ? (
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
                          removePath(path.id);
                        }}
                      >
                        <LucideIcon icon={Trash2} size="sm" />
                      </button>
                    </div>

                    {isExpanded ? (
                      <div className="structure-qualification__panel">
                        <ProgPathEditor
                          draft={path}
                          sourceStage={stage}
                          peerStages={peerStages}
                          placesLabeled={placesLabeled}
                          fixtureOptions={fixtureOptions}
                          fixtureLabelById={fixtureLabelById}
                          fixturesLoading={overviewQuery.isLoading}
                          draftEntriesByDestination={draftEntriesByDestination}
                          onChange={updatePath}
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

function ProgPathEditor({
  draft,
  sourceStage,
  peerStages,
  placesLabeled,
  fixtureOptions,
  fixtureLabelById,
  fixturesLoading,
  draftEntriesByDestination,
  onChange,
}: {
  draft: ProgPathDraft;
  sourceStage: StructureStageHubSummary;
  peerStages: StructureStageHubSummary[];
  placesLabeled: boolean;
  fixtureOptions: { id: string; label: string }[];
  fixtureLabelById: Map<string, string>;
  fixturesLoading: boolean;
  draftEntriesByDestination: Map<string, number>;
  onChange: (next: ProgPathDraft) => void;
}) {
  const { t } = useTranslation('structure');

  const fixtureSelectOptions = useMemo(() => {
    const options = [
      { value: '', label: t('graph.chooseFixture') },
      ...fixtureOptions.map((o) => ({ value: o.id, label: o.label })),
    ];
    if (
      draft.sourceFixtureId &&
      !fixtureOptions.some((o) => o.id === draft.sourceFixtureId)
    ) {
      options.push({
        value: draft.sourceFixtureId,
        label:
          draft.sourceLabel ||
          t('graph.unknownFixture', {
            id: draft.sourceFixtureId.slice(0, 8),
          }),
      });
    }
    return options;
  }, [
    draft.sourceFixtureId,
    draft.sourceLabel,
    fixtureOptions,
    t,
  ]);

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
        legacyInterPhasePlace: false,
      });
      return;
    }
    onChange({
      ...draft,
      targetKind: 'place',
      destinationStageId: sourceStage.stageId,
      destinationSlotKey: draft.destinationSlotKey,
      legacyInterPhasePlace: false,
    });
  }

  return (
    <div className="structure-qualification__editor">
      <p className="structure-qualification__section-title">
        {t('progression.source')}
      </p>

      <Field label={t('progression.match')}>
        <Select
          options={fixtureSelectOptions}
          value={draft.sourceFixtureId || null}
          placeholder={t('graph.chooseFixture')}
          disabled={fixturesLoading}
          onChange={(value) => {
            const id = value ?? '';
            onChange({
              ...draft,
              sourceFixtureId: id,
              sourceLabel: id ? (fixtureLabelById.get(id) ?? '') : '',
            });
          }}
        />
      </Field>

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

      {draft.legacyInterPhasePlace ? (
        <Alert tone="warning" role="status">
          {t('progression.legacyInterPhasePlace')}
        </Alert>
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
                      legacyInterPhasePlace: false,
                    });
                  }}
                />
              );
            })}
          </div>
        )
      ) : placesLabeled ? (
        <p className="structure-qualification__field-hint" role="status">
          {t('progression.placeSelectPending')}
        </p>
      ) : null}
    </div>
  );
}

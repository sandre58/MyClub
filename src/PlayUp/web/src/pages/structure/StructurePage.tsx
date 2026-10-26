import {
  type UseQueryResult,
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query';
import { type ReactNode, useMemo, useState } from 'react';
import { useSyncWhen } from '../../design-system/useRenderSync';
import { useTranslation } from 'react-i18next';
import { useParams, useSearchParams } from 'react-router-dom';
import {
  fetchNeedsAttention,
  fetchStructureView,
  setCompetitionStageOrder,
} from '../../api';
import { partitionPresentableAttention } from '../../shell/attentionPartition';
import { FormSection } from '../../design-system/components/FormSection';
import { SelectableTile } from '../../design-system/components/SelectableTile';
import { PageHead } from '../../design-system/components/PageHead';
import { Tooltip } from '../../design-system/components/Tooltip';
import {
  ChampionshipFormatIcon,
  CheckIcon,
  CupFormatIcon,
  GroupsFormatIcon,
  PlusIcon,
  StructureIcon,
  SwissFormatIcon,
  EmptySelectionIcon,
  InfoIcon,
} from '../../design-system/icons/contentIcons';
import { structurePageHeadHubStepLabelKey } from './structurePageHeadHubStepCopy';
import { structureFormatKindLabel } from '../../i18n/enumLabels';
import { queryKeys } from '../../queryKeys';
import {
  EmptyState,
  ErrorState,
  LoadingState,
  MutationError,
  StageStatusBadge,
} from '../../ui';
import {
  type StructureFormatKind,
  type StructureHubNextStep,
  type StageInsertPosition,
  type StructureStageHubSummary,
  type NeedsAttention,
  type StructureView,
} from '../../types';
import {
  type AttentionQueryPhase,
  isStructureHubOperationalSlicePending,
  resolveStructurePageHeadNoteKind,
} from './structurePageHeadReadiness';
import { AddPhaseDialog, RemovePhaseDialog } from './StructureGraphDialogs';
import {
  StructurePhaseInsertSlot,
  StructurePhaseMoveControls,
  StructurePhaseRemoveControl,
  StructureSinglePhaseBirth,
} from './structurePhaseOrderControls';
import { scheduleStructureRefresh } from './structureInvalidation';
import { StructureReadinessHint } from './StructureReadinessHint';
import { StructurePageHeadReadinessStatus } from './structurePageHeadReadinessStatus';
import { StructurePhaseDrawHint } from './StructurePhaseDrawHint';
import { StructurePhaseFiche } from './StructurePhaseFiche';
import { type StructureSectionId } from './structureHubSections';
import { resolveStructureHubStages } from './structureResolveStages';
import { outboundDestinationStageIds } from './structureStageQualProgProjection';
import { topologyCardFacts } from './structureTopologyFacts';
import {
  computeStructureReadinessSummary,
  type StructureReadinessSummary,
} from './structureReadinessSummary';
import { StructureDiagnosticIssueIcon } from './StructureDiagnosticIssueIcon';
import { stageWorstPresentedDiagnosticTone } from './structureStageTopologyPresentation';
import {
  parseStructureDeepLink,
  STRUCTURE_COMPOSE_PARAM,
  STRUCTURE_DRAW_PARAM,
  STRUCTURE_FOCUS_PARAM,
  STRUCTURE_MANUAL_PARAM,
  STRUCTURE_ROUND_PARAM,
  STRUCTURE_SECTION_PARAM,
  STRUCTURE_STAGE_PARAM,
  type StructureFocusId,
} from './structureNavigation';
import './structure.css';

/**
 * Structure hub — master-detail N1|N2 + graph mutations (dialogs).
 * Route `/structure`. Editing = dialogs only.
 */
export function StructurePage() {
  const { competitionId = '' } = useParams();

  const query = useQuery({
    queryKey: queryKeys.competitions.structure(competitionId),
    queryFn: () => fetchStructureView(competitionId),
    enabled: competitionId.length > 0,
  });

  return (
    <main id="main" className="page page--structure">
      {query.isPending && !query.data && <LoadingState />}
      {query.isError && !query.data && <ErrorState error={query.error} />}
      {query.data && <StructureHub data={query.data} />}
    </main>
  );
}

function StructureHub({ data }: { data: StructureView }) {
  const { t } = useTranslation('structure');
  const [searchParams, setSearchParams] = useSearchParams();
  const deepLink = parseStructureDeepLink(searchParams.toString());
  const canAddPhase = data.actions.includes('AddCompetitionStage');
  const [addPhaseOpen, setAddPhaseOpen] = useState(false);
  const [insertPlacement, setInsertPlacement] = useState<{
    anchorStageId: string;
    position: StageInsertPosition;
    anchorName: string;
  } | null>(null);
  const stages = useMemo(() => resolveStructureHubStages(data), [data]);
  const multiPhase = stages.length > 1;
  const [selectedStageId, setSelectedStageId] = useState<string | null>(() => {
    if (
      deepLink.stageId &&
      stages.some((stage) => stage.stageId === deepLink.stageId)
    ) {
      return deepLink.stageId;
    }
    return stages[0]?.stageId ?? null;
  });
  const [pendingEdit, setPendingEdit] = useState<StructureSectionId | null>(
    () => deepLink.section,
  );
  const [pendingCompose, setPendingCompose] = useState(() => deepLink.compose);
  const [pendingFocus, setPendingFocus] = useState<StructureFocusId | null>(
    () => deepLink.focus,
  );
  const [pendingManual, setPendingManual] = useState(() => deepLink.manual);
  const [pendingDrawWorkflow, setPendingDrawWorkflow] = useState(
    () => deepLink.openDrawWorkflow,
  );

  const structureLinkKey = `${searchParams.toString()}:${stages.map((s) => s.stageId).join(',')}`;
  useSyncWhen(true, structureLinkKey, () => {
    const parsed = parseStructureDeepLink(searchParams.toString());
    if (stages.length === 0) {
      setSelectedStageId(null);
      setPendingEdit(null);
      setPendingCompose(false);
      setPendingFocus(null);
      setPendingManual(false);
      setPendingDrawWorkflow(false);
      return;
    }
    if (
      parsed.stageId &&
      stages.some((stage) => stage.stageId === parsed.stageId)
    ) {
      setSelectedStageId(parsed.stageId);
      setPendingEdit(parsed.section);
      setPendingCompose(parsed.compose);
      setPendingFocus(parsed.focus);
      setPendingManual(parsed.manual);
      setPendingDrawWorkflow(parsed.openDrawWorkflow);
      return;
    }
    setSelectedStageId((current) => {
      if (current && stages.some((stage) => stage.stageId === current)) {
        return current;
      }
      return stages[0]!.stageId;
    });
  });

  const selectedStage =
    stages.find((stage) => stage.stageId === selectedStageId) ?? null;

  const selectStage = (stageId: string) => {
    setSelectedStageId(stageId);
    setPendingEdit(null);
    setPendingCompose(false);
    setPendingFocus(null);
    setPendingManual(false);
    setPendingDrawWorkflow(false);
    setSearchParams(
      (prev) => {
        const next = new URLSearchParams(prev);
        next.set(STRUCTURE_STAGE_PARAM, stageId);
        next.delete(STRUCTURE_SECTION_PARAM);
        next.delete(STRUCTURE_ROUND_PARAM);
        next.delete(STRUCTURE_COMPOSE_PARAM);
        next.delete(STRUCTURE_FOCUS_PARAM);
        next.delete(STRUCTURE_MANUAL_PARAM);
        next.delete(STRUCTURE_DRAW_PARAM);
        return next;
      },
      { replace: true },
    );
  };

  const readinessSummary = useMemo(
    () => computeStructureReadinessSummary(data, stages),
    [data, stages],
  );

  const attentionQuery = useQuery({
    queryKey: queryKeys.competitions.attention(data.competitionId),
    queryFn: () => fetchNeedsAttention(data.competitionId),
    enabled: data.competitionId.length > 0 && stages.length > 0,
    staleTime: 30_000,
  });

  const openAppend = () => {
    setInsertPlacement(null);
    setAddPhaseOpen(true);
  };

  const openInsert = (
    stageId: string,
    position: StageInsertPosition,
    anchorName: string,
  ) => {
    setInsertPlacement({ anchorStageId: stageId, position, anchorName });
    setAddPhaseOpen(true);
  };

  const closeBirth = () => {
    setAddPhaseOpen(false);
    setInsertPlacement(null);
  };

  const addPhaseAction = (
    <Tooltip
      content={
        canAddPhase ? t('graph.addPhase') : t('hub.addPhaseDisabledHint')
      }
    >
      <span>
        <button
          type="button"
          className="ds-btn ds-btn--primary"
          disabled={!canAddPhase}
          onClick={openAppend}
        >
          <PlusIcon size="sm" />
          <span>{t('graph.addPhase')}</span>
        </button>
      </span>
    </Tooltip>
  );

  const bootstrapEmpty =
    stages.length === 0 ? (
      <EmptyState
        variant="idle"
        icon={<EmptySelectionIcon size="lg" />}
        title={t('structure.emptyTitle')}
        action={addPhaseAction}
      >
        {t('structure.emptyBody')}
      </EmptyState>
    ) : null;

  return (
    <div className="structure-hub">
      <PageHead
        title={t('title')}
        actions={
          stages.length === 1 ? (
            canAddPhase && selectedStage ? (
              <StructureSinglePhaseBirth
                stageName={selectedStage.name}
                onAppend={openAppend}
                onInsertBefore={() =>
                  openInsert(
                    selectedStage.stageId,
                    'Before',
                    selectedStage.name,
                  )
                }
                onInsertAfter={() =>
                  openInsert(selectedStage.stageId, 'After', selectedStage.name)
                }
              />
            ) : (
              addPhaseAction
            )
          ) : undefined
        }
        note={readinessStatusNote(
          data,
          stages,
          readinessSummary,
          attentionQuery,
        )}
      />

      {bootstrapEmpty ?? (
        <div
          className={`structure-hub__layout${multiPhase ? '' : ' structure-hub__layout--single-phase'}`}
        >
          {multiPhase ? (
            <TopologyPanel
              data={data}
              stages={stages}
              selectedStageId={selectedStageId}
              canReorder={canAddPhase}
              onSelectStage={selectStage}
              onInsert={openInsert}
            />
          ) : null}
          <div
            className={
              multiPhase
                ? 'structure-hub__fiche-column'
                : 'structure-hub__single-phase'
            }
          >
            <StructurePhaseFiche
              data={data}
              stage={selectedStage}
              singlePhaseHub={!multiPhase}
              onSelectStage={selectStage}
              initialEdit={pendingEdit}
              initialCompose={pendingCompose}
              initialFocus={pendingFocus}
              initialManual={pendingManual}
              initialDrawWorkflow={pendingDrawWorkflow}
              onInitialEditConsumed={() => {
                setPendingEdit(null);
                setPendingCompose(false);
                setPendingFocus(null);
                setPendingManual(false);
                setPendingDrawWorkflow(false);
                setSearchParams(
                  (prev) => {
                    const next = new URLSearchParams(prev);
                    next.delete(STRUCTURE_SECTION_PARAM);
                    next.delete(STRUCTURE_ROUND_PARAM);
                    next.delete(STRUCTURE_COMPOSE_PARAM);
                    next.delete(STRUCTURE_FOCUS_PARAM);
                    next.delete(STRUCTURE_MANUAL_PARAM);
                    next.delete(STRUCTURE_DRAW_PARAM);
                    return next;
                  },
                  { replace: true },
                );
              }}
            />
          </div>
        </div>
      )}

      <AddPhaseDialog
        competitionId={data.competitionId}
        structureStages={stages}
        open={addPhaseOpen}
        onClose={closeBirth}
        onCreated={selectStage}
        placement={
          insertPlacement
            ? {
                anchorStageId: insertPlacement.anchorStageId,
                position: insertPlacement.position,
              }
            : null
        }
        anchorName={insertPlacement?.anchorName ?? ''}
      />
    </div>
  );
}

function TopologyPanel({
  data,
  stages,
  selectedStageId,
  canReorder,
  onSelectStage,
  onInsert,
}: {
  data: StructureView;
  stages: StructureStageHubSummary[];
  selectedStageId: string | null;
  canReorder: boolean;
  onSelectStage: (stageId: string) => void;
  onInsert: (
    stageId: string,
    position: StageInsertPosition,
    anchorName: string,
  ) => void;
}) {
  const { t } = useTranslation('structure');
  const queryClient = useQueryClient();
  const [pendingMove, setPendingMove] = useState<{
    stageId: string;
    direction: 'up' | 'down';
  } | null>(null);
  const [removeStage, setRemoveStage] =
    useState<StructureStageHubSummary | null>(null);
  const reorder = useMutation({
    mutationFn: (stageIds: string[]) =>
      setCompetitionStageOrder(data.competitionId, { stageIds }),
    onSuccess: (structure) => {
      queryClient.setQueryData(
        queryKeys.competitions.structure(data.competitionId),
        structure,
      );
      scheduleStructureRefresh(queryClient, data.competitionId, ['graph']);
    },
  });
  const multiPhase = stages.length > 1;

  const moveStage = (index: number, direction: 'up' | 'down') => {
    const target = direction === 'up' ? index - 1 : index + 1;
    if (target < 0 || target >= stages.length || reorder.isPending) {
      return;
    }
    const stageIds = stages.map((stage) => stage.stageId);
    const current = stageIds[index];
    const neighbor = stageIds[target];
    if (!current || !neighbor) {
      return;
    }
    stageIds[index] = neighbor;
    stageIds[target] = current;
    setPendingMove({ stageId: current, direction });
    reorder.mutate(stageIds, {
      onSettled: () => setPendingMove(null),
    });
  };
  const stageNameById = useMemo(() => {
    const map = new Map<string, string>();
    for (const stage of stages) {
      map.set(stage.stageId, stage.name);
    }
    return map;
  }, [stages]);

  return (
    <>
      <FormSection
        id="topology"
        className="structure-topology"
        icon={<StructureIcon size="md" aria-hidden="true" />}
        title={t('hub.topology')}
      >
        {reorder.isError ? (
          <MutationError error={reorder.error} structureStages={stages} />
        ) : null}
        <ol
          className={`structure-topology__list${multiPhase ? '' : ' structure-topology__list--single'}`}
        >
          {stages.map((stage, index) => {
            const selected = stage.stageId === selectedStageId;
            const next = stages[index + 1];
            const outbound = outboundDestinations(stage);
            const facts = topologyCardFacts(stage, t);
            const topologyIssueTone = stageWorstPresentedDiagnosticTone(
              data,
              stage,
            );
            const formatLabel = stage.formatKind
              ? structureFormatKindLabel(stage.formatKind)
              : t('structure.formatNotConfigured');
            const univocalToNext =
              outbound.length === 1 &&
              next != null &&
              outbound[0] === next.stageId;
            const bridgeMode: 'connector' | 'outbound' | 'empty' =
              univocalToNext
                ? 'connector'
                : outbound.length > 0
                  ? 'outbound'
                  : 'empty';
            const edgeLabel =
              bridgeMode === 'connector' ? edgeLabelFromSource(stage, t) : null;

            const canRemoveStage = (stage.actions ?? []).includes(
              'RemoveStage',
            );
            const showFoot =
              canReorder || canRemoveStage || stage.hasDrawRules === true;

            return (
              <li key={stage.stageId} className="structure-topology__item">
                {canReorder && index === 0 ? (
                  <StructurePhaseInsertSlot
                    stageName={stage.name}
                    position="before"
                    disabled={reorder.isPending}
                    onInsert={() =>
                      onInsert(stage.stageId, 'Before', stage.name)
                    }
                  />
                ) : null}
                <SelectableTile
                  as="div"
                  className="structure-topology__tile"
                  selected={selected}
                >
                  <button
                    type="button"
                    className="structure-topology__select"
                    aria-current={selected ? 'true' : undefined}
                    onClick={() => onSelectStage(stage.stageId)}
                  >
                    <span className="structure-topology__tile-head">
                      <span className="structure-topology__tile-title">
                        <Tooltip content={formatLabel}>
                          <span className="structure-topology__format-icon">
                            <StageFormatGlyph
                              kind={stage.formatKind}
                              size="sm"
                            />
                          </span>
                        </Tooltip>
                        <span className="structure-topology__tile-name">
                          {stage.name}
                        </span>
                      </span>
                      <span className="structure-topology__tile-status">
                        <StageStatusBadge
                          status={stage.status}
                          density="compact"
                        />
                        {topologyIssueTone != null ? (
                          <span
                            className="structure-topology__issue"
                            data-tone={topologyIssueTone}
                            aria-label={t('hub.phaseGapsAmbient')}
                          >
                            <StructureDiagnosticIssueIcon
                              tone={topologyIssueTone}
                              size="sm"
                            />
                          </span>
                        ) : null}
                      </span>
                    </span>

                    <span className="structure-topology__tile-body">
                      <span className="structure-topology__tile-facts">
                        {facts.map((fact) => (
                          <span
                            key={fact.id}
                            className="structure-topology__fact"
                          >
                            <span className="structure-topology__fact-icon">
                              {fact.icon}
                            </span>
                            <span className="structure-topology__fact-value">
                              {fact.value}
                            </span>
                          </span>
                        ))}
                      </span>

                      {showFoot ? (
                        <span
                          className="structure-topology__tile-foot"
                          data-move={canReorder ? 'true' : 'false'}
                          data-remove={canRemoveStage ? 'true' : 'false'}
                        >
                          <StructurePhaseDrawHint
                            stage={stage}
                            variant="topology"
                          />
                        </span>
                      ) : null}
                    </span>
                  </button>
                  {canReorder && stages.length > 1 ? (
                    <StructurePhaseMoveControls
                      stageName={stage.name}
                      disabled={reorder.isPending}
                      pending={
                        pendingMove?.stageId === stage.stageId
                          ? pendingMove.direction
                          : null
                      }
                      moveUpDisabled={index === 0}
                      moveDownDisabled={index === stages.length - 1}
                      onMoveUp={() => moveStage(index, 'up')}
                      onMoveDown={() => moveStage(index, 'down')}
                    />
                  ) : null}
                  {canRemoveStage ? (
                    <StructurePhaseRemoveControl
                      stageName={stage.name}
                      onRemove={() => setRemoveStage(stage)}
                    />
                  ) : null}
                </SelectableTile>

                {index < stages.length - 1 ? (
                  <div
                    className="structure-topology__bridge"
                    data-bridge={bridgeMode}
                  >
                    {canReorder ? (
                      <StructurePhaseInsertSlot
                        stageName={stage.name}
                        position="after"
                        disabled={reorder.isPending}
                        onInsert={() =>
                          onInsert(stage.stageId, 'After', stage.name)
                        }
                      />
                    ) : null}
                    {bridgeMode === 'outbound' ? (
                      <TopologyOutboundLinks
                        destinationIds={outbound}
                        stageNameById={stageNameById}
                        onSelectStage={onSelectStage}
                      />
                    ) : bridgeMode === 'connector' && edgeLabel != null ? (
                      <div
                        className="structure-topology__connector"
                        aria-hidden="true"
                      >
                        <span className="structure-topology__edge">
                          {edgeLabel}
                        </span>
                        <span className="structure-topology__connector-arrow">
                          <span className="structure-topology__connector-shaft" />
                          <span className="structure-topology__connector-head" />
                        </span>
                      </div>
                    ) : null}
                  </div>
                ) : canReorder ? (
                  <StructurePhaseInsertSlot
                    stageName={stage.name}
                    position="after"
                    disabled={reorder.isPending}
                    onInsert={() =>
                      onInsert(stage.stageId, 'After', stage.name)
                    }
                  />
                ) : null}
              </li>
            );
          })}
        </ol>
      </FormSection>
      {removeStage ? (
        <RemovePhaseDialog
          data={data}
          stage={removeStage}
          open
          onClose={() => setRemoveStage(null)}
        />
      ) : null}
    </>
  );
}

const OUTBOUND_VISIBLE_MAX = 4;

function TopologyOutboundLinks({
  destinationIds,
  stageNameById,
  onSelectStage,
}: {
  destinationIds: string[];
  stageNameById: Map<string, string>;
  onSelectStage: (stageId: string) => void;
}) {
  const { t } = useTranslation('structure');
  const truncated = destinationIds.length > OUTBOUND_VISIBLE_MAX;
  const visible = truncated
    ? destinationIds.slice(0, OUTBOUND_VISIBLE_MAX)
    : destinationIds;

  if (truncated) {
    return (
      <div className="structure-topology__outbound">
        <span className="structure-topology__outbound-summary">
          {t('hub.multiDestinations', { count: destinationIds.length })}
        </span>
      </div>
    );
  }

  return (
    <div className="structure-topology__outbound">
      {visible.map((id) => (
        <button
          key={id}
          type="button"
          className="structure-topology__outbound-link"
          aria-label={t('hub.outboundTo', {
            name: stageNameById.get(id) ?? id,
          })}
          onClick={() => onSelectStage(id)}
        >
          <span aria-hidden="true">→ </span>
          {stageNameById.get(id) ?? id}
        </button>
      ))}
    </div>
  );
}

function StageFormatGlyph({
  kind,
  size = 'sm',
}: {
  kind?: StructureFormatKind | null;
  size?: 'sm' | 'md' | 'lg';
}) {
  switch (kind) {
    case 'Cup':
      return <CupFormatIcon size={size} aria-hidden="true" />;
    case 'Championship':
      return <ChampionshipFormatIcon size={size} aria-hidden="true" />;
    case 'Groups':
      return <GroupsFormatIcon size={size} aria-hidden="true" />;
    case 'Swiss':
      return <SwissFormatIcon size={size} aria-hidden="true" />;
    default:
      return <StructureIcon size={size} aria-hidden="true" />;
  }
}

function outboundDestinations(stage: StructureStageHubSummary): string[] {
  return outboundDestinationStageIds(stage);
}

function edgeLabelFromSource(
  source: StructureStageHubSummary,
  t: (key: string, options?: Record<string, unknown>) => string,
): string {
  if (source.hasProgressionRules && source.progressionPathCount > 0) {
    return t('hub.edgeProgression', { count: source.progressionPathCount });
  }
  if (source.hasQualificationRules && source.qualificationPathCount > 0) {
    return t('hub.edgeQualification', {
      count: source.qualificationPathCount,
    });
  }
  return t('hub.edgeGeneric');
}

function attentionQueryPhase(
  query: UseQueryResult<NeedsAttention>,
): AttentionQueryPhase {
  if (query.isPending) {
    return 'pending';
  }
  if (query.isError) {
    return 'error';
  }
  return 'success';
}

function readinessStatusNote(
  data: StructureView,
  stages: StructureStageHubSummary[],
  summary: StructureReadinessSummary,
  attentionQuery: UseQueryResult<NeedsAttention>,
): ReactNode {
  const lifecycleClosed =
    data.status === 'Completed' || data.status === 'Archived';
  const attentionPhase = attentionQueryPhase(attentionQuery);
  const attentionStages = stages.map((stage) => ({
    stageId: stage.stageId,
    name: stage.name,
    status: stage.status,
  }));
  const attentionPartition =
    attentionPhase === 'success'
      ? partitionPresentableAttention(attentionQuery.data?.items ?? [], {
          stages: attentionStages,
        })
      : { blocking: [], attention: [] };

  const noteKind = resolveStructurePageHeadNoteKind({
    lifecycleClosed,
    stagesEmpty: stages.length === 0,
    structurePresentedDanger: summary.showPageHeadNegative,
    structureHasPresentedProblems: summary.hasAny,
    attentionPhase,
    presentableBlockingAttentionCount: attentionPartition.blocking.length,
    presentableNonBlockingAttentionCount: attentionPartition.attention.length,
    hubNextStep: data.hubNextStep,
    stages: stages.map((stage) => ({ status: stage.status })),
    operationalSlicePending: isStructureHubOperationalSlicePending(
      data.readiness.readyForNextSlice,
      data.readiness.nextConstructionStepHint,
    ),
  });

  if (noteKind === 'negative') {
    return <StructureReadinessHint severity="error" />;
  }

  if (noteKind === 'warning') {
    return <StructureReadinessHint severity="warning" />;
  }

  if (noteKind === 'positiveNextStep' && data.hubNextStep) {
    return (
      <StructurePageHeadNextStepNote
        hubNextStep={data.hubNextStep}
        includeStageName={stages.length > 1}
      />
    );
  }

  if (noteKind === 'positiveCalm') {
    return <StructurePageHeadCalmNote />;
  }

  return null;
}

function StructurePageHeadNextStepNote({
  hubNextStep,
  includeStageName,
}: {
  hubNextStep: StructureHubNextStep;
  includeStageName: boolean;
}) {
  const { t } = useTranslation('structure');
  const labelKey = structurePageHeadHubStepLabelKey(hubNextStep.code, {
    includeStageName,
  });
  return (
    <StructurePageHeadReadinessStatus tone="info" icon={<InfoIcon size="sm" />}>
      {includeStageName
        ? t(labelKey, { stageName: hubNextStep.stageName })
        : t(labelKey)}
    </StructurePageHeadReadinessStatus>
  );
}

function StructurePageHeadCalmNote() {
  const { t } = useTranslation('structure');
  return (
    <StructurePageHeadReadinessStatus
      tone="success"
      icon={<CheckIcon size="sm" />}
    >
      {t('readiness.structureCalm')}
    </StructurePageHeadReadinessStatus>
  );
}

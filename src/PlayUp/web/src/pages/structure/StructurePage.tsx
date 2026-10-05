import { useQuery } from '@tanstack/react-query';
import { type ReactNode, useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams, useSearchParams } from 'react-router-dom';
import { fetchStructureView } from '../../api';
import { FormSection } from '../../design-system/components/FormSection';
import { PageHead } from '../../design-system/components/PageHead';
import { Tooltip } from '../../design-system/components/Tooltip';
import {
  ChampionshipFormatIcon,
  CheckIcon,
  CupFormatIcon,
  DrawPendingIcon,
  GroupsFormatIcon,
  LayersIcon,
  LegsStatIcon,
  MatchdayStatIcon,
  MatchesStatIcon,
  PlusIcon,
  RoundsStatIcon,
  StructureIcon,
  StructureIssueIcon,
  SwissFormatIcon,
  EmptySelectionIcon,
} from '../../design-system/icons/contentIcons';
import { structureFormatKindLabel } from '../../i18n/enumLabels';
import { queryKeys } from '../../queryKeys';
import {
  EmptyState,
  ErrorState,
  LoadingState,
  StageStatusBadge,
} from '../../ui';
import {
  type StructureFormatKind,
  type StructureStageHubSummary,
  type StructureView,
} from '../../types';
import { AddPhaseDialog } from './StructureGraphDialogs';
import { StructureReadinessBanner } from './StructureReadinessBanner';
import { StructurePhaseFiche } from './StructurePhaseFiche';
import { type StructureSectionId } from './structureHubSections';
import { resolvePlacesN, resolvePlacesPerGroup } from './structurePlaces';
import {
  computeStructureReadinessSummary,
  stageAmbientKinds,
  type StructureReadinessSummary,
} from './structureReadinessSummary';
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
  const stages = useMemo(() => resolveStages(data), [data]);
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

  useEffect(() => {
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
  }, [searchParams, stages]);

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
          onClick={() => setAddPhaseOpen(true)}
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
        actions={stages.length === 0 ? undefined : addPhaseAction}
        note={readinessStatusNote(
          data,
          stages,
          readinessSummary,
          selectedStageId,
        )}
      />

      {bootstrapEmpty ?? (
        <div className="structure-hub__layout">
          <TopologyPanel
            data={data}
            stages={stages}
            selectedStageId={selectedStageId}
            onSelectStage={selectStage}
          />
          <StructurePhaseFiche
            data={data}
            stage={selectedStage}
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
      )}

      <AddPhaseDialog
        competitionId={data.competitionId}
        open={addPhaseOpen}
        onClose={() => setAddPhaseOpen(false)}
        onCreated={selectStage}
      />
    </div>
  );
}

/** Prefer Host `stages[]`; synthesize one card from format summary when empty. */
function resolveStages(data: StructureView): StructureStageHubSummary[] {
  if (data.stages.length > 0) {
    return data.stages;
  }
  const primaryStageId = data.format.primaryStageId;
  if (!primaryStageId) {
    return [];
  }
  const kind = data.format.kind;
  return [
    {
      stageId: primaryStageId,
      name: data.format.primaryStageName ?? '—',
      status: data.format.primaryStageStatus ?? 'Draft',
      teamCount: data.participants.occupyingCount,
      matchCount: data.readiness.attachedMatchCount,
      groupCount: data.structure.groupCount,
      roundCount: data.structure.roundCount,
      numberOfPeriods: data.regulation.numberOfPeriods,
      durationPerPeriod: data.regulation.durationPerPeriod,
      hasExtraTime: data.regulation.hasExtraTime ?? false,
      hasPenaltyShootout: data.regulation.hasPenaltyShootout ?? false,
      hasStandingRules:
        kind === 'Championship' || kind === 'Groups' || kind === 'Swiss',
      hasDrawRules: data.structure.hasDrawRules,
      numberOfPots: data.structure.numberOfPots,
      hasQualificationRules: false,
      qualificationPathCount: 0,
      hasProgressionRules: false,
      progressionPathCount: 0,
      hasTieFormat: kind === 'Cup',
      formatKind: kind,
      swissRoundCount: data.structure.swissRoundCount,
    },
  ];
}

function TopologyPanel({
  data,
  stages,
  selectedStageId,
  onSelectStage,
}: {
  data: StructureView;
  stages: StructureStageHubSummary[];
  selectedStageId: string | null;
  onSelectStage: (stageId: string) => void;
}) {
  const { t } = useTranslation('structure');
  const multiPhase = stages.length > 1;
  const stageNameById = useMemo(() => {
    const map = new Map<string, string>();
    for (const stage of stages) {
      map.set(stage.stageId, stage.name);
    }
    return map;
  }, [stages]);

  return (
    <FormSection
      id="topology"
      className="structure-topology"
      icon={<StructureIcon size="md" aria-hidden="true" />}
      title={t('hub.topology')}
    >
      <ol
        className={`structure-topology__list${multiPhase ? '' : ' structure-topology__list--single'}`}
      >
        {stages.map((stage, index) => {
          const selected = stage.stageId === selectedStageId;
          const next = stages[index + 1];
          const outbound = outboundDestinations(stage);
          const facts = topologyCardFacts(stage, t);
          const ambient = stageAmbientKinds(data, stage);
          const formatLabel = stage.formatKind
            ? structureFormatKindLabel(stage.formatKind)
            : t('structure.formatNotConfigured');
          const univocalToNext =
            outbound.length === 1 &&
            next != null &&
            outbound[0] === next.stageId;
          const bridgeMode: 'connector' | 'outbound' | 'empty' = univocalToNext
            ? 'connector'
            : outbound.length > 0
              ? 'outbound'
              : 'empty';
          const edgeLabel =
            bridgeMode === 'connector' ? edgeLabelFromSource(stage, t) : null;

          return (
            <li key={stage.stageId} className="structure-topology__item">
              <button
                type="button"
                className="structure-topology__tile ds-selectable-tile"
                data-selected={selected ? 'true' : 'false'}
                aria-current={selected ? 'true' : undefined}
                onClick={() => onSelectStage(stage.stageId)}
              >
                <span className="structure-topology__tile-head">
                  <span className="structure-topology__tile-title">
                    <Tooltip content={formatLabel}>
                      <span className="structure-topology__format-icon">
                        <StageFormatGlyph kind={stage.formatKind} size="sm" />
                      </span>
                    </Tooltip>
                    <span className="structure-topology__tile-name">
                      {stage.name}
                    </span>
                  </span>
                  <span className="structure-topology__tile-status">
                    <StageStatusBadge status={stage.status} density="compact" />
                    {ambient.length > 0 && (
                      <span
                        className="structure-topology__issue"
                        aria-label={t('hub.phaseGapsAmbient')}
                      >
                        <StructureIssueIcon size="sm" aria-hidden="true" />
                      </span>
                    )}
                  </span>
                </span>

                <span className="structure-topology__tile-facts">
                  {facts.map((fact) => (
                    <span key={fact.id} className="structure-topology__fact">
                      <span className="structure-topology__fact-icon">
                        {fact.icon}
                      </span>
                      <span className="structure-topology__fact-value">
                        {fact.value}
                      </span>
                    </span>
                  ))}
                </span>

                <TopologyDrawHint stage={stage} />
              </button>

              {index < stages.length - 1 && (
                <div
                  className="structure-topology__bridge"
                  data-bridge={bridgeMode}
                >
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
              )}
            </li>
          );
        })}
      </ol>
    </FormSection>
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

function TopologyDrawHint({ stage }: { stage: StructureStageHubSummary }) {
  const { t } = useTranslation('structure');
  // E6 — execution state only (Draft/Published…). Never construction gaps (pots).
  if (!stage.hasDrawRules) {
    return null;
  }
  const badge = stage.drawExecutionBadge ?? 'ToLaunch';

  const key =
    badge === 'ToLaunch'
      ? 'toLaunch'
      : badge === 'InProgress'
        ? 'inProgress'
        : badge === 'ToApply'
          ? 'toApply'
          : 'applied';
  const tone =
    badge === 'ToLaunch'
      ? 'info'
      : badge === 'InProgress'
        ? 'live'
        : badge === 'ToApply'
          ? 'attention'
          : 'done';
  return (
    <Tooltip content={t(`hub.drawExecution.${key}Tooltip`)}>
      <span
        className={`structure-topology__signal structure-topology__signal--${tone}`}
      >
        <DrawPendingIcon size="sm" aria-hidden="true" />
        <span>{t(`hub.drawExecution.${key}`)}</span>
      </span>
    </Tooltip>
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
  const destinations: string[] = [];
  const seen = new Set<string>();
  for (const path of stage.progressionPaths ?? []) {
    if (
      path.destinationStageId !== stage.stageId &&
      !seen.has(path.destinationStageId)
    ) {
      seen.add(path.destinationStageId);
      destinations.push(path.destinationStageId);
    }
  }
  for (const path of stage.qualificationPaths ?? []) {
    if (
      path.destinationStageId !== stage.stageId &&
      !seen.has(path.destinationStageId)
    ) {
      seen.add(path.destinationStageId);
      destinations.push(path.destinationStageId);
    }
  }
  return destinations;
}

function topologyCardFacts(
  stage: StructureStageHubSummary,
  t: (key: string, options?: Record<string, unknown>) => string,
): { id: string; icon: ReactNode; value: string }[] {
  const kind = stage.formatKind;
  const placesN = resolvePlacesN(stage);
  const placesFact =
    placesN != null
      ? {
          id: 'places',
          icon: <LayersIcon size="md" aria-hidden="true" />,
          value: t('hub.topologyStatPlaces', { count: placesN }),
        }
      : null;
  const matchdays = t('hub.topologyStatMatchdays', {
    count: stage.matchdayCount ?? 0,
  });
  const matches = t('hub.topologyStatMatches', { count: stage.matchCount });

  switch (kind) {
    case 'Cup': {
      return [
        {
          id: 'rounds',
          icon: <RoundsStatIcon size="md" aria-hidden="true" />,
          value: t('hub.topologyStatRounds', {
            count: stage.roundCount ?? 0,
          }),
        },
        {
          id: 'legs',
          icon: <LegsStatIcon size="md" aria-hidden="true" />,
          value:
            (stage.numberOfLegs ?? 1) >= 2
              ? t('hub.topologyLegsReturn')
              : t('hub.topologyLegsSingle'),
        },
        ...(placesFact ? [placesFact] : []),
      ];
    }
    case 'Championship':
      return [
        {
          id: 'matchdays',
          icon: <MatchdayStatIcon size="md" aria-hidden="true" />,
          value: matchdays,
        },
        {
          id: 'matches',
          icon: <MatchesStatIcon size="md" aria-hidden="true" />,
          value: matches,
        },
        ...(placesFact ? [placesFact] : []),
      ];
    case 'Groups': {
      const groups = stage.groupCount ?? 0;
      const perGroup = resolvePlacesPerGroup(stage);
      const sizeFact =
        groups > 0 && perGroup != null
          ? {
              id: 'grid',
              icon: <GroupsFormatIcon size="md" aria-hidden="true" />,
              value: t('hub.topologyStatGroupGrid', {
                groups,
                size: perGroup,
              }),
            }
          : {
              id: 'groups',
              icon: <GroupsFormatIcon size="md" aria-hidden="true" />,
              value: t('hub.topologyStatGroups', { count: groups }),
            };
      return [
        {
          id: 'matchdays',
          icon: <MatchdayStatIcon size="md" aria-hidden="true" />,
          value: matchdays,
        },
        sizeFact,
        ...(placesFact ? [placesFact] : []),
      ];
    }
    case 'Swiss':
      return [
        {
          id: 'swiss',
          icon: <RoundsStatIcon size="md" aria-hidden="true" />,
          value: t('hub.topologyStatSwiss', {
            count: stage.swissRoundCount ?? 0,
          }),
        },
        {
          id: 'matches',
          icon: <MatchesStatIcon size="md" aria-hidden="true" />,
          value: matches,
        },
        ...(placesFact ? [placesFact] : []),
      ];
    default:
      return placesFact ? [placesFact] : [];
  }
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

function readinessStatusNote(
  data: StructureView,
  stages: StructureStageHubSummary[],
  summary: StructureReadinessSummary,
  selectedStageId: string | null,
): ReactNode {
  const readiness = data.readiness;
  const formatKind = data.format.kind;
  // Banner only for problems on an existing structure — never birth (0 phase).
  const incomplete = summary.hasAny;
  const readyToMaterialize = readiness.readyForMaterialization;
  const lifecycleClosed =
    data.status === 'Completed' || data.status === 'Archived';
  // ReadyForNextSlice = user can still build next construction — not after lifecycle end.
  const readyNext =
    !lifecycleClosed &&
    !incomplete &&
    !readyToMaterialize &&
    readiness.readyForNextSlice;

  if (incomplete) {
    return (
      <StructureReadinessBanner
        data={data}
        stages={stages}
        summary={summary}
        selectedStageId={selectedStageId}
      />
    );
  }

  // Draw readiness is not a Structure-global status (draw optional).
  // CTA + topology badge carry execution; Hub stays construction-focused.

  if (lifecycleClosed) {
    return null;
  }

  if (readyToMaterialize) {
    const isCup = formatKind === 'Cup';
    return (
      <ReadinessReadyStatus
        titleKey={
          isCup
            ? 'readiness.readyForCupSkeleton'
            : 'readiness.readyForMaterialization'
        }
      />
    );
  }

  if (readyNext) {
    return <ReadinessReadyStatus titleKey="readiness.readyForNextSlice" />;
  }

  return null;
}

function ReadinessReadyStatus({
  titleKey,
  detailKey,
  detailCount,
}: {
  titleKey: string;
  detailKey?: string;
  detailCount?: number;
}) {
  const { t } = useTranslation('structure');

  return (
    <div className="structure-status" role="status">
      <p
        id="readiness-heading"
        className="structure-status__title structure-status__title--ready"
      >
        <CheckIcon size="sm" aria-hidden="true" />
        <span>{t(titleKey)}</span>
      </p>
      {detailKey != null && detailCount != null && detailCount > 0 ? (
        <p className="structure-status__detail">
          {t(detailKey, { count: detailCount })}
        </p>
      ) : null}
    </div>
  );
}

import {useMutation, useQuery, useQueryClient} from '@tanstack/react-query';
import {type ReactNode, type SubmitEvent, useEffect, useId, useMemo, useState,} from 'react';
import {useTranslation} from 'react-i18next';
import {Link, useParams, useSearchParams} from 'react-router-dom';
import {configureStructure, fetchStructureView,} from '../api';
import {Alert} from '../design-system/components/Alert';
import {Dialog} from '../design-system/components/Dialog';
import {FormSection} from '../design-system/components/FormSection';
import {PageHead} from '../design-system/components/PageHead';
import {Tooltip} from '../design-system/components/Tooltip';
import {
  ChampionshipFormatIcon,
  CheckIcon,
  CupFormatIcon,
  DrawPendingIcon,
  GroupsFormatIcon,
  LegsStatIcon,
  MatchdayStatIcon,
  MatchesStatIcon,
  OverviewAttentionIcon,
  PlusIcon,
  RoundsStatIcon,
  StructureIcon,
  StructureIssueIcon,
  SwissFormatIcon,
  LayersIcon,
} from '../design-system/icons/contentIcons';
import { CloseIcon } from '../design-system/icons/shellIcons';
import {attentionSourceLabel, matchGenerationFormatLabel, structureFormatKindLabel,} from '../i18n/enumLabels';
import {queryKeys} from '../queryKeys';
import {EmptyState, ErrorState, LoadingState, MutationError, PendingLabel, StageStatusBadge,} from '../ui';
import {
  type MatchGenerationFormat,
  type StructureFormatKind,
  type StructureStageHubSummary,
  type StructureView,
} from '../types';
import {invalidateAfterStructureMutation} from './structureInvalidation';
import {AddPhaseDialog} from './StructureGraphDialogs';
import {StructurePhaseFiche} from './StructurePhaseFiche';
import {type StructureSectionId} from './structureHubSections';
import {resolvePlacesN, resolvePlacesPerGroup} from './structurePlaces';
import {
  parseStructureDeepLink,
  STRUCTURE_COMPOSE_PARAM,
  STRUCTURE_ROUND_PARAM,
  STRUCTURE_SECTION_PARAM,
  STRUCTURE_STAGE_PARAM,
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
  const canConfigure = data.actions.includes('ConfigureStructure');
  const canAddPhase = data.actions.includes('AddCompetitionStage');
  const [structureEditorOpen, setStructureEditorOpen] = useState(false);
  const [addPhaseOpen, setAddPhaseOpen] = useState(false);
  const stages = useMemo(() => resolveStages(data), [data]);
  const structuralAnomalies = useMemo(
    () => collectStructuralAnomalies(stages),
    [stages],
  );
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

  useEffect(() => {
    const parsed = parseStructureDeepLink(searchParams.toString());
    if (stages.length === 0) {
      setSelectedStageId(null);
      setPendingEdit(null);
      setPendingCompose(false);
      return;
    }
    if (
      parsed.stageId &&
      stages.some((stage) => stage.stageId === parsed.stageId)
    ) {
      setSelectedStageId(parsed.stageId);
      setPendingEdit(parsed.section);
      setPendingCompose(parsed.compose);
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
    setSearchParams(
      (prev) => {
        const next = new URLSearchParams(prev);
        next.set(STRUCTURE_STAGE_PARAM, stageId);
        next.delete(STRUCTURE_SECTION_PARAM);
        next.delete(STRUCTURE_ROUND_PARAM);
        next.delete(STRUCTURE_COMPOSE_PARAM);
        return next;
      },
      { replace: true },
    );
  };

  const openRelationFix = (
    stageId: string,
    section: 'qualification' | 'progression',
  ) => {
    setSelectedStageId(stageId);
    setPendingEdit(section);
    setSearchParams(
      (prev) => {
        const next = new URLSearchParams(prev);
        next.set(STRUCTURE_STAGE_PARAM, stageId);
        next.set(STRUCTURE_SECTION_PARAM, section);
        next.delete(STRUCTURE_ROUND_PARAM);
        next.delete(STRUCTURE_COMPOSE_PARAM);
        return next;
      },
      { replace: true },
    );
  };

  /** Bootstrap only — first structure. Rebuild lives on the Forme tile. */
  const openBootstrapConfigure = () => setStructureEditorOpen(true);

  /** Readiness blockers: bootstrap configure, or Forme on the selected phase. */
  const openReadinessConfigure = () => {
    if (stages.length === 0) {
      setStructureEditorOpen(true);
      return;
    }
    setPendingEdit('construction');
  };

  const headActions = (
    <>
      {stages.length === 0 ? (
        <Tooltip
          content={
            canConfigure
              ? t('hub.configureStructure')
              : t('hub.configureDisabledHint')
          }
        >
          <span>
            <button
              type="button"
              className="ds-btn ds-btn--secondary"
              disabled={!canConfigure}
              onClick={openBootstrapConfigure}
            >
              <StructureIcon size="sm" />
              <span>{t('hub.configureStructure')}</span>
            </button>
          </span>
        </Tooltip>
      ) : null}
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
    </>
  );

  return (
    <div className="structure-hub">
      <PageHead
        title={t('title')}
        actions={headActions}
        note={readinessStatusNote(data, openReadinessConfigure)}
      />

      <div className="structure-hub__layout">
        <TopologyPanel
          stages={stages}
          selectedStageId={selectedStageId}
          onSelectStage={selectStage}
          anomalies={structuralAnomalies}
          onFixRelation={openRelationFix}
        />
        <StructurePhaseFiche
          data={data}
          stage={selectedStage}
          onSelectStage={selectStage}
          initialEdit={pendingEdit}
          initialCompose={pendingCompose}
          onInitialEditConsumed={() => {
            setPendingEdit(null);
            setPendingCompose(false);
            setSearchParams(
              (prev) => {
                const next = new URLSearchParams(prev);
                next.delete(STRUCTURE_SECTION_PARAM);
                next.delete(STRUCTURE_ROUND_PARAM);
                next.delete(STRUCTURE_COMPOSE_PARAM);
                return next;
              },
              { replace: true },
            );
          }}
        />
      </div>

      <StructureEditorDialog
        data={data}
        open={structureEditorOpen}
        onClose={() => setStructureEditorOpen(false)}
      />
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

type StructuralAnomaly = {
  key: string;
  stageId: string;
  stageName: string;
  code: string;
  section: 'qualification' | 'progression';
};

function collectStructuralAnomalies(
  stages: StructureStageHubSummary[],
): StructuralAnomaly[] {
  const anomalies: StructuralAnomaly[] = [];
  for (const stage of stages) {
    for (const code of stage.structureIssues ?? []) {
      anomalies.push({
        key: `${stage.stageId}:${code}`,
        stageId: stage.stageId,
        stageName: stage.name,
        code,
        section: sectionForStructureIssue(code),
      });
    }
  }
  return anomalies;
}

function sectionForStructureIssue(
  code: string,
): 'qualification' | 'progression' {
  return code.includes('Progression') ? 'progression' : 'qualification';
}

function TopologyPanel({
  stages,
  selectedStageId,
  onSelectStage,
  anomalies,
  onFixRelation,
}: {
  stages: StructureStageHubSummary[];
  selectedStageId: string | null;
  onSelectStage: (stageId: string) => void;
  anomalies: StructuralAnomaly[];
  onFixRelation: (
    stageId: string,
    section: 'qualification' | 'progression',
  ) => void;
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
      {stages.length === 0 ? (
        <EmptyState title={t('structure.emptyTitle')}>
          {t('structure.emptyBody')}
        </EmptyState>
      ) : (
        <ol
          className={`structure-topology__list${multiPhase ? '' : ' structure-topology__list--single'}`}
        >
          {stages.map((stage, index) => {
            const selected = stage.stageId === selectedStageId;
            const next = stages[index + 1];
            const outbound = outboundDestinations(stage);
            const facts = topologyCardFacts(stage, t);
            const hasIssues = (stage.structureIssues ?? []).length > 0;
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
              bridgeMode === 'connector'
                ? edgeLabelFromSource(stage, t)
                : null;

            return (
              <li key={stage.stageId} className="structure-topology__item">
                <button
                  type="button"
                  className="structure-topology__card ds-selectable-tile"
                  data-selected={selected ? 'true' : 'false'}
                  aria-current={selected ? 'true' : undefined}
                  onClick={() => onSelectStage(stage.stageId)}
                >
                  <span className="structure-topology__card-head">
                    <span className="structure-topology__card-title">
                      <Tooltip content={formatLabel}>
                        <span className="structure-topology__format-icon">
                          <StageFormatGlyph
                            kind={stage.formatKind}
                            size="sm"
                          />
                        </span>
                      </Tooltip>
                      <span className="structure-topology__card-name">
                        {stage.name}
                      </span>
                    </span>
                    <span className="structure-topology__card-status">
                      <StageStatusBadge
                        status={stage.status}
                        density="compact"
                      />
                      {hasIssues && (
                        <Tooltip content={t('hub.phaseStructuralAnomaly')}>
                          <span className="structure-topology__issue">
                            <StructureIssueIcon size="sm" aria-hidden="true" />
                            <span className="ds-visually-hidden">
                              {t('hub.phaseStructuralAnomaly')}
                            </span>
                          </span>
                        </Tooltip>
                      )}
                    </span>
                  </span>

                  <span className="structure-topology__card-facts">
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
      )}

      {anomalies.length > 0 && (
        <Alert tone="danger" role="status">
          <div className="structure-topology__alert">
            <p className="structure-topology__alert-title">
              {t('hub.structuralAnomaly', { count: anomalies.length })}
            </p>
            <ul className="structure-topology__alert-list">
              {anomalies.map((anomaly) => (
                <li key={anomaly.key} className="structure-topology__alert-item">
                  <div className="structure-topology__alert-copy">
                    <span className="structure-topology__alert-stage">
                      {anomaly.stageName}
                    </span>
                    <span className="structure-topology__alert-issue">
                      {t(`graph.issues.${anomaly.code}`, {
                        defaultValue: anomaly.code,
                      })}
                    </span>
                  </div>
                  <button
                    type="button"
                    className="structure-topology__alert-fix"
                    onClick={() =>
                      onFixRelation(anomaly.stageId, anomaly.section)
                    }
                  >
                    {t('hub.fixRelation')}
                    <span aria-hidden="true">→</span>
                  </button>
                </li>
              ))}
            </ul>
          </div>
        </Alert>
      )}
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
      <span className={`structure-topology__signal structure-topology__signal--${tone}`}>
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
          icon: <RoundsStatIcon size="md" aria-hidden="true"/>,
          value: t('hub.topologyStatRounds', {
            count: stage.roundCount ?? 0,
          }),
        },
        {
          id: 'legs',
          icon: <LegsStatIcon size="md" aria-hidden="true"/>,
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
  onConfigure: () => void,
): ReactNode {
  const readiness = data.readiness;
  const formatKind = data.format.kind;
  const blockers = readiness.blockers;
  /** StructureGraphInvalid counted in readiness but listed only in Topology. */
  const headerBlockers = blockers.filter(
    (code) => code !== 'StructureGraphInvalid',
  );
  const incomplete = blockers.length > 0;
  const readyToMaterialize = readiness.readyForMaterialization;
  const readyNext =
    !incomplete && !readyToMaterialize && readiness.readyForNextSlice;

  if (incomplete) {
    return (
      <ReadinessNotReadyStatus
        competitionId={data.competitionId}
        headerBlockers={headerBlockers}
        onConfigure={onConfigure}
      />
    );
  }

  // Draw readiness is not a Structure-global status (draw optional).
  // CTA + topology badge carry execution; Hub stays construction-focused.

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

function ReadinessNotReadyStatus({
  competitionId,
  headerBlockers,
  onConfigure,
}: {
  competitionId: string;
  headerBlockers: string[];
  onConfigure: () => void;
}) {
  const { t } = useTranslation('structure');

  return (
    <div className="structure-status" role="status">
      <p
        id="readiness-heading"
        className="structure-status__title structure-status__title--attention"
      >
        <OverviewAttentionIcon size="sm" aria-hidden="true" />
        <span>{t('readiness.structureNotReady')}</span>
      </p>
      {headerBlockers.length > 0 && (
        <ul
          className="structure-status__actions"
          aria-labelledby="readiness-heading"
        >
          {headerBlockers.map((code) => (
            <li key={code}>
              <IncompleteBlockerAction
                code={code}
                competitionId={competitionId}
                onConfigure={onConfigure}
              />
            </li>
          ))}
        </ul>
      )}
    </div>
  );
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

function IncompleteBlockerAction({
  code,
  competitionId,
  onConfigure,
}: {
  code: string;
  competitionId: string;
  onConfigure: () => void;
}) {
  const label = attentionSourceLabel(code);

  if (code === 'InsufficientParticipants') {
    return (
      <Link
        className="structure-status__action"
        to={`/competitions/${competitionId}/teams`}
      >
        {label}
        <span aria-hidden="true">→</span>
      </Link>
    );
  }

  if (
    code === 'MissingStage' ||
    code === 'MissingStructure' ||
    code === 'MissingPotRules' ||
    code === 'CupBracketInvalid'
  ) {
    return (
      <button
        type="button"
        className="structure-status__action"
        onClick={onConfigure}
      >
        {label}
        <span aria-hidden="true">→</span>
      </button>
    );
  }

  return (
    <span className="structure-status__action structure-status__action--static">
      {label}
    </span>
  );
}

function StructureEditorDialog({
  data,
  open,
  onClose,
}: {
  data: StructureView;
  open: boolean;
  onClose: () => void;
}) {
  const { t } = useTranslation('structure');
  const { t: tCommon } = useTranslation('common');
  const queryClient = useQueryClient();
  const formId = useId();
  const [format, setFormat] = useState<StructureFormatKind>(
    data.format.kind ?? 'Championship',
  );
  const [stageName, setStageName] = useState('');
  const [groupCount, setGroupCount] = useState(2);
  const [participantsPerGroup, setParticipantsPerGroup] = useState(2);
  const [bracketSize, setBracketSize] = useState(4);
  const [swissRoundCount, setSwissRoundCount] = useState(3);
  const [matchGenerationFormat, setMatchGenerationFormat] =
    useState<MatchGenerationFormat>('SingleRoundRobin');

  useEffect(() => {
    if (open) {
      setFormat(data.format.kind ?? 'Championship');
      setStageName('');
      setGroupCount(2);
      setParticipantsPerGroup(2);
      setBracketSize(4);
      setSwissRoundCount(3);
      setMatchGenerationFormat('SingleRoundRobin');
    }
  }, [open, data.format.kind]);

  const mutation = useMutation({
    mutationFn: () =>
      configureStructure(data.competitionId, {
        format,
        stageName: stageName.trim() || null,
        groupCount: format === 'Groups' ? groupCount : null,
        participantsPerGroup: format === 'Groups' ? participantsPerGroup : null,
        bracketSize: format === 'Cup' ? bracketSize : null,
        swissRoundCount: format === 'Swiss' ? swissRoundCount : null,
        matchGenerationFormat:
          format === 'Championship' || format === 'Groups'
            ? matchGenerationFormat
            : null,
      }),
    onSuccess: async (response) => {
      queryClient.setQueryData(
        queryKeys.competitions.structure(data.competitionId),
        response.structure,
      );
      await invalidateAfterStructureMutation(
        queryClient,
        data.competitionId,
      );
      onClose();
    },
  });

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={t('structure.configureLegend')}
      description={t('structure.configureHint')}
      closeLabel={tCommon('close')}
      closeDisabled={mutation.isPending}
      size="md"
      footer={
        <>
          <button
            type="button"
            className="ds-btn ds-btn--ghost"
            disabled={mutation.isPending}
            onClick={onClose}
          >
            <CloseIcon size="sm" />
            {tCommon('cancel')}
          </button>
          <button
            type="submit"
            form={formId}
            className="ds-btn ds-btn--primary"
            disabled={mutation.isPending}
          >
            {mutation.isPending ? (
              <PendingLabel>{t('structure.configuring')}</PendingLabel>
            ) : (
              <>
                <CheckIcon size="sm" />
                {t('structure.configure')}
              </>
            )}
          </button>
        </>
      }
      footerStatus={
        mutation.isError ? <MutationError error={mutation.error} /> : null
      }
    >
      <form
        id={formId}
        className="form"
        onSubmit={(event: SubmitEvent) => {
          event.preventDefault();
          if (mutation.isPending) {
            return;
          }
          mutation.mutate();
        }}
      >
        <fieldset className="fieldset" disabled={mutation.isPending}>
          <legend className="fieldset__legend">
            {t('structure.configureLegend')}
          </legend>
          <label className="field">
            {t('structure.format')}
            <select
              value={format}
              onChange={(event) =>
                setFormat(event.target.value as StructureFormatKind)
              }
            >
              <option value="Championship">
                {structureFormatKindLabel('Championship')}
              </option>
              <option value="Groups">
                {structureFormatKindLabel('Groups')}
              </option>
              <option value="Cup">{structureFormatKindLabel('Cup')}</option>
              <option value="Swiss">{structureFormatKindLabel('Swiss')}</option>
            </select>
          </label>
          <label className="field">
            {t('structure.stageName')}
            <input
              value={stageName}
              onChange={(event) => setStageName(event.target.value)}
              placeholder={t('structure.stageNamePlaceholder')}
            />
          </label>
          {(format === 'Championship' || format === 'Groups') && (
            <label className="field">
              {t('structure.matchGenerationFormat')}
              <select
                value={matchGenerationFormat}
                onChange={(event) =>
                  setMatchGenerationFormat(
                    event.target.value as MatchGenerationFormat,
                  )
                }
                aria-describedby="match-generation-hint"
              >
                <option value="SingleRoundRobin">
                  {matchGenerationFormatLabel('SingleRoundRobin')}
                </option>
                <option value="DoubleRoundRobin">
                  {matchGenerationFormatLabel('DoubleRoundRobin')}
                </option>
              </select>
              <span id="match-generation-hint" className="caption">
                {t('structure.matchGenerationHint')}
              </span>
            </label>
          )}
          {format === 'Groups' && (
            <div className="form-row">
              <label className="field">
                {t('structure.groupCount')}
                <input
                  type="number"
                  min={2}
                  value={groupCount}
                  onChange={(event) =>
                    setGroupCount(Number(event.target.value) || 2)
                  }
                  required
                />
              </label>
              <label className="field">
                {t('structure.participantsPerGroup')}
                <input
                  type="number"
                  min={2}
                  value={participantsPerGroup}
                  onChange={(event) =>
                    setParticipantsPerGroup(Number(event.target.value) || 2)
                  }
                  required
                />
              </label>
            </div>
          )}
          {format === 'Cup' && (
            <label className="field">
              {t('structure.bracketSize')}
              <input
                type="number"
                min={2}
                max={64}
                value={bracketSize}
                onChange={(event) =>
                  setBracketSize(Number(event.target.value) || 2)
                }
                required
              />
              <span className="caption">{t('structure.bracketHint')}</span>
            </label>
          )}
          {format === 'Swiss' && (
            <label className="field">
              {t('structure.swissRoundCount')}
              <input
                type="number"
                min={1}
                value={swissRoundCount}
                onChange={(event) =>
                  setSwissRoundCount(Number(event.target.value) || 1)
                }
                required
                aria-describedby="swiss-round-hint"
              />
              <span id="swiss-round-hint" className="caption">
                {t('structure.swissRoundHint')}
              </span>
            </label>
          )}
        </fieldset>
        <p className="caption">{t('structure.configureHint')}</p>
      </form>
    </Dialog>
  );
}

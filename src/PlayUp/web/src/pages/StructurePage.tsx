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
  RoundsStatIcon,
  StructureIcon,
  StructureIssueIcon,
  SwissFormatIcon,
  TeamsIcon,
} from '../design-system/icons/contentIcons';
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
import {AddPhaseDialog, RelationEditors, RemovePhaseAction,} from './StructureGraphDialogs';
import {ConstructionLocaleActions} from './StructureLocaleActions';
import {
  constructionSummaryFacts,
  isMatchFrameBound,
  isStandingFrameBound,
  relevantSwitcherSections,
  STRUCTURE_SWITCHER_SECTIONS,
  type StructureSectionId,
} from './structureHubSections';
import {
  parseStructureDeepLink,
  STRUCTURE_ROUND_PARAM,
  STRUCTURE_SECTION_PARAM,
  STRUCTURE_STAGE_PARAM,
} from './structureNavigation';
import {ConfrontationEditors, MatchStandingEditors, TirageEditors,} from './StructureRegulationDialogs';
import './structure.css';

type DrillInSection = StructureSectionId | null;

/**
 * Structure hub — master-detail N1|N2 + Lot 2 graph mutations (dialogs).
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
  const [drillIn, setDrillIn] = useState<DrillInSection>(() => deepLink.section);

  useEffect(() => {
    const parsed = parseStructureDeepLink(searchParams.toString());
    if (stages.length === 0) {
      setSelectedStageId(null);
      setDrillIn(null);
      return;
    }
    if (
      parsed.stageId &&
      stages.some((stage) => stage.stageId === parsed.stageId)
    ) {
      setSelectedStageId(parsed.stageId);
      setDrillIn(parsed.section);
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
    setDrillIn(null);
    setSearchParams(
      (prev) => {
        const next = new URLSearchParams(prev);
        next.set(STRUCTURE_STAGE_PARAM, stageId);
        next.delete(STRUCTURE_SECTION_PARAM);
        next.delete(STRUCTURE_ROUND_PARAM);
        return next;
      },
      { replace: true },
    );
  };

  const selectDrillIn = (section: DrillInSection) => {
    setDrillIn(section);
    setSearchParams(
      (prev) => {
        const next = new URLSearchParams(prev);
        if (selectedStageId) {
          next.set(STRUCTURE_STAGE_PARAM, selectedStageId);
        }
        if (section) {
          next.set(STRUCTURE_SECTION_PARAM, section);
        } else {
          next.delete(STRUCTURE_SECTION_PARAM);
        }
        next.delete(STRUCTURE_ROUND_PARAM);
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
    setDrillIn(section);
    setSearchParams(
      (prev) => {
        const next = new URLSearchParams(prev);
        next.set(STRUCTURE_STAGE_PARAM, stageId);
        next.set(STRUCTURE_SECTION_PARAM, section);
        next.delete(STRUCTURE_ROUND_PARAM);
        return next;
      },
      { replace: true },
    );
  };

  const headActions = (
    <>
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
            onClick={() => setStructureEditorOpen(true)}
          >
            {t('hub.configureStructure')}
          </button>
        </span>
      </Tooltip>
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
            {t('graph.addPhase')}
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
        note={readinessStatusNote(data, () => setStructureEditorOpen(true))}
      />

      <div className="structure-hub__layout">
        <TopologyPanel
          stages={stages}
          selectedStageId={selectedStageId}
          onSelectStage={selectStage}
          anomalies={structuralAnomalies}
          onFixRelation={openRelationFix}
        />
        <PhaseFiche
          data={data}
          stage={selectedStage}
          drillIn={drillIn}
          onDrillIn={selectDrillIn}
          canConfigure={canConfigure}
          onConfigure={() => setStructureEditorOpen(true)}
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
  const needsDraw = stage.formatKind === 'Groups' || stage.formatKind === 'Cup';
  if (!needsDraw) {
    return null;
  }

  if (stage.hasDrawRules) {
    const pots = stage.numberOfPots;
    const label =
      pots != null && pots > 0
        ? t('hub.drawPots', { count: pots })
        : t('hub.drawConfigured');
    return (
      <Tooltip content={t('hub.drawConfiguredTooltip')}>
        <span className="structure-topology__signal">
          <DrawPendingIcon size="sm" aria-hidden="true" />
          <span>{label}</span>
        </span>
      </Tooltip>
    );
  }

  return (
    <span className="structure-topology__signal structure-topology__signal--warning">
      <DrawPendingIcon size="sm" aria-hidden="true" />
      <span>{t('hub.drawPending')}</span>
    </span>
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
  const teams = t('hub.teamCount', { count: stage.teamCount });
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
        {
          id: 'slots',
          icon: <CupFormatIcon size="md" aria-hidden="true"/>,
          value: t('hub.topologyStatSlots', {
            count: stage.slotCount ?? 0,
          }),
        },
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
        {
          id: 'teams',
          icon: <TeamsIcon size="md" aria-hidden="true" />,
          value: teams,
        },
      ];
    case 'Groups': {
      const groups = stage.groupCount ?? 0;
      const sizeFact =
        groups > 0 && stage.teamCount % groups === 0
          ? {
              id: 'grid',
              icon: <GroupsFormatIcon size="md" aria-hidden="true" />,
              value: t('hub.topologyStatGroupGrid', {
                groups,
                size: stage.teamCount / groups,
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
        {
          id: 'teams',
          icon: <TeamsIcon size="md" aria-hidden="true" />,
          value: teams,
        },
      ];
    }
    case 'Swiss':
      return [
        {
          id: 'swiss',
          icon: <SwissFormatIcon size="md" aria-hidden="true" />,
          value: t('hub.topologyStatSwiss', {
            count: stage.swissRoundCount ?? 0,
          }),
        },
        {
          id: 'matches',
          icon: <MatchesStatIcon size="md" aria-hidden="true" />,
          value: matches,
        },
        {
          id: 'teams',
          icon: <TeamsIcon size="md" aria-hidden="true" />,
          value: teams,
        },
      ];
    default:
      return [
        {
          id: 'teams',
          icon: <TeamsIcon size="md" aria-hidden="true" />,
          value: teams,
        },
      ];
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

function PhaseFiche({
  data,
  stage,
  drillIn,
  onDrillIn,
  canConfigure,
  onConfigure,
}: {
  data: StructureView;
  stage: StructureStageHubSummary | null;
  drillIn: DrillInSection;
  onDrillIn: (section: DrillInSection) => void;
  canConfigure: boolean;
  onConfigure: () => void;
}) {
  const { t } = useTranslation('structure');

  if (!stage) {
    return (
      <section className="structure-fiche" aria-labelledby="fiche-empty">
        <h2 id="fiche-empty" className="structure-fiche__title">
          {t('hub.phaseFiche')}
        </h2>
        <EmptyState title={t('hub.noPhaseSelectedTitle')}>
          {t('hub.noPhaseSelectedBody')}
        </EmptyState>
      </section>
    );
  }

  if (drillIn) {
    return (
      <PhaseDrillIn
        data={data}
        stage={stage}
        section={drillIn}
        onBack={() => onDrillIn(null)}
        onSwitch={onDrillIn}
        canConfigure={canConfigure}
        onConfigure={onConfigure}
      />
    );
  }

  return (
    <PhaseOverview
      data={data}
      stage={stage}
      onDrillIn={onDrillIn}
      canConfigure={canConfigure}
      onConfigure={onConfigure}
    />
  );
}

function PhaseOverview({
  data,
  stage,
  onDrillIn,
  canConfigure,
  onConfigure,
}: {
  data: StructureView;
  stage: StructureStageHubSummary;
  onDrillIn: (section: StructureSectionId) => void;
  canConfigure: boolean;
  onConfigure: () => void;
}) {
  const { t } = useTranslation('structure');
  const formatLabel = stage.formatKind
    ? structureFormatKindLabel(stage.formatKind)
    : t('structure.formatNotConfigured');
  const facts = constructionSummaryFacts(stage);
  const matchBound = isMatchFrameBound(stage.defaultsBinding);
  const standingBound = isStandingFrameBound(stage.defaultsBinding);
  const regulationHref = `/competitions/${data.competitionId}/regulation`;

  return (
    <section
      className="structure-fiche"
      aria-labelledby="phase-overview-heading"
    >
      <header className="structure-fiche__header">
        <div className="structure-fiche__titles">
          <h2 id="phase-overview-heading" className="structure-fiche__title">
            {stage.name}
          </h2>
          <ul className="structure-fiche__pills">
            <li className="structure-fiche__pill">{formatLabel}</li>
            <li className="structure-fiche__pill">
              {t('hub.teamCount', { count: stage.teamCount })}
            </li>
            <li className="structure-fiche__pill structure-fiche__pill--status">
              <StageStatusBadge status={stage.status} />
            </li>
          </ul>
        </div>
        <div className="structure-fiche__actions">
          <RemovePhaseAction data={data} stage={stage} />
          {canConfigure && (
            <button
              type="button"
              className="structure-action"
              onClick={onConfigure}
            >
              {t('structure.editPhase')}
              <span aria-hidden="true">→</span>
            </button>
          )}
        </div>
      </header>

      <div className="structure-overview">
        {(stage.hasQualificationRules ||
          stage.hasProgressionRules ||
          (stage.actions ?? []).includes('ReplaceQualificationRules') ||
          (stage.actions ?? []).includes('ReplaceProgressionRules')) && (
          <OverviewBlock title={t('hub.overview.parcours')}>
            {((stage.actions ?? []).includes('ReplaceQualificationRules') ||
              stage.hasQualificationRules) && (
              <OverviewPointer
                label={t('hub.overview.qualificationIn', {
                  count: stage.qualificationPathCount,
                })}
                onOpen={() => onDrillIn('qualification')}
              />
            )}
            {((stage.actions ?? []).includes('ReplaceProgressionRules') ||
              stage.hasProgressionRules) && (
              <OverviewPointer
                label={t('hub.overview.progressionOut', {
                  count: stage.progressionPathCount,
                })}
                onOpen={() => onDrillIn('progression')}
              />
            )}
          </OverviewBlock>
        )}

        <OverviewBlock title={t('hub.overview.construction')}>
          <OverviewPointer
            label={constructionPointerLabel(facts, t)}
            onOpen={() => onDrillIn('construction')}
          />
        </OverviewBlock>

        <OverviewBlock title={t('hub.overview.cadre')}>
          <OverviewPointer
            label={
              matchBound
                ? t('hub.overview.matchBound')
                : t('hub.overview.matchCustom')
            }
            onOpen={() => onDrillIn('matchs')}
            trailing={
              <Link className="structure-overview__side-link" to={regulationHref}>
                {t('hub.overview.openRegulation')}
              </Link>
            }
          />
          {standingBound !== null && (
            <OverviewPointer
              label={
                standingBound
                  ? t('hub.overview.standingBound')
                  : t('hub.overview.standingCustom')
              }
              onOpen={() => onDrillIn('classement')}
            />
          )}
        </OverviewBlock>

        {relevantSwitcherSections(stage).includes('tirage') && (
          <OverviewBlock title={t('hub.overview.tirage')}>
            <OverviewPointer
              label={
                stage.hasDrawRules
                  ? t('hub.overview.tirageConfigured', {
                      pots: stage.numberOfPots ?? '—',
                    })
                  : t('hub.overview.tirageRequired')
              }
              onOpen={() => onDrillIn('tirage')}
            />
          </OverviewBlock>
        )}

        {stage.hasTieFormat && (
          <OverviewBlock title={t('hub.overview.confrontation')}>
            <OverviewPointer
              label={t('hub.overview.confrontationSummary', {
                legs: stage.numberOfLegs ?? 1,
              })}
              onOpen={() => onDrillIn('confrontation')}
            />
          </OverviewBlock>
        )}
      </div>
    </section>
  );
}

function constructionPointerLabel(
  facts: ReturnType<typeof constructionSummaryFacts>,
  t: (key: string, options?: Record<string, unknown>) => string,
): string {
  if (facts.groupCount > 0) {
    return t('hub.overview.constructionGroups', {
      groups: facts.groupCount,
      teams: facts.teamCount,
    });
  }
  if (facts.legs != null) {
    return t('hub.overview.constructionLegs', {
      teams: facts.teamCount,
      legs: facts.legs,
    });
  }
  return t('hub.overview.constructionTeams', { teams: facts.teamCount });
}

function OverviewBlock({
  title,
  children,
}: {
  title: string;
  children: ReactNode;
}) {
  return (
    <section className="structure-overview__block">
      <h3 className="structure-overview__block-title">{title}</h3>
      <ul className="structure-overview__list">{children}</ul>
    </section>
  );
}

function OverviewPointer({
  label,
  onOpen,
  trailing,
}: {
  label: string;
  onOpen: () => void;
  trailing?: ReactNode;
}) {
  return (
    <li className="structure-overview__row">
      <button type="button" className="structure-overview__pointer" onClick={onOpen}>
        <span>{label}</span>
        <span aria-hidden="true">→</span>
      </button>
      {trailing}
    </li>
  );
}

function PhaseDrillIn({
  data,
  stage,
  section,
  onBack,
  onSwitch,
  canConfigure,
  onConfigure,
}: {
  data: StructureView;
  stage: StructureStageHubSummary;
  section: StructureSectionId;
  onBack: () => void;
  onSwitch: (section: StructureSectionId) => void;
  canConfigure: boolean;
  onConfigure: () => void;
}) {
  const { t } = useTranslation('structure');
  const switcherSections = relevantSwitcherSections(stage);
  const showSwitcher =
    (STRUCTURE_SWITCHER_SECTIONS as StructureSectionId[]).includes(section) &&
    switcherSections.length > 1;

  return (
    <section
      className="structure-fiche structure-fiche--drill"
      aria-labelledby="drill-heading"
    >
      <header className="structure-drill__header">
        <button type="button" className="structure-drill__back" onClick={onBack}>
          <span aria-hidden="true">←</span>
          {stage.name}
        </button>
        <h2 id="drill-heading" className="structure-fiche__title">
          {t(`hub.section.${section}`)}
        </h2>
      </header>

      {showSwitcher && (
        <div
          className="structure-switcher"
          role="tablist"
          aria-label={t('hub.sectionSwitcher')}
        >
          {switcherSections.map((id) => (
            <button
              key={id}
              type="button"
              role="tab"
              aria-selected={id === section}
              className={`structure-switcher__tab${id === section ? ' structure-switcher__tab--active' : ''}`}
              onClick={() => onSwitch(id)}
            >
              {t(`hub.section.${id}`)}
            </button>
          ))}
        </div>
      )}

      <div className="structure-drill__body">
        <SectionDetail
          data={data}
          stage={stage}
          section={section}
          canConfigure={canConfigure}
          onConfigure={onConfigure}
        />
      </div>
    </section>
  );
}

function SectionDetail({
  data,
  stage,
  section,
  canConfigure,
  onConfigure,
}: {
  data: StructureView;
  stage: StructureStageHubSummary;
  section: StructureSectionId;
  canConfigure: boolean;
  onConfigure: () => void;
}) {
  const { t } = useTranslation('structure');
  const regulationHref = `/competitions/${data.competitionId}/regulation`;

  switch (section) {
    case 'construction':
      return (
        <DetailCard
          action={
            canConfigure ? (
              <button
                type="button"
                className="ds-btn ds-btn--secondary"
                onClick={onConfigure}
              >
                {t('hub.editConstruction')}
              </button>
            ) : null
          }
        >
          <dl className="structure-detail__grid">
            <DetailCell
              label={t('structure.format')}
              value={
                stage.formatKind
                  ? structureFormatKindLabel(stage.formatKind)
                  : t('structure.formatNotConfigured')
              }
            />
            {(stage.formatKind === 'Championship' ||
              stage.formatKind === 'Groups') && (
              <DetailCell
                label={t('structure.matchGenerationFormat')}
                value={matchGenerationFormatLabel(
                  data.structure.matchGenerationFormat,
                )}
              />
            )}
            {(stage.groupCount ?? 0) > 0 && (
              <DetailCell
                label={t('structure.groups')}
                value={String(stage.groupCount)}
              />
            )}
            <DetailCell
              label={t('hub.detail.teams')}
              value={String(stage.teamCount)}
            />
            {(stage.roundCount ?? 0) > 0 && (
              <DetailCell
                label={t('structure.rounds')}
                value={String(stage.roundCount)}
              />
            )}
            {stage.formatKind === 'Swiss' && stage.swissRoundCount != null && (
              <DetailCell
                label={t('structure.swissRoundCount')}
                value={String(stage.swissRoundCount)}
              />
            )}
            <DetailCell
              label={t('hub.detail.matches')}
              value={String(stage.matchCount)}
            />
          </dl>
          <ConstructionLocaleActions data={data} stage={stage} />
        </DetailCard>
      );
    case 'qualification':
      return (
        <DetailCard>
          <p className="structure-detail__lede">
            {t('hub.detail.qualificationLede', {
              count: stage.qualificationPathCount,
              teams: stage.teamCount,
            })}
          </p>
          {(stage.qualificationPaths ?? []).length > 0 && (
            <ul className="structure-path-list">
              {(stage.qualificationPaths ?? []).map((path) => (
                <li key={`${path.order}-${path.destinationSlotKey}`}>
                  #{path.order} · {path.selectionMode} {path.selectionValue} →{' '}
                  {path.destinationSlotKey}
                </li>
              ))}
            </ul>
          )}
          <RelationEditors data={data} stage={stage} section="qualification" />
        </DetailCard>
      );
    case 'progression':
      return (
        <DetailCard>
          <p className="structure-detail__lede">
            {t('hub.detail.progressionLede', {
              count: stage.progressionPathCount,
            })}
          </p>
          {(stage.progressionPaths ?? []).length > 0 && (
            <ul className="structure-path-list">
              {(stage.progressionPaths ?? []).map((path) => (
                <li
                  key={`${path.sourceFixtureId}-${path.outcome}-${path.destinationSlotKey}`}
                >
                  {path.outcome} → {path.destinationSlotKey}
                </li>
              ))}
            </ul>
          )}
          <RelationEditors data={data} stage={stage} section="progression" />
        </DetailCard>
      );
    case 'confrontation':
      return (
        <DetailCard
          action={<ConfrontationEditors data={data} stage={stage} />}
        >
          <dl className="structure-detail__grid">
            <DetailCell
              label={t('hub.detail.legs')}
              value={String(stage.numberOfLegs ?? 1)}
            />
            <DetailCell
              label={t('hub.detail.aggregate')}
              value={
                stage.aggregateScoring
                  ? t('hub.detail.yes')
                  : t('hub.detail.no')
              }
            />
            <DetailCell
              label={t('hub.detail.awayGoals')}
              value={
                stage.hasAwayGoalsRule
                  ? t('hub.detail.yes')
                  : t('hub.detail.no')
              }
            />
          </dl>
        </DetailCard>
      );
    case 'tirage':
      return (
        <DetailCard action={<TirageEditors data={data} stage={stage} />}>
          <dl className="structure-detail__grid">
            <DetailCell
              label={t('structure.draw')}
              value={
                stage.hasDrawRules
                  ? t('structure.drawConfigured', {
                      pots: stage.numberOfPots ?? '—',
                    })
                  : t('structure.drawMissing')
              }
            />
            {stage.numberOfSeeds != null && (
              <DetailCell
                label={t('hub.detail.seeds')}
                value={String(stage.numberOfSeeds)}
              />
            )}
          </dl>
          <p className="structure-detail__hint">{t('hub.detail.tirageOpsHint')}</p>
        </DetailCard>
      );
    case 'matchs':
      return (
        <DetailCard
          action={<MatchStandingEditors data={data} stage={stage} section="matchs" />}
        >
          <p className="structure-detail__lede">
            {isMatchFrameBound(stage.defaultsBinding)
              ? t('hub.detail.matchBoundLede')
              : t('hub.detail.matchCustomLede')}
          </p>
          <dl className="structure-detail__grid">
            <DetailCell
              label={t('regulation.numberOfPeriods')}
              value={String(stage.numberOfPeriods)}
            />
            <DetailCell
              label={t('regulation.durationPerPeriod')}
              value={String(stage.durationPerPeriod)}
            />
          </dl>
          <p className="structure-detail__footer">
            <Link className="structure-link" to={regulationHref}>
              {t('hub.overview.openRegulation')}
              <span aria-hidden="true">→</span>
            </Link>
          </p>
        </DetailCard>
      );
    case 'classement':
      return (
        <DetailCard
          action={
            <MatchStandingEditors data={data} stage={stage} section="classement" />
          }
        >
          <p className="structure-detail__lede">
            {isStandingFrameBound(stage.defaultsBinding)
              ? t('hub.detail.standingBoundLede')
              : t('hub.detail.standingCustomLede')}
          </p>
          {stage.winPoints != null && (
            <dl className="structure-detail__grid">
              <DetailCell
                label={t('regulation.winPoints')}
                value={String(stage.winPoints)}
              />
              <DetailCell
                label={t('regulation.drawPoints')}
                value={String(stage.drawPoints ?? 0)}
              />
              <DetailCell
                label={t('regulation.lossPoints')}
                value={String(stage.lossPoints ?? 0)}
              />
            </dl>
          )}
          <p className="structure-detail__footer">
            <Link className="structure-link" to={regulationHref}>
              {t('hub.overview.openRegulation')}
              <span aria-hidden="true">→</span>
            </Link>
          </p>
        </DetailCard>
      );
  }
}

function DetailCard({
  children,
  action,
}: {
  children: ReactNode;
  action?: ReactNode;
}) {
  return (
    <div className="structure-detail">
      {action && <div className="structure-detail__actions">{action}</div>}
      {children}
    </div>
  );
}

function DetailCell({ label, value }: { label: string; value: string }) {
  return (
    <div className="structure-detail__cell">
      <dt>{label}</dt>
      <dd>{value}</dd>
    </div>
  );
}

function readinessStatusNote(
  data: StructureView,
  onConfigure: () => void,
): ReactNode {
  const readiness = data.readiness;
  const formatKind = data.format.kind;
  const needsDraw = formatKind === 'Groups' || formatKind === 'Cup';
  const blockers = readiness.blockers;
  /** StructureGraphInvalid counted in readiness but listed only in Topology. */
  const headerBlockers = blockers.filter(
    (code) => code !== 'StructureGraphInvalid',
  );
  const incomplete = blockers.length > 0;
  const readyToMaterialize = readiness.readyForMaterialization;
  const drawRequired =
    !incomplete &&
    needsDraw &&
    readiness.readyForDraw &&
    !readyToMaterialize;
  const readyNext =
    !incomplete &&
    !readyToMaterialize &&
    !drawRequired &&
    readiness.readyForNextSlice;

  if (incomplete) {
    return (
      <ReadinessNotReadyStatus
        competitionId={data.competitionId}
        headerBlockers={headerBlockers}
        onConfigure={onConfigure}
      />
    );
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

  if (drawRequired) {
    return <ReadinessReadyStatus titleKey="readiness.drawRequired" />;
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

function ReadinessReadyStatus({ titleKey }: { titleKey: string }) {
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
  const [matchdayCount, setMatchdayCount] = useState(
    Math.max(1, data.structure.matchdayCount || 1),
  );
  const [groupCount, setGroupCount] = useState(
    Math.max(1, data.structure.groupCount || 2),
  );
  const [participantsPerGroup, setParticipantsPerGroup] = useState(2);
  const [bracketSize, setBracketSize] = useState(
    Math.max(2, data.structure.slotCount || 4),
  );
  const [swissRoundCount, setSwissRoundCount] = useState(
    Math.max(1, data.structure.swissRoundCount || 3),
  );
  const [matchGenerationFormat, setMatchGenerationFormat] =
    useState<MatchGenerationFormat>(
      data.structure.matchGenerationFormat ?? 'SingleRoundRobin',
    );

  const isRebuild = data.stages.length > 0 || data.format.primaryStageId != null;
  const [confirmRebuild, setConfirmRebuild] = useState(false);

  useEffect(() => {
    if (open) {
      setConfirmRebuild(false);
    }
  }, [open]);

  const mutation = useMutation({
    mutationFn: () =>
      configureStructure(data.competitionId, {
        format,
        stageName: stageName.trim() || null,
        matchdayCount: format === 'Championship' ? matchdayCount : null,
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

  const submitLabel = isRebuild
    ? t('structure.rebuildSubmit')
    : t('structure.configure');

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={
        isRebuild
          ? t('structure.rebuildLegend')
          : t('structure.configureLegend')
      }
      description={
        isRebuild ? t('structure.rebuildHint') : t('structure.configureHint')
      }
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
            {tCommon('cancel')}
          </button>
          <button
            type="submit"
            form={formId}
            className="ds-btn ds-btn--primary"
            disabled={mutation.isPending || (isRebuild && !confirmRebuild)}
          >
            {mutation.isPending ? (
              <PendingLabel>{t('structure.configuring')}</PendingLabel>
            ) : (
              submitLabel
            )}
          </button>
        </>
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
            {isRebuild
              ? t('structure.rebuildLegend')
              : t('structure.configureLegend')}
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
          {format === 'Championship' && (
            <label className="field">
              {t('structure.matchdayCount')}
              <input
                type="number"
                min={1}
                value={matchdayCount}
                onChange={(event) =>
                  setMatchdayCount(Number(event.target.value) || 1)
                }
                required
              />
            </label>
          )}
          {format === 'Groups' && (
            <div className="form-row">
              <label className="field">
                {t('structure.groupCount')}
                <input
                  type="number"
                  min={1}
                  value={groupCount}
                  onChange={(event) =>
                    setGroupCount(Number(event.target.value) || 1)
                  }
                  required
                />
              </label>
              <label className="field">
                {t('structure.participantsPerGroup')}
                <input
                  type="number"
                  min={1}
                  value={participantsPerGroup}
                  onChange={(event) =>
                    setParticipantsPerGroup(Number(event.target.value) || 1)
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
                value={bracketSize}
                onChange={(event) =>
                  setBracketSize(Number(event.target.value) || 2)
                }
                required
              />
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
        {isRebuild ? (
          <label className="field">
            <input
              type="checkbox"
              checked={confirmRebuild}
              onChange={(event) => setConfirmRebuild(event.target.checked)}
            />{' '}
            {t('structure.rebuildConfirm', {
              matchdays: data.structure.matchdayCount,
              groups: data.structure.groupCount,
              rounds: data.structure.roundCount,
              slots: data.structure.slotCount,
            })}
          </label>
        ) : (
          <p className="caption">{t('structure.configureHint')}</p>
        )}
        {mutation.isError && <MutationError error={mutation.error} />}
      </form>
    </Dialog>
  );
}

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  useEffect,
  useId,
  useMemo,
  useState,
  type ReactNode,
  type SubmitEvent,
} from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useParams, useSearchParams } from 'react-router-dom';
import {
  configureOrganisationStructure,
  fetchOrganisationView,
} from '../api';
import { Dialog } from '../design-system/components/Dialog';
import { PageHead } from '../design-system/components/PageHead';
import {
  attentionSourceLabel,
  matchGenerationFormatLabel,
  structureFormatKindLabel,
} from '../i18n/enumLabels';
import { queryKeys } from '../queryKeys';
import {
  EmptyState,
  ErrorState,
  LoadingState,
  MutationError,
  PendingLabel,
  StageStatusBadge,
} from '../ui';
import {
  type MatchGenerationFormat,
  type OrganisationStageHubSummary,
  type OrganisationView,
  type StructureFormatKind,
} from '../types';
import { invalidateAfterStructureMutation } from './structureInvalidation';
import {
  RelationEditors,
  StructureGraphToolbar,
  StructureIssuesBanner,
} from './StructureGraphDialogs';
import { ConstructionLocaleActions } from './StructureLocaleActions';
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
import {
  ConfrontationEditors,
  MatchStandingEditors,
  TirageEditors,
} from './StructureRegulationDialogs';
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
    queryFn: () => fetchOrganisationView(competitionId),
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

function StructureHub({ data }: { data: OrganisationView }) {
  const { t } = useTranslation('structure');
  const [searchParams, setSearchParams] = useSearchParams();
  const deepLink = parseStructureDeepLink(searchParams.toString());
  const canConfigure = data.actions.includes('ConfigureStructure');
  const [structureEditorOpen, setStructureEditorOpen] = useState(false);
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

  return (
    <div className="structure-hub">
      <PageHead
        title={t('title')}
        actions={
          canConfigure ? (
            <button
              type="button"
              className="ds-btn ds-btn--secondary"
              onClick={() => setStructureEditorOpen(true)}
            >
              {t('hub.configureStructure')}
            </button>
          ) : undefined
        }
      />

      <ReadinessStrip data={data} onConfigure={() => setStructureEditorOpen(true)} />

      <StructureGraphToolbar data={data} stage={selectedStage} />

      <div className="structure-hub__layout">
        <TopologyPanel
          stages={stages}
          selectedStageId={selectedStageId}
          onSelectStage={selectStage}
          canConfigure={canConfigure}
          onConfigure={() => setStructureEditorOpen(true)}
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
    </div>
  );
}

/** Prefer Host `stages[]`; synthesize one card from format summary when empty. */
function resolveStages(data: OrganisationView): OrganisationStageHubSummary[] {
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
  stages,
  selectedStageId,
  onSelectStage,
  canConfigure,
  onConfigure,
}: {
  stages: OrganisationStageHubSummary[];
  selectedStageId: string | null;
  onSelectStage: (stageId: string) => void;
  canConfigure: boolean;
  onConfigure: () => void;
}) {
  const { t } = useTranslation('structure');
  const multiPhase = stages.length > 1;

  return (
    <aside className="structure-topology" aria-labelledby="topology-heading">
      <h2 id="topology-heading" className="structure-topology__title">
        {t('hub.topology')}
      </h2>

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
            const edge = multiPhase
              ? edgeLabelBetween(stages[index - 1], stage, t)
              : null;
            return (
              <li key={stage.stageId} className="structure-topology__item">
                {edge && (
                  <p className="structure-topology__edge" aria-hidden="true">
                    {edge}
                  </p>
                )}
                <button
                  type="button"
                  className={`structure-topology__card${selected ? ' structure-topology__card--selected' : ''}`}
                  aria-current={selected ? 'true' : undefined}
                  onClick={() => onSelectStage(stage.stageId)}
                >
                  <span className="structure-topology__card-head">
                    <span className="structure-topology__card-name">
                      {stage.name}
                    </span>
                    <StageStatusBadge status={stage.status} />
                  </span>
                  <span className="structure-topology__card-meta">
                    {topologyCardMeta(stage, t)}
                  </span>
                </button>
              </li>
            );
          })}
        </ol>
      )}

      {canConfigure && (
        <div className="structure-topology__footer">
          <button
            type="button"
            className="structure-action"
            onClick={onConfigure}
          >
            {stages.length === 0
              ? t('structure.configureAction')
              : t('hub.configureStructure')}
            <span aria-hidden="true">→</span>
          </button>
        </div>
      )}
    </aside>
  );
}

function topologyCardMeta(
  stage: OrganisationStageHubSummary,
  t: (key: string, options?: Record<string, unknown>) => string,
): string {
  const kind = stage.formatKind
    ? structureFormatKindLabel(stage.formatKind)
    : t('structure.formatNotConfigured');
  const teams = t('hub.teamCount', { count: stage.teamCount });
  if ((stage.groupCount ?? 0) > 0) {
    return t('hub.topologyMetaGroups', {
      format: kind,
      groups: stage.groupCount,
      teams: stage.teamCount,
    });
  }
  return `${kind} · ${teams}`;
}

function edgeLabelBetween(
  previous: OrganisationStageHubSummary | undefined,
  current: OrganisationStageHubSummary,
  t: (key: string, options?: Record<string, unknown>) => string,
): string | null {
  if (!previous) {
    return null;
  }
  if (previous.hasProgressionRules && previous.progressionPathCount > 0) {
    return t('hub.edgeProgression', { count: previous.progressionPathCount });
  }
  if (current.hasQualificationRules && current.qualificationPathCount > 0) {
    return t('hub.edgeQualification', { count: current.qualificationPathCount });
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
  data: OrganisationView;
  stage: OrganisationStageHubSummary | null;
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
  data: OrganisationView;
  stage: OrganisationStageHubSummary;
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
      </header>

      <StructureIssuesBanner stage={stage} />

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
  data: OrganisationView;
  stage: OrganisationStageHubSummary;
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
  data: OrganisationView;
  stage: OrganisationStageHubSummary;
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

function ReadinessStrip({
  data,
  onConfigure,
}: {
  data: OrganisationView;
  onConfigure: () => void;
}) {
  const { t } = useTranslation('structure');
  const readiness = data.readiness;
  const formatKind = data.format.kind;
  const needsDraw = formatKind === 'Groups' || formatKind === 'Cup';
  const readyToMaterialize = readiness.readyForMaterialization;
  const blockers = readiness.blockers;
  const openCount = blockers.length;

  if (readyToMaterialize) {
    const isCup = formatKind === 'Cup';
    return (
      <section
        className="structure-strip structure-strip--ready"
        aria-labelledby="readiness-heading"
      >
        <div className="structure-strip__head">
          <h2 id="readiness-heading" className="structure-strip__title">
            {isCup
              ? t('readiness.readyForCupSkeleton')
              : t('readiness.readyForMaterialization')}
          </h2>
        </div>
        <div className="structure-strip__actions">
          <p className="structure-panel__muted">
            {isCup
              ? t('readiness.cupSkeletonHint')
              : t('readiness.materializeHint')}
          </p>
          <Link
            className="structure-link"
            to={`/competitions/${data.competitionId}`}
          >
            {isCup
              ? t('readiness.goToOverviewCupSkeleton')
              : t('readiness.goToOverviewMaterialize')}
            <span aria-hidden="true">→</span>
          </Link>
        </div>
      </section>
    );
  }

  if (openCount === 0 && needsDraw && readiness.readyForDraw) {
    return (
      <section
        className="structure-strip structure-strip--ready"
        aria-labelledby="readiness-heading"
      >
        <div className="structure-strip__head">
          <h2 id="readiness-heading" className="structure-strip__title">
            {t('readiness.readyForDraw')}
          </h2>
        </div>
      </section>
    );
  }

  if (openCount === 0) {
    return null;
  }

  return (
    <section className="structure-strip" aria-labelledby="readiness-heading">
      <div className="structure-strip__head">
        <h2 id="readiness-heading" className="structure-strip__title">
          {t('readiness.openItems', { count: openCount })}
        </h2>
      </div>
      <ul className="structure-strip__actions-list">
        {blockers.map((code) => {
          const label = attentionSourceLabel(code);
          const teamsHref =
            code === 'InsufficientParticipants'
              ? `/competitions/${data.competitionId}/teams`
              : null;
          return (
            <li key={code}>
              {teamsHref ? (
                <Link className="structure-strip__action" to={teamsHref}>
                  <span aria-hidden="true">•</span>
                  {label}
                  <span aria-hidden="true">→</span>
                </Link>
              ) : code === 'MissingStage' ? (
                <button
                  type="button"
                  className="structure-strip__action"
                  onClick={onConfigure}
                >
                  <span aria-hidden="true">•</span>
                  {label}
                  <span aria-hidden="true">→</span>
                </button>
              ) : (
                <span className="structure-strip__action structure-strip__action--static">
                  <span aria-hidden="true">•</span>
                  {label}
                </span>
              )}
            </li>
          );
        })}
      </ul>
    </section>
  );
}

function StructureEditorDialog({
  data,
  open,
  onClose,
}: {
  data: OrganisationView;
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
      configureOrganisationStructure(data.competitionId, {
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
        response.organisation,
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

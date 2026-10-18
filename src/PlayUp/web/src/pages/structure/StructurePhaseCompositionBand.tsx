import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';

import { GradientSeparator } from '../../design-system/components/GradientSeparator';
import { PanelHead } from '../../design-system/components/PanelHead';
import { Tooltip } from '../../design-system/components/Tooltip';
import {
  ArrowRightIcon,
  DrawPendingIcon,
  EmptySelectionIcon,
  LayersIcon,
  PencilIcon,
  PlusIcon,
  TeamsIcon,
} from '../../design-system/icons/contentIcons';
import type {
  CompetitionStatus,
  StructureEntry,
  StructureFormatKind,
  StructureInboundComposition,
  StructureStageHubSummary,
} from '../../types';
import { EmptyState } from '../../ui';
import { CompositionQualifiersPanel } from './CompositionQualifiersPanel';
import { RootEntriesRail } from './StructurePhaseRails';
import { StructurePhaseOperationalStrip } from './StructurePhaseOperationalStrip';
import type { CompositionQualifierFeedGroup } from './structureCompositionQualifierFeeds';
import type { EntryPlaceProjection } from './structureEntryPlacementProjection';
import type { StructurePlacementOperationalSectionProps } from './structurePlacementOperationalProps';
import type { MaterializePresentation } from './structureMaterializeCopy';
import type {
  CompositionTileMode,
  PlacesStripLayout,
  TeamsSectionRuntimePresentation,
} from './compositionTilePresentation';
import { shouldShowCompositionPrepActions } from './compositionTilePresentation';

export type StructurePhaseCompositionBandProps = {
  showCompositionBandEmpty: boolean;
  compositionGridClass: string;
  compositionEmptyAction: ReactNode;
  showQualifiersSection: boolean;
  structureHubBackgroundRefresh: boolean;
  qualifiersResolvedEyebrow: ReactNode;
  compositionComposeToolbar: ReactNode;
  feedGroups: CompositionQualifierFeedGroup[];
  inboundComposition: StructureInboundComposition | null | undefined;
  entries: readonly StructureEntry[];
  formatKind: StructureFormatKind | null | undefined;
  schematicReady: boolean;
  entryPlaceProjections: ReadonlyMap<string, EntryPlaceProjection>;
  onViewPhase?: (peerId: string) => void;
  renderAvalSourceAction: (group: CompositionQualifierFeedGroup) => ReactNode;
  compositionPopulationGaugeFooter: ReactNode;
  sharedCompositionPopulationGauge: boolean;
  showTeamsSection: boolean;
  showCompositionContentEmpty: boolean;
  showPopulationColumn: boolean;
  showPlaceAssignmentSection: boolean;
  showCompositionDrawSection: boolean;
  entryProvenanceByEntryId: ReadonlyMap<string, import('./compositionTeamsInboundRows').CompositionTeamProvenance>;
  compositionTeamRows: import('./compositionTeamsInboundRows').CompositionTeamRow[] | null;
  canCompose: boolean;
  teamsComposeActionLabel: string;
  compactIcon: string;
  openCompose: (initial: boolean) => void;
  teamsHasListContent: boolean;
  teamsRuntimePresentation: TeamsSectionRuntimePresentation;
  capacityIssueBanner: ReactNode;
  stage: StructureStageHubSummary;
  placedEntryIds: ReadonlySet<string>;
  populationFeedVolume: number;
  placementOperationalProps: StructurePlacementOperationalSectionProps | null;
  showPlacementBlock: boolean;
  showDrawCta: boolean;
  showActivateDrawCta: boolean;
  competitionId: string;
  competitionStatus: CompetitionStatus;
  structureStages: readonly StructureStageHubSummary[];
  materializePresentation: MaterializePresentation | null;
  stageHasAppliedDraw: boolean;
  compositionTileMode: CompositionTileMode;
  placesStripLayout: PlacesStripLayout;
};

export function StructurePhaseCompositionBand({
  showCompositionBandEmpty,
  compositionGridClass,
  compositionEmptyAction,
  showQualifiersSection,
  structureHubBackgroundRefresh,
  qualifiersResolvedEyebrow,
  compositionComposeToolbar,
  feedGroups,
  inboundComposition,
  entries,
  formatKind,
  schematicReady,
  entryPlaceProjections,
  onViewPhase,
  renderAvalSourceAction,
  compositionPopulationGaugeFooter,
  sharedCompositionPopulationGauge,
  showTeamsSection,
  showCompositionContentEmpty,
  showPopulationColumn,
  showPlaceAssignmentSection,
  showCompositionDrawSection,
  entryProvenanceByEntryId,
  compositionTeamRows,
  canCompose,
  teamsComposeActionLabel,
  compactIcon,
  openCompose,
  teamsHasListContent,
  teamsRuntimePresentation,
  capacityIssueBanner,
  stage,
  placedEntryIds,
  populationFeedVolume,
  placementOperationalProps,
  showPlacementBlock,
  showDrawCta,
  showActivateDrawCta,
  competitionId,
  competitionStatus,
  structureStages,
  materializePresentation,
  stageHasAppliedDraw,
  compositionTileMode,
  placesStripLayout,
}: StructurePhaseCompositionBandProps) {
  const { t } = useTranslation('structure');
  const compositionPrep = shouldShowCompositionPrepActions(compositionTileMode);
  if (showCompositionBandEmpty) {
    return (
      <EmptyState
        variant="idle"
        iconSize="lg"
        icon={<EmptySelectionIcon />}
        action={compositionEmptyAction}
      >
        {t('entries.emptyCompositionBandHint')}
      </EmptyState>
    );
  }

  return (
    <div className={compositionGridClass}>
      {showQualifiersSection ? (
        <section
          className="structure-phase-composition__section"
          aria-labelledby="composition-qualifiers-heading"
        >
          <PanelHead
            id="composition-qualifiers-heading"
            density="inset"
            titleStyle="section"
            icon={<ArrowRightIcon size="sm" />}
            iconRefreshing={structureHubBackgroundRefresh}
            title={t('population.sectionSources')}
            aside={qualifiersResolvedEyebrow}
            metaVariant="count"
            actions={
              compositionPrep && !showTeamsSection
                ? compositionComposeToolbar
                : undefined
            }
          />
          <CompositionQualifiersPanel
            groups={feedGroups}
            inboundComposition={inboundComposition}
            entries={entries}
            formatKind={formatKind}
            entryPlaceProjections={entryPlaceProjections}
            onViewPhase={onViewPhase}
            renderGroupAction={renderAvalSourceAction}
          />
          {compositionPopulationGaugeFooter &&
          !sharedCompositionPopulationGauge &&
          showQualifiersSection &&
          !showTeamsSection &&
          !showCompositionContentEmpty
            ? compositionPopulationGaugeFooter
            : null}
        </section>
      ) : null}

      {showQualifiersSection &&
      (showPopulationColumn ||
        showPlaceAssignmentSection ||
        showCompositionDrawSection) ? (
        <GradientSeparator
          orientation="responsive"
          className="structure-phase-composition__sep structure-phase-composition__sep--population"
        />
      ) : null}

      {showTeamsSection ? (
        <section
          id="rail-entries"
          className="structure-phase-composition__section"
          aria-labelledby="composition-teams-heading"
        >
          <PanelHead
            id="composition-teams-heading"
            density="inset"
            titleStyle="section"
            icon={<TeamsIcon size="sm" />}
            iconRefreshing={structureHubBackgroundRefresh}
            title={t('population.sectionAffectation')}
            actions={
              compositionPrep && canCompose ? (
                <Tooltip content={teamsComposeActionLabel}>
                  <button
                    type="button"
                    className={compactIcon}
                    aria-label={teamsComposeActionLabel}
                    onClick={() => openCompose(false)}
                  >
                    {teamsHasListContent ? (
                      <PencilIcon size="sm" />
                    ) : (
                      <PlusIcon size="sm" />
                    )}
                  </button>
                </Tooltip>
              ) : undefined
            }
          />
          {capacityIssueBanner}
          {teamsRuntimePresentation.kind === 'empty' ? (
            <EmptyState
              variant="idle"
              icon={<EmptySelectionIcon size="lg" />}
              title={t('composition.tile.engagedPoolEmpty')}
            />
          ) : teamsRuntimePresentation.kind === 'undetermined' ? (
            <EmptyState variant="idle" icon={<EmptySelectionIcon size="lg" />}>
              <p className="structure-entries__engaged-undetermined-title">
                {t('composition.tile.engagedPoolUndeterminedTitle')}
              </p>
              <p className="structure-entries__engaged-undetermined-body">
                {t('composition.tile.engagedPoolUndeterminedBody')}
              </p>
            </EmptyState>
          ) : (
            <RootEntriesRail
              variant="composition-band"
              stage={stage}
              entries={entries}
              formatKind={formatKind}
              placedEntryIds={placedEntryIds}
              entryPlaceProjections={entryPlaceProjections}
              schematicReady={schematicReady}
              sourcesConfiguredVolume={populationFeedVolume}
              compositionTileMode={compositionTileMode}
              entryProvenanceByEntryId={entryProvenanceByEntryId}
              compositionTeamRows={compositionTeamRows}
              suppressRuntimeEmptyPresentation
            />
          )}
          {compositionPopulationGaugeFooter &&
          showTeamsSection &&
          !sharedCompositionPopulationGauge
            ? compositionPopulationGaugeFooter
            : null}
        </section>
      ) : null}

      {sharedCompositionPopulationGauge
        ? compositionPopulationGaugeFooter
        : null}

      {showCompositionContentEmpty && !showTeamsSection ? (
        <section
          className="structure-phase-composition__section structure-phase-composition__section--population-empty"
          aria-label={t('entries.emptyCompositionBandHint')}
        >
          <EmptyState
            variant="idle"
            icon={<EmptySelectionIcon size="lg" />}
            action={compositionEmptyAction}
          >
            {t('entries.emptyCompositionBandHint')}
          </EmptyState>
          {compositionPopulationGaugeFooter
            ? compositionPopulationGaugeFooter
            : null}
        </section>
      ) : null}

      {showPopulationColumn &&
      (showPlaceAssignmentSection || showCompositionDrawSection) ? (
        <GradientSeparator
          orientation="responsive"
          className="structure-phase-composition__sep structure-phase-composition__sep--before-affectation"
        />
      ) : null}

      {showPlaceAssignmentSection ? (
        <section
          className="structure-phase-composition__section structure-phase-composition__section--affectation"
          aria-labelledby="composition-place-assignment-heading"
        >
          <PanelHead
            id="composition-place-assignment-heading"
            density="inset"
            titleStyle="section"
            icon={<LayersIcon size="sm" />}
            iconRefreshing={structureHubBackgroundRefresh}
            title={t('fiche.composition.placeAssignment')}
          />
          <div className="structure-phase-operational-bar">
            <section
              className="structure-fiche-section structure-fiche-section--operational"
              aria-label={t('fiche.section.phaseState')}
            >
              <StructurePhaseOperationalStrip
                layoutVariant="composition"
                placesStripLayout={placesStripLayout}
                placement={placementOperationalProps}
                showPlaces={showPlacementBlock}
                showDraw={showDrawCta || showActivateDrawCta}
                competitionId={competitionId}
                competitionStatus={competitionStatus}
                stage={stage}
                structureStages={structureStages}
                presentation={materializePresentation}
                structureHubBackgroundRefresh={structureHubBackgroundRefresh}
                drawApplied={stageHasAppliedDraw}
              />
            </section>
          </div>
        </section>
      ) : null}

      {showCompositionDrawSection ? (
        <section
          className="structure-phase-composition__section structure-phase-composition__section--composition-draw"
          aria-labelledby="composition-draw-heading"
        >
          <PanelHead
            id="composition-draw-heading"
            density="inset"
            titleStyle="section"
            icon={<DrawPendingIcon size="sm" />}
            iconRefreshing={structureHubBackgroundRefresh}
            title={t('fiche.operational.cluster.draw')}
          />
          <div className="structure-phase-operational-bar">
            <section
              className="structure-fiche-section structure-fiche-section--operational"
              aria-label={t('fiche.operational.cluster.draw')}
            >
              <StructurePhaseOperationalStrip
                layoutVariant="composition"
                placesStripLayout="full"
                compositionDrawColumnHead
                placement={placementOperationalProps}
                showPlaces={false}
                showDraw={true}
                competitionId={competitionId}
                competitionStatus={competitionStatus}
                stage={stage}
                structureStages={structureStages}
                presentation={materializePresentation}
                structureHubBackgroundRefresh={structureHubBackgroundRefresh}
                drawApplied={stageHasAppliedDraw}
              />
            </section>
          </div>
        </section>
      ) : null}
    </div>
  );
}

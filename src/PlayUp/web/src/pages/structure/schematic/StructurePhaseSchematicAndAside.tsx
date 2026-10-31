import type { UseQueryResult } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';

import { GradientSeparator } from '../../../design-system/components/GradientSeparator';
import { Tooltip } from '../../../design-system/components/Tooltip';
import {
  ArrowRightIcon,
  AttributionIcon,
  EmptySelectionIcon,
  SettingsIcon,
} from '../../../design-system/icons/contentIcons';
import type {
  CompetitionStatus,
  SchematicCase,
  StructurePlacementAward,
  StructureStageHubSummary,
  StageSchematic,
} from '../../../types';
import { EmptyState } from '../../../ui';
import { CompositionPlacementAwardLabel } from '../composition/CompositionPlacementAwardLabel';
import { PhaseSchematic, type CupPairProductionState } from './phaseSchematic';
import { StructurePhaseMatchTilePanel } from '../matchs/StructurePhaseMatchTilePanel';
import { StructurePhaseOperationalStrip } from '../fiche/StructurePhaseOperationalStrip';
import type { StructurePhaseHeroFact } from '../fiche/StructurePhaseFicheHeader';
import { FluxGroupList, FluxRail } from './StructurePhaseRails';
import { placementRuleParts } from '../composition/structurePhaseFeeds';
import type { FeedGroup } from '../composition/structurePhaseFeeds';
import { shouldShowMatchsSection } from '../matchs/structureMatchsSectionUi';
import { StructureSchematicViewport } from './StructureSchematicViewport';
import type { MaterializePresentation } from '../fiche/structureMaterializeCopy';
import type { StructurePlacementOperationalSectionProps } from '../fiche/structurePlacementOperationalProps';
import { buildStructureResolutionOccupantIndex } from '../resolution/structureResolutionOccupants';
export function StructurePhaseSchematicAndAside({
  showHeroStats,
  heroFacts,
  canRebuild,
  onOpenSkeleton,
  schematicQuery,
  stage,
  hasExits,
  cupPairProductionStates,
  canManualPlace,
  onManualPlaceActivate,
  schematicIssuesBanner,
  showAsideMatchsSection,
  structureHubBackgroundRefresh,
  placementOperationalProps,
  showPlacementBlock,
  showDrawCta,
  showActivateDrawCta,
  competitionId,
  competitionStatus,
  structureStages,
  materializePresentation,
  stageHasAppliedDraw,
  showOutRail,
  showSortiesRail,
  showAttributionRail,
  sortiesEditControl,
  exitsBanners,
  outboundGroups,
  onSelectStage,
  attributionEditControl,
  attributionBanners,
  hasAttribution,
  placementAwards,
}: {
  showHeroStats: boolean;
  heroFacts: StructurePhaseHeroFact[];
  canRebuild: boolean;
  onOpenSkeleton: () => void;
  schematicQuery: Pick<
    UseQueryResult<StageSchematic>,
    'data' | 'isError' | 'isFetching' | 'isSuccess'
  >;
  stage: StructureStageHubSummary;
  hasExits: boolean;
  cupPairProductionStates?: Readonly<Record<string, CupPairProductionState>>;
  canManualPlace: boolean;
  onManualPlaceActivate: (place: SchematicCase) => void;
  schematicIssuesBanner: ReactNode;
  showAsideMatchsSection: boolean;
  structureHubBackgroundRefresh: boolean;
  placementOperationalProps: StructurePlacementOperationalSectionProps | null;
  showPlacementBlock: boolean;
  showDrawCta: boolean;
  showActivateDrawCta: boolean;
  competitionId: string;
  competitionStatus: CompetitionStatus;
  structureStages: readonly StructureStageHubSummary[];
  materializePresentation: MaterializePresentation | null;
  stageHasAppliedDraw: boolean;
  showOutRail: boolean;
  showSortiesRail: boolean;
  showAttributionRail: boolean;
  sortiesEditControl: ReactNode;
  exitsBanners: ReactNode;
  outboundGroups: FeedGroup[];
  onSelectStage?: (stageId: string) => void;
  attributionEditControl: ReactNode;
  attributionBanners: ReactNode;
  hasAttribution: boolean;
  placementAwards: readonly StructurePlacementAward[];
}) {
  const { t } = useTranslation('structure');
  const showHeroChrome = showHeroStats || canRebuild;
  const resolutionIndex = useMemo(
    () => buildStructureResolutionOccupantIndex(stage.resolutionOccupants),
    [stage.resolutionOccupants],
  );

  return (
    <div className="structure-phase-main">
      <div
        className="structure-phase-tile structure-phase-tile--schematic"
        id="tile-places"
      >
        {showHeroChrome ? (
          <div className="structure-phase-hero__chrome">
            <div className="structure-phase-hero__chrome-inner">
              {showHeroStats ? (
                <ul
                  className="structure-phase-hero__stats"
                  aria-label={t('fiche.statsAria')}
                >
                  {heroFacts.map((fact) => (
                    <li
                      key={fact.statKey}
                      className="structure-phase-hero__stat"
                    >
                      <span
                        className="structure-phase-hero__stat-icon"
                        aria-hidden="true"
                      >
                        {fact.icon}
                      </span>
                      <span className="structure-phase-hero__stat-value">
                        {fact.value}
                      </span>
                      <span className="structure-phase-hero__stat-label">
                        {fact.label}
                      </span>
                    </li>
                  ))}
                </ul>
              ) : null}
              {canRebuild ? (
                <Tooltip content={t('fiche.editForme')}>
                  <button
                    type="button"
                    className="ds-btn ds-btn--ghost ds-icon-button ds-icon-button--compact structure-phase-hero__edit-forme"
                    aria-label={t('fiche.editForme')}
                    onClick={onOpenSkeleton}
                  >
                    <SettingsIcon size="sm" />
                  </button>
                </Tooltip>
              ) : null}
            </div>
          </div>
        ) : null}
        <div className="structure-schematic-viewport">
          <StructureSchematicViewport
            schematic={schematicQuery.data}
            isError={schematicQuery.isError}
            isFetching={schematicQuery.isFetching}
            loadingLabel={t('fiche.schematicLoading')}
            unavailableLabel={t('fiche.occupantsUnavailable')}
          >
            {(schematic) => (
              <PhaseSchematic
                schematic={schematic}
                terminal={!hasExits}
                cupRoundCount={stage.roundCount}
                cupPairProductionStates={cupPairProductionStates}
                resolutionIndex={resolutionIndex}
                onPlaceActivate={
                  canManualPlace ? onManualPlaceActivate : undefined
                }
              />
            )}
          </StructureSchematicViewport>
          {schematicIssuesBanner ? (
            <div className="structure-schematic-viewport__issues">
              {schematicIssuesBanner}
            </div>
          ) : null}
        </div>
      </div>

      <aside className="structure-phase-aside">
        {showAsideMatchsSection ? (
          <div className="structure-phase-tile structure-phase-tile--matchs">
            <StructurePhaseMatchTilePanel
              stage={stage}
              iconRefreshing={structureHubBackgroundRefresh}
            />
            {shouldShowMatchsSection(stage) ? (
              <div className="structure-phase-operational-bar">
                <section
                  className="structure-fiche-section structure-fiche-section--operational"
                  aria-labelledby="aside-matchs-heading"
                >
                  <StructurePhaseOperationalStrip
                    layoutVariant="aside-matchs"
                    placement={placementOperationalProps}
                    showPlaces={showPlacementBlock}
                    showDraw={showDrawCta || showActivateDrawCta}
                    competitionId={competitionId}
                    competitionStatus={competitionStatus}
                    stage={stage}
                    structureStages={structureStages}
                    presentation={materializePresentation}
                    structureHubBackgroundRefresh={
                      structureHubBackgroundRefresh
                    }
                    drawApplied={stageHasAppliedDraw}
                  />
                </section>
              </div>
            ) : null}
          </div>
        ) : null}

        {showOutRail ? (
          <div className="structure-phase-tile structure-phase-tile--flux">
            <div className="structure-phase-hero__out">
              {showSortiesRail ? (
                <FluxRail
                  id="rail-exits"
                  side="out"
                  title={t('fiche.tiles.exits')}
                  icon={<ArrowRightIcon size="sm" />}
                  editControl={sortiesEditControl}
                  contentEmpty={!hasExits}
                  emptyState={
                    <EmptyState
                      variant="idle"
                      size="compact"
                      iconSize="lg"
                      icon={<EmptySelectionIcon />}
                      title={t('fiche.flux.sortiesEmptyTitle')}
                    >
                      {t('fiche.flux.sortiesEmptyHint')}
                    </EmptyState>
                  }
                  attentionBanner={exitsBanners}
                >
                  {hasExits ? (
                    <FluxGroupList
                      groups={outboundGroups}
                      onOpenPeer={onSelectStage}
                    />
                  ) : null}
                </FluxRail>
              ) : null}
              {showSortiesRail && showAttributionRail ? (
                <GradientSeparator
                  orientation="horizontal"
                  className="structure-phase-flux__sep"
                />
              ) : null}
              {showAttributionRail ? (
                <FluxRail
                  id="rail-attribution"
                  side="out"
                  title={t('fiche.tiles.attribution')}
                  icon={<AttributionIcon size="sm" />}
                  editControl={attributionEditControl}
                  contentEmpty={!hasAttribution}
                  emptyState={
                    <EmptyState
                      variant="idle"
                      size="compact"
                      iconSize="lg"
                      icon={<EmptySelectionIcon />}
                      title={t('fiche.flux.attributionEmptyTitle')}
                    >
                      {t('fiche.flux.attributionEmptyHint')}
                    </EmptyState>
                  }
                  attentionBanner={attributionBanners}
                >
                  {hasAttribution ? (
                    <ul className="structure-feed-group__rules">
                      {[...(placementAwards ?? [])]
                        .sort((a, b) => a.rank - b.rank)
                        .map((award) => {
                          const parts = placementRuleParts(award, t);
                          return (
                            <li
                              key={`${award.rank}-${award.outcome}-${award.sourcePairKey}`}
                              className="structure-feed-group__rule-line"
                            >
                              <CompositionPlacementAwardLabel
                                rank={parts.rank}
                                medal={parts.medal}
                                rule={{
                                  key: `${award.rank}-${award.outcome}-${award.sourcePairKey}`,
                                  badge: parts.badge,
                                  badgeTone: parts.badgeTone,
                                  context: parts.context,
                                  volume: 1,
                                  family: 'result',
                                  sortPrimary:
                                    award.outcome === 'Winner' ? 0 : 1,
                                  sortSecondary: award.sourcePairKey ?? '',
                                }}
                              />
                            </li>
                          );
                        })}
                    </ul>
                  ) : null}
                </FluxRail>
              ) : null}
            </div>
          </div>
        ) : null}
      </aside>
    </div>
  );
}

import { useTranslation } from 'react-i18next';
import type { CompetitionStatus, StructureStageHubSummary } from '../../types';
import {
  materializePresentationForStage,
  showProductionOperationalSection,
} from './structureOperationalCopy';
import { StructurePhaseOperationalStrip } from './StructurePhaseOperationalStrip';
import type { StructurePlacementOperationalSectionProps } from './StructurePlacementOperationalSection';

export function StructurePhaseOperationalBar({
  structureHubRefreshing,
  showPlaces,
  showDraw,
  placement,
  competitionId,
  competitionStatus,
  stage,
  drawApplied,
}: {
  structureHubRefreshing: boolean;
  showPlaces: boolean;
  showDraw: boolean;
  placement: StructurePlacementOperationalSectionProps | null;
  competitionId: string;
  competitionStatus: CompetitionStatus;
  stage: StructureStageHubSummary;
  drawApplied: boolean;
}) {
  const { t } = useTranslation('structure');
  const presentation = materializePresentationForStage(
    stage,
    competitionStatus,
    stage.status,
  );
  const showProduction = showProductionOperationalSection(stage);

  const showMatches = showProduction || showPlaces || showDraw;

  if (!showMatches) {
    return null;
  }

  const sectionAriaLabel = t('fiche.section.phaseState');

  return (
    <div className="structure-phase-operational-bar">
      <section
        className="structure-fiche-section structure-fiche-section--operational"
        aria-label={sectionAriaLabel}
      >
        <StructurePhaseOperationalStrip
          placement={placement}
          showPlaces={showPlaces}
          showDraw={showDraw}
          competitionId={competitionId}
          competitionStatus={competitionStatus}
          stage={stage}
          presentation={presentation}
          structureHubRefreshing={structureHubRefreshing}
          drawApplied={drawApplied}
        />
      </section>
    </div>
  );
}

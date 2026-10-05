import type { CompetitionStatus, StructureStageHubSummary } from '../../types';
import {
  materializePresentationForStage,
  showProductionOperationalSection,
} from './structureOperationalCopy';
import {
  StructurePlacementOperationalSection,
  type StructurePlacementOperationalSectionProps,
} from './StructurePlacementOperationalSection';
import { StructureProductionOperationalSection } from './StructureProductionOperationalSection';

export function StructurePhaseOperationalBar({
  structureHubRefreshing,
  showPlacement,
  placement,
  competitionId,
  competitionStatus,
  stage,
}: {
  structureHubRefreshing: boolean;
  showPlacement: boolean;
  placement: StructurePlacementOperationalSectionProps | null;
  competitionId: string;
  competitionStatus: CompetitionStatus;
  stage: StructureStageHubSummary;
}) {
  const presentation = materializePresentationForStage(
    stage,
    competitionStatus,
    stage.status,
  );
  const showProduction = showProductionOperationalSection(stage);

  if (!showPlacement && !showProduction) {
    return null;
  }

  return (
    <div className="structure-phase-operational-bar">
      {showPlacement && placement ? (
        <StructurePlacementOperationalSection
          {...placement}
          structureHubRefreshing={structureHubRefreshing}
        />
      ) : null}
      {showProduction ? (
        <StructureProductionOperationalSection
          competitionId={competitionId}
          competitionStatus={competitionStatus}
          stage={stage}
          presentation={presentation}
          structureHubRefreshing={structureHubRefreshing}
        />
      ) : null}
    </div>
  );
}

import { useTranslation } from 'react-i18next';
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
  const { t } = useTranslation('structure');
  const presentation = materializePresentationForStage(
    stage,
    competitionStatus,
    stage.status,
  );
  const showProduction = showProductionOperationalSection(stage);

  if (!showPlacement && !showProduction) {
    return null;
  }

  const sectionAriaLabel =
    showPlacement && showProduction
      ? `${t('fiche.section.phaseState')} — ${t('fiche.section.phaseProduction')}`
      : showPlacement
        ? t('fiche.section.phaseState')
        : t('fiche.section.phaseProduction');

  return (
    <div className="structure-phase-operational-bar">
      <section
        className="structure-fiche-section structure-fiche-section--operational"
        aria-label={sectionAriaLabel}
      >
        <div className="structure-operational-cluster-stack">
          {showPlacement && placement ? (
            <StructurePlacementOperationalSection
              {...placement}
              embeddedInSection
              structureHubRefreshing={structureHubRefreshing}
            />
          ) : null}
          {showProduction ? (
            <StructureProductionOperationalSection
              competitionId={competitionId}
              competitionStatus={competitionStatus}
              stage={stage}
              presentation={presentation}
              embeddedInSection
              structureHubRefreshing={structureHubRefreshing}
            />
          ) : null}
        </div>
      </section>
    </div>
  );
}

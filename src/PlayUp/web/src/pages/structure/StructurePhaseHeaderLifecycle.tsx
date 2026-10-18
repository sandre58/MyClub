import type { StructureStageHubSummary } from '../../types';
import { shouldShowMatchsLifecycle } from './structurePhaseStatusBandModel';
import {
  StructurePhaseDraftMatchsPrepare,
  StructurePhaseMatchsLifecycleBlock,
} from './StructurePhaseMatchsLifecycleBlock';

export function StructurePhaseHeaderLifecycle({
  competitionId,
  stage,
  productionBusy = false,
}: {
  competitionId: string;
  stage: StructureStageHubSummary;
  productionBusy?: boolean;
}) {
  if (!shouldShowMatchsLifecycle(stage)) {
    return null;
  }

  if (stage.status === 'Draft') {
    return (
      <StructurePhaseDraftMatchsPrepare
        competitionId={competitionId}
        stage={stage}
        part="header"
      />
    );
  }

  if (stage.status === 'Completed') {
    return null;
  }

  return (
    <StructurePhaseMatchsLifecycleBlock
      competitionId={competitionId}
      stage={stage}
      productionBusy={productionBusy}
      part="header"
    />
  );
}

import type { ReactNode } from 'react';
import type { StructureStageHubSummary } from '../../types';
import { shouldShowMatchsLifecycle } from './structurePhaseStatusBandModel';
import {
  StructurePhaseDraftMatchsPrepare,
  StructurePhaseMatchsLifecycleBlock,
} from './StructurePhaseMatchsLifecycleBlock';

export function StructurePhaseFicheHeaderLifecycle({
  competitionId,
  stage,
  productionBusy = false,
  part,
}: {
  competitionId: string;
  stage: StructureStageHubSummary;
  productionBusy?: boolean;
  part: 'toolbar' | 'messages';
}) {
  if (!shouldShowMatchsLifecycle(stage)) {
    return null;
  }

  const chromePart = part === 'toolbar' ? 'actions' : 'messages';

  let node: ReactNode;

  if (stage.status === 'Draft') {
    node = (
      <StructurePhaseDraftMatchsPrepare
        competitionId={competitionId}
        stage={stage}
        part={chromePart}
      />
    );
  } else if (stage.status === 'Completed') {
    return null;
  } else {
    node = (
      <StructurePhaseMatchsLifecycleBlock
        competitionId={competitionId}
        stage={stage}
        productionBusy={productionBusy}
        part={chromePart}
      />
    );
  }

  if (part === 'messages') {
    return (
      <div className="structure-fiche__header-lifecycle-messages">{node}</div>
    );
  }

  return node;
}

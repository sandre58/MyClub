import type { ReactNode } from 'react';

import type { StructureStageHubSummary } from '../../types';
import { shouldShowMatchsLifecycle } from './structurePhaseStatusBandModel';
import {
  StructurePhaseDraftMatchsPrepare,
  StructurePhaseMatchsLifecycleBlock,
} from './StructurePhaseMatchsLifecycleBlock';

function HeaderLifecycleChrome({ children }: { children: ReactNode }) {
  return <div className="structure-fiche__header-lifecycle">{children}</div>;
}

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
      <HeaderLifecycleChrome>
        <StructurePhaseDraftMatchsPrepare
          competitionId={competitionId}
          stage={stage}
          part="header"
        />
        <div className="structure-fiche__header-lifecycle-messages">
          <StructurePhaseDraftMatchsPrepare
            competitionId={competitionId}
            stage={stage}
            part="headerAlerts"
          />
        </div>
      </HeaderLifecycleChrome>
    );
  }

  if (stage.status === 'Completed') {
    return null;
  }

  return (
    <HeaderLifecycleChrome>
      <StructurePhaseMatchsLifecycleBlock
        competitionId={competitionId}
        stage={stage}
        productionBusy={productionBusy}
        part="header"
      />
      <div className="structure-fiche__header-lifecycle-messages">
        <StructurePhaseMatchsLifecycleBlock
          competitionId={competitionId}
          stage={stage}
          productionBusy={productionBusy}
          part="headerAlerts"
        />
      </div>
    </HeaderLifecycleChrome>
  );
}

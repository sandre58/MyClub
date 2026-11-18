import { useCallback, useMemo, useState, type ReactNode } from 'react';

import type { DefinitiveResolutionReleaseResponse } from '../../../api';
import { StructureDefinitiveReleaseDialog } from './StructureDefinitiveReleaseDialog';
import {
  DefinitiveReleaseOfferContext,
  type DefinitiveOffer,
} from './definitiveReleaseOfferContext';

/**
 * Holds the grouped offer after a phase is closed. The lifecycle block unmounts
 * once the phase is Completed, so the dialog lives here, on the phase fiche.
 */
export function StructureDefinitiveReleaseOffer({
  competitionId,
  stageId,
  children,
}: {
  competitionId: string;
  stageId: string;
  children: ReactNode;
}) {
  const [offer, setOffer] = useState<DefinitiveOffer | null>(null);
  const publish = useCallback(
    (sourceId: string, preview: DefinitiveResolutionReleaseResponse) => {
      setOffer({ sourceId, preview });
    },
    [],
  );
  const dismiss = useCallback(() => setOffer(null), []);
  const value = useMemo(
    () => ({ offer, publish, dismiss }),
    [offer, publish, dismiss],
  );
  const visible = offer !== null && offer.sourceId === stageId;

  return (
    <DefinitiveReleaseOfferContext.Provider value={value}>
      {children}
      <StructureDefinitiveReleaseDialog
        open={visible}
        competitionId={competitionId}
        sourceId={offer?.sourceId ?? stageId}
        destinationStageId={null}
        preview={visible ? offer.preview : null}
        onClose={dismiss}
      />
    </DefinitiveReleaseOfferContext.Provider>
  );
}

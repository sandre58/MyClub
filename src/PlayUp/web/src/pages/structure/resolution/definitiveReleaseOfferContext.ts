import { createContext, useContext } from 'react';

import type { DefinitiveResolutionReleaseResponse } from '../../../api';

export type DefinitiveOffer = {
  sourceId: string;
  preview: DefinitiveResolutionReleaseResponse;
};

export type DefinitiveReleaseOfferContextValue = {
  offer: DefinitiveOffer | null;
  publish: (
    sourceId: string,
    preview: DefinitiveResolutionReleaseResponse,
  ) => void;
  dismiss: () => void;
};

export const DefinitiveReleaseOfferContext =
  createContext<DefinitiveReleaseOfferContextValue | null>(null);

export function useDefinitiveReleaseOffer(): DefinitiveReleaseOfferContextValue | null {
  return useContext(DefinitiveReleaseOfferContext);
}

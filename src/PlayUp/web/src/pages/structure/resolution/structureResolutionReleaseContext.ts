import { createContext, useContext } from 'react';

import type { StructureResolutionOccupant } from '../../../types';

export type ResolutionReleaseContextValue = {
  requestRelease: (occupant: StructureResolutionOccupant) => void;
};

export const ResolutionReleaseContext =
  createContext<ResolutionReleaseContextValue | null>(null);

export function useStructureResolutionRelease(): ResolutionReleaseContextValue | null {
  return useContext(ResolutionReleaseContext);
}

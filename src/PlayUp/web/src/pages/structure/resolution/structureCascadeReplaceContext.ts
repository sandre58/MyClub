import { createContext, useContext } from 'react';

import type { StructureResolutionOccupant } from '../../../types';

export type CascadeReplaceContextValue = {
  requestReplace: (occupant: StructureResolutionOccupant) => void;
};

export const CascadeReplaceContext =
  createContext<CascadeReplaceContextValue | null>(null);

export function useStructureCascadeReplace(): CascadeReplaceContextValue | null {
  return useContext(CascadeReplaceContext);
}

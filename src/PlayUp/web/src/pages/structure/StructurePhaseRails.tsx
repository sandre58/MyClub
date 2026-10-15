// Re-export barrel — keep imports stable (StructurePhaseFiche, RegulationStrip).
export {
  FluxRuleRow,
  PlacementAwardRow,
  FluxGroupList,
  FluxRail,
} from './structureFluxRails';

export {
  CompositionMeter,
  listComposedAffectationEntries,
  RootEntriesRail,
} from './structurePhaseEntriesRail';

export { ExitKindMenu, ConfrontationPanel } from './structurePhaseRailPanels';

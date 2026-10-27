import { describe, expect, it } from 'vitest';
import { structureDiagnosticMeta } from './structureDiagnosticPresentation';

/**
 * Issue codes emitted on StructureView stages (Host StructureViewAssembler +
 * CompetitionFlowAnalyzer for RESULT). Guardrail: every code must map in BY_CODE.
 */
const HOST_STRUCTURE_ISSUE_CODES = [
  'DanglingQualificationTarget',
  'MissingQualificationDestinationSlot',
  'MissingQualificationDestinationGroup',
  'DanglingProgressionTarget',
  'MissingProgressionDestinationSlot',
  'MissingProgressionDestinationGroup',
  'SlotFeedInvalid',
  'SlotFeedMultiple',
  'SlotFeedMissing',
  'ConfrontationUndecidable',
  'CycleDetected',
  'NonRootUnfed',
  'RoutingNondeterminism',
  'OverCapacityEdge',
  'OverExtraction',
  'SourcePopulationShortfall',
  'PopulationShortfall',
  'AuthoredAwardUnreachable',
  'ExpectedRankUnreachable',
] as const;

describe('Host structureIssues ↔ SPA presentation', () => {
  it('maps every Host structure issue code to structureDiagnosticMeta', () => {
    for (const code of HOST_STRUCTURE_ISSUE_CODES) {
      expect(structureDiagnosticMeta(code), code).not.toBeNull();
    }
  });
});

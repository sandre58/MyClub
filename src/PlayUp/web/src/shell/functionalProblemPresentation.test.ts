import { describe, expect, it } from 'vitest';
import {
  functionalProblemPresentation,
  situationPresentation,
  structureIssuePresentation,
} from './functionalProblemPresentation';

describe('functionalProblemPresentation', () => {
  it('maps StructureGraphInvalid to Structure SoT + ConfigureStructure CTA', () => {
    const presentation = functionalProblemPresentation({
      source: 'StructureGraphInvalid',
      competitionId: 'comp-1',
      targetType: 'Structure',
      targetId: 'comp-1',
    });

    expect(presentation.canal).toBe('overview-echo');
    expect(presentation.tone).toBe('danger');
    expect(presentation.sotHref).toBe('/competitions/comp-1/structure');
    expect(presentation.cta?.actionCode).toBe('ConfigureStructure');
    expect(presentation.titleKey).toBe('attentionSource.StructureGraphInvalid');
    expect(presentation.titleNs).toBe('enums');
  });

  it('maps InsufficientParticipants to Teams SoT (not Structure)', () => {
    const presentation = functionalProblemPresentation({
      source: 'InsufficientParticipants',
      competitionId: 'comp-1',
      params: { activeCount: '2', minimumTeams: '8' },
    });

    expect(presentation.canal).toBe('teams-plateau');
    expect(presentation.sotHref).toBe('/competitions/comp-1/teams');
    expect(presentation.cta?.actionCode).toBe('AddEntry');
  });

  it('maps construction blockers to Structure PageHead canal', () => {
    const presentation = functionalProblemPresentation({
      source: 'MissingStructure',
      competitionId: 'comp-1',
    });

    expect(presentation.canal).toBe('structure-pagehead');
    expect(presentation.tone).toBe('danger');
    expect(presentation.sotHref).toBe('/competitions/comp-1/structure');
  });

  it('maps draw create blocks never-danger (info|warning) with Structure fiche i18n', () => {
    const emptyPool = functionalProblemPresentation({
      source: 'emptyPool',
      competitionId: 'comp-1',
    });
    expect(emptyPool.tone).toBe('info');
    expect(emptyPool.titleNs).toBe('structure');
    expect(emptyPool.titleKey).toContain('createBlocked.short.emptyPool');

    const missingPots = functionalProblemPresentation({
      source: 'missingPots',
      competitionId: 'comp-1',
      stageId: 'stage-1',
    });
    expect(missingPots.tone).toBe('warning');
    expect(missingPots.cta?.actionCode).toBe('ConfigureDrawParams');

    const belowMin = functionalProblemPresentation({
      source: 'belowMinimumTeams',
      competitionId: 'comp-1',
    });
    expect(belowMin.tone).toBe('warning');
    expect(belowMin.sotHref).toBe('/competitions/comp-1/teams');
    expect(belowMin.cta?.actionCode).toBe('AddEntry');
  });

  it('maps structureIssues children via structureIssuePresentation', () => {
    const presentation = structureIssuePresentation({
      code: 'MissingQualificationDestinationSlot',
      competitionId: 'comp-1',
      stageId: 'stage-9',
    });

    expect(presentation.canal).toBe('structure-topology');
    expect(presentation.titleKey).toBe(
      'graph.issues.MissingQualificationDestinationSlot',
    );
    expect(presentation.titleNs).toBe('structure');
    expect(presentation.sotHref).toContain('stage=stage-9');
  });

  it('contextualises SlotFeedMissing when draw is awaiting', () => {
    const awaiting = structureIssuePresentation({
      code: 'SlotFeedMissing',
      competitionId: 'comp-1',
      stageId: 'stage-9',
      awaitingDraw: true,
    });
    // Copy is no longer draw-specific — silence is handled by projection filters.
    expect(awaiting.titleKey).toBe('graph.issues.SlotFeedMissing');
    expect(awaiting.tone).toBe('attention');

    const plain = structureIssuePresentation({
      code: 'SlotFeedMissing',
      competitionId: 'comp-1',
      stageId: 'stage-9',
      awaitingDraw: false,
      structureDiagnosticContext: { stageStatus: 'Ready' },
    });
    expect(plain.titleKey).toBe('graph.issues.SlotFeedMissing');
    expect(plain.tone).toBe('danger');
  });

  it('maps RESULT codes with attention tone for classifying formats', () => {
    const presentation = structureIssuePresentation({
      code: 'AuthoredAwardUnreachable',
      competitionId: 'comp-1',
      stageId: 'stage-9',
      structureDiagnosticContext: { formatKind: 'Groups', stageStatus: 'Draft' },
    });
    expect(presentation.tone).toBe('attention');
    expect(presentation.titleKey).toBe('graph.issues.AuthoredAwardUnreachable');
  });

  it('maps SlotFeedMissing as attention in Draft and danger when Ready', () => {
    const draft = structureIssuePresentation({
      code: 'SlotFeedMissing',
      competitionId: 'comp-1',
      stageId: 'stage-9',
      structureDiagnosticContext: { stageStatus: 'Draft' },
    });
    expect(draft.tone).toBe('attention');
    const ready = structureIssuePresentation({
      code: 'SlotFeedMissing',
      competitionId: 'comp-1',
      stageId: 'stage-9',
      structureDiagnosticContext: { stageStatus: 'Ready' },
    });
    expect(ready.tone).toBe('danger');
    const presentation = draft;
    expect(presentation.titleKey).toBe('graph.issues.SlotFeedMissing');
  });

  it('maps NonRootUnfed progress when incompleteness is expected', () => {
    const presentation = structureIssuePresentation({
      code: 'NonRootUnfed',
      competitionId: 'comp-1',
      stageId: 'stage-9',
      structureDiagnosticContext: {
        stageStatus: 'Draft',
        isEntryBaselineStage: false,
        hasInboundQualProgFeed: false,
        affectationEntryCount: 0,
      },
    });
    expect(presentation.tone).toBe('info');
    expect(presentation.titleKey).toBe('graph.issues.NonRootUnfed');
  });

  it('situationPresentation forwards Overview actionCode and matchId', () => {
    const presentation = situationPresentation(
      {
        source: 'ProgressionPending',
        nature: 'Blocking',
        targetType: 'Fixture',
        targetId: 'fix-1',
        matchId: 'match-9',
        actionable: true,
        actionCode: 'ApplyProgression',
        impactCode: 'BlocksProgression',
        params: {},
      },
      'comp-1',
    );

    expect(presentation.cta?.actionCode).toBe('ApplyProgression');
    expect(presentation.sotHref).toBe('/matches/match-9');
  });

  it('situationPresentation uses structure short title for drawer structure diagnostics', () => {
    const presentation = situationPresentation(
      {
        source: 'OverCapacityEdge',
        nature: 'Blocking',
        targetType: 'Stage',
        targetId: 'stage-1',
        matchId: null,
        actionable: false,
        actionCode: null,
        impactCode: null,
        params: { stageId: 'stage-1' },
      },
      'comp-1',
      [{ stageId: 'stage-1', name: 'Poules', status: 'Draft' }],
    );
    expect(presentation.titleKey).toBe('graph.issuesShort.OverCapacityEdge');
    expect(presentation.titleNs).toBe('structure');
  });

  it('situationPresentation maps StructureGraphInvalid to Structure SoT', () => {
    const presentation = situationPresentation(
      {
        source: 'StructureGraphInvalid',
        nature: 'Blocking',
        targetType: 'Structure',
        targetId: 'comp-1',
        matchId: null,
        actionable: true,
        actionCode: 'ConfigureStructure',
        impactCode: 'BlocksConstruction',
        params: {},
      },
      'comp-1',
    );

    expect(presentation.cta?.actionCode).toBe('ConfigureStructure');
    expect(presentation.sotHref).toBe('/competitions/comp-1/structure');
  });
});
